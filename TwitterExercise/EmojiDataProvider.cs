using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Security.Permissions;

namespace TwitterExercise
{
    class EmojiDataProvider
    {
        //This version of c# does not allow static methods in interfaces so this is left un-abstracted
        //If updated to c# v8 this class can be set up with an interface for more maintainable code.
        static public  List<EmojiData> GetEmojiData(ILog log)
        {
            List<EmojiData> retVal = new List<EmojiData>();
            try
            {
                string data = string.Empty;
                using (TextReader tr = new StreamReader(@"emoji.json"))
                {
                    data = tr.ReadToEnd();
                    tr.Close();
                }
                retVal = JsonConvert.DeserializeObject<List<EmojiData>>(data);
                if (retVal.Count == 0)
                {
                    Console.WriteLine("Could not load Emojis.");
                }
                else
                {
                    foreach (EmojiData ed in retVal)
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
                log.HandleException(ex, "GetEmojiData Function");
            }
            return retVal;
        }
    }
}
