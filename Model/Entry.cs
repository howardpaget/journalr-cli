using System;
using System.Collections.Generic;

namespace JournalrApp.Model
{
    public class Entry
    {
        public string EntryId { get; set; }
        public string Text { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Tags { get; set; }
    }
}