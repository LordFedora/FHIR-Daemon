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
using Hl7.Fhir.Rest;
using Newtonsoft.Json;
using System.Net.Http;


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
        private Stack<Condition> filterConditions;
        private string remoteAddr;

        public AuditGeneratorTests(ScenarioContext scenarioContext)
        {
            number = 0;
            logFileName = "temp.txt"; //TODO make this more resilieant
            MainWindow.logFilePath = logFileName;
            MainWindow.Filters = new List<Filter>();
            File.Delete(MainWindow.logFilePath);
            auditEvent = null;
            searchBundle = new Bundle();
            filterConditions = new Stack<Condition>();
            remoteAddr = null;

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
            auditEvent.Subtype = null;
        }

        [Given(@"The AuditEvent has a Type of (.*)")]
        public void GivenTheAuditEventHasATypeOf(string p0)
        {
            if (auditEvent is null) throw new ArgumentNullException("auditEvent", "AuditEvent not initilized");

            auditEvent.Type.Display = p0;
        }

        [Given(@"The AuditEvent has an ID of (.*)")]
        public void GivenTheAuditEventHasAnIDOf(string p0)
        {
            if (auditEvent is null) throw new ArgumentNullException("auditEvent", "AuditEvent not initilized");

            auditEvent.Id = p0;
        }

        [Given(@"The AuditEvent has a Subtype of (.*)")]
        public void GivenTheAuditEventHasASubtypeOf(string p0)
        {
            if(auditEvent is null) throw new ArgumentNullException("auditEvent", "AuditEvent not initilized");

            if(auditEvent.Subtype is null)
            {
                auditEvent.Subtype = new List<Coding>();
            }

            Coding subType = new Coding();
            subType.Display = p0;
            auditEvent.Subtype.Add(subType);
        }

        [Given(@"The Filter checks that (.*) Exists")]
        public void GivenTheFilterChecksThatExists(string p0)
        {
            filterConditions.Push(new ExistsCondition(p0));
        }



        [Given(@"A Filter exists")]
        public void GivenAFilterExists()
        {
            //TODO determine what needs to be here? just a verbal step?
        }

        [Given(@"The Filter checks a (.*) of (.*)")]
        public void GivenTheFilterChecks(string p0, string p1)
        {
            filterConditions.Push(new ValueCondition(p0, p1));
        }

        [Given(@"The Filter paths to (.*)")]
        public void GivenTheFilterChecksPath(string p0)
        {
            if (filterConditions.Count < 1) throw new ArgumentNullException("filterCondition", "Path Condition requires 1 argument");

            filterConditions.Push(new PathCondition(p0, filterConditions.Pop()));
        }

        [Given(@"The Filter negates it's input")]
        public void GivenTheFilterNegatesItSInput()
        {
            if (filterConditions.Count < 1) throw new ArgumentNullException("filterCondition", "NOT Condition requires 1 argument");

            filterConditions.Push(new NotCondition(filterConditions.Pop()));
        }

        [Given(@"The Filter requires either to be true")]
        public void GivenTheFilterRequiresEitherToBeTrue()
        {
            if (filterConditions.Count < 2) throw new ArgumentNullException("filterCondition", "OR Condition requires 2 arguments");

            filterConditions.Push(new OrCondition(filterConditions.Pop()).addCondition(filterConditions.Pop()));
        }

        [Given(@"The Filter requires both to be true")]
        public void GivenTheFilterRequiresBothToBeTrue()
        {
            if (filterConditions.Count < 2) throw new ArgumentNullException("filterCondition", "AND Condition requires 2 arguments");

            filterConditions.Push(new AndCondition(filterConditions.Pop()).addCondition(filterConditions.Pop()));
        }

        [Given(@"The Filter checks the Types are (.*)/(.*)")]
        public void GivenTheFilterChecksTheTypeIs2(string p0, string p1)
        {
            filterConditions.Push(new TypeCondition(p0, p1));
        }

        [Given(@"The Filter checks the Type is (.*)")]
        public void GivenTheFilterChecksTheTypeIs1(string p0)
        {
            filterConditions.Push(new TypeCondition(p0));
        }




        [Given(@"The Filter will (.*) matches")]
        public void GivenTheFilterMatches(string p0)
        {
            if (filterConditions.Count != 1) throw new ArgumentNullException("filterCondition", "filterCondtion doesn't exist");

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

            MainWindow.Filters.Add(new Filter(state, filterConditions.Pop()));
        }

        [Given(@"The remote server is (.*)")]
        public void GivenTheRemoteServerIs(string p0)
        {
            remoteAddr = p0;
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
            if (filterConditions.Count != 0) throw new ArgumentNullException("filterCondition", "filterCondition should be null");

            FhirJsonSerializer serializer = new FhirJsonSerializer();

            dynamic json = JsonConvert.DeserializeObject(serializer.SerializeToString(searchBundle));
            foreach (dynamic entry in json?.entry)
            {
                MainWindow.parseEntry(entry);
            }
        }

        [When(@"The system sends the audit events to the remote server")]
        public void ThenTheSystemShouldSendTheAuditEventsToTheRemoteServer()
        {
            if (remoteAddr is null) throw new ArgumentNullException("Remote Address is null");

            if (auditEvent is null) throw new ArgumentNullException("Audit Event is null");

            FhirClient client = new FhirClient(remoteAddr);

            AuditEvent tempEvent = client.Create<AuditEvent>(auditEvent);

            auditEvent.Id = tempEvent.Id;
            //is... is that it?


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

        [Then(@"Log file line number (.*) should be ""(.*)""")]
        public void ThenLogFileLineNumberShouldBe(int p0, string p1)
        {
            if ((p0 < 0) || (p0 > File.ReadAllLines(MainWindow.logFilePath).Length)) throw new ArgumentException("Wrong number of lines in file");

            Assert.AreEqual(p1, File.ReadAllLines(MainWindow.logFilePath)[p0-1]);
        }

        [Then(@"The remote server should have a matching audit event")]
        public void ThenTheRemoteServerShouldHaveAMatchingAuditEvent()
        {

            if (remoteAddr is null) throw new ArgumentNullException("Remote Address is null");

            if (auditEvent is null) throw new ArgumentNullException("Audit Event is null");


            FhirClient client = new FhirClient(remoteAddr);

            SearchParams searchParams = new SearchParams();

            AuditEvent remoteEvent = client.Read<AuditEvent>("AuditEvent/"+auditEvent.Id);

            Assert.AreEqual(auditEvent.Type.Display, remoteEvent.Type.Display);
            
            for(int i = 0; i < auditEvent.Subtype.Count; i++)
            {
                Assert.AreEqual(auditEvent.Subtype[i].Display, remoteEvent.Subtype[i].Display);

            }


            //is this testing enough?
        }



    }
}
