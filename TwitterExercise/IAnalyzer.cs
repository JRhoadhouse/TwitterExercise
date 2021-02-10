using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterExercise
{
    interface IAnalyzer
    {
        Task Analyze(IRawDataQueue rawData, IDataStore store, CancellationToken cancel);
    }
}
