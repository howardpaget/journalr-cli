# Journalr

Journalr is a CLI journaling tool helps you keep track of your day. Entries can be tagged to enable search.

## Add an Entry

The command below creates an entry with body "Lunch with Barry at Lovely Burger Co." at 12:30 today tagged with lunch. A random id of 2 words and a number is created of the entry.

`jlr add -d "12:30" -b "Lunch with Barry at Lovely Burger Co." -t "lunch"`

### Arguments

    - d (optional default = now): The entry date. The string is parsed using Chronic to allow natural language date input such as "tomorrow 13:45"
    - b: The entry body
    - t (optional): The tags to apply to the entry as a comma separated string

## Remove an Entry

The first command below removes the entry with id = "curious-cat-1", the second removes the 5 latest entries, and the third removes the latest. 

```
jlr rm -i curious-cat-1
jlr rm -n 5
jlr rm
```

### Arguments

    - i (optional): The id of the entry to remove
    - n (optional default = 1): The number of entries to remove ordered by entry date from most recent to least
    
## List Entries

The command lists latest 10 entries from this week tagged as exercise.

```
jlr ls -n 10 -d "this week" -t "exercise"
```

### Arguments

    - n (optional): The number of entries to list ordered by entry date from most recent to least
    - d (optional): The date range to filter by
    - t (optional): The tag to filter by

## Add Additional Tags to an Entry

The command tags the entry with id = "blue-dog-12" as social and entertainment.

```
jlr tag -i blur-dog-12 -t "social,entertainment"
```

### Arguments

    - i (optional): The id of the entry to tag
    - n (optional default = 1): The number of entries to tag
    - t: The tags to apply to the entry as a comma separated string 
