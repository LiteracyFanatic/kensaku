# kensaku

検索, pronounced kensaku, means "search" or "retrieval" in Japanese. This project aims to make it easier for Japanese learners to find information about kanji, radicals, and vocabulary. It does this by bundling information from the [JMdict](https://www.edrdg.org/jmdict/j_jmdict.html), [kanjidic2](http://www.edrdg.org/wiki/index.php/KANJIDIC_Project), and [kradfile](http://www.edrdg.org/krad/kradinf.html) projects into an SQLite database. While excellent resources, the complicated XML schemas and reliance on state tracking during parsing make the first two projects awkward to work with. Others have made this information available in more convenient formats like JSON, but the issues of memory usage and lookup performance with such large files remain. A number of database generators for a subset of this data exist, but to my knowledge this is the only project that provides simple and comprehensive access to all of it.

## Status

This project is under active development. It currently features a dotnet console application written in F# which downloads and parses the aforementioned files and uses them to populate an SQLite database. The database is already in a usable state and contains all of the information made available by the different projects. However, the exact schema is still subject to change.

For now, queries must be made directly using SQL, but a library making common tasks available directly from dotnet code is planned as well as a command line application and possibly a GUI.

## Example usage

```sql
select k.Value, r.Value, g.Value
from Entries as e
join KanjiElements as k on k.EntryId = e.id
join ReadingElements as r on r.EntryId = e.id
join Senses as s on s.EntryId = e.Id
join Glosses as g on g.SenseId = s.Id
where g.Language = "eng" and k.Value = "検索"
```

| kanji | hiragana | meaning |
|-----|-------|------------------------------------------|
| 検索 | けんさく | looking up (e.g. a word in a dictionary) |
| 検索 | けんさく | retrieval (e.g. data)                    |
| 検索 | けんさく | searching for                            |
| 検索 | けんさく | referring to                             |

## Why not just use Jisho?

Valid question. For those who don't know, [jisho.org](https://jisho.org) is a fantastic online Japanese-English dictionary which is built on top of the same datasets as this project. I have 3 main reasons:

1. **Offline access and latency.** A native application can offer faster and more reliable results than a web application. When learning a new language, it is common to make dozens of searches a day, and the longer it takes to get results, the more one's reading immersion is broken.
2. **More customizable and powerful search.** Jisho lets you lookup unknown kanji by inputting a stroke count and selecting radicals from a table. This is really useful, but scanning through the table to find the radical you're looking for can take a long time. This project aims to let users refer to radicals by commonly used mnemonics. For example, one could search for `賀` by typing `power` and `shellfish` instead of clicking on `力` and `貝`.
3. **Easy integration into other software.** Other dotnet applications should be able to easily build features for rich Japanese language support on top of this library without reimplementing everything from scratch.
