namespace Kensaku.DataSources

open FSharp.Control
open System
open System.IO
open System.Text
open System.Threading

/// <summary>
/// Represents header metadata from the Kanjidic2 file.
/// </summary>
type Kanjidic2Info = {
    FileVersion: int
    DatabaseVersion: string
    DateOfCreation: DateTime
}

/// <summary>
/// Represents an encoded form (cp_value) of the character.
/// </summary>
type CodePoint = {
    Value: string
    Type: string
}

/// <summary>
/// Represents a radical (rad_value) for the character including numbering system.
/// </summary>
type KeyRadical = {
    Value: int
    Type: string
}

/// <summary>
/// Represents a variant form (variant) of the character.
/// </summary>
type CharacterVariant = {
    Value: string
    Type: string
}

/// <summary>
/// Represents a reference (dic_ref) to another dictionary source.
/// </summary>
type DictionaryReference = {
    IndexNumber: string
    Type: string
    Volume: int option
    Page: int option
}

/// <summary>
/// Represents a query code (q_code) used for searching (e.g., SKIP, Four Corner).
/// </summary>
type QueryCode = {
    Value: string
    Type: string
    SkipMisclassification: string option
}

/// <summary>
/// Represents a reading (reading) for the character (on, kun, pinyin, etc.).
/// </summary>
type CharacterReading = {
    Value: string
    Type: string
}

/// <summary>
/// Represents a meaning (meaning) in a given language (defaults to English).
/// </summary>
type CharacterMeaning = {
    Value: string
    LanguageCode: string
}

/// <summary>
/// Represents a single kanji character entry from Kanjidic2 including code points,
/// radicals, dictionary references, readings, and meanings.
/// </summary>
type Character = {
    Value: Rune
    CodePoints: CodePoint list
    KeyRadicals: KeyRadical list
    Grade: int option
    StrokeCount: int
    Variants: CharacterVariant list
    StrokeMiscounts: int list
    Frequency: int option
    IsRadical: bool
    RadicalNames: string list
    OldJlptLevel: int option
    DictionaryReferences: DictionaryReference list
    QueryCodes: QueryCode list
    Readings: CharacterReading list
    Meanings: CharacterMeaning list
    Nanori: string list
}

