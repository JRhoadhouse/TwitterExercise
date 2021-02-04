using System;
using System.Threading;

namespace TwitterExercise
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting test exercise");
                TwitterWrapper tw = new TwitterWrapper();

                //Set up a cancellation token to end the collection when appropriate
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;
                    tw.SampleAsynch(cancellationToken, null);
                    Console.WriteLine("Starting Data Sampling, Press 'Enter' to stop.");
                    Console.ReadLine();
                    cancellationTokenSource.Cancel();
                }
                Console.WriteLine("Stopping Data Retrieval, Press 'Enter' to close program.");
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
