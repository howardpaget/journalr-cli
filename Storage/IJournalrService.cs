using JournalrApp.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace JournalrApp.Storage
{
    public interface IJournalrService
    {
        bool AddEntry(Entry entry);
        List<Entry> ListEntries(Dictionary<string, Object> query);
        bool RemoveEntry(int count);
        bool RemoveEntry(string id);
        bool TagEntry(string id, List<string> tags);
        bool TagEntry(int count, List<string> tags);
    }
}