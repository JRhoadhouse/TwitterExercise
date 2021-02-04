using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Permissions;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Configuration;

namespace TwitterExercise
{
    class TwitterWrapper
    {
        private List<TweetMetadata> tweetsMemoryStore;
        private List<EmojiData> emojis;
        public TwitterWrapper()
        {
            try
            {
                tweetsMemoryStore = new List<TweetMetadata>();
                GetEmojiData();
            }
            catch(Exception ex)
            {
                HandleException(ex, "Twitter Wrapper Constructor");
            }
        }
        private string GetBearerToken()
        {
            //This should be included in a encrypted source but is left in the configuration file for portablity to the reviewer
            string returnValue = string.Empty;
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                returnValue = ConfigurationManager.AppSettings.Get("BearerToken");
            }
            catch(Exception ex)
            {
                HandleException(ex, "GetBearerToken Function");
                returnValue = string.Empty;
            }
            return returnValue;
        }
        //Pulls sampling from twitter until cancellationToken is cancelled, reports statisics every [span] time unit (default 10s)
        public async void SampleAsynch(CancellationToken cancellationToken, TimeSpan? timeSpan)
        {
            try
            {
                DateTime lastReport = DateTime.Now;

                //get real value from nullable type.
                TimeSpan realSpan = new TimeSpan(0, 0, 10);
                if (timeSpan.HasValue)
                {
                    realSpan = timeSpan.Value;
                }

                // Establish connection.
                Log("Calling Service");
                using (HttpClient client = new HttpClient())
                {
                    string token = GetBearerToken();
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        throw new Exception("Could not retrieve bearer token.");
                    }
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    Task<Stream> response = client.GetStreamAsync(
                            "https://api.twitter.com/2/tweets/sample/stream?tweet.fields=attachments,created_at,entities&expansions=attachments.media_keys");

                    // Get the stream of the content.
                    Log("Retrieving Data");
                    List<string> messages = new List<string>();
                    using (var reader = new StreamReader(response.Result))
                    {
                        //will stop sampling when token is cancelled
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            //if one call fails we want to continue on
                            try
                            {
                                //using async method allows cancellation easier. 
                                string jsonData = await reader.ReadLineAsync();
                                //for now just store the raw string, will process data in a seperate thread
                                messages.Add(jsonData);
                                //once the span has elapsed process the data that is collected in a seperate thread
                                if (DateTime.Now >= lastReport.Add(realSpan))
                                {
                                    lastReport = DateTime.Now;
                                    ProcessAsynch(messages);
                                    messages = new List<string>();
                                    //This message included to show that data collection is proceeding
                                    //while processing the last batch
                                    Log("Data store initiated, continuing to retrieve data.");
                                }
                            }
                            catch (Exception ex)
                            {
                                HandleException(ex, "Retrieving sample data from twitter and processing.");
                            }
                        }
                    }
                }
                Log("Data Retrieval Stopped");
            }
            catch (Exception ex)
            {
                HandleException(ex, "SampleAsynch Function");
            }
        }

        private async void ProcessAsynch (List<string> messages)
        {
            try
            {
                //start a new task for processing data
                int retVal = await Task.Factory.StartNew(() => StoreData(messages));
                if (retVal != 0)
                {
                    Log(String.Format("Store Data encountered an error.  Report may not be accurate.  Error code: {0}", retVal));
                }
                retVal = await Task.Factory.StartNew(() => Report());
                if (retVal != 0)
                {
                    Log(String.Format("Report encountered an error.  Please refer to next or previous report.  Error code: {0}", retVal));
                }
            }
            catch(Exception ex)
            {
                HandleException(ex, "ProcessAsynch Function");
            }
        }

        #region Data Accessor Methods
        //these methods can be replaced to use database web service and / or store different metrics
        private int StoreData(List<string> messages)
        {
            int status = 0;
            try
            {
                foreach (string jsonData in messages)
                {
                    if (!string.IsNullOrWhiteSpace(jsonData))
                    {
                        //If one tweet is problematic we want to carry on.
                        try
                        {
                            //The message object and it's children or more complex than they need to be.
                            //They are built to mimic the Json structure as closely as neccessary.
                            //This will make it easier to collect additonal statistics in the future.
                            Message message = JsonConvert.DeserializeObject<Message>(jsonData);
                            tweetsMemoryStore.Add(new TweetMetadata()
                            {
                                TimeStamp = message.data.created_at,
                                Id = message.data.id,
                                Data = message.data.text,
                                Emojis = (message.data.text == null) ?
                                    new List<EmojiData>() :
                                    //for each emoji add it to the list if it appears in the text
                                    emojis.Where(x => !string.IsNullOrWhiteSpace(x.character)
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
                            HandleException(ex, string.Format("Processing Tweet:\n{0}\n",jsonData));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "StoreData Function");
                status = (ex.HResult == 0) ? 1 : ex.HResult;
            }

            return status;
        }

        private List<TweetMetadata> RetrieveData()
        {
            List<TweetMetadata> returnValue = new List<TweetMetadata>();
            /*this doesn't really need a try catch but is included for consistancy 
            * and it can be used as a template for other RetrieveData methods*/
            try
            {
                returnValue = tweetsMemoryStore;
            }
            catch (Exception ex)
            {
                HandleException(ex, "RetrieveData Function");
            }
            return tweetsMemoryStore;
        }
        #endregion

        private int Report()
        {
            int returnValue = 0;
            try
            {
                //retrieve data
                List<TweetMetadata> tweets = RetrieveData();


                //count and time metrics
                int totalCount = tweets.Count;
                Log(String.Format("Total count: {0}", totalCount));
                IEnumerable<DateTime> times = tweets.Select(x => x.TimeStamp);
                TimeSpan span = times.Max().Subtract(times.Min());
                double perHour = totalCount / span.TotalHours;
                double perMinute = totalCount / span.TotalMinutes;
                double perSecond = totalCount / span.TotalSeconds;
                Log(string.Format("Rate by Hour: {0}, Minute: {1}, Second: {2}", perHour, perMinute, perSecond));

                /* To find the most common items in a collection:
                 * 1.Get a list of each distinct item to be ranked
                 * 2.For each distinct item get a count of how many times it occurs (this is stored in a tuple with the item)
                 * 3.Order the list by counts and take the first x (in this case 5)
                 * This process is performed on emojis, hashtags, and domains*/

                //emoji metrics
                List<Tuple<EmojiData, int>> topEmoji = tweets.SelectMany(x => x.Emojis).Distinct()
                    .Select(emoji => new Tuple<EmojiData, int>
                        //When comparing emojis compare the unicode number not the object 
                        //this way if future modifications change the equality operator of the class this will still work.
                        (emoji, tweets.Count(tweet => tweet.Emojis.Any(e => e.unified == emoji.unified))))
                    .OrderByDescending(x => x.Item2).Take(5).ToList();
                Log("Top Emojis:");
                foreach (Tuple<EmojiData, int> tup in topEmoji)
                {
                    Log(String.Format("     Character: {0}, Name: {1}, Code: {2}, Tweets: {3}",
                        tup.Item1.character, tup.Item1.name, tup.Item1.unified, tup.Item2));
                }
                Log(String.Format("{0:P2} of Sampled tweets have emojis.",
                    Convert.ToDouble(tweets.Count(x => x.Emojis.Any())) / totalCount));

                //hashtag metrics
                List<Tuple<string, int>> topHashTags = tweets.SelectMany(x => x.HashTags).Distinct()
                    .Select(hashTag => new Tuple<string, int>(hashTag, tweets.Count(tweet => tweet.HashTags.Contains(hashTag))))
                    .OrderByDescending(x => x.Item2).Take(5).ToList();
                Log("Top HashTags:");
                foreach (Tuple<string, int> tup in topHashTags)
                {
                    Log(String.Format("     Hashtag: {0}, Tweets: {1}", tup.Item1, tup.Item2));
                }
                Log(String.Format("{0:P2} of Sampled tweets have hashtags.",
                    Convert.ToDouble(tweets.Count(x => x.HashTags.Any())) / totalCount));

                //Domain metrics
                List<Tuple<string, int>> topDomains = tweets.SelectMany(x => x.Domains).Distinct()
                    .Select(domain => new Tuple<string, int>(domain, tweets.Count(tweet => tweet.Domains.Contains(domain))))
                    .OrderByDescending(x => x.Item2).Take(5).ToList();
                Log("Top Domains:");
                foreach (Tuple<string, int> tup in topDomains)
                {
                    Log(String.Format("     Domain: {0}, Tweets: {1}", tup.Item1, tup.Item2));
                }
                Log(String.Format("{0:P2} of Sampled tweets have urls.",
                    Convert.ToDouble(tweets.Count(x => x.Domains.Any())) / totalCount));

                //media metrics
                int totalMediaCount = tweets.Count(x=>x.MediaTypes.Any());
                Log(String.Format("{0} sampled tweets have a media attachment ({1:P2} of total)",
                    totalMediaCount, Convert.ToDouble(totalMediaCount) / totalCount));
                foreach (string type in tweets.SelectMany(x => x.MediaTypes).Distinct().OrderBy(x=>x).ToList())
                {
                    int mediaTypeCount = tweets.Count(x => x.MediaTypes.Contains(type));
                    Log(String.Format("{0} sampled tweets have a {1} attachment ({2:P2} of total)",
                    mediaTypeCount, type, Convert.ToDouble(mediaTypeCount) / totalCount));
                }
                int photoUrls = tweets.Count(t => t.Domains.Any(d => 
                    (d.ToLower() == "pic.twitter.com") || d.ToLower().Contains("instagram")));
                Log(String.Format("{0} sampled tweets have a photo url (pic.twitter.com or instagram) ({1:P2} of total)",
                    photoUrls, Convert.ToDouble(photoUrls) / totalCount));
            }
            catch (Exception ex)
            {
                HandleException(ex, "Report Function");
                returnValue = (ex.HResult == 0) ? 1 : ex.HResult;
            }
            return returnValue;
        }

        private void GetEmojiData()
        {
            try
            {
                string data = string.Empty;
                using (TextReader tr = new StreamReader(@"emoji.json"))
                {
                    data = tr.ReadToEnd();
                    tr.Close();
                }
                emojis = JsonConvert.DeserializeObject<List<EmojiData>>(data);
                if (emojis.Count() == 0)
                {
                    Console.WriteLine("Could not load Emojis.");
                }
                else
                {
                    foreach (EmojiData ed in emojis)
                    {
                        if (ed.non_qualified != "null" && !string.IsNullOrWhiteSpace(ed.non_qualified))
                        {
                            string[] edCodes = ed.non_qualified.Split("-");
                            foreach (string x in edCodes)
                            {
                                if (x.Length > 4)
                                {
                                    //5 character hex is too large for unicode
                                    ed.character = string.Empty;
                                    break;
                                }
                                ed.character = string.Format("{0}{1}", ed.character, Convert.ToChar(Convert.ToInt32(x, 16)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "GetEmojiData Function");
            }
        }

        //Change this method if writing to a logfile or other media is preferable to the console
        private void Log(string message)
        {
            /*this doesn't really need a try catch but is included for consistancy 
            * and it can be used as a template for other Log methods*/
            try
            {
                Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                /*Shouldn't use error handling method as that calls this method
                * So it could result in a loop if there is a problem here.
                * Also console.writeline should be used here even if it is changed to another log method above
                * since the other log method is clearly not reliable in this case.
                * It is reccomended that the console output is captured in some way.*/
                Console.WriteLine("An exception occurred in \"Log Function\".  See details below:\n{0}", ex.Message);
            }
        }

        private void HandleException(Exception ex, string source)
        {
            try
            {
                Log(String.Format("An exception occurred in \"{0}\".  See details below:\n{1}", source, ex.Message));
                if (ex.InnerException != null)
                {
                    Log("Inner Exception:");
                    HandleException(ex.InnerException, source);
                }
            }
            catch (Exception e)
            {
                /*console.writeline should be used here instead of log as there might be a problem with the log method
                * It is reccomended that the console output is captured in some way.*/
                Console.WriteLine("Can not handle exception: {0}", e.Message);
            }
        }
    }
}
