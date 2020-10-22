using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FHIR_daemon
{
    public partial class FHIR_Daemon_Service : ServiceBase
    {
        private int eventId = 0;
        private Timer timer = null;

        public FHIR_Daemon_Service()
        {
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("LogSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "LogSource", "DaemonLog");
            }
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Startup",EventLogEntryType.Information,eventId++);
            this.timer = new Timer();
            this.timer.Interval = 60 * 1000; //60s * 1000ms/s
            this.timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            this.timer.Start();
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            eventLog1.WriteEntry("Polling Start", EventLogEntryType.Information, eventId++);
        }

        protected override void OnStop()
        {
            this.timer.Stop();
        }
    }
}
