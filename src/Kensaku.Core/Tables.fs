namespace Kensaku.Core.Tables

open System
open System.Text

[<CLIMutable>]
type internal Entry = {
    Id: int
}

[<CLIMutable>]
type internal SenseCrossReference = {
    SenseId: int
    ReferenceKanjiElement: string option
    ReferenceReadingElement: string option
    ReferenceSense: int option
}

[<CLIMutable>]
type internal Field = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type internal PartOfSpeech = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type internal Dialect = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type internal SenseInformation = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type internal KanjiElementInformation = {
    KanjiElementId: int
    Value: string
}

[<CLIMutable>]
type internal KanjiElementPriority = {
    KanjiElementId: int
    Value: string
}

[<CLIMutable>]
type internal ReadingElementPriority = {
    ReadingElementId: int
    Value: string
}

[<CLIMutable>]
type internal KanjiElement = {
    Id: int
    EntryId: int
    Value: string
}

[<CLIMutable>]
type internal ReadingElement = {
    Id: int
    EntryId: int
    Value: string
    IsTrueReading: bool
}

[<CLIMutable>]
type internal ReadingElementInformation = {
    ReadingElementId: int
    Value: string
}

[<CLIMutable>]
type internal Sense = {
    Id: int
    EntryId: int
}

[<CLIMutable>]
type internal SenseKanjiElementRestriction = {
    SenseId: int
    KanjiElement: string
}

[<CLIMutable>]
type internal ReadingElementRestriction = {
    ReadingElementId: int
    Value: string
}

[<CLIMutable>]
type internal SenseReadingElementRestriction = {
    SenseId: int
    ReadingElement: string
}

[<CLIMutable>]
type internal MiscellaneousInformation = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type internal LanguageSource = {
    SenseId: int
    Value: string
    LanguageCode: string
    IsPartial: bool
    IsWasei: bool
}

[<CLIMutable>]
type internal Gloss = {
    SenseId: int
    Value: string
    Language: string
    Type: string option
}

[<CLIMutable>]
type internal Antonym = {
    SenseId: int
    ReferenceKanjiElement: string option
    ReferenceReadingElement: string option
}

[<CLIMutable>]
type internal Translation = {
    Id: int
    EntryId: int
}

[<CLIMutable>]
type internal NameType = {
    TranslationId: int
    Value: string
}

[<CLIMutable>]
type internal TranslationCrossReference = {
    TranslationId: int
    ReferenceKanjiElement: string option
    ReferenceReadingElement: string option
    ReferenceTranslation: int option
}

[<CLIMutable>]
type internal TranslationContent = {
    TranslationId: int
    Value: string
    Language: string
}

[<CLIMutable>]
type internal Kanjidic2Info = {
    FileVersion: int
    DatabaseVersion: string
    DateOfCreation: DateTime
}

[<CLIMutable>]
type internal StrokeMiscount = {
    CharacterId: int
    Value: int
}

[<CLIMutable>]
type internal Character = {
    Id: int
    Value: Rune
    Grade: int option
    StrokeCount: int
    Frequency: int option
    IsRadical: bool
    OldJlptLevel: int option
}

[<CLIMutable>]
type internal Codepoint = {
    CharacterId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type internal KeyRadical = {
    CharacterId: int
    Value: int
    Type: string
}

[<CLIMutable>]
type internal CharacterVariant = {
    CharacterId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type internal RadicalName = {
    CharacterId: int
    Value: string
}

[<CLIMutable>]
type internal CharacterDictionaryReference = {
    CharacterId: int
    IndexNumber: string
    Type: string
    Volume: int option
    Page: int option
}

[<CLIMutable>]
type internal CharacterQueryCode = {
    CharacterId: int
    Value: string
    Type: string
    SkipMisclassification: string option
}

[<CLIMutable>]
type internal CharacterReading = {
    CharacterId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type internal CharacterMeaning = {
    CharacterId: int
    Value: string
    Language: string
}

[<CLIMutable>]
type internal Nanori = {
    CharacterId: int
    Value: string
}

[<CLIMutable>]
type internal Radical = {
    Id: int
    Number: int option
    StrokeCount: int
}

[<CLIMutable>]
type internal RadicalValue = {
    RadicalId: int
    Value: Rune
    Type: string
}

[<CLIMutable>]
type internal RadicalMeaning = {
    RadicalId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type internal Characters_Radical = {
    CharacterId: int
    RadicalId: int
}
