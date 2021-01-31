module DataParsing

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Linq

type RadkEntry = {
    Radical: char
    StrokeCount: int
    Kanji: Set<char>
}

let parseRadkFile (path: string) =
    let text = File.ReadAllText(path, Encoding.GetEncoding("EUC-JP"))
    Regex.Matches(text, @"^\$ (.) (\d).*$([^$]+)", RegexOptions.Multiline)
    |> Seq.toList
    |> List.map (fun m ->
        {
            Radical = char m.Groups.[1].Value
            StrokeCount= int m.Groups.[2].Value
            // Remove newlines and katakana middle dots
            Kanji = set m.Groups.[3].Value - set ['\n'; '\u30FB']
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
            failwithf "Expected exactly one entry for %c in each radk file." radical
    )

let streamXmlElements (elementName: string) (path: string) =
    let settings = XmlReaderSettings(DtdProcessing = DtdProcessing.Parse)
    let reader = XmlReader.Create(path, settings)
    reader.MoveToContent() |> ignore
    seq {
        try
            while (reader.Read()) do
                match reader.NodeType, reader.Name with
                | XmlNodeType.Element, elementName ->
                    yield XElement.ReadFrom(reader) :?> XElement
                | _ -> ()
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
    Sense: int option
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
        Information = parseElementList "ke_inf" (fun p -> p.Value) el
        Priority = parseElementList "re_pri" (fun p -> p.Value) el
    }

type ReferenceComponent =
    | Kanji of string
    | Reading of string
    | Sense of int

let tryParseReferenceComponent (text: string) =
    if Seq.forall isKana text then
        Some (Reading text)
    else
        match Int32.TryParse(text) with
        | true, i -> Some (Sense i)
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
    let k, r, s =
        match a, b, c with
        // Regular 3 component case
        | Some (Kanji k), Some (Reading r), Some (Sense s) -> Some k, Some r, Some s
        // Regular 2 component cases
        | Some (Kanji k), Some (Reading r), None -> Some k, Some r, None
        | Some (Kanji k), Some (Sense s), None -> Some k, None, Some s
        // It isn't obvious from the description in the JMdict DTD, but a
        // reading and sense can occur without out a kanji component.
        | Some (Reading r), Some (Sense s), None -> None, Some r, Some s
        // These two cases are weird. The katakana middle dot only acts as a
        // separator when there is more than one reference component. This means
        // that a single kanji or reading component containing a literal
        // katakana middle dot constitutes a valid cross-reference. Because we
        // already split the entry above, we check for this here and assign the
        // whole reference to the appropriate component if necessary.
        | Some (Reading _), Some (Reading _), None -> None, Some el.Value, None
        | Some (Reading _), Some (Kanji _), None -> Some el.Value, None, None
        // Regular one component cases
        | Some (Kanji k), None, None -> Some k, None, None
        | Some (Reading r), None, None -> None, Some r, None
        | _ -> failwithf "%s is not a valid cross reference." el.Value
    {
        Kanji = k
        Reading = r
        Sense = s
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
        IsWasei = el.Attribute("wasei") <> null
    }

let parseGloss (el: XElement) =
    let glossType =
        match el.Element("g_type") with
        | null -> None
        | t -> Some t.Value
    {
        Value = el.Value
        LanguageCode = parseLanguageCode el
        Type = glossType
    }

let parseSense (el: XElement) =
    {
        KanjiRestrictions = parseElementList "stagk" (fun p -> p.Value) el
        ReadingRestrictions = parseElementList "stagr" (fun p -> p.Value) el
        PartsOfSpeech = parseElementList "pos" (fun p -> p.Value) el
        CrossReferences = parseElementList "xref" parseCrossReference el
        Antonyms = parseElementList "re_pri" parseAntonym el
        Fields = parseElementList "field" (fun p -> p.Value) el
        MiscellaneousInformation = parseElementList "misc" (fun p -> p.Value) el
        AdditionalInformation = parseElementList "s_inf" (fun p -> p.Value) el
        LanguageSources = parseElementList "re_pri" parseLanguageSource el
        Dialects = parseElementList "dial" (fun p -> p.Value) el
        Glosses = parseElementList "gloss" parseGloss el
    }

let getJmdictEntries () =
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
