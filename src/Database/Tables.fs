module Kensaku.Database.Tables

open System
open System.Text

[<CLIMutable>]
type Entry = {
    Id: int
    IsProperName: bool
}

[<CLIMutable>]
type SenseCrossReference = {
    SenseId: int
    ReferenceKanjiElement: string option
    ReferenceReadingElement: string option
    ReferenceSense: int option
}

[<CLIMutable>]
type Field = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type PartOfSpeech = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type Dialect = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type SenseInformation = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type KanjiElementInformation = {
    KanjiElementId: int
    Value: string
}

[<CLIMutable>]
type KanjiElementPriority = {
    KanjiElementId: int
    Value: string
}

[<CLIMutable>]
type ReadingElementPriority = {
    ReadingElementId: int
    Value: string
}

[<CLIMutable>]
type KanjiElement = {
    Id: int
    EntryId: int
    Value: string
}

[<CLIMutable>]
type ReadingElement = {
    Id: int
    EntryId: int
    Value: string
    IsTrueReading: bool
}

[<CLIMutable>]
type ReadingElementInformation = {
    ReadingElementId: int
    Value: string
}

[<CLIMutable>]
type Sense = {
    Id: int
    EntryId: int
}

[<CLIMutable>]
type SenseKanjiElementRestriction = {
    SenseId: int
    KanjiElement: string
}

[<CLIMutable>]
type ReadingElementRestriction = {
    ReadingElementId: int
    Value: string
}

[<CLIMutable>]
type SenseReadingElementRestriction = {
    SenseId: int
    ReadingElement: string
}

[<CLIMutable>]
type MiscellaneousInformation = {
    SenseId: int
    Value: string
}

[<CLIMutable>]
type LanguageSource = {
    SenseId: int
    Value: string
    LanguageCode: string
    IsPartial: bool
    IsWasei: bool
}

[<CLIMutable>]
type Gloss = {
    SenseId: int
    Value: string
    Language: string
    Type: string option
}

[<CLIMutable>]
type Antonym = {
    SenseId: int
    ReferenceKanjiElement: string option
    ReferenceReadingElement: string option
}

[<CLIMutable>]
type Translation = {
    Id: int
    EntryId: int
}

[<CLIMutable>]
type NameType = {
    TranslationId: int
    Value: string
}

[<CLIMutable>]
type TranslationCrossReference = {
    TranslationId: int
    ReferenceKanjiElement: string option
    ReferenceReadingElement: string option
    ReferenceTranslation: int option
}

[<CLIMutable>]
type TranslationContent = {
    TranslationId: int
    Value: string
    Language: string
}

[<CLIMutable>]
type Kanjidic2Info = {
    FileVersion: int
    DatabaseVersion: string
    DateOfCreation: DateTime
}

[<CLIMutable>]
type StrokeMiscount = {
    CharacterId: int
    Value: int
}

[<CLIMutable>]
type Character = {
    Id: int
    Value: Rune
    Grade: int option
    StrokeCount: int
    Frequency: int option
    IsRadical: bool
    OldJlptLevel: int option
}

[<CLIMutable>]
type Codepoint = {
    CharacterId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type KeyRadical = {
    CharacterId: int
    Value: int
    Type: string
}

[<CLIMutable>]
type CharacterVariant = {
    CharacterId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type RadicalName = {
    CharacterId: int
    Value: string
}

[<CLIMutable>]
type CharacterDictionaryReference = {
    CharacterId: int
    IndexNumber: string
    Type: string
    Volume: int option
    Page: int option
}

[<CLIMutable>]
type CharacterQueryCode = {
    CharacterId: int
    Value: string
    Type: string
    SkipMisclassification: string option
}

[<CLIMutable>]
type CharacterReading = {
    CharacterId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type CharacterMeaning = {
    CharacterId: int
    Value: string
    Language: string
}

[<CLIMutable>]
type Nanori = {
    CharacterId: int
    Value: string
}

[<CLIMutable>]
type Radical = {
    Id: int
    Number: int option
    StrokeCount: int
}

[<CLIMutable>]
type RadicalValue = {
    RadicalId: int
    Value: Rune
    Type: string
}

[<CLIMutable>]
type RadicalMeaning = {
    RadicalId: int
    Value: string
    Type: string
}

[<CLIMutable>]
type Characters_Radical = {
    CharacterId: int
    RadicalId: int
}
