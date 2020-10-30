using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FHIR_Desktop
{
    class Program
    {
        private static Timer mainTimer;

        static void Main(string[] args)
        {
            mainTimer = new Timer(1000*60); //Once every minuite
            mainTimer.Elapsed += OnTimer;
            mainTimer.AutoReset = true;
            mainTimer.Enabled = true;
            Console.ReadLine();
        }

        private static void OnTimer(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Event Triggered");
        }
    }
}
