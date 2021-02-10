using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TwitterExercise
{
    interface IReporter
    {
        Task Send(IDataStore ds, CancellationToken cancel);
    }
}
