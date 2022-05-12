module Kensaku.Domain

open System
open System.Text

[<CLIMutable>]
type RadkEntry = {
    Radical: Rune
    StrokeCount: int
    Kanji: Set<Rune>
}

[<CLIMutable>]
type KanjiElement = {
    Value: string
    Information: string list
    Priority: string list
}

[<CLIMutable>]
type ReadingElement = {
    Value: string
    IsTrueReading: bool
    Restrictions: string list
    Information: string list
    Priority: string list
}

[<CLIMutable>]
type CrossReference = {
    Kanji: string option
    Reading: string option
    Index: int option
}

[<CLIMutable>]
type Antonym = {
    Kanji: string option
    Reading: string option
}

[<CLIMutable>]
type LanguageSource = {
    Value: string
    Code: string
    IsPartial: bool
    IsWasei: bool
}

[<CLIMutable>]
type Gloss = {
    Value: string
    LanguageCode: string
    Type: string option
}

[<CLIMutable>]
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

[<CLIMutable>]
type JMdictEntry = {
    Id: int
    // Where did this come from?
    IsProperName: bool
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Senses: Sense list
}

[<CLIMutable>]
type TranslationContents = {
    Value: string
    LanguageCode: string
}

[<CLIMutable>]
type Translation = {
    NameTypes: string list
    CrossReferences: CrossReference list
    Contents: TranslationContents list
}

[<CLIMutable>]
type JMnedictEntry = {
    Id: int
    // Where did this come from?
    IsProperName: bool
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Translations: Translation list
}

[<CLIMutable>]
type Kanjidic2Info = {
    FileVersion: int
    DatabaseVersion: string
    DateOfCreation: DateTime
}

[<CLIMutable>]
type CodePoint = {
    Value: string
    Type: string
}

[<CLIMutable>]
type KeyRadical = {
    Value: string
    Type: string
}

[<CLIMutable>]
type CharacterVariant = {
    Value: string
    Type: string
}

[<CLIMutable>]
type DictionaryReference = {
    IndexNumber: string
    Type: string
    Volume: int option
    Page: int option
}

[<CLIMutable>]
type QueryCode = {
    Value: string
    Type: string
    SkipMisclassification: string option
}

[<CLIMutable>]
type CharacterReading = {
    Value: string
    Type: string
}

[<CLIMutable>]
type CharacterMeaning = {
    Value: string
    LanguageCode: string
}

[<CLIMutable>]
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
