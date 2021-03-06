using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using System.IO;
using NUnit.Framework;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;


namespace FHIR_App
{
    [Binding]
    class AuditGeneratorTests
    {

        private readonly ScenarioContext _scenarioContext;
        private int number;
        private AuditEvent auditEvent;
        private string logFileName;
        private Bundle searchBundle;
        private Condition filterCondition;

        public AuditGeneratorTests(ScenarioContext scenarioContext)
        {
            number = 0;
            logFileName = "temp.txt"; //TODO make this more resilieant
            MainWindow.logFilePath = logFileName;
            MainWindow.Filters = new List<Filter>();
            File.Delete(MainWindow.logFilePath);
            auditEvent = null;
            searchBundle = new Bundle();
            filterCondition = null;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };


            _scenarioContext = scenarioContext;
        }

        [Given(@"The Event is a simple one")]
        public void GivenTheEventIsASimpleOne()
        {
            number += 1;
        }

        [Given(@"We add a line to the Log File")]
        public void GivenWeAddALineToTheLogFile()
        {
            using (StreamWriter outputFile = new StreamWriter(MainWindow.logFilePath)) //Not append
            {
                outputFile.WriteLine("Hello World!");
            }
        }

        [Given(@"An AuditEvent exists")]
        public void GivenAnAuditEventExists()
        {
            if(!(auditEvent is null)) //if we're not the first, add the previous
            {
                Bundle.EntryComponent temp = new Bundle.EntryComponent();
                temp.Resource = auditEvent;
                searchBundle.Entry.Add(temp);
            }


            auditEvent = new AuditEvent();
            auditEvent.Type = new Coding();
            auditEvent.Type.Display = "Test";
            auditEvent.Subtype = new List<Coding>();
            Coding subType = new Coding();
            subType.Display = "SubTest";
            auditEvent.Subtype.Add(subType);
        }

        [Given(@"The AuditEvent has a Type of (.*)")]
        public void GivenTheAuditEventHasATypeOf(string p0)
        {
            if (auditEvent is null) throw new ArgumentNullException("auditEvent", "AuditEvent not initilized");

            auditEvent.Type.Display = p0;


        }

        [Given(@"A Filter exists")]
        public void GivenAFilterExists()
        {
            //TODO determine what needs to be here? just a verbal step?
        }

        [Given(@"The Filter checks a (.*) of (.*)")]
        public void GivenTheFilterChecks(string p0, string p1)
        {
            if (!(filterCondition is null)) throw new ArgumentNullException("filterCondition", "filterCondition already exists");

            filterCondition = new ValueCondition(p0, p1);
        }

        [Given(@"The Filter paths to (.*)")]
        public void GivenTheFilterChecksPath(string p0)
        {
            if (filterCondition is null) throw new ArgumentNullException("filterCondition", "filterCondtion doesn't exist");

            filterCondition = new PathCondition(p0, filterCondition);
        }

        [Given(@"The Filter will (.*) matches")]
        public void GivenTheFilterMatches(string p0)
        {
            if (filterCondition is null) throw new ArgumentNullException("filterCondition", "filterCondtion doesn't exist");

            FilterStates state;

            switch (p0)
            {
                case "Hide":
                case "HIDE":
                case "hide":
                    state = FilterStates.HIDE;
                    break;
                case "Show":
                case "SHOW":
                case "show":
                    state = FilterStates.SHOW;
                    break;
                case "Default":
                case "DEFAULT":
                case "default":
                    state = FilterStates.DEFAULT;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("p0", "p0 value of " + p0 + " not Hide/Show/Default");

            }

            MainWindow.Filters.Add(new Filter(state, filterCondition));
            filterCondition = null;
        }



        [When(@"The AuditEvents are recived")]
        public void WhenTheAuditEventsAreRecived()
        {
            if (!(auditEvent is null))
            {
                Bundle.EntryComponent temp = new Bundle.EntryComponent();
                temp.Resource = auditEvent;
                searchBundle.Entry.Add(temp);
            }
            if (!(filterCondition is null)) throw new ArgumentNullException("filterCondition", "filterCondition should be null");

            FhirJsonSerializer serializer = new FhirJsonSerializer();

            dynamic json = JsonConvert.DeserializeObject(serializer.SerializeToString(searchBundle));
            foreach (dynamic entry in json?.entry)
            {
                MainWindow.parseEntry(entry);
            }
        }


        [Then(@"The Log file should exist")]
        public void ThenTheLogFileShouldExist()
        {
            Assert.True(File.Exists(MainWindow.logFilePath));
        }

        [Then(@"The Log file should have a line in it")]
        public void ThenTheLogFileShouldHaveALineInIt()
        {
            Assert.Greater(File.ReadAllLines(MainWindow.logFilePath).Length,0);
        }


        [Then(@"The Test should pass")]
        public void ThenTheTestShouldPass()
        {
            Assert.AreEqual(1,number,"Numbers aren't the same");
        }

        [Then(@"The Log file should have exactly (.*) line in it")]
        public void ThenTheLogFileShouldHaveExactlyLineInIt(int p0)
        {
            Assert.AreEqual(p0, File.ReadAllLines(MainWindow.logFilePath).Length);
        }


    }
}
