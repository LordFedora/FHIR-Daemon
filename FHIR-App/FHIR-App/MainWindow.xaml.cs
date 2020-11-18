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
        static private String BaseAPIURL = "http://test.fhir.org/r2/AuditEvent/1/_history/1?_format=json";
        //http://test.fhir.org/r2/AuditEvent/_search?_lastUpdated=gt2020-11-06T21:52:30.300Z&_sort=_lastUpdated&_format=json&_count=10


        public MainWindow()
        {
            InitializeComponent();
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<ToastNotificationActivator>("BIA.FHIR_DEAMON");
            DesktopNotificationManagerCompat.RegisterActivator<ToastNotificationActivator>();

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("BIA");
            key.SetValue("BIA_SERVER_NAME_1", "http://test.fhir.org/r2");
            DateTime timestamp;
            dynamic value = key.GetValue("BIA_LAST_UPDATED_1");
            if (value is null)
            {
                key.SetValue("BIA_LAST_UPDATED_1", -1);
                timestamp = DateTime.FromFileTimeUtc(1);
            }
            else
            {
                timestamp = DateTime.FromFileTimeUtc(value);
            }
            key.Close();


            mainLoopTimer = new Timer(1000 * 30);
            mainLoopTimer.AutoReset = true;
            mainLoopTimer.Elapsed += onTimerElapsed;
            mainLoopTimer.Enabled = true;
        }

        private static void onTimerElapsed(Object source, ElapsedEventArgs e)
        {

            String content = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseAPIURL);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }


            dynamic temp = JsonConvert.DeserializeObject(content);

            String text = temp?.text?.div;

            ToastContent toastContent = new ToastContentBuilder()
                //.AddToastActivationInfo("action=viewConversation&conversationId=5", ToastActivationType.Foreground)
                .AddText(text)
                .SetToastScenario(ToastScenario.Reminder)
                .GetToastContent();
            

            var toast = new ToastNotification(toastContent.GetXml());

            Console.WriteLine("Toast?");
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);

        }
    }
}
