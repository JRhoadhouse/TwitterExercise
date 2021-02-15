using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterExercise
{
    public interface ISocialMediaProvider
    {
        Task Retrieve(IRawDataQueue rdq, CancellationToken cancel);
    }
}
