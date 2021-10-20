module DataParsing

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Linq

type RadkEntry = {
    Radical: Rune
    StrokeCount: int
    Kanji: Set<Rune>
}
let parseRadkFile (path: string) =
    let text = File.ReadAllText(path, Encoding.GetEncoding("EUC-JP"))
    Regex.Matches(text, @"^\$ (.) (\d+).*$([^$]+)", RegexOptions.Multiline)
    |> Seq.toList
    |> List.map (fun m ->
        {
            Radical = rune m.Groups.[1].Value
            StrokeCount= int m. Groups.[2].Value
            // Remove newlines and katakana middle dots
            Kanji = set (m.Groups.[3].Value.EnumerateRunes()) - set [rune '\n'; rune '\u30FB']
        }
    )

let getRadkEntries () =
    [
        "data/radkfile"
        "data/radkfile2"
    ] |> List.collect parseRadkFile
    |> List.groupBy (fun x -> x.Radical)
    |> List.map (fun (radical, pair) ->
        match pair with
        | [ a; b ] ->
            { a with Kanji = a.Kanji + b.Kanji }
        | _ ->
            failwithf "Expected exactly one entry for %A in each radk file." radical
    )

let streamXmlElements (elementName: string) (path: string) =
    // Parse the DTD and expand all entities
    let settings = XmlReaderSettings(DtdProcessing = DtdProcessing.Parse, MaxCharactersFromEntities = 0L)
    let reader = XmlReader.Create(path, settings)
    reader.MoveToContent() |> ignore
    seq {
        try
            while (reader.Read()) do
                if reader.NodeType = XmlNodeType.Element && reader.Name = elementName then
                    yield XElement.ReadFrom(reader) :?> XElement
        finally
            reader.Dispose()
    } 

type KanjiElement = {
    Value: string
    Information: string list
    Priority: string list
}

type ReadingElement = {
    Value: string
    IsTrueReading: bool
    Restrictions: string list
    Information: string list
    Priority: string list
}

type CrossReference = {
    Kanji: string option
    Reading: string option
    Index: int option
}

type Antonym = {
    Kanji: string option
    Reading: string option
}

type LanguageSource = {
    Value: string
    Code: string
    IsPartial: bool
    IsWasei: bool
}

type Gloss = {
    Value: string
    LanguageCode: string
    Type: string option
}

type Sense = {
    KanjiRestrictions: string list
    ReadingRestrictions: string list
    PartsOfSpeech: string list
    CrossReferences: CrossReference list
    Antonyms: Antonym list
    Fields: string list
    MiscellaneousInformation: string list
    AdditionalInformation: string list
    LanguageSources: LanguageSource list
    Dialects: string list
    Glosses: Gloss list
}

type JMdictEntry = {
    Id: int
    // Where did this come from?
    IsProperName: bool
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Senses: Sense list
}

