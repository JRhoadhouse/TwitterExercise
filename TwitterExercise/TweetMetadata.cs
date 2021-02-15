using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    public class TweetMetadata
    {
        public DateTime TimeStamp;
        public String Id;
        public String Data;
        public List<EmojiData> Emojis;
        public List<string> HashTags;
        public List<string> Domains;
        public List<string> MediaTypes;

        public bool Equals(TweetMetadata c)
        {
            bool retVal = (c.TimeStamp == TimeStamp) &&
                (c.Id == Id) &&
                (c.Data == Data) &&
                (c.Emojis.Count == Emojis.Count) &&
                (c.HashTags.Count == HashTags.Count) &&
                (c.Domains.Count == Domains.Count) &&
                (c.MediaTypes.Count == MediaTypes.Count);

            if (retVal)
            {
                foreach (EmojiData e in Emojis)
                {
                    if (c.Emojis.Find(x => x.unified == e.unified) == null)
                    {
                        retVal = false;
                        break;
                    }
                }
            }
            if (retVal)
            {
                foreach (string h in HashTags)
                {
                    if (!c.HashTags.Contains(h))
                    {
                        retVal = false;
                        break;
                    }
                }
            }
            if (retVal)
            {
                foreach (string d in Domains)
                {
                    if (!c.Domains.Contains(d))
                    {
                        retVal = false;
                        break;
                    }
                }
            }
            if (retVal)
            {
                foreach (string m in MediaTypes)
                {
                    if (!c.MediaTypes.Contains(m))
                    {
                        retVal = false;
                        break;
                    }
                }
            }


            return retVal;

        }
    }
}
