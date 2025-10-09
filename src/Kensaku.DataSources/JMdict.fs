namespace Kensaku.DataSources

open FSharp.Control
open System.IO
open System.Text
open System.Threading

/// <summary>
/// Represents a written kanji form of a dictionary headword (k_ele element).
/// </summary>
type KanjiElement = {
    Value: string
    Information: string list
    Priority: string list
}

/// <summary>
/// Represents a reading (pronunciation) of a dictionary headword (r_ele element).
/// </summary>
type ReadingElement = {
    Value: string
    IsTrueReading: bool
    Restrictions: string list
    Information: string list
    Priority: string list
}

/// <summary>
/// Represents a cross reference (xref) to a related JMdict entry.
/// Any of the components (kanji, reading, sense index) may be omitted.
/// </summary>
type CrossReference = {
    Kanji: string option
    Reading: string option
    Index: int option
} with

    override this.ToString() =
        [ this.Kanji; this.Reading; this.Index |> Option.map (sprintf "%i") ]
        |> List.filter Option.isSome
        |> List.map Option.get
        |> String.concat " "
        |> sprintf "See also %s"

/// <summary>
/// Represents an antonym reference (ant) to another entry.
/// </summary>
type Antonym = {
    Kanji: string option
    Reading: string option
} with

    override this.ToString() =
        [ this.Kanji; this.Reading ]
        |> List.filter Option.isSome
        |> List.map Option.get
        |> String.concat " "

/// <summary>
/// Represents etymological information (lsource) about a borrowing from another language.
/// </summary>
type LanguageSource = {
    Value: string
    Code: string
    IsPartial: bool
    IsWasei: bool
} with

    override this.ToString() =
        let sb = StringBuilder()

        sb.AppendLine($"From %s{this.Code} \"%s{this.Value}\"") |> ignore

        if this.IsWasei then
            sb.AppendLine(". Wasei (word made in Japan)") |> ignore

        sb.ToString()

/// <summary>
/// Represents a gloss (translation) for a sense in a given language.
/// </summary>
type Gloss = {
    Value: string
    LanguageCode: string
    Type: string option
}

/// <summary>
/// Represents a single sense of a dictionary entry including part-of-speech,
/// semantic fields, glosses, and cross references.
/// </summary>
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

/// <summary>
/// Represents a JMdict entry composed of kanji elements, reading elements, and senses.
/// </summary>
type JMdictEntry = {
    Id: int
    // Where did this come from?
    IsProperName: bool
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Senses: Sense list
}

module private JMdict =
    open System
    open System.Xml.Linq

    let internal parseKanjiElement (el: XElement) = {
        Value = el.Element("keb").Value
        Information = parseElementList "ke_inf" (_.Value) el
        Priority = parseElementList "ke_pri" (_.Value) el
    }

    let internal parseReadingElement (el: XElement) = {
        Value = el.Element("reb").Value
        IsTrueReading = isNull (el.Element("re_nokanji"))
        Restrictions = parseElementList "re_restr" (_.Value) el
        Information = parseElementList "re_inf" (_.Value) el
        Priority = parseElementList "re_pri" (_.Value) el
    }

    type private ReferenceComponent =
        | Kanji of string
        | Reading of string
        | Index of int

    let private tryParseReferenceComponent (text: string) =
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

    let internal parseCrossReference (el: XElement) =
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
            // reading and sense can occur without a kanji component.
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

    let private parseAntonym (el: XElement) =
        let k, r =
            match tryParseReferenceComponent el.Value with
            | Some(Kanji k) -> Some k, None
            | Some(Reading r) -> None, Some r
            | _ -> failwithf "%s is not a valid antonym reference." el.Value

        {
            Kanji = k
            Reading = r
        }

    let internal parseLanguageCode (el: XElement) =
        match el.Attribute(XNamespace.Xml + "lang") with
        | null -> "eng"
        | l -> l.Value

    let private parseLanguageSource (el: XElement) = {
        Value = el.Value
        Code = parseLanguageCode el
        IsPartial = el.Attribute("ls_type") <> null
        IsWasei = el.Attribute("ls_wasei") <> null
    }

    let private parseGloss (el: XElement) = {
        Value = el.Value
        LanguageCode = parseLanguageCode el
        Type = el.Attribute("g_type").TryGetValue()
    }

    let private parseSense (el: XElement) = {
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

    let parseEntriesAsync (stream: Stream) (closeStream: bool) (ct: CancellationToken) =
        streamXmlElementsAsync "entry" stream closeStream ct
        |> TaskSeq.map (fun entry -> {
            Id = entry.Element("ent_seq").Value |> int
            IsProperName = false
            KanjiElements = parseElementList "k_ele" parseKanjiElement entry
            ReadingElements = parseElementList "r_ele" parseReadingElement entry
            Senses = parseElementList "sense" parseSense entry
        })

/// <summary>
/// Provides methods to parse JMdict entries asynchronously.
/// </summary>
[<AbstractClass; Sealed>]
type JMdict =
    /// <summary>
    /// Parses the JMdict entries from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the JMdict data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A <see cref="FSharp.Control.TaskSeq{T}"/> that produces <see cref="JMdictEntry"/> values.</returns>
    static member ParseEntriesAsync(stream: Stream, ?ct: CancellationToken) =
        let ct = defaultArg ct CancellationToken.None
        JMdict.parseEntriesAsync stream false ct

    /// <summary>
    /// Parses the JMdict entries from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path containing the JMdict data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A <see cref="FSharp.Control.TaskSeq{T}"/> that produces <see cref="JMdictEntry"/> values.</returns>
    static member ParseEntriesAsync(path: string, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        let ct = defaultArg ct CancellationToken.None
        JMdict.parseEntriesAsync stream true ct
