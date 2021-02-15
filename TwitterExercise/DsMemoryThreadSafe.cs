using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace TwitterExercise
{
    public class DsMemoryThreadSafe : IDataStore
    {
        private ConcurrentDictionary<long, TweetMetadata> _data;
        private ILog _log;
        public DsMemoryThreadSafe(ILog log)
        {
            _data = new ConcurrentDictionary<long, TweetMetadata>();
            _log = log;
        }
        public void Store(TweetMetadata data)
        {
            try
            {
                _data.TryAdd(long.Parse(data.Id), data);
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "DsMemory.Store");
            }
        }
        public List<TweetMetadata> Retrieve()
        {
            //try catch included for consistency
            List<TweetMetadata> retVal = new List<TweetMetadata>();
            try
            {
                retVal = _data.ToArray().Select(x => x.Value).ToList();
                return retVal;
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "DsMemory.Store");
            }
            return retVal;

        }
    }
}

