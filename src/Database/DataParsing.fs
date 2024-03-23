module Kensaku.DataParsing

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Linq
open Kensaku.Domain

let hexStringToInt (hexString: string) =
    Int32.Parse(hexString, NumberStyles.HexNumber)

let hexStringToRune (hexString: string) =
    Rune(Int32.Parse(hexString, NumberStyles.HexNumber))

let getEquivalentCharacters (path: string) =
    let groups = ResizeArray<Set<Rune>>()

    for line in File.ReadAllLines(path) do
        let m =
            Regex.Match(
                line,
                @"^(?<in1>\w{4})(?:\.\.(?<in2>\w{4}))? +; +(?<out>\w{4,6}) +# +(?:\[(?<n>\d)\])? +(?<description>.+)"
            )

        if m.Success then
            let in1 = hexStringToInt (m.Groups["in1"].Value)
            let out = hexStringToInt (m.Groups["out"].Value)
            let description = m.Groups["description"].Value
            printfn "%A" description

            let newGroup =
                match m.Groups["in2"].Value, m.Groups["n"].Value with
                | "", "" -> [ in1; out ]
                | _, "" -> failwith "in2 was present but n was not"
                | "", _ -> failwith "n was present but in2 was not"
                | in2, _ -> out :: [ in1 .. (hexStringToInt in2) ]
                |> List.map Rune
                |> Set.ofList

            match Seq.tryFindIndex (fun g -> (Set.intersect newGroup g).Count > 0) groups with
            | Some i -> groups[i] <- groups[i] + newGroup
            | None -> groups.Add(newGroup)

    groups
    |> Seq.collect (fun group -> group |> Seq.map (fun c -> c, group))
    |> Map.ofSeq

let getIdeographicVariants (variantGroups: Map<Rune, Set<Rune>>) (character: Rune) =
    variantGroups
    |> Map.tryFind character
    |> Option.defaultValue (Set.singleton character)

let getCJKRadicals (path: string) =
    let radicals = Dictionary<int, CJKRadical>()

    for line in File.ReadAllLines(path) do
        let m = Regex.Match(line, @"(\d+'?); (.{4}); (.{4})")

        if m.Success then
            let n = int (m.Groups[1].Value.Replace("'", ""))

            let radicalValue = {
                RadicalCharacter = hexStringToRune (m.Groups[2].Value)
                UnifiedIdeographCharacter = hexStringToRune (m.Groups[3].Value)
            }

            if m.Groups[1].Value.EndsWith("'") then
                radicals[n] <- { radicals[n] with Simplified = Some radicalValue }
            else
                radicals[n] <- {
                    RadicalNumber = n
                    Standard = radicalValue
                    Simplified = None
                }

    radicals |> Seq.map (_.Value) |> Seq.toList

let replace (pattern: string) (replacement: string) (input: string) =
    Regex.Replace(input, pattern, replacement)

let getDerivedRadicalNames (path: string) =
    File.ReadAllLines(path)
    |> Array.choose (fun line ->
        let m = Regex.Match(line, @"^(\w{4,6}) +; (?:CJK|KANGXI) RADICAL (.+)")

        if m.Success then
            let radical = hexStringToRune (m.Groups[1].Value)

            let name =
                m.Groups[2].Value
                |> replace "^(C-|J-)?SIMPLIFIED " ""
                |> replace "((?<!(SMALL|LAME)) (ONE|TWO|THREE|FOUR))$" ""

            Some(radical, name.ToLowerInvariant())
        else
            None)
    |> Map.ofArray

let replacements =
    Map [
        '化', '\u2E85'
        '个', '\u2F09'
        '并', '\u4E37'
        '刈', '\u2E89'
        '込', '\u2ECC'
        '尚', '\u2E8C'
        '忙', '\u2E96'
        '扎', '\u2E97'
        '汁', '\u2EA1'
        '犯', '\u2EA8'
        '艾', '\u2EBE'
        '邦', '\u2ECF'
        '阡', '\u2ED9'
        '老', '\u2EB9'
        '杰', '\u2EA3'
        '礼', '\u2EAD'
        '疔', '\u2F67'
        '禹', '\u2F71'
        '初', '\u2EC2'
        '買', '\u2EB2'
        '滴', '\u5547'
    ]

