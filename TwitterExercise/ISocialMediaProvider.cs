﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterExercise
{
    interface ISocialMediaProvider
    {
        Task Retrieve(IRawDataQueue rdq, CancellationToken cancel);
    }
}
