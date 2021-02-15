using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

using Newtonsoft.Json;
using System.IO;

namespace TwitterExercise
{
    public class ReporterConsole : IReporter
    {
        private ILog _log;
        public ReporterConsole(ILog log)
        {
            _log = log;
        }
        public async Task Send(IDataStore ds, CancellationToken cancel)
        {
            try
            {
                int retVal = await Task.Factory.StartNew(() => SendReport(ds, cancel));
                if (retVal != 0)
                {
                    _log.Write(String.Format("Reporter encountered an error.  Report may not be accurate.  Error code: {0}", retVal));
                }

            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "ReporterLocal.Send");
            }

        }

        private int SendReport(IDataStore ds, CancellationToken cancel)
        {
            int returnValue = 0;
            try
            {
                _log.Write("Starting Report.");
                Thread.Sleep(10000);
                while (!cancel.IsCancellationRequested)
                {
                    //retrieve data
                    List<TweetMetadata> tweets = ds.Retrieve();

                    //begin capture sample
                    using (TextWriter tw = new StreamWriter(@"testdata.json"))
                    {
                        tw.Write(JsonConvert.SerializeObject(tweets));
                    }

                    string report = string.Empty;

                    //count and time metrics
                    int totalCount = tweets.Count;
                    report = string.Format("{0}Total count: {1}\n", report, totalCount);
                    IEnumerable<DateTime> times = tweets.Select(x => x.TimeStamp);
                    TimeSpan span = times.Max().Subtract(times.Min());
                    double perHour = totalCount / span.TotalHours;
                    double perMinute = totalCount / span.TotalMinutes;
                    double perSecond = totalCount / span.TotalSeconds;
                    report = string.Format("{0}Rate by Hour: {1}, Minute: {2}, Second: {3}\n", report, perHour, perMinute, perSecond);

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
                    report = string.Format("{0}Top Emojis:\n", report);
                    foreach (Tuple<EmojiData, int> tup in topEmoji)
                    {
                        report = string.Format("{0}     Character: {1}, Name: {2}, Code: {3}, Tweets: {4}\n",
                            report, tup.Item1.character, tup.Item1.name, tup.Item1.unified, tup.Item2);
                    }
                    report = string.Format("{0}{1:P2} of Sampled tweets have emojis.\n",
                        report, Convert.ToDouble(tweets.Count(x => x.Emojis.Any())) / totalCount);

                    //hashtag metrics
                    List<Tuple<string, int>> topHashTags = tweets.SelectMany(x => x.HashTags).Distinct()
                        .Select(hashTag => new Tuple<string, int>(hashTag, tweets.Count(tweet => tweet.HashTags.Contains(hashTag))))
                        .OrderByDescending(x => x.Item2).Take(5).ToList();
                    report = string.Format("{0}Top HashTags:\n", report);
                    foreach (Tuple<string, int> tup in topHashTags)
                    {
                        report = string.Format("{0}     Hashtag: {1}, Tweets: {2}\n", report, tup.Item1, tup.Item2);
                    }
                    report = String.Format("{0}{1:P2} of Sampled tweets have hashtags.\n",
                        report, Convert.ToDouble(tweets.Count(x => x.HashTags.Any())) / totalCount);

                    //Domain metrics
                    List<Tuple<string, int>> topDomains = tweets.SelectMany(x => x.Domains).Distinct()
                        .Select(domain => new Tuple<string, int>(domain, tweets.Count(tweet => tweet.Domains.Contains(domain))))
                        .OrderByDescending(x => x.Item2).Take(5).ToList();
                    report = string.Format("{0}Top Domains:\n", report);
                    foreach (Tuple<string, int> tup in topDomains)
                    {
                        report = string.Format("{0}     Domain: {1}, Tweets: {2}\n", report, tup.Item1, tup.Item2);
                    }
                    report = String.Format("{0}{1:P2} of Sampled tweets have urls.\n",
                        report, Convert.ToDouble(tweets.Count(x => x.Domains.Any())) / totalCount);

                    //media metrics
                    int totalMediaCount = tweets.Count(x => x.MediaTypes.Any());
                    report = String.Format("{0}{1} sampled tweets have a media attachment ({2:P2} of total)\n",
                        report, totalMediaCount, Convert.ToDouble(totalMediaCount) / totalCount);
                    foreach (string type in tweets.SelectMany(x => x.MediaTypes).Distinct().OrderBy(x => x).ToList())
                    {
                        int mediaTypeCount = tweets.Count(x => x.MediaTypes.Contains(type));
                        report = string.Format("{0}{1} sampled tweets have a {2} attachment ({3:P2} of total)\n",
                            report, mediaTypeCount, type, Convert.ToDouble(mediaTypeCount) / totalCount);
                    }
                    int photoUrls = tweets.Count(t => t.Domains.Any(d =>
                        (d.ToLower() == "pic.twitter.com") || d.ToLower().Contains("instagram")));
                    report = string.Format("{0}{1} sampled tweets have a photo url (pic.twitter.com or instagram) ({2:P2} of total)",
                        report, photoUrls, Convert.ToDouble(photoUrls) / totalCount);
                    Console.WriteLine(report);
                    Thread.Sleep(10000);
                }
                _log.Write("Reporting thread finished");
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, "ReportConsole.SendReport");
                returnValue = (ex.HResult == 0) ? 1 : ex.HResult;
            }
            return returnValue;
        }
    }
}