let charToRadicalNumber =
    Map [
        rune "｜", 2 // 00FF5C | Halfwidth and Fullwidth Forms | FULLWIDTH VERTICAL LINE
        rune "ノ", 0 // 4? 0030CE | Katakana                      | KATAKANA LETTER NO
        rune "⺅", 9 // 002E85 | CJK Radicals Supplement       | CJK RADICAL PERSON
        rune "ハ", 12 // 0030CF | Katakana                      | KATAKANA LETTER HA
        rune "丷", 12 // 004E37 | CJK Unified Ideographs        | No character name available
        rune "⺉", 18 // 002E89 | CJK Radicals Supplement       | CJK RADICAL KNIFE TWO
        rune "マ", 0 // 0030DE | Katakana                      | KATAKANA LETTER MA
        rune "九", 19 // 004E5D | CJK Unified Ideographs        | CJK character Nelson  146
        rune "ユ", 0 // 0030E6 | Katakana                      | KATAKANA LETTER YU
        rune "乃", 0 // 004E43 | CJK Unified Ideographs        | CJK character Nelson  145
        rune "乞", 0 // 004E5E | CJK Unified Ideographs        | CJK character Nelson  262
        rune "⺌", 42 // 002E8C | CJK Radicals Supplement       | CJK RADICAL SMALL ONE
        rune "川", 47 // 005DDD | CJK Unified Ideographs        | CJK character Nelson 1447
        rune "已", 49 // 005DF2 | CJK Unified Ideographs        | CJK character Nelson 1461
        rune "ヨ", 58 // 0030E8 | Katakana                      | KATAKANA LETTER YO
        rune "彑", 58 // 005F51 | CJK Unified Ideographs        | No character name available
        rune "⺖", 61 // 002E96 | CJK Radicals Supplement       | CJK RADICAL HEART ONE
        rune "⺗", 61 // 002E97 | CJK Radicals Supplement       | CJK RADICAL HEART TWO
        rune "⺡", 0 // 42? 002EA1 | CJK Radicals Supplement       | CJK RADICAL WATER ONE
        rune "⺨", 94 // 002EA8 | CJK Radicals Supplement       | CJK RADICAL DOG
        rune "⺾", 140 // 002EBE | CJK Radicals Supplement       | CJK RADICAL GRASS ONE
        rune "⻏", 163 // 002ECF | CJK Radicals Supplement       | CJK RADICAL CITY
        rune "也", 0 // 004E5F | CJK Unified Ideographs        | CJK character Nelson   75
        rune "亡", 0 // 004EA1 | CJK Unified Ideographs        | CJK character Nelson  281
        rune "及", 0 // 0053CA | CJK Unified Ideographs        | CJK character Nelson  154, 157
        rune "久", 0 // 004E45 | CJK Unified Ideographs        | CJK character Nelson  153
        rune "⺹", 125 // 002EB9 | CJK Radicals Supplement       | CJK RADICAL OLD
        rune "戸", 63 // 006238 | CJK Unified Ideographs        | CJK character Nelson 1817
        rune "攵", 66 // 006535 | CJK Unified Ideographs        | No character name available
        rune "⺣", 86 // 002EA3 | CJK Radicals Supplement       | CJK RADICAL FIRE
        rune "⺭", 113 // 002EAD | CJK Radicals Supplement       | CJK RADICAL SPIRIT TWO
        rune "王", 96 // 00738B | CJK Unified Ideographs        | CJK character Nelson 2922
        rune "元", 0 // 005143 | CJK Unified Ideographs        | CJK character Nelson  275
        rune "井", 0 // 004E95 | CJK Unified Ideographs        | CJK character Nelson  165
        rune "勿", 0 // 0052FF | CJK Unified Ideographs        | CJK character Nelson  743
        rune "尤", 0 // 43? 005C24 | CJK Unified Ideographs        | CJK character Nelson  128
        rune "五", 0 // 004E94 | CJK Unified Ideographs        | CJK character Nelson   15
        rune "屯", 0 // 005C6F | CJK Unified Ideographs        | CJK character Nelson  264
        rune "巴", 0 // 005DF4 | CJK Unified Ideographs        | CJK character Nelson  263
        rune "⻂", 145 // 002EC2 | CJK Radicals Supplement       | CJK RADICAL CLOTHES
        rune "世", 0 // 004E16 | CJK Unified Ideographs        | CJK character Nelson   84,  95
        rune "巨", 0 // 005DE8 | CJK Unified Ideographs        | CJK character Nelson   19, 758
        rune "冊", 0 // 00518A | CJK Unified Ideographs        | CJK character Nelson   88
        rune "母", 80 // 006BCD | CJK Unified Ideographs        | CJK character Nelson 2466
        rune "⺲", 122 // 109? 002EB2 | CJK Radicals Supplement       | CJK RADICAL NET TWO
        rune "西", 146 // 00897F | CJK Unified Ideographs        | CJK character Nelson 4273
        rune "青", 174 // 009752 | CJK Unified Ideographs        | CJK character Nelson 5076
        rune "奄", 0 // 005944 | CJK Unified Ideographs        | CJK character Nelson 1173
        rune "岡", 0 // 005CA1 | CJK Unified Ideographs        | CJK character Nelson  621
        rune "免", 0 // 00514D | CJK Unified Ideographs        | CJK character Nelson  189, 573
        rune "斉", 210 // 006589 | CJK Unified Ideographs        | CJK character Nelson 5423
        rune "品", 0 // 0054C1 | CJK Unified Ideographs        | CJK character Nelson  889, 923
        rune "竜", 0 // 007ADC | CJK Unified Ideographs        | CJK character Nelson 3351,5440
        rune "亀", 0 // 004E80 | CJK Unified Ideographs        | CJK character Nelson 5445
        rune "啇", 0 // 005547 | CJK Unified Ideographs        | No character name available
        rune "黒", 0 // 203? 009ED2 | CJK Unified Ideographs        | CJK character Nelson 5403
        rune "無", 0 // 007121 | CJK Unified Ideographs        | CJK character Nelson 2773
        rune "歯", 0 // 006B6F | CJK Unified Ideographs        | CJK character Nelson 5428
    ]

