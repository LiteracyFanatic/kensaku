# Kensaku.Core

Query interface for the Kensaku Japanese dictionary database. Provides high-level APIs for searching kanji, words, and radicals with strongly-typed query builders and result types. This project is one layer of the broader Kensaku stack and focuses on querying a pre-built SQLite database containing JMdict, Kanjidic2, and related data.

## Features

- **Kanji Search**: Query kanji by stroke count, radicals, readings, meanings, and various character codes (SKIP, Four Corner, etc.)
- **Word Search**: Search Japanese dictionary entries by reading or meaning
- **Radical Search**: Find radicals by number, name, meaning, or stroke count

## Usage

This library requires a pre-built Kensaku database file. See the main Kensaku repository to download or create one.

```fsharp
open Kensaku.Core.Radicals
open Kensaku.Core.Kanji
open Kensaku.Core.Words

task {
    // Open a connection to the database
    use ctx = new KensakuConnection("Data Source=kensaku.db")

    // Search for radicals
    let radicalQuery = {
        RadicalNumber = None
        RadicalName = None
        RadicalMeaning = Some "water"
        MinStrokeCount = None
        MaxStrokeCount = None
    }
    let! radicals = getRadicalsAsync radicalQuery ctx

    for r in radicals do
        printfn "%A" r

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
    let! kanji = getKanjiAsync kanjiQuery ctx

    for k in kanji do
        printfn "%A" k

    // Search for words
    let wordQuery: GetWordsQuery = {
        Reading = Some "こんにちは"
        Meaning = None
    }
    let! words = getWordsAsync wordQuery ctx

    for w in words do
        printfn "%A" w
} |> Async.AwaitTask |> Async.RunSynchronously
```
