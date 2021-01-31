open System.Net
open System.Net.Http
open System.IO
open System.IO.Compression
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Linq
open Microsoft.Data.Sqlite

let downloadGZippedResource (hc: HttpClient) (url: string) (fileName: string) =
    async {
        let! ms = hc.GetStreamAsync(url) |> Async.AwaitTask
        use data = new GZipStream(ms, CompressionMode.Decompress)
        let fs = File.Create($"data/{fileName}")
        return! data.CopyToAsync(fs) |> Async.AwaitTask
    }

let downloadRadicalFiles =
    async {
        let request = WebRequest.Create("ftp://ftp.monash.edu/pub/nihongo/kradzip.zip")
        let! response = request.GetResponseAsync() |> Async.AwaitTask
        use archive = new ZipArchive(response.GetResponseStream())
        let files = [
            "kradfile"
            "kradfile2"
            "radkfile"
            "radkfile2"
        ]
        archive.Entries
        |> Seq.filter (fun x -> List.contains x.Name files)
        |> Seq.iter (fun x -> x.ExtractToFile($"data/{x.Name}"))
    }

let downloadData () =
    let dir = DirectoryInfo("data")
    dir.Create()
    dir.EnumerateFileSystemInfos()
    |> Seq.iter (fun x -> x.Delete())
    let hc = new HttpClient()
    [
        downloadGZippedResource hc "http://ftp.edrdg.org/pub/Nihongo/JMdict.gz" "JMdict.xml"
        downloadGZippedResource hc "http://ftp.monash.edu/pub/nihongo/JMnedict.xml.gz" "JMnedict.xml"
        downloadGZippedResource hc "http://www.edrdg.org/kanjidic/kanjidic2.xml.gz" "kanjidic2.xml"
        downloadRadicalFiles
    ] |> Async.Parallel

let createSchema (connection: SqliteConnection) =
    let cmd = connection.CreateCommand()
    cmd.CommandText <- File.ReadAllText("sql/schema.sql")
    cmd.ExecuteNonQuery() |> ignore

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

let isKana (text: char) =
    true

let isKanji (text: char) =
    true

type ReferenceComponent =
    | Kanji of string
    | Reading of string
    | Sense of int

let tryParseReferenceComponent (text: string) =
    if Seq.forall isKana text then
        Some (Reading text)
    else if Seq.exists isKanji text then
        Some (Kanji text)
    else
        match System.Int32.TryParse(text) with
        | true, i -> Some (Sense i)
        | false, _ -> None


let parseCrossReference (el: XElement) =
    // Split on JIS center dot
    let parts = el.Value.Split('\u2126')
    let a = parts |> Array.tryItem 0 |> Option.collect tryParseReferenceComponent
    let b = parts |> Array.tryItem 1 |> Option.collect tryParseReferenceComponent
    let c = parts |> Array.tryItem 2 |> Option.collect tryParseReferenceComponent
    let k, r, s =
        match a, b, c with
        | Some (Kanji k), Some (Reading r), Some (Sense s) -> Some k, Some r, Some s
        | Some (Kanji k), Some (Reading r), None -> Some k, Some r, None
        | Some (Kanji k), Some (Sense s), None -> Some k, None, Some s
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

let populateRadicals (connection: SqliteConnection) (radkEntries: RadkEntry list) =
    use transation = connection.BeginTransaction()
    let cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO RADICALS ('value', 'strokeCount') VALUES (@value, @strokeCount)"
    let value = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let strokeCount = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@strokeCount"))
    for entry in radkEntries do
        value.Value <- entry.Radical
        strokeCount.Value <- entry.StrokeCount
        cmd.ExecuteNonQuery() |> ignore
    transation.Commit()

let populateTables (connection: SqliteConnection) =
    let radkEntries = getRadkEntries ()
    populateRadicals connection radkEntries

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    getJmdictEntries ()
    |> Seq.take 10
    |> Seq.toList
    |> printfn "%A"
    0
