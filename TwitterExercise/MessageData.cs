using System;
using System.Collections.Generic;
using System.Text;

namespace TwitterExercise
{
    internal class MessageData
    {
        public string id;
        public string text;
        public entity entities;
        public Attachments attachments;
        public DateTime created_at;
    }
}
