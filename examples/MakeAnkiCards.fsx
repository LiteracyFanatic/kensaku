#r "../src/Database/bin/Debug/net9.0/Database.dll"
#r "../src/Core/bin/Debug/net9.0/Core.dll"
#r "nuget: Feliz.ViewEngine"
#r "nuget: CsvHelper"
#r "nuget: Microsoft.Data.Sqlite"
#r "nuget: Dapper"

open System
open System.IO
open CsvHelper.Configuration
open Kensaku.Core.Words
open Kensaku.Domain
open Feliz.ViewEngine
open Microsoft.Data.Sqlite
open Kensaku.Database
open CsvHelper
open System.Globalization

module List =
    let tryNotEmpty (list: 'a list) =
        if list.IsEmpty then None else Some list

type AnkiNote = {
    Word: string
    Front: string
    Back: string
}

let getPrimaryForm (word: GetWordQueryResult) =
    let primaryEntryLabel, _ = getPrimaryAndAlternateForms word
    primaryEntryLabel


let printWord (word: GetWordQueryResult) (needsFurigana: bool) =
    let primaryEntryLabel, alternateForms = getPrimaryAndAlternateForms word

    let frontText =
        if needsFurigana then
            primaryEntryLabel.ToString()
        else
            primaryEntryLabel.Kanji |> Option.defaultValue primaryEntryLabel.Reading

    let front = [ Html.h1 [ prop.className "expression"; prop.text frontText ] ]

    let back = [
        yield Html.h1 [ prop.className "expression"; prop.text (primaryEntryLabel.ToString()) ]

        yield Html.hr []

        let senses =
            word.Senses
            |> List.filter (fun sense -> sense.Glosses |> List.exists (fun gloss -> gloss.LanguageCode = "eng"))

        if senses.Length > 0 then
            yield Html.h2 [ prop.text "Meanings" ]

            yield
                Html.ol [
                    prop.children [
                        for i in 0 .. senses.Length - 1 do
                            let sense = senses[i]
                            let partsOfSpeech = sense.PartsOfSpeech |> String.concat ", "

                            let glosses = sense.Glosses |> List.map _.Value |> String.concat "; "

                            let kanjiRestrictions =
                                sense.KanjiRestrictions
                                |> List.map (sprintf "Only applies to %s")
                                |> String.concat ", "

                            let readingRestrictions =
                                sense.ReadingRestrictions
                                |> List.map (sprintf "Only applies to %s")
                                |> String.concat ", "

                            let crossReferences =
                                sense.CrossReferences |> List.map _.ToString() |> String.concat ", "

                            let antonyms = sense.Antonyms |> List.map _.ToString() |> String.concat ", "
                            let fields = sense.Fields |> String.concat ", "
                            let miscellaneousInformation = sense.MiscellaneousInformation |> String.concat ", "
                            let additionalInformation = sense.AdditionalInformation |> String.concat ", "
                            let dialects = sense.Dialects |> String.concat ", "

                            let languageSources =
                                sense.LanguageSources |> List.map _.ToString() |> String.concat ", "

                            let details =
                                [
                                    if fields.Length > 0 then
                                        $"%s{fields}"
                                    if antonyms.Length > 0 then
                                        $"Antonyms: %s{antonyms}"
                                    if miscellaneousInformation.Length > 0 then
                                        miscellaneousInformation
                                    if dialects.Length > 0 then
                                        $"%s{dialects}"
                                    if languageSources.Length > 0 then
                                        languageSources
                                    if crossReferences.Length > 0 then
                                        crossReferences
                                    if additionalInformation.Length > 0 then
                                        additionalInformation
                                    if kanjiRestrictions.Length > 0 then
                                        kanjiRestrictions
                                    if readingRestrictions.Length > 0 then
                                        readingRestrictions
                                ]
                                |> String.concat ", "

                            Html.p [ prop.className "parts-of-speech"; prop.text $"{partsOfSpeech}" ]

                            Html.li [
                                prop.children [
                                    Html.span [ prop.className "glosses"; prop.text glosses ]
                                    if details.Length > 0 then
                                        Html.span [ prop.className "details"; prop.text details ]
                                ]
                            ]
                    ]
                ]

        let otherForms = (alternateForms |> List.map _.ToString()) |> String.concat ", "

        if otherForms.Length > 0 then
            yield Html.h2 [ prop.text "Other Forms" ]
            yield Html.p [ prop.text otherForms ]

        let kanjiNotes =
            word.KanjiElements
            |> List.map (fun ke -> {
                ke with
                    Information = ke.Information |> List.filter (fun i -> i <> "search-only kanji form")
            })
            |> List.filter (fun ke -> ke.Information.Length > 0)
            |> List.map (fun ke ->
                let info = ke.Information |> String.concat ", "
                $"{ke.Value}: {info}")

        let readingNotes =
            word.ReadingElements
            |> List.map (fun re -> {
                re with
                    Information = re.Information |> List.filter (fun i -> i <> "search-only kana form")
            })
            |> List.filter (fun re -> re.Information.Length > 0)
            |> List.map (fun re ->
                let info = re.Information |> String.concat ", "
                $"{re.Value}: {info}")

        let notes = List.append kanjiNotes readingNotes

        if notes.Length > 0 then
            yield Html.h2 [ prop.text "Notes" ]

            yield
                Html.ul [
                    prop.children [
                        for note in notes do
                            Html.li [ prop.text note ]
                    ]
                ]
    ]

    front, back

let getDbConnection () =
    let dbPath =
        match Environment.GetEnvironmentVariable("KENSAKU_DB_PATH") with
        | null
        | "" ->
            if OperatingSystem.IsWindows() then
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "kensaku", "kensaku.db")
            else if OperatingSystem.IsLinux() then
                "/usr/share/kensaku/kensaku.db"
            else
                failwith "Unsupported operating system"
        | path -> path

    let ctx = new SqliteConnection($"Data Source=%s{dbPath}")
    Schema.registerTypeHandlers ()
    Schema.registerRegexpFunction ctx
    ctx

let getPreferredEntries (word: string) (entries: GetWordQueryResult list) =
    let nonNameEntries = entries |> List.where (fun e -> e.Senses.Length > 0)

    nonNameEntries
    |> List.where (fun e ->
        let primaryEntryLabel, _ = getPrimaryAndAlternateForms e
        primaryEntryLabel.Kanji = Some word)
    |> List.tryNotEmpty
    |> Option.defaultValue nonNameEntries

let processFile (fileName: string) =
    let words = File.ReadAllLines(fileName)

    let ctx = getDbConnection ()

    let notes =
        words
        |> Seq.choose (fun word ->
            getWordLiterals word ctx
            |> getPreferredEntries word
            |> List.tryNotEmpty
            |> Option.map (fun w -> word, w))
        |> Seq.collect (fun (word, results) ->
            results
            |> List.map (fun result ->
                let needsFurigana = results.Length > 1
                let front, back = printWord result needsFurigana

                let expression =
                    if needsFurigana then
                        (getPrimaryForm result).ToString()
                    else
                        word

                {
                    Word = expression
                    Front = Render.htmlView front
                    Back = Render.htmlView back
                }))

    let config = CsvConfiguration(CultureInfo.InvariantCulture)
    config.ShouldQuote <- fun _ -> true
    let csvWriter = new CsvWriter(Console.Out, config)
    csvWriter.WriteRecords(notes)

if Environment.GetCommandLineArgs().Length = 3 then
    processFile (Environment.GetCommandLineArgs()[2])
else
    printfn $"Usage: dotnet fsi %s{Environment.GetCommandLineArgs()[1]} <file>"
