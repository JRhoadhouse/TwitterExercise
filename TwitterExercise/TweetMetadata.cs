using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    class TweetMetadata
    {
        public DateTime TimeStamp;
        public String Id;
        public String Data;
        public List<EmojiData> Emojis;
        public List<string> HashTags;
        public List<string> Domains;
        public List<string> MediaTypes;
    }
}
