using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    class RdqMemory : IRawDataQueue
    {
        private Queue<string> _data;
        private ILog _log;
        public RdqMemory(ILog log)
        {
            _log = log;
            _data = new Queue<string>();
        }
        public void Add(string data)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(data))
                {
                    _data.Enqueue(data);
                }
                else
                {
                    _log.Write("Tried to add empty data to raw data store.");
                }
            }
            catch(Exception ex)
            {
                _log.HandleException(ex, String.Format("Adding raw data:\n{0}\n", data));
            }
        }
        public string Retrieve()
        {
            string retVal = string.Empty;
            try
            {
                _data.TryDequeue(out retVal);
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "Retrieve from Raw Data Queue");
            }
            return retVal;
        }
        public int Count()
        {
            int retVal = -1;
            try
            {
                retVal = _data.Count;
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "Count from Raw Data Queue");
                retVal = -1;
            }
            return retVal;

        }
    }
}
