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
        static private String BaseAPIURL = "http://test.fhir.org/r4";
        static private String SEARCH_PREFIX = "/AuditEvent/_search?_lastUpdated=gt";
        static private String SEARCH_SUFFIX = "Z&_sort=_lastUpdated&_format=json&_count=10";

        static private String SEARCH_INITIAL = "/AuditEvent/_search?_sort=_lastUpdated&_format=json&_count=10";

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

            dynamic entryArray = temp?.entry;

            string lastUpdated = "";

            foreach(dynamic entry in entryArray)
            {
                bool pass = true; //default case is it passes

                foreach(Filter f in Filters)
                {
                    bool filterPass = f.CheckConditions(entry);
                    if(filterPass && f.getState() == FilterStates.SHOW)
                    {
                        pass = true;
                        break;
                    }
                    if(filterPass && f.getState() == FilterStates.HIDE)
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
                }
                lastUpdated = entry?.resource?.meta?.lastUpdated;
            }

            timestamp = DateTime.Parse(lastUpdated);
            updateKey(1, timestamp);

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
