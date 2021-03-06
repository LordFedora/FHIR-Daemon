﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Timers;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI;
using Windows.UI.Notifications;
using Newtonsoft.Json;

namespace FHIR_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static private Timer mainLoopTimer;
        static private int PAGE_COUNT = 10;
        static private int ALERT_COUNT = 7;


        static private String BaseAPIURL = "http://test.fhir.org/r4/";
        //static private String BaseAPIURL = "http://wildfhir4.aegis.net/fhir4-0-1/";
        //static private String BaseAPIURL = "http://sqlonfhir-r4.azurewebsites.net/fhir/";
        static private String SEARCH_PREFIX = "AuditEvent/_search?_lastUpdated=ge";
        static private String SEARCH_SUFFIX = "Z&_sort=_lastUpdated&_format=json&_count="+PAGE_COUNT;

        static private String SEARCH_INITIAL = "AuditEvent/_search?_sort=_lastUpdated&_format=json&_count="+PAGE_COUNT;

        static private String METADATA_SUFFIX = "metadata?_format=json";


        //http://test.fhir.org/r4/AuditEvent/_search?_lastUpdated=gt2020-11-06T21:52:30.300Z&_sort=_lastUpdated&_format=json&_count=10
        static private DateTime timestamp;

        public static List<Filter> Filters;
        private static Queue<KeyValuePair<int,String>> PageList;
        private static List<String> BaseURLS;
        private static string _logFilePath;
        public static string logFilePath
        {
            get
            {
                return _logFilePath;
            }
            set {
                _logFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), value);
            }


        }
        

        public MainWindow()
        {
            InitializeComponent();
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<ToastNotificationActivator>("BIA.FHIR_DEAMON");
            DesktopNotificationManagerCompat.RegisterActivator<ToastNotificationActivator>();

            Filters = new List<Filter>();
            PageList = new Queue<KeyValuePair<int,String>>();
            BaseURLS = new List<String>();

            /*
            BaseURLS.Add("http://wildfhir4.aegis.net/fhir4-0-1/");
            BaseURLS.Add("http://52.10.37.229:52774/hubonfhir/r4/");
            BaseURLS.Add("http://sqlonfhir-r4.azurewebsites.net/fhir/");
            BaseURLS.Add("https://terminz.azurewebsites.net/fhir/");
            BaseURLS.Add("http://r4.heliossoftware.com/fhir/");
            BaseURLS.Add("https://fhirsandbox1.tsysinteropsvcs.net:8100/r4/sites/123/");
            BaseURLS.Add("https://davinci-drug-formulary-ri.logicahealth.org/fhir/");
            BaseURLS.Add("https://davinci-plan-net-ri.logicahealth.org/fhir/");
            BaseURLS.Add("https://davinci-crd.logicahealth.org/r4/");
            BaseURLS.Add("https://appnameformularlyconnectathonjan2021.azurewebsites.net/R4/");
            BaseURLS.Add("https://cds-sandbox.alphora.com/cqf-ruler-r4/fhir/");
            BaseURLS.Add("https://cqm-sandbox.alphora.com/cqf-ruler-r4/fhir/");


            foreach (string baseurl in BaseURLS) testURL(baseurl);

            return; //TEMP DISABLE OF MAIN FUNCTION
            */


            //Filters.Add(new Filter(FilterStates.HIDE, new PathCondition("resource//type", new ValueCondition("display", "User Authentication"))));

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            timestamp = getKey(1);

            {
                int i = 1;
                while (!(restoreServerPage(i,loadPage(i++)) is null));
            }

            if(PageList.Count < 1)
            {
                addServerPage(1, BaseAPIURL);
                //addServerPage(2, BaseAPIURL2);
            }

            mainLoopTimer = new Timer(1000 * 30);
            mainLoopTimer.AutoReset = false;
            mainLoopTimer.Elapsed += onTimerElapsed;
            mainLoopTimer.Enabled = true;
        }

        private static void onTimerElapsed(Object source, ElapsedEventArgs e)
        {

            int displayedToasts = 0;
            string lastUpdated = "";
            string lastId = "";
            while (displayedToasts < ALERT_COUNT)
            {
                KeyValuePair<int, String> page = getServerPage();
                string url = page.Value;
                dynamic temp = getJsonFromURL(url);

                if(!(temp?.issue?[0]?.severity is null) || temp is null)
                {
                    //page expired, build the backup
                    url = loadPage(page.Key);
                    temp = getJsonFromURL(url);
                    if(!(temp?.issue?[0]?.severity is null) || temp is null)
                    {
                        //even the backup errored, something went wrong
                        createToast("WARNING, FHIR SERVER ERRORED\n" + temp?.issue?[0]?.details?.text?.Split("\n\r", 2)?[0] + "\n"+BaseURLS[page.Key]);
                        break;
                    }
                }

                dynamic nextPages = temp?["link"];
                dynamic entryArray = temp?.entry;
                string searchID = getID(page.Key);
                bool foundId = (searchID is null); //if we don't have a search id, then we start with the first

                foreach (dynamic entry in entryArray)
                {

                    string id = entry?.resource?.id;
                    if(id is null)
                    {
                        continue; //not worth looking when it's not up to spec
                    } else if(!foundId && searchID.Equals(id))
                    {
                        foundId = true;
                        continue; //this is the one we're starting after
                    } else if (!foundId)
                    {
                        continue; //not there yet
                    }

                    if (parseEntry(entry))
                    {
                        displayedToasts++;
                    }
                    

                    lastId = entry?.resource?.id;
                    lastUpdated = entry?.resource?.meta?.lastUpdated;
                }

                dynamic nextPage = null;
                foreach(dynamic links in nextPages)
                {
                    String relation = links?["relation"]?.ToString();
                    if ("next".Equals(relation))
                    {
                        nextPage = links?["url"]?.ToString();
                    }
                }

                timestamp = DateTime.Parse(lastUpdated);
                updateKey(page.Key, timestamp);
                savePage(page.Key, lastId, timestamp);
                cycleServerPage(page.Key, nextPage, timestamp);
                if (entryArray.Count < PAGE_COUNT) break; //if we didn't get a full page then we're at the present and we'll never get a full page
            }

        }

        public static bool parseEntry(dynamic entry)
        {
            bool pass = true; //default case is it passes

            foreach (Filter f in Filters)
            {
                bool filterPass = f.CheckConditions(entry);
                if (filterPass && f.getState() == FilterStates.SHOW)
                {
                    pass = true;
                    break;
                }
                if (filterPass && f.getState() == FilterStates.HIDE)
                {
                    pass = false;
                    break;
                }
            }


            if (pass)
            {
                string text = "";
                text += entry?.resource?.type?.display;
                text += " (";
                text += entry?.resource?.subtype?[0]?.display;
                text += ")";
                createToast(text);
                return true;
            }
            return false;
        }

        private static Boolean doesServerSupportAudits(String baseUrl)
        {
            if (baseUrl.Last() != '/') baseUrl = baseUrl += '/';
            dynamic metadataJson = getJsonFromURL(baseUrl + METADATA_SUFFIX);

            dynamic restList = metadataJson?["rest"];
            if (restList is null) return false;
            foreach(dynamic restElement in restList)
            {
                dynamic modeElement = restElement?["mode"];
                if (modeElement is null) continue;

                if (modeElement.ToString().Equals("server"))
                {
                    dynamic resourceList = restElement?["resource"];
                    if (resourceList is null) continue;
                    foreach(dynamic resourceElement in resourceList)
                    {
                        dynamic type = resourceElement?["type"];
                        if (type is null) continue;
                        if (type.ToString().Equals("AuditEvent"))
                        {
                            dynamic interactionList = resourceElement?["interaction"];
                            if (type is null) continue;
                            
                            foreach(dynamic interactionElement in interactionList)
                            {
                                dynamic codeElement = interactionElement?["code"];
                                if (codeElement is null) continue;
                                if (codeElement.ToString().Equals("read"))
                                {
                                    return true;
                                };
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static void updateKey(int index, DateTime time)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            key.SetValue("BIA_LAST_UPDATED_"+index, time.ToFileTimeUtc());
 
            key.Close();
        }

        private static DateTime getKey(int index)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            dynamic time = key.GetValue("BIA_LAST_UPDATED_" + index);
            if (time is null) return DateTime.MinValue;
            if (time is String) time = long.Parse(time);
            DateTime iReturn = DateTime.FromFileTimeUtc(time);

            key.Close();

            return iReturn;
        }

        private static string getID(int index)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            dynamic sReturn = key.GetValue("BIA_LAST_UPDATED_" + index); 
            if (!(sReturn is null) && !(sReturn is string)) sReturn = sReturn.toString();

            key.Close();

            return sReturn;

        }

        private static void savePage(int index, String id, DateTime lastUpdated)
        {
            if (index >= BaseURLS.Count) return;

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            key.SetValue("BIA_LAST_ID_" + index, id);
            key.SetValue("BIA_LAST_UPDATED_" + index, lastUpdated.ToFileTimeUtc());
            key.SetValue("BIA_BASE_URL_" + index, BaseURLS[index]);

        }

        private static String loadPage(int index)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            dynamic time = key.GetValue("BIA_LAST_UPDATED_" + index);
            if (time is null) return null;
            if (time is String) time = long.Parse(time);
            DateTime date = DateTime.FromFileTimeUtc(time);
            String id = key.GetValue("BIA_LAST_ID_" + index)?.ToString();
            if (id is null) return null;
            String url = key.GetValue("BIA_BASE_URL_" + index)?.ToString();
            if (url is null) return null;

            key.Close();

            BaseURLS.Add(url);

            return url+SEARCH_PREFIX+date.ToString("yyyy-MM-ddTHH:mm:ss")+SEARCH_SUFFIX;
        }

        private static void updatePage(int index, String url)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            key.SetValue("BIA_NEXT_PAGE_" + index, url);

            key.Close();
        }

        private static String getPage(int index)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            String sReturn = key.GetValue("BIA_NEXT_PAGE_" + index)?.ToString();
            if (sReturn is null) return null;

            key.Close();

            return sReturn;
        }

        private static object getJsonFromURL(string url)
        {
            try
            {
                String content = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }


                return JsonConvert.DeserializeObject(content);
            } catch(System.Net.WebException e)
            {
                //TODO: better error handeling, but this should work for now
                return null;
            }
        }

        private static void createToast(string text)
        {
            ToastContent toastContent = new ToastContentBuilder()
                //.AddToastActivationInfo("action=viewConversation&conversationId=5", ToastActivationType.Foreground)
                .AddText(text)
                .SetToastScenario(ToastScenario.Reminder)
                .GetToastContent();


            var toast = new ToastNotification(toastContent.GetXml());

            using (StreamWriter outputFile = new StreamWriter(logFilePath, true))
            {
                outputFile.WriteLine(text.Replace("\n", "").Replace("\r", ""));
            }
            try {
                DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
            } catch(System.Exception e)
            {

            }
        }

        private static void addServerPage(int index, string baseUrl)
        {
            if (baseUrl.Last() != '/') baseUrl = baseUrl += '/';
            if (!doesServerSupportAudits(baseUrl))
            {
                createToast("Server doesn't support audits");
            }
            else
            {
                DateTime date = new DateTime();
                PageList.Enqueue(new KeyValuePair<int, String>(index, baseUrl + SEARCH_PREFIX + date.ToString("yyyy-MM-ddTHH:mm:ss") + SEARCH_SUFFIX));
                BaseURLS.Add(baseUrl);
                updatePage(PageList.Count, baseUrl);
            }
        }

        private static void cycleServerPage(int index, string newUrl, DateTime timestamp)
        {
            KeyValuePair<int, String> queueTop = PageList.Dequeue();
            if (queueTop.Key == index)
            {
                if(newUrl is null)
                {
                    newUrl = queueTop.Value + SEARCH_PREFIX + timestamp.ToString("yyyy-MM-ddTHH:mm:ss") + SEARCH_SUFFIX;
                }

                PageList.Enqueue(new KeyValuePair<int, String>(index,newUrl));
                updatePage(index, newUrl);
            } else //should never occur
            {
                PageList.Enqueue(queueTop);
            }
        }

        private static String restoreServerPage(int index, string fullUrl)
        {
            if (fullUrl is null) return null;
            PageList.Enqueue(new KeyValuePair<int, String>(index,fullUrl));
            return fullUrl;
        }

        private static KeyValuePair<int, String> getServerPage()
        {
            return PageList.Peek();
        }

        private static void testURL(string url)
        {
            dynamic jsonDump;

            Console.Write(url);
            try
            {
                jsonDump = getJsonFromURL(url + SEARCH_INITIAL);
            } catch(Exception e)
            {
                jsonDump = null;
            }

            if (jsonDump?["total"] is null)
            {
                Console.WriteLine(" Errored");
            }
            else
            {
                Console.Write(" | ");
                Console.WriteLine(jsonDump?["total"]);
            }
        }


    }
}
