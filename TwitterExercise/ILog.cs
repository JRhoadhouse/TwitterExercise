using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    public interface ILog
    {
        void Write(string message);
        void HandleException(Exception ex, string source);
    }
}
