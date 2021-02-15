using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace TwitterExercise
{
    public class AnalyzerLocal: IAnalyzer
    {
        private ILog _log;
        private List<EmojiData> _emojis;
        public AnalyzerLocal(ILog log)
        {
            _log = log;
            _emojis = EmojiDataProvider.GetEmojiData(_log);
        }
        public async Task Analyze(IRawDataQueue rdq, IDataStore ds, CancellationToken cancel)
        {
            try
            {
                int retVal = await Task.Factory.StartNew(() => StoreData(rdq, ds, cancel));
                if (retVal != 0)
                {
                    _log.Write(String.Format("Analyzer encountered an error.  Report may not be accurate.  Error code: {0}", retVal));
                }

            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "AnalyzerLocal.Analyze");
            }

        }

        private int StoreData(IRawDataQueue rdq, IDataStore ds, CancellationToken cancel)
        {
            int status = 0;
            try
            {
                _log.Write("Starting to Analyze.");
                bool cancelMessageDisplayed = false;
                while(!(cancel.IsCancellationRequested && rdq.Count() > 0))
                {
                    //if queue is empty, wait then continue
                    if (rdq.Count() <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    else if(!cancelMessageDisplayed && cancel.IsCancellationRequested && rdq.Count()>0)
                    {
                        _log.Write(string.Format(
                            "Analyzer recieved cancel request, continuing to process {0} remaining queued items.", rdq.Count()));
                        cancelMessageDisplayed = true;
                    }
                    string rawData = rdq.Retrieve();
                    if (!string.IsNullOrWhiteSpace(rawData))
                    {
                        //If one tweet is problematic we want to carry on.
                        try
                        {
                            //The message object and it's children or more complex than they need to be.
                            //They are built to mimic the Json structure as closely as neccessary.
                            //This will make it easier to collect additonal statistics in the future.
                            Message message = JsonConvert.DeserializeObject<Message>(rawData);
                            ds.Store(new TweetMetadata()
                            {
                                TimeStamp = message.data.created_at,
                                Id = message.data.id,
                                Data = message.data.text,
                                Emojis = (message.data.text == null) ?
                                    new List<EmojiData>() :
                                    //for each emoji add it to the list if it appears in the text
                                    _emojis.Where(x => !string.IsNullOrWhiteSpace(x.character)
                                        && message.data.text.Contains(x.character)).ToList(),
                                HashTags = (message.data.entities == null) ?
                                    new List<String>() :
                                    (message.data.entities.hashtags == null) ?
                                        new List<String>() :
                                        message.data.entities.hashtags.Select(x => x.tag).Distinct().ToList(),
                                Domains = (message.data.entities == null) ?
                                    new List<String>() :
                                    (message.data.entities.urls == null) ?
                                        new List<String>() :
                                        //for each url extract the domain and add it to the list
                                        message.data.entities.urls.Select(x => new Uri(x.expanded_url).Host).Distinct().ToList(),
                                MediaTypes = (message.data.attachments == null) ?
                                    new List<String>() :
                                    (message.data.attachments.media_keys == null) ?
                                        new List<String>() :
                                        //for each media attachment find it's entry in the "includes" section
                                        //and add it's type to the list if it is not already there
                                        message.data.attachments.media_keys
                                            .Select(x => message.includes.media.FirstOrDefault(m => m.media_key == x).type)
                                            .Distinct().ToList()
                            });
                        }
                        catch (Exception ex)
                        {
                            _log.HandleException(ex, string.Format("Processing Tweet:\n{0}\n", rawData));
                        }
                    } //if message not empty
                }// while analyzing
                _log.Write("All messages analyzed.");
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "AnalyzerLocal.StoreData");
                status = (ex.HResult == 0) ? 1 : ex.HResult;
            }

            return status;
        }
    }
}
