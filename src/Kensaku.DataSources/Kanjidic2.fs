namespace Kensaku.DataSources

open System
open System.Text

type Kanjidic2Info = {
    FileVersion: int
    DatabaseVersion: string
    DateOfCreation: DateTime
}

type CodePoint = {
    Value: string
    Type: string
}

type KeyRadical = {
    Value: int
    Type: string
}

type CharacterVariant = {
    Value: string
    Type: string
}

type DictionaryReference = {
    IndexNumber: string
    Type: string
    Volume: int option
    Page: int option
}

type QueryCode = {
    Value: string
    Type: string
    SkipMisclassification: string option
}

type CharacterReading = {
    Value: string
    Type: string
}

type CharacterMeaning = {
    Value: string
    LanguageCode: string
}

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

[<RequireQualifiedAccess>]
module Kanjidic2 =
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

    let getInfo (path: string) =
        streamXmlElements "header" path |> Seq.head |> parseHeader

    let getEntries (path: string) =
        streamXmlElements "character" path
        |> Seq.map (fun entry -> {
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
