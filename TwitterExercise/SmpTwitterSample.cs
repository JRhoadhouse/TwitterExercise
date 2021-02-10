using System;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace TwitterExercise
{
    class SmpTwitterSample :ISocialMediaProvider
    {
        private ILog _log;
        public SmpTwitterSample(ILog log)
        {
            _log = log;
        }
        public async Task Retrieve(IRawDataQueue rawData, CancellationToken cancel)
        {
            try
            {
                // Establish connection.
                _log.Write("Calling Service");
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
                    _log.Write("Retrieving Data");
                    using (var reader = new StreamReader(response.Result))
                    {
                        //will stop sampling when token is cancelled
                        while (!cancel.IsCancellationRequested)
                        {
                            //if one call fails we want to continue on
                            try
                            {
                                //using async method allows cancellation easier. 
                                string jsonData = await reader.ReadLineAsync();
                                //_log.Write(string.Format("Retrieved Data:")); //debug code
                                //for now just store the raw string, will process data in a seperate thread
                                rawData.Add(jsonData);
                            }
                            catch (Exception ex)
                            {
                                _log.HandleException(ex, "Retrieving sample data from twitter.");
                            }
                        }
                        _log.Write("Data retrieval stopped.");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.HandleException(ex, String.Format("SmpTwitterSample.Retrieve"));
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
            catch (Exception ex)
            {
                _log.HandleException(ex, "GetBearerToken Function");
                returnValue = string.Empty;
            }
            return returnValue;
        }

    }
}
