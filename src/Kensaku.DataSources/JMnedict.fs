namespace Kensaku.DataSources

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

[<RequireQualifiedAccess>]
module JMnedict =
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

    let getEntries (path: string) =
        streamXmlElements "entry" path
        |> Seq.map (fun entry -> {
            Id = entry.Element("ent_seq").Value |> int
            IsProperName = false
            KanjiElements = parseElementList "k_ele" JMdict.parseKanjiElement entry
            ReadingElements = parseElementList "r_ele" JMdict.parseReadingElement entry
            Translations = parseElementList "trans" parseTranslation entry
        })
