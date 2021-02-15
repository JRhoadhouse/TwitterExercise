using NUnit.Framework;
using TwitterExercise;
using System.Collections.Generic;
using System.Text;
using System;
using System.Threading;
using Unity;
using System.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NUnitTwitterExerciseTests
{
    public class Tests
    {
        private ILog log;
        private Exception inner;
        private Exception outer;
        private List<string> rdqTestStrings;
        private int rdqValidStrings;
        private UnityContainer container;
        private List<TweetMetadata> dataStoreTestData;

        /* This sets up some objects we will use for testing.
         * Log console just writes to the console so we can be fairly confident that it will execute*/
        [SetUp]
        public void Setup()
        {
            try
            {
                container = new UnityContainer();
                container.RegisterType<ILog, LogConsole>();
                container.RegisterType<IRawDataQueue, RdqMemory>();
                container.RegisterType<IDataStore, DsMemory>();

                inner = new Exception("Inner Exception");
                outer = new Exception("Outer Exception", inner);
                log = container.Resolve<ILog>();
                rdqTestStrings = new List<string>();
                rdqValidStrings = 0;
                using (TextReader tr = new StreamReader(@"RawTestData.txt"))
                {
                    string data = string.Empty;
                    while (data != null)
                    {
                        data = tr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            rdqTestStrings.Add(data);
                            rdqValidStrings++;
                        }
                    }
                }

                using (TextReader tr = new StreamReader(@"testdata.json"))
                {
                    dataStoreTestData =
                        JsonConvert.DeserializeObject<List<TweetMetadata>>(tr.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in setting up Tests.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }

        }

        /* This tests the log object write method
         * though since the log we are currently using just writes to the console,
         * we will simply test to make sure the methods do not throw any exceptions */
        [Test]
        public void LogWriteTest()
        {
            try
            {
                ILog logConsole = new LogConsole();
                logConsole.Write("Test");
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in Log Write Test.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass();
        }

        /* This tests the log object write method
         * though since the log we are currently using just writes to the console,
         * we will simply test to make sure the methods do not throw any exceptions */
        [Test]
        public void LogHandleErrorTest()
        {
            try
            {
                ILog logConsole = new LogConsole();
                logConsole.HandleException(outer, "Test method");
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in Log HandleErrorTest Test.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass();
        }

        [Test]
        public void rdqMemory()
        {
            try
            {
                IRawDataQueue rdqm = new RdqMemory(log);
                foreach (string s in rdqTestStrings)
                {
                    rdqm.Add(s);
                }
                rdqm.Add(string.Empty);
                rdqm.Add(" ");
                rdqm.Add(null);
                if (rdqm.Count() != rdqValidStrings)
                {
                    Assert.Fail("rdqMemory did not add correct number of strings expected:{0}, actual:{1}",
                        rdqValidStrings, rdqm.Count());
                }
                foreach (string s in rdqTestStrings)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        string data = rdqm.Retrieve();
                        if (data != s)
                        {
                            Assert.Fail(
                                "rdqMemory did not retrieve the proper string expected:\"{0}\", actual:\"{1}\"",
                                s, data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("An exception occurred in rdqMemory Test.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass();
        }

        // testing configuration manager
        [Test]
        public void configTest()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                Console.WriteLine(config.FilePath);
                string returnValue = ConfigurationManager.AppSettings.Get("BearerToken");
                if (string.IsNullOrWhiteSpace(returnValue))
                {
                    Assert.Fail("Could not get Bearer Token");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("An exception occurred retrieving config.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass();
        }



        /* This tests the SMPTwitterSampleRetrieve function.
         * Here we are only concerned with getting content from the endpoint
         * We will test if we can parse the data when we test the analyzer and end-to-end testing*/
        [Test]
        public void SMPTwitterSampleRetrieve()
        {
            int countPrev = 0, countCurrent = 0;
            try
            {
                ISocialMediaProvider twitterSample = new SmpTwitterSample(log);
                IRawDataQueue rdq = container.Resolve<IRawDataQueue>();
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancel = cancellationTokenSource.Token;
                    twitterSample.Retrieve(rdq, cancel);
                    for (int i = 0; i < 5; i++)
                    {
                        countPrev = countCurrent;
                        Thread.Sleep(1000);
                        countCurrent = rdq.Count();
                        if (countCurrent <= countPrev)
                        {
                            Assert.Fail(
                                "TwitterSample Retrieve did not add to queue.\nPrevious Count: {0}\nCurrent count: {1}",
                                countPrev, countCurrent);
                        }
                    }
                    cancellationTokenSource.Cancel();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in SMPTwitterSample Retrieve Test.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass("TwitterSample retrieved {0} messages", countCurrent);
        }

        [Test]
        public void DSMemoryTest()
        {
            try
            {
                IDataStore dsMemory = new DsMemory(log);
                foreach (TweetMetadata tmd in dataStoreTestData)
                {
                    dsMemory.Store(tmd);
                }
                List<TweetMetadata> retrieves = dsMemory.Retrieve();
                if (dataStoreTestData.Count != retrieves.Count)
                {
                    Assert.Fail("DSMemory has incorrect number of items\nExpected:{0}\nRetrieved:{1}",
                        dataStoreTestData.Count, retrieves.Count);
                }
                foreach (TweetMetadata tmd in dataStoreTestData)
                {
                    if (!retrieves.Any(x => x.Equals(tmd)))
                    {
                        Assert.Fail("Could not find tweet {0} in DSMemory test", tmd.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in DSMemoryTest.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass("Stored and retrieved {0} items with DSMemory.", dataStoreTestData.Count);
        }

        [Test]
        public void AnalyzerLocalTest()
        {
            try
            {
                IRawDataQueue rdq = container.Resolve<IRawDataQueue>();
                IDataStore ds = container.Resolve<IDataStore>();
                foreach (string rd in rdqTestStrings)
                {
                    rdq.Add(rd);
                }
                IAnalyzer analyzer = new AnalyzerLocal(log);
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancel = cancellationTokenSource.Token;
                    analyzer.Analyze(rdq, ds, cancel);
                    while (rdq.Count() > 0)
                    {
                        Console.WriteLine("Waiting for analyzer to finish");
                        Thread.Sleep(10);

                    }
                    cancellationTokenSource.Cancel();
                }
                List<TweetMetadata> tmds = ds.Retrieve();
                foreach (TweetMetadata tmd in tmds)
                {
                    TweetMetadata dstd = dataStoreTestData.FirstOrDefault(x => x.Id == tmd.Id);
                    if (dstd == null)
                    {
                        Assert.Fail("Analyzer created tweet with invalid id: {0}", tmd.Id);
                    }
                    if (!dstd.Equals(tmd))
                    {
                        Assert.Fail("Analyzer did not properly analyze tweet with id: {0}", tmd.Id);
                    }
                    Console.WriteLine("Analyzer test Matched tweet: {0}", tmd.Id);
                }
                if (tmds.Count() != rdqValidStrings)
                {
                    Assert.Fail("Analyzer did not create correct number of elements.\nExpected: {0}\nRetrieved:{1}\n",
                        rdqValidStrings, tmds.Count());
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in AnalyzerLocalTest.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass();
        }

        [Test]
        public void ReporterConsoleTest()
        {
            try
            {
                IReporter reporter = new ReporterConsole(log);
                IDataStore ds = container.Resolve<IDataStore>();
                foreach(TweetMetadata tmd in dataStoreTestData)
                {
                    ds.Store(tmd);
                }
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancel = cancellationTokenSource.Token;
                    reporter.Send(ds,cancel);
                    Thread.Sleep(15000);
                    cancellationTokenSource.Cancel();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    "An exception occurred in ReporterConsole test.  See details below:({0})\n{1}\n{2}",
                    ex.HResult, ex.Message, ex.StackTrace);
            }
            Assert.Pass();
        }
    }
}