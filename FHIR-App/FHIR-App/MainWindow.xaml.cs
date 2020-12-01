using System;
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


        static private String BaseAPIURL = "http://test.fhir.org/r4";
        static private String SEARCH_PREFIX = "/AuditEvent/_search?_lastUpdated=gt";
        static private String SEARCH_SUFFIX = "Z&_sort=_lastUpdated&_format=json&_count="+PAGE_COUNT;

        static private String SEARCH_INITIAL = "/AuditEvent/_search?_sort=_lastUpdated&_format=json&_count="+PAGE_COUNT;


        //http://test.fhir.org/r4/AuditEvent/_search?_lastUpdated=gt2020-11-06T21:52:30.300Z&_sort=_lastUpdated&_format=json&_count=10
        static private DateTime timestamp;

        private static List<Filter> Filters;

        public MainWindow()
        {
            InitializeComponent();
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<ToastNotificationActivator>("BIA.FHIR_DEAMON");
            DesktopNotificationManagerCompat.RegisterActivator<ToastNotificationActivator>();

            Filters = new List<Filter>();

            Filters.Add(new Filter(FilterStates.HIDE, new PathCondition("resource//type", new ValueCondition("display", "User Authentication"))));


            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            key.SetValue("BIA_SERVER_NAME_1", "http://test.fhir.org/r4");
            timestamp = getKey(1);


            mainLoopTimer = new Timer(1000 * 30);
            mainLoopTimer.AutoReset = true;
            mainLoopTimer.Elapsed += onTimerElapsed;
            mainLoopTimer.Enabled = true;
        }

        private static void onTimerElapsed(Object source, ElapsedEventArgs e)
        {

            int displayedToasts = 0;
            string lastUpdated = "";
            while (displayedToasts < ALERT_COUNT)
            {
                string url = "";
                if(timestamp == DateTime.MinValue)
                {
                    url = BaseAPIURL + SEARCH_INITIAL;
                }
                else
                {
                    url = BaseAPIURL + SEARCH_PREFIX + timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff") + SEARCH_SUFFIX;
                }
                dynamic temp = getJsonFromURL(url);
                dynamic nextPages = temp?["link"];
                dynamic entryArray = temp?.entry;

                foreach (dynamic entry in entryArray)
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
                        displayedToasts++;
                    }
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
                updateKey(1, timestamp);
                updatePage(1, nextPage);
                if (entryArray.Count < PAGE_COUNT) break; //if we didn't get a full page then we're at the present and we'll never get a full page
            }

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
            String sReturn = key.GetValue("BIA_NEXT_PAGE_" + index).ToString();
            if (sReturn is null) return null;

            key.Close();

            return sReturn;
        }

        private static object getJsonFromURL(string url)
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
        }

        private static void createToast(string text)
        {
            ToastContent toastContent = new ToastContentBuilder()
                //.AddToastActivationInfo("action=viewConversation&conversationId=5", ToastActivationType.Foreground)
                .AddText(text)
                .SetToastScenario(ToastScenario.Reminder)
                .GetToastContent();


            var toast = new ToastNotification(toastContent.GetXml());
            
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }




    }
}
