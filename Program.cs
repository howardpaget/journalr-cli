using System;
using Microsoft.Data.Sqlite;
using JournalrApp.Model;
using JournalrApp.Storage;
using System.Runtime.InteropServices;
using System.IO;
using CommandLine;
using System.Linq;
using System.Collections.Generic;

namespace JournalrApp
{
    class Program
    {
        [Verb("add", HelpText = "Add an entry")]
        class AddOptions
        {
            [Option('t', "tags")]
            public string Tags { get; set; }

            [Option('b', "body", Required = true)]
            public string Body { get; set; }

            [Option('d', "datetime")]
            public string DateTime { get; set; }
        }

        [Verb("ls", HelpText = "List entries")]
        class ListOptions
        {
            [Option('n', Default = null)]
            public int? Count { get; set; }

            [Option('d', "datetime")]
            public string DateTime { get; set; }

            [Option('t', "tag")]
            public string Tag { get; set; }
        }

        [Verb("rm", HelpText = "Remove an entry")]
        class RemoveOptions
        {
            [Option('i', "id")]
            public string Id { get; set; }

            [Option('n', Default = 1)]
            public int Count { get; set; }
        }

        [Verb("tag", HelpText = "Add tag to entry")]
        class TagOptions
        {
            [Option('i', "id")]
            public string Id { get; set; }

            [Option('n', Default = 1)]
            public int Count { get; set; }

            [Option('t', "tags")]
            public string Tags { get; set; }
        }

        [Verb("date", HelpText = "Test date")]
        class DateOptions
        {
            [Option('d', "datetime")]
            public string DateTime { get; set; }
        }

        static void Main(string[] args)
        {

            var service = new SQLiteJournalrService();
            CommandLine.Parser.Default.ParseArguments<AddOptions, ListOptions, RemoveOptions, TagOptions, DateOptions>(args)
            .MapResult(
                (AddOptions opts) => HandleAdd(service, opts),
                (ListOptions opts) => HandleList(service, opts),
                (RemoveOptions opts) => HandleRemove(service, opts),
                (TagOptions opts) => HandleTag(service, opts),
                (DateOptions opts) => HandleDate(opts),
                errs => 1);
        }

        private static int HandleAdd(IJournalrService service, AddOptions opts)
        {
            var datetime = new Chronic.Parser().Parse(opts.DateTime ?? "").Start;
            if (datetime == null)
            {
                Console.WriteLine($"Could not parse {opts.DateTime}");
                return 1;
            }

            var entry = new Entry { EntryId = GenerateId(), Text = opts.Body, EntryDate = datetime.Value, CreatedDate = DateTime.Now, Tags = (opts.Tags ?? "").Split(',').ToList() };
            if (service.AddEntry(entry))
                Console.WriteLine($"Added entry: { entry.EntryId }");
            else
                Console.WriteLine("Failed to add entry");

            return 0;
        }

        private static int HandleList(IJournalrService service, ListOptions opts)
        {
            var query = new Dictionary<string, Object>();

            if (opts.Count != null)
                query["count"] = opts.Count.Value;

            if (opts.DateTime != null)
            {
                var span = new Chronic.Parser().Parse(opts.DateTime);

                if (span != null)
                {
                    query["start"] = span.Start;
                    query["end"] = span.End;
                }
            }

            var entries = service.ListEntries(query);
            if (entries.Count > 0)
                entries.ForEach(e => PrintEntry(e));
            else
                Console.WriteLine("No entries found");

            return 0;
        }

        private static int HandleRemove(IJournalrService service, RemoveOptions opts)
        {
            var success = false;
            if (opts.Id != null)
                success = service.RemoveEntry(opts.Id);
            else
                success = service.RemoveEntry(opts.Count);

            if (success)
                Console.WriteLine("Deleted");
            else
                Console.WriteLine("Delete failed");

            return 0;
        }

        private static int HandleTag(IJournalrService service, TagOptions opts)
        {
            var success = false;
            if (opts.Id == null)
                success = service.TagEntry(opts.Count, opts.Tags.Split(',').ToList());
            else
                success = service.TagEntry(opts.Id, opts.Tags.Split(',').ToList());

            if (success)
                Console.WriteLine("Tagged");
            else
                Console.WriteLine("Tag failed");

            return 0;
        }

        private static int HandleDate(DateOptions opts)
        {
            var datetime = new Chronic.Parser().Parse(opts.DateTime).Start;
            if(datetime == null)
                Console.WriteLine("Parse failed");
            else
                Console.WriteLine(datetime);
            return 0;
        }

        public static string GenerateId()
        {
            var words = File.ReadLines("words.txt").ToList();
            var random = new Random();
            return words[random.Next(words.Count)] + "-" + words[random.Next(words.Count)] + "-" + random.Next(1, 99);
        }

        private static void PrintEntry(Entry entry)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.Write(entry.EntryDate);
            Console.Write("\t");
            Console.Write(entry.EntryId);

            Console.Write("\t");
            Console.WriteLine(String.Join(", ", entry.Tags));

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(entry.Text);
            Console.WriteLine();
        }
    }
}
