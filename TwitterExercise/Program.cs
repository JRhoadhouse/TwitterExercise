using System;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace TwitterExercise
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                UnityContainer container = new UnityContainer();
                container.RegisterType<ILog, LogConsole>();
                container.RegisterType<IRawDataQueue, RdqMemory>();
                container.RegisterType<ISocialMediaProvider, SmpTwitterSample>();
                container.RegisterType<IDataStore, DsMemory>();
                container.RegisterType<IAnalyzer, AnalyzerLocal>();
                container.RegisterType<IReporter, ReporterConsole>();

                IRawDataQueue rdq = container.Resolve<IRawDataQueue>();
                ISocialMediaProvider smp = container.Resolve<ISocialMediaProvider>();
                IDataStore ds = container.Resolve<IDataStore>();
                IAnalyzer analyzer = container.Resolve<IAnalyzer>();
                IReporter reporter = container.Resolve<IReporter>();

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
