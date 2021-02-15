using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    public interface IRawDataQueue
    {
        void Add(string data);
        string Retrieve();
        int Count();
    }
}
