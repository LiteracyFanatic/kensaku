namespace Kensaku.DataSources

open FSharp.Control
open System.IO
open System.Threading

/// <summary>
/// Represents a translated name string (trans_det element) with its language code.
/// </summary>
type TranslationContents = {
    Value: string
    LanguageCode: string
}

/// <summary>
/// Represents a translation block (trans element) including name types and cross references.
/// </summary>
type Translation = {
    NameTypes: string list
    CrossReferences: CrossReference list
    Contents: TranslationContents list
}

/// <summary>
/// Represents a JMnedict entry composed of kanji elements, reading elements, and translations.
/// </summary>
type JMnedictEntry = {
    Id: int
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Translations: Translation list
}

module private JMnedict =
    open System.Xml.Linq

    let private parseTranslationContents (el: XElement) : TranslationContents = {
        Value = el.Value
        LanguageCode = JMdict.parseLanguageCode el
    }

    let private parseTranslation (el: XElement) = {
        NameTypes = parseElementList "name_type" (_.Value) el
        CrossReferences = parseElementList "xref" JMdict.parseCrossReference el
        Contents = parseElementList "trans_det" parseTranslationContents el
    }

    let parseEntriesAsync (stream: Stream) (closeStream: bool) (ct: CancellationToken) =
        streamXmlElementsAsync "entry" stream closeStream ct
        |> TaskSeq.map (fun entry -> {
            Id = entry.Element("ent_seq").Value |> int
            KanjiElements = parseElementList "k_ele" JMdict.parseKanjiElement entry
            ReadingElements = parseElementList "r_ele" JMdict.parseReadingElement entry
            Translations = parseElementList "trans" parseTranslation entry
        })

/// <summary>
/// Provides methods to parse JMnedict entries asynchronously.
/// </summary>
[<AbstractClass; Sealed>]
type JMnedict =
    /// <summary>
    /// Parses the JMnedict entries from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the JMnedict data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A <see cref="FSharp.Control.TaskSeq{T}"/> that produces <see cref="JMnedictEntry"/> values.</returns>
    static member ParseEntriesAsync(stream: Stream, ?ct: CancellationToken) =
        let ct = defaultArg ct CancellationToken.None
        JMnedict.parseEntriesAsync stream false ct

    /// <summary>
    /// Parses the JMnedict entries from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path containing the JMnedict data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A <see cref="FSharp.Control.TaskSeq{T}"/> that produces <see cref="JMnedictEntry"/> values.</returns>
    static member ParseEntriesAsync(path: string, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        let ct = defaultArg ct CancellationToken.None
        JMnedict.parseEntriesAsync stream true ct
