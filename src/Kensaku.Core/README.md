# Kensaku.Core

Query interface for the Kensaku Japanese dictionary database. Provides high-level APIs for searching kanji, words, and radicals with strongly-typed query builders and result types. This project is one layer of the broader Kensaku stack and focuses on querying a pre-built SQLite database containing JMdict, Kanjidic2, and related data.

## Features

- **Kanji Search**: Query kanji by stroke count, radicals, readings, meanings, and various character codes (SKIP, Four Corner, etc.)
- **Word Search**: Search Japanese dictionary entries by reading or meaning
- **Radical Search**: Find radicals by number, name, meaning, or stroke count
- **Strongly-typed Results**: All queries return rich domain models with comprehensive metadata
- **Database Connection**: Custom connection type with optimized type handlers for Japanese text

## Usage

This library requires a pre-built Kensaku database file. See the main Kensaku repository for database creation tools.

```fsharp
open Kensaku.Core
open Kensaku.Core.Domain

// Open a connection to the database
use ctx = new KensakuConnection("Data Source=kensaku.db")
ctx.OpenAsync() |> Async.AwaitTask |> Async.RunSynchronously

// Search for kanji
let kanjiQuery = {
    MinStrokeCount = Some 5
    MaxStrokeCount = Some 10
    IncludeStrokeMiscounts = false
    SearchRadicals = []
    SearchRadicalMeanings = []
    CharacterCode = None
    CharacterReading = Some "あい"
    CharacterMeaning = None
    Nanori = None
    CommonOnly = false
    Pattern = None
    KeyRadical = None
}
let! kanji = Kanji.getKanjiAsync kanjiQuery ctx

// Search for words
let wordQuery = {
    Reading = Some "こんにちは"
    Meaning = None
}
let! words = Words.getWordsAsync wordQuery ctx
```

## Related Packages

- **Kensaku.DataSources**: Parsers for source dictionary files (JMdict, Kanjidic2, etc.)
- **Kensaku.CLI**: Command-line interface for the Kensaku dictionary
