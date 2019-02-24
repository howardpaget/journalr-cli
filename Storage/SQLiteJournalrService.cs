using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using JournalrApp.Model;
using Microsoft.Data.Sqlite;

namespace JournalrApp.Storage
{
    public class SQLiteJournalrService : IJournalrService
    {
        private string Home;

        public SQLiteJournalrService()
        {
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            this.Home = isWindows ?
                Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH")
                : "$home";

            Directory.CreateDirectory(Path.Combine(this.Home, ".journalr"));

            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var existsCommand = connection.CreateCommand();
                    existsCommand.Transaction = transaction;
                    existsCommand.CommandText = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "create.sql"));
                    existsCommand.ExecuteNonQuery();

                    transaction.Commit();
                }
            }

        }
        public bool AddEntry(Entry entry)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = "INSERT INTO Entry ( id, text, entry_date, created_date, tags ) VALUES ( $id, $text, $entry_date, $created_date, $tags )";
                    insertCommand.Parameters.AddWithValue("$id", entry.EntryId);
                    insertCommand.Parameters.AddWithValue("$text", entry.Text);
                    insertCommand.Parameters.AddWithValue("$entry_date", entry.EntryDate);
                    insertCommand.Parameters.AddWithValue("$created_date", entry.CreatedDate);
                    insertCommand.Parameters.AddWithValue("$tags", string.Join(',', entry.Tags));

                    var result = insertCommand.ExecuteNonQuery() == 1;

                    foreach (var tag in entry.Tags)
                    {
                        if(string.IsNullOrWhiteSpace(tag))
                            continue;
                        var insertTagCommand = connection.CreateCommand();
                        insertTagCommand.Transaction = transaction;
                        insertTagCommand.CommandText = "INSERT INTO Tag ( entry_id, tag ) VALUES ( $id, $tag )";
                        insertTagCommand.Parameters.AddWithValue("$id", entry.EntryId);
                        insertTagCommand.Parameters.AddWithValue("$tag", tag);

                        insertTagCommand.ExecuteNonQuery();
                    }


                    transaction.Commit();

                    return result;
                }
            }
        }

        public bool UpdateEntry(Entry entry)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = "UPDATE Entry SET text = $text, entry_date = $entry_date WHERE id = $id";
                    insertCommand.Parameters.AddWithValue("$id", entry.EntryId);
                    insertCommand.Parameters.AddWithValue("$text", entry.Text);
                    insertCommand.Parameters.AddWithValue("$entry_date", entry.EntryDate);

                    var result = insertCommand.ExecuteNonQuery() == 1;

                    var deleteTagsCommand = connection.CreateCommand();
                    deleteTagsCommand.Transaction = transaction;
                    deleteTagsCommand.CommandText = "DELETE FROM Tag WHERE entry_id = $id";
                    deleteTagsCommand.Parameters.AddWithValue("$id", entry.EntryId);

                    deleteTagsCommand.ExecuteNonQuery();

                    foreach (var tag in entry.Tags)
                    {
                        if(string.IsNullOrWhiteSpace(tag))
                            continue;
                        var insertTagCommand = connection.CreateCommand();
                        insertTagCommand.Transaction = transaction;
                        insertTagCommand.CommandText = "INSERT INTO Tag ( entry_id, tag ) VALUES ( $id, $tag )";
                        insertTagCommand.Parameters.AddWithValue("$id", entry.EntryId);
                        insertTagCommand.Parameters.AddWithValue("$tag", tag);

                        insertTagCommand.ExecuteNonQuery();
                    }


                    transaction.Commit();

                    return result;
                }
            }
        }

        public Entry GetEntry(string id)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var selectCommand = connection.CreateCommand();
                    selectCommand.Transaction = transaction;

                    var selectTagCommand = connection.CreateCommand();
                    selectTagCommand.Transaction = transaction;

                    var sql = "SELECT id, text, entry_date, created_date, tags FROM Entry WHERE id = $id";
                    var tagSql = "SELECT id, Tag.tag FROM Entry LEFT JOIN Tag ON Entry.id = Tag.entry_id WHERE id = $id ORDER BY entry_date DESC";

                    selectCommand.CommandText = sql;
                    selectTagCommand.CommandText = tagSql;

                    selectCommand.Parameters.AddWithValue("$id", id);
                    selectTagCommand.Parameters.AddWithValue("$id", id);

                    var tagMap = new Dictionary<string, List<string>>();
                    using (var reader = selectTagCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entryId = reader.GetString(0);
                            if (!reader.IsDBNull(1))
                            {
                                var tag = reader.GetString(1);
                                if (!tagMap.ContainsKey(entryId))
                                    tagMap[entryId] = new List<string>();

                                tagMap[entryId].Add(tag);
                            }
                        }
                    }

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        var entries = new List<Entry>();
                        while (reader.Read())
                        {
                            return new Entry
                            {
                                EntryId = reader.GetString(0),
                                Text = reader.GetString(1),
                                EntryDate = reader.GetDateTime(2),
                                CreatedDate = reader.GetDateTime(3),
                                Tags = tagMap.ContainsKey(reader.GetString(0)) ? tagMap[reader.GetString(0)] : new List<string>()
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public List<Entry> ListEntries(Dictionary<string, object> query)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var selectCommand = connection.CreateCommand();
                    selectCommand.Transaction = transaction;

                    var selectTagCommand = connection.CreateCommand();
                    selectTagCommand.Transaction = transaction;

                    var sql = "SELECT id, text, entry_date, created_date, tags FROM Entry ORDER BY entry_date DESC";
                    var tagSql = "SELECT id, Tag.tag FROM Entry LEFT JOIN Tag ON Entry.id = Tag.entry_id ORDER BY entry_date DESC";
                    if (query.ContainsKey("start") && query.ContainsKey("end"))
                    {
                        sql = "SELECT id, text, entry_date, created_date, tags FROM Entry WHERE entry_date >= $start AND entry_date <= $end ORDER BY entry_date DESC";
                        tagSql = "SELECT id, Tag.tag FROM Entry LEFT JOIN Tag ON Entry.id = Tag.entry_id WHERE entry_date >= $start AND entry_date <= $end ORDER BY entry_date DESC";
                        selectCommand.Parameters.AddWithValue("$start", query["start"]);
                        selectCommand.Parameters.AddWithValue("$end", query["end"]);

                        selectTagCommand.Parameters.AddWithValue("$start", query["start"]);
                        selectTagCommand.Parameters.AddWithValue("$end", query["end"]);
                    }

                    if (query.ContainsKey("count"))
                    {
                        sql += " LIMIT $n";
                        selectCommand.Parameters.AddWithValue("$n", query["count"]);
                    }

                    selectCommand.CommandText = sql;
                    selectTagCommand.CommandText = tagSql;

                    var tagMap = new Dictionary<string, List<string>>();
                    using (var reader = selectTagCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entryId = reader.GetString(0);
                            if (!reader.IsDBNull(1))
                            {
                                var tag = reader.GetString(1);
                                if (!tagMap.ContainsKey(entryId))
                                    tagMap[entryId] = new List<string>();

                                tagMap[entryId].Add(tag);
                            }
                        }
                    }

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        var entries = new List<Entry>();
                        while (reader.Read())
                        {
                            var entry = new Entry
                            {
                                EntryId = reader.GetString(0),
                                Text = reader.GetString(1),
                                EntryDate = reader.GetDateTime(2),
                                CreatedDate = reader.GetDateTime(3),
                                Tags = tagMap.ContainsKey(reader.GetString(0)) ? tagMap[reader.GetString(0)] : new List<string>()
                            };
                            entries.Add(entry);
                        }
                        return entries;
                    }
                }
            }
        }

        public bool RemoveEntry(int count)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    var deleteTagsCommand = connection.CreateCommand();
                    deleteTagsCommand.Transaction = transaction;
                    deleteTagsCommand.CommandText = "DELETE FROM Tag WHERE entry_id IN (SELECT id FROM Entry ORDER BY entry_date DESC LIMIT $n)";
                    deleteTagsCommand.Parameters.AddWithValue("$n", count);

                    var result1 = deleteTagsCommand.ExecuteNonQuery() > 0;

                    var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = "DELETE FROM Entry WHERE id IN (SELECT id FROM Entry ORDER BY entry_date DESC LIMIT $n)";
                    insertCommand.Parameters.AddWithValue("$n", count);

                    var result = insertCommand.ExecuteNonQuery() > 0;

                    transaction.Commit();

                    return result;
                }
            }
        }
        public bool RemoveEntry(string id)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var deleteTagsCommand = connection.CreateCommand();
                    deleteTagsCommand.Transaction = transaction;
                    deleteTagsCommand.CommandText = "DELETE FROM Tag WHERE entry_id = $id";
                    deleteTagsCommand.Parameters.AddWithValue("$id", id);

                    var result1 = deleteTagsCommand.ExecuteNonQuery() == 1;


                    var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = "DELETE FROM Entry WHERE id = $id";
                    insertCommand.Parameters.AddWithValue("$id", id);

                    var result = insertCommand.ExecuteNonQuery() == 1;

                    transaction.Commit();

                    return result;
                }
            }
        }

        public bool TagEntry(string id, List<string> tags)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var tag in tags)
                    {
                        var selectCommand = connection.CreateCommand();
                        selectCommand.Transaction = transaction;
                        selectCommand.CommandText = "SELECT id FROM Entry WHERE id = $id";
                        selectCommand.Parameters.AddWithValue("$id", id);

                        if (!selectCommand.ExecuteReader().Read())
                        {
                            return false;
                        }
                    }

                    foreach (var tag in tags)
                    {
                        var insertTagCommand = connection.CreateCommand();
                        insertTagCommand.Transaction = transaction;
                        insertTagCommand.CommandText = "INSERT INTO Tag ( entry_id, tag ) VALUES ( $id, $tag )";
                        insertTagCommand.Parameters.AddWithValue("$id", id);
                        insertTagCommand.Parameters.AddWithValue("$tag", tag);

                        insertTagCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    return true;
                }
            }
        }

        public bool TagEntry(int count, List<string> tags)
        {
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(this.Home, ".journalr", "journalr.db") }.ToString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var entryIds = new List<string>();
                    var selectCommand = connection.CreateCommand();
                    selectCommand.Transaction = transaction;
                    selectCommand.CommandText = "SELECT id FROM Entry ORDER BY entry_date DESC LIMIT $n";
                    selectCommand.Parameters.AddWithValue("$n", count);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entryIds.Add(reader.GetString(0));
                        }
                    }


                    foreach (var id in entryIds)
                    {
                        foreach (var tag in tags)
                        {
                            var insertTagCommand = connection.CreateCommand();
                            insertTagCommand.Transaction = transaction;
                            insertTagCommand.CommandText = "INSERT INTO Tag ( entry_id, tag ) VALUES ( $id, $tag )";
                            insertTagCommand.Parameters.AddWithValue("$id", id);
                            insertTagCommand.Parameters.AddWithValue("$tag", tag);

                            insertTagCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    return true;
                }
            }
        }
    }
}