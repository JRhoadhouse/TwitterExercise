using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    public class LogConsole : ILog
    {
        public void Write(string message)
        {
            /*this doesn't really need a try catch but is included for consistancy 
            * and it can be used as a template for other Log methods*/
            try
            {
                Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                /*Shouldn't use error handling method as that calls this method
                * So it could result in a loop if there is a problem here.
                * Also console.writeline should be used here even if it is changed to another log method above
                * since the other log method is clearly not reliable in this case.
                * It is reccomended that the console output is captured in some way.*/
                Console.WriteLine("An exception occurred in \"Log Function\".  See details below:\n{0}", ex.Message);
            }
        }

        public void HandleException(Exception ex, string source)
        {
            try
            {
                Write(String.Format("An exception occurred in \"{0}\".  See details below:\n{1}", source, ex.Message));
                if (ex.InnerException != null)
                {
                    Write("Inner Exception:");
                    HandleException(ex.InnerException, source);
                }
            }
            catch (Exception e)
            {
                /*console.writeline should be used here instead of log as there might be a problem with the log method
                * It is reccomended that the console output is captured in some way.*/
                Console.WriteLine("Can not handle exception: {0}", e.Message);
            }
        }
    }
}
