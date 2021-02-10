using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    class DsMemory : IDataStore
    {
        private List<TweetMetadata> _data;
        private ILog _log;
        public DsMemory(ILog log)
        {
            _data = new List<TweetMetadata>();
            _log = log;
        }
        public void Store(TweetMetadata data)
        {
            try
            {
                _data.Add(data);
            }
            catch(Exception ex)
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
                retVal.AddRange(_data);
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "DsMemory.Store");
            }
            return retVal;

        }
    }
}