let parseRadkFile (path: string) =
    let text = File.ReadAllText(path, Encoding.GetEncoding("EUC-JP"))

    Regex.Matches(text, @"^\$ (.) (\d+).*$([^$]+)", RegexOptions.Multiline)
    |> Seq.toList
    |> List.map (fun m ->
        let radical = char (m.Groups[1].Value)

        {
            Radical = radical |> replacements.TryFind |> Option.defaultValue radical |> rune
            StrokeCount = int m.Groups[2].Value
            // Remove newlines and katakana middle dots
            Kanji = set (m.Groups[3].Value.EnumerateRunes()) - set [ rune '\n'; rune '\u30FB' ]
        })

let getRadkEntries () =
    [ "data/radkfile"; "data/radkfile2" ]
    |> List.collect parseRadkFile
    |> List.groupBy _.Radical
    |> List.map (fun (radical, pair) ->
        match pair with
        | [ a; b ] -> { a with Kanji = a.Kanji + b.Kanji }
        | _ -> failwithf "Expected exactly one entry for %A in each radk file. Received %A." radical pair)

let streamXmlElements (elementName: string) (path: string) =
    // Parse the DTD and expand all entities
    let settings =
        XmlReaderSettings(DtdProcessing = DtdProcessing.Parse, MaxCharactersFromEntities = 0L)

    let reader = XmlReader.Create(path, settings)
    reader.MoveToContent() |> ignore

    seq {
        try
            while reader.Read() do
                if reader.NodeType = XmlNodeType.Element && reader.Name = elementName then
                    XElement.ReadFrom(reader) :?> XElement
        finally
            reader.Dispose()
    }

