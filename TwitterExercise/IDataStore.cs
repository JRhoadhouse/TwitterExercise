﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    public interface IDataStore
    {
        void Store(TweetMetadata post);
        List<TweetMetadata> Retrieve();
    }
}