module private Kanjidic2 =
    open System.Globalization
    open System.Xml.Linq

    let private parseHeader (el: XElement) = {
        FileVersion = int (el.Element("file_version").Value)
        DatabaseVersion = el.Element("database_version").Value
        DateOfCreation = DateTime.Parse(el.Element("date_of_creation").Value, DateTimeFormatInfo.InvariantInfo)
    }

    let private parseCodePoint (el: XElement) : CodePoint = {
        Value = el.Value
        Type = el.Attribute("cp_type").Value
    }

    let private parseKeyRadical (el: XElement) : KeyRadical = {
        Value = int el.Value
        Type = el.Attribute("rad_type").Value
    }

    let private parseGrade (el: XElement) =
        match el.Element("misc").Element("grade") with
        | null -> None
        | g -> Some(int g.Value)

    let private parseStrokeMiscounts (el: XElement) =
        el.Element("misc").Elements("stroke_count")
        |> Seq.skip 1
        |> Seq.map (fun p -> int p.Value)
        |> Seq.toList

    let private parseVariant (el: XElement) : CharacterVariant = {
        Value = el.Value
        Type = el.Attribute("var_type").Value
    }

    let private parseFrequency (el: XElement) =
        match el.Element("misc").Element("freq") with
        | null -> None
        | f -> Some(int f.Value)

    let private parseOldJlptLevel (el: XElement) =
        match el.Element("misc").Element("jlpt") with
        | null -> None
        | l -> Some(int l.Value)

    let private parseDictionaryReferences (el: XElement) =
        match el.Element("dic_number") with
        | null -> []
        | d ->
            parseElementList
                "dic_ref"
                (fun el -> {
                    IndexNumber = el.Value
                    Type = el.Attribute("dr_type").Value
                    Volume = el.Attribute("m_vol").TryGetValue() |> Option.map int
                    Page = el.Attribute("m_page").TryGetValue() |> Option.map int
                })
                d

    let private parseQueryCodes (el: XElement) =
        match el.Element("query_code") with
        | null -> []
        | q ->
            parseElementList
                "q_code"
                (fun el -> {
                    Value = el.Value
                    Type = el.Attribute("qc_type").Value
                    SkipMisclassification = el.Attribute("skip_misclass").TryGetValue()
                })
                q

    let private parseCharacterReadings (el: XElement) =
        match el.Element("reading_meaning") with
        | null -> []
        | rm ->
            parseElementList
                "reading"
                (fun el -> {
                    Value = el.Value
                    Type = el.Attribute("r_type").Value
                })
                (rm.Element("rmgroup"))

    let private parseCharacterMeanings (el: XElement) =
        match el.Element("reading_meaning") with
        | null -> []
        | rm ->
            parseElementList
                "meaning"
                (fun el -> {
                    Value = el.Value
                    LanguageCode =
                        match el.Attribute("m_lang") with
                        | null -> "en"
                        | l -> l.Value
                })
                (rm.Element("rmgroup"))

    let private parseNanori (el: XElement) =
        match el.Element("reading_meaning") with
        | null -> []
        | rm -> parseElementList "nanori" (_.Value) rm

    let parseInfoAsync (stream: Stream) (closeStream: bool) (ct: CancellationToken) =
        task {
            let! el = streamXmlElementsAsync "header" stream closeStream ct |> TaskSeq.head
            return parseHeader el
        }

    let parseEntriesAsync (stream: Stream) (closeStream: bool) (ct: CancellationToken) =
        streamXmlElementsAsync "character" stream closeStream ct
        |> TaskSeq.map (fun entry -> {
            Value = rune (entry.Element("literal").Value)
            CodePoints = parseElementList "cp_value" parseCodePoint (entry.Element("codepoint"))
            KeyRadicals = parseElementList "rad_value" parseKeyRadical (entry.Element("radical"))
            Grade = parseGrade entry
            StrokeCount = int (entry.Element("misc").Element("stroke_count").Value)
            StrokeMiscounts = parseStrokeMiscounts entry
            Variants = entry.Element("misc") |> parseElementList "variant" parseVariant
            Frequency = parseFrequency entry
            IsRadical = entry.Element("misc").Element("rad_name") <> null
            RadicalNames = entry.Element("misc") |> parseElementList "rad_name" (_.Value)
            OldJlptLevel = parseOldJlptLevel entry
            DictionaryReferences = parseDictionaryReferences entry
            QueryCodes = parseQueryCodes entry
            Readings = parseCharacterReadings entry
            Meanings = parseCharacterMeanings entry
            Nanori = parseNanori entry
        })

/// <summary>
/// Provides methods to parse Kanjidic2 information and entries from a stream or file path.
/// </summary>
[<AbstractClass; Sealed>]
type Kanjidic2 =

    /// <summary>
    /// Parses the Kanjidic2 header information from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the Kanjidic2 data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces the parsed <see cref="Kanjidic2Info"/>.</returns>
    static member ParseInfoAsync(stream: Stream, ?ct: CancellationToken) =
        let ct = defaultArg ct CancellationToken.None
        Kanjidic2.parseInfoAsync stream false ct

    /// <summary>
    /// Parses the Kanjidic2 header information from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path containing the Kanjidic2 data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces the parsed <see cref="Kanjidic2Info"/>.</returns>
    static member ParseInfoAsync(path: string, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        let ct = defaultArg ct CancellationToken.None
        Kanjidic2.parseInfoAsync stream true ct

    /// <summary>
    /// Parses the Kanjidic2 entries from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the Kanjidic2 data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>An asynchronous sequence of <see cref="Character"/> values.</returns>
    static member ParseEntriesAsync(stream: Stream, ?ct: CancellationToken) =
        let ct = defaultArg ct CancellationToken.None
        Kanjidic2.parseEntriesAsync stream false ct

    /// <summary>
    /// Parses the Kanjidic2 entries from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path containing the Kanjidic2 data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>An asynchronous sequence of <see cref="Character"/> values.</returns>
    static member ParseEntriesAsync(path: string, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        let ct = defaultArg ct CancellationToken.None
        Kanjidic2.parseEntriesAsync stream true ct
