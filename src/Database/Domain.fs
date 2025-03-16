module Kensaku.Domain

open System
open System.Text

type RadkEntry = {
    Radical: Rune
    StrokeCount: int
    Kanji: Set<Rune>
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
} with

    override this.ToString() =
        [ this.Kanji; this.Reading; this.Index |> Option.map (sprintf "%i") ]
        |> List.filter Option.isSome
        |> List.map Option.get
        |> String.concat " "
        |> sprintf "See also %s"

type Antonym = {
    Kanji: string option
    Reading: string option
} with

    override this.ToString() =
        [ this.Kanji; this.Reading ]
        |> List.filter Option.isSome
        |> List.map Option.get
        |> String.concat " "

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

type CJKRadicalValue = {
    RadicalCharacter: Rune
    UnifiedIdeographCharacter: Rune
}

type CJKRadical = {
    RadicalNumber: int
    Standard: CJKRadicalValue
    Simplified: CJKRadicalValue option
}