let parseElementList (elementName: string) (f: XElement -> 'a) (el: XElement) =
    el.Elements(elementName)
    |> Seq.map f
    |> Seq.toList

let parseKanjiElement (el: XElement) =
    {
        Value = el.Element("keb").Value
        Information = parseElementList "ke_inf" (fun p -> p.Value) el
        Priority = parseElementList "ke_pri" (fun p -> p.Value) el
    }

let parseReadingElement (el: XElement) =
    {
        Value = el.Element("reb").Value
        IsTrueReading = isNull (el.Element("re_nokanji"))
        Restrictions = parseElementList "re_restr" (fun p -> p.Value) el
        Information = parseElementList "re_inf" (fun p -> p.Value) el
        Priority = parseElementList "re_pri" (fun p -> p.Value) el
    }

type ReferenceComponent =
    | Kanji of string
    | Reading of string
    | Index of int

let tryParseReferenceComponent (text: string) =
    if Seq.forall isKana text then
        Some (Reading text)
    else
        match Int32.TryParse(text) with
        | true, i -> Some (Index i)
        | false, _ ->
            if Seq.exists (not << isKana) text then
                Some (Kanji text)
            else
                None

let parseCrossReference (el: XElement) =
    // Split on katakana middle dot (・)
    let parts = el.Value.Split('\u30FB')
    // A cross-reference consists of a kanji, reading, and sense component
    // appearing in that order. Any of the parts may be omitted, so the type of
    // each position varies.
    let a = parts |> Array.tryItem 0 |> Option.collect tryParseReferenceComponent
    let b = parts |> Array.tryItem 1 |> Option.collect tryParseReferenceComponent
    let c = parts |> Array.tryItem 2 |> Option.collect tryParseReferenceComponent
    let k, r, i =
        match a, b, c with
        // Regular 3 component case
        | Some (Kanji k), Some (Reading r), Some (Index i) -> Some k, Some r, Some i
        // Regular 2 component cases
        | Some (Kanji k), Some (Reading r), None -> Some k, Some r, None
        | Some (Kanji k), Some (Index i), None -> Some k, None, Some i
        // It isn't obvious from the description in the JMdict DTD, but a
        // reading and sense can occur without out a kanji component.
        | Some (Reading r), Some (Index i), None -> None, Some r, Some i
        // These three cases are weird. The katakana middle dot only acts as a
        // separator when there is more than one reference component. This means
        // that a single kanji or reading component containing a literal
        // katakana middle dot constitutes a valid cross-reference. Because we
        // already split the entry above, we check for this here and assign the
        // whole reference to the appropriate component if necessary.
        | Some (Reading _), Some (Reading _), None -> None, Some el.Value, None
        | Some (Kanji _), Some (Kanji _), None -> Some el.Value, None, None
        | Some (Reading _), Some (Kanji _), None -> Some el.Value, None, None
        // Regular one component cases
        | Some (Kanji k), None, None -> Some k, None, None
        | Some (Reading r), None, None -> None, Some r, None
        | _ -> failwithf "%s is not a valid cross reference." el.Value
    {
        Kanji = k
        Reading = r
        Index = i
    }

let parseAntonym (el: XElement) =
    let k, r =
        match tryParseReferenceComponent el.Value with
        | Some (Kanji k) -> Some k, None
        | Some (Reading r) -> None, Some r
        | _ -> failwithf "%s is not a valid antonym reference." el.Value
    {
        Kanji = k
        Reading = r
    }

let parseLanguageCode (el: XElement) =
    match el.Attribute(XName.Get("lang", XNamespace.Xml.NamespaceName)) with
    | null -> "eng"
    | l -> l.Value

let parseLanguageSource (el: XElement) =
    {
        Value = el.Value
        Code = parseLanguageCode el
        IsPartial = el.Attribute("ls_type") <> null
        IsWasei = el.Attribute("ls_wasei") <> null
    }

let parseGloss (el: XElement) =
    {
        Value = el.Value
        LanguageCode = parseLanguageCode el
        Type = el.Attribute("g_type").TryGetValue()
    }

let parseSense (el: XElement) =
    {
        KanjiRestrictions = parseElementList "stagk" (fun p -> p.Value) el
        ReadingRestrictions = parseElementList "stagr" (fun p -> p.Value) el
        PartsOfSpeech = parseElementList "pos" (fun p -> p.Value) el
        CrossReferences = parseElementList "xref" parseCrossReference el
        Antonyms = parseElementList "ant" parseAntonym el
        Fields = parseElementList "field" (fun p -> p.Value) el
        MiscellaneousInformation = parseElementList "misc" (fun p -> p.Value) el
        AdditionalInformation = parseElementList "s_inf" (fun p -> p.Value) el
        LanguageSources = parseElementList "lsource" parseLanguageSource el
        Dialects = parseElementList "dial" (fun p -> p.Value) el
        Glosses = parseElementList "gloss" parseGloss el
    }

let getJMdictEntries () =
    streamXmlElements "entry" "data/JMdict.xml"
    |> Seq.map (fun entry ->
        {
            Id = entry.Element("ent_seq").Value |> int
            IsProperName = false
            KanjiElements = parseElementList "k_ele" parseKanjiElement entry
            ReadingElements = parseElementList "r_ele" parseReadingElement entry
            Senses = parseElementList "sense" parseSense entry
        }
    )

type TranslationContents = {
    Value: string
    LanguageCode: string
}

type Translation = {
    NameTypes: string list
    CrossReferences: CrossReference list
    Contents: TranslationContents list
}

type JMnedictEntry = {
    Id: int
    // Where did this come from?
    IsProperName: bool
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Translations: Translation list
}

let parseTranslationContents (el: XElement) =
    {
        Value = el.Value
        LanguageCode = parseLanguageCode el
    }

let parseTranslation (el: XElement) =
    {
        NameTypes = parseElementList "name_type" (fun p -> p.Value) el
        CrossReferences = parseElementList "xref" parseCrossReference el
        Contents = parseElementList "trans_det" parseTranslationContents el
    }

let getJMnedictEntries () =
    streamXmlElements "entry" "data/JMnedict.xml"
    |> Seq.map (fun entry ->
        {
            Id = entry.Element("ent_seq").Value |> int
            IsProperName = false
            KanjiElements = parseElementList "k_ele" parseKanjiElement entry
            ReadingElements = parseElementList "r_ele" parseReadingElement entry
            Translations = parseElementList "trans" parseTranslation entry
        }
    )

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
    Value: string
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

let parseHeader (el: XElement) =
    {
        FileVersion = int (el.Element("file_version").Value)
        DatabaseVersion = el.Element("database_version").Value
        DateOfCreation = DateTime.Parse(el.Element("date_of_creation").Value, DateTimeFormatInfo.InvariantInfo)
    }

let parseCodePoint (el: XElement): CodePoint =
    {
        Value = el.Value
        Type = el.Attribute("cp_type").Value
    }

let parseKeyRadical (el: XElement): KeyRadical =
    {
        Value = el.Value
        Type = el.Attribute("rad_type").Value
    }

let parseGrade (el: XElement) =
    match el.Element("misc").Element("grade") with
    | null -> None
    | g -> Some (int g.Value)

let parseStrokeMiscounts (el: XElement) =
    el.Element("misc").Elements("stroke_count")
    |> Seq.skip 1
    |> Seq.map (fun p -> int p.Value)
    |> Seq.toList

let parseVariant (el: XElement): CharacterVariant =
    {
        Value = el.Value
        Type = el.Attribute("var_type").Value
    }

let parseFrequency (el: XElement) =
    match el.Element("misc").Element("freq") with
    | null -> None
    | f -> Some (int f.Value)

let parseOldJlptLevel (el: XElement) =
    match el.Element("misc").Element("jlpt") with
    | null -> None
    | l -> Some (int l.Value)

let parseDictionaryReferences (el: XElement) =
    match el.Element("dic_number") with
    | null -> []
    | d ->
        parseElementList "dic_ref" (fun el ->
            {
                IndexNumber = el.Value
                Type = el.Attribute("dr_type").Value
                Volume = el.Attribute("m_vol").TryGetValue() |> Option.map int
                Page = el.Attribute("m_page").TryGetValue() |> Option.map int
            }
        ) d

let parseQueryCodes (el: XElement) =
    match el.Element("query_code") with
    | null -> []
    | q ->
        parseElementList "q_code" (fun el ->
            {
                Value = el.Value
                Type = el.Attribute("qc_type").Value
                SkipMisclassification = el.Attribute("skip_misclass").TryGetValue()
            }
        ) q

let parseCharacterReadings (el: XElement) =
    match el.Element("reading_meaning") with
    | null -> []
    | rm ->
        parseElementList "reading" (fun el ->
            {
                Value = el.Value
                Type = el.Attribute("r_type").Value
            }
        ) (rm.Element("rmgroup"))

let parseCharacterMeanings (el: XElement) =
    match el.Element("reading_meaning") with
    | null -> []
    | rm ->
        parseElementList "meaning" (fun el ->
            {
                Value = el.Value
                LanguageCode = parseLanguageCode el
            }
        ) (rm.Element("rmgroup"))

let parseNanori (el: XElement) =
    match el.Element("reading_meaning") with
    | null -> []
    | rm ->
        parseElementList "nanori" (fun n -> n.Value) rm

let getKanjidic2Info () =
    streamXmlElements "header" "data/kanjidic2.xml"
    |> Seq.head
    |> parseHeader

let getKanjidic2Entries () =
    streamXmlElements "character" "data/kanjidic2.xml"
    |> Seq.map (fun entry ->
        {
            Value = Rune.GetRuneAt(entry.Element("literal").Value, 0)
            CodePoints = parseElementList "cp_value" parseCodePoint (entry.Element("codepoint"))
            KeyRadicals = parseElementList "rad_value" parseKeyRadical (entry.Element("radical"))
            Grade = parseGrade entry
            StrokeCount = int (entry.Element("misc").Element("stroke_count").Value)
            StrokeMiscounts = parseStrokeMiscounts entry
            Variants = entry.Element("misc") |> parseElementList "variant" parseVariant
            Frequency = parseFrequency entry
            IsRadical = entry.Element("misc").Element("rad_name") <> null
            RadicalNames = entry.Element("misc") |> parseElementList "rad_name" (fun p -> p.Value)
            OldJlptLevel = parseOldJlptLevel entry
            DictionaryReferences = parseDictionaryReferences entry
            QueryCodes = parseQueryCodes entry
            Readings = parseCharacterReadings entry
            Meanings = parseCharacterMeanings entry
            Nanori = parseNanori entry
        }
    )
