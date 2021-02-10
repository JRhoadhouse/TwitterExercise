using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterExercise
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ILog log = new LogConsole();
                IRawDataQueue rdq = new RdqMemory(log);
                ISocialMediaProvider smp = new SmpTwitterSample(log);
                IDataStore ds = new DsMemory(log);
                IAnalyzer analyzer = new AnalyzerLocal(log);
                IReporter reporter = new ReporterConsole(log);

                //Set up a cancellation token to end the collection when appropriate
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancel = cancellationTokenSource.Token;
                    smp.Retrieve(rdq, cancel);
                    analyzer.Analyze(rdq, ds, cancel);
                    reporter.Send(ds, cancel);
                    Console.WriteLine("Press 'Enter' to stop.");
                    Console.ReadLine();
                    cancellationTokenSource.Cancel();
                }
                Console.WriteLine("Cancellation request recieved. Press 'Enter' to exit program.");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occurred. See details below.  Press 'Enter' to close program.\n{0}", ex.Message);
                Console.ReadLine();
            }
        }
    }
}