let parseElementList (elementName: string) (f: XElement -> 'a) (el: XElement) =
    el.Elements(elementName) |> Seq.map f |> Seq.toList

let parseKanjiElement (el: XElement) = {
    Value = el.Element("keb").Value
    Information = parseElementList "ke_inf" (_.Value) el
    Priority = parseElementList "ke_pri" (_.Value) el
}

let parseReadingElement (el: XElement) = {
    Value = el.Element("reb").Value
    IsTrueReading = isNull (el.Element("re_nokanji"))
    Restrictions = parseElementList "re_restr" (_.Value) el
    Information = parseElementList "re_inf" (_.Value) el
    Priority = parseElementList "re_pri" (_.Value) el
}

type ReferenceComponent =
    | Kanji of string
    | Reading of string
    | Index of int

let tryParseReferenceComponent (text: string) =
    if Seq.forall isKana text then
        Some(Reading text)
    else
        match Int32.TryParse(text) with
        | true, i -> Some(Index i)
        | false, _ ->
            if Seq.exists (not << isKana) text then
                Some(Kanji text)
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
        | Some(Kanji k), Some(Reading r), Some(Index i) -> Some k, Some r, Some i
        // Regular 2 component cases
        | Some(Kanji k), Some(Reading r), None -> Some k, Some r, None
        | Some(Kanji k), Some(Index i), None -> Some k, None, Some i
        // It isn't obvious from the description in the JMdict DTD, but a
        // reading and sense can occur without out a kanji component.
        | Some(Reading r), Some(Index i), None -> None, Some r, Some i
        // These three cases are weird. The katakana middle dot only acts as a
        // separator when there is more than one reference component. This means
        // that a single kanji or reading component containing a literal
        // katakana middle dot constitutes a valid cross-reference. Because we
        // already split the entry above, we check for this here and assign the
        // whole reference to the appropriate component if necessary.
        | Some(Reading _), Some(Reading _), None -> None, Some el.Value, None
        | Some(Kanji _), Some(Kanji _), None -> Some el.Value, None, None
        | Some(Reading _), Some(Kanji _), None -> Some el.Value, None, None
        // Regular one component cases
        | Some(Kanji k), None, None -> Some k, None, None
        | Some(Reading r), None, None -> None, Some r, None
        | _ -> failwithf "%s is not a valid cross reference." el.Value

    {
        Kanji = k
        Reading = r
        Index = i
    }

let parseAntonym (el: XElement) =
    let k, r =
        match tryParseReferenceComponent el.Value with
        | Some(Kanji k) -> Some k, None
        | Some(Reading r) -> None, Some r
        | _ -> failwithf "%s is not a valid antonym reference." el.Value

    {
        Kanji = k
        Reading = r
    }

let parseLanguageCode (el: XElement) =
    match el.Attribute(XNamespace.Xml + "lang") with
    | null -> "eng"
    | l -> l.Value

let parseLanguageSource (el: XElement) = {
    Value = el.Value
    Code = parseLanguageCode el
    IsPartial = el.Attribute("ls_type") <> null
    IsWasei = el.Attribute("ls_wasei") <> null
}

let parseGloss (el: XElement) = {
    Value = el.Value
    LanguageCode = parseLanguageCode el
    Type = el.Attribute("g_type").TryGetValue()
}

let parseSense (el: XElement) = {
    KanjiRestrictions = parseElementList "stagk" (_.Value) el
    ReadingRestrictions = parseElementList "stagr" (_.Value) el
    PartsOfSpeech = parseElementList "pos" (_.Value) el
    CrossReferences = parseElementList "xref" parseCrossReference el
    Antonyms = parseElementList "ant" parseAntonym el
    Fields = parseElementList "field" (_.Value) el
    MiscellaneousInformation = parseElementList "misc" (_.Value) el
    AdditionalInformation = parseElementList "s_inf" (_.Value) el
    LanguageSources = parseElementList "lsource" parseLanguageSource el
    Dialects = parseElementList "dial" (_.Value) el
    Glosses = parseElementList "gloss" parseGloss el
}

let getJMdictEntries (path: string) =
    streamXmlElements "entry" path
    |> Seq.map (fun entry -> {
        Id = entry.Element("ent_seq").Value |> int
        IsProperName = false
        KanjiElements = parseElementList "k_ele" parseKanjiElement entry
        ReadingElements = parseElementList "r_ele" parseReadingElement entry
        Senses = parseElementList "sense" parseSense entry
    })

let parseTranslationContents (el: XElement) : TranslationContents = {
    Value = el.Value
    LanguageCode = parseLanguageCode el
}

let parseTranslation (el: XElement) = {
    NameTypes = parseElementList "name_type" (_.Value) el
    CrossReferences = parseElementList "xref" parseCrossReference el
    Contents = parseElementList "trans_det" parseTranslationContents el
}

let getJMnedictEntries (path: string) =
    streamXmlElements "entry" path
    |> Seq.map (fun entry -> {
        Id = entry.Element("ent_seq").Value |> int
        IsProperName = false
        KanjiElements = parseElementList "k_ele" parseKanjiElement entry
        ReadingElements = parseElementList "r_ele" parseReadingElement entry
        Translations = parseElementList "trans" parseTranslation entry
    })

let parseHeader (el: XElement) = {
    FileVersion = int (el.Element("file_version").Value)
    DatabaseVersion = el.Element("database_version").Value
    DateOfCreation = DateTime.Parse(el.Element("date_of_creation").Value, DateTimeFormatInfo.InvariantInfo)
}

let parseCodePoint (el: XElement) : CodePoint = {
    Value = el.Value
    Type = el.Attribute("cp_type").Value
}

let parseKeyRadical (el: XElement) : KeyRadical = {
    Value = int el.Value
    Type = el.Attribute("rad_type").Value
}

let parseGrade (el: XElement) =
    match el.Element("misc").Element("grade") with
    | null -> None
    | g -> Some(int g.Value)

let parseStrokeMiscounts (el: XElement) =
    el.Element("misc").Elements("stroke_count")
    |> Seq.skip 1
    |> Seq.map (fun p -> int p.Value)
    |> Seq.toList

let parseVariant (el: XElement) : CharacterVariant = {
    Value = el.Value
    Type = el.Attribute("var_type").Value
}

let parseFrequency (el: XElement) =
    match el.Element("misc").Element("freq") with
    | null -> None
    | f -> Some(int f.Value)

let parseOldJlptLevel (el: XElement) =
    match el.Element("misc").Element("jlpt") with
    | null -> None
    | l -> Some(int l.Value)

let parseDictionaryReferences (el: XElement) =
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

let parseQueryCodes (el: XElement) =
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

let parseCharacterReadings (el: XElement) =
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

let parseCharacterMeanings (el: XElement) =
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

let parseNanori (el: XElement) =
    match el.Element("reading_meaning") with
    | null -> []
    | rm -> parseElementList "nanori" (_.Value) rm

let getKanjidic2Info (path: string) =
    streamXmlElements "header" path |> Seq.head |> parseHeader

let getKanjidic2Entries (path: string) =
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
