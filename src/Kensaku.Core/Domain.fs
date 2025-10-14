namespace Kensaku.Core.Domain

open System
open System.Text

/// <summary>
/// Represents an entry from a radkfile mapping a radical to its kanji.
/// </summary>
type RadkEntry = {
    Radical: Rune
    StrokeCount: int
    Kanji: Set<Rune>
}

/// <summary>
/// Represents a kanji element (headword) of a dictionary entry.
/// </summary>
type KanjiElement = {
    Value: string
    Information: string list
    Priority: string list
}

/// <summary>
/// Represents a reading element (kana) of a dictionary entry.
/// </summary>
type ReadingElement = {
    Value: string
    IsTrueReading: bool
    Restrictions: string list
    Information: string list
    Priority: string list
}

/// <summary>
/// Represents a cross-reference to another dictionary entry.
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
/// Represents an antonym reference to another dictionary entry.
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
/// Represents a language source for loanword etymology.
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
/// Represents a gloss (translation) in a given language.
/// </summary>
type Gloss = {
    Value: string
    LanguageCode: string
    Type: string option
}

/// <summary>
/// Represents a sense (meaning) of a dictionary entry including glosses, parts of speech, and usage information.
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
/// Represents a complete JMdict dictionary entry including kanji, readings, and senses.
/// </summary>
type JMdictEntry = {
    Id: int
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Senses: Sense list
}

/// <summary>
/// Represents translation content for a proper name entry.
/// </summary>
type TranslationContents = {
    Value: string
    LanguageCode: string
}

/// <summary>
/// Represents a translation of a proper name including name types and cross-references.
/// </summary>
type Translation = {
    NameTypes: string list
    CrossReferences: CrossReference list
    Contents: TranslationContents list
}

/// <summary>
/// Represents a complete JMnedict proper name entry including kanji, readings, and translations.
/// </summary>
type JMnedictEntry = {
    Id: int
    KanjiElements: KanjiElement list
    ReadingElements: ReadingElement list
    Translations: Translation list
}

/// <summary>
/// Represents header metadata from the Kanjidic2 file.
/// </summary>
type Kanjidic2Info = {
    FileVersion: int
    DatabaseVersion: string
    DateOfCreation: DateTime
}

/// <summary>
/// Represents an encoded form (codepoint) of a character.
/// </summary>
type CodePoint = {
    Value: string
    Type: string
}

/// <summary>
/// Represents a key radical for a character including numbering system.
/// </summary>
type KeyRadical = {
    Value: int
    Type: string
}

/// <summary>
/// Represents a variant form of a character.
/// </summary>
type CharacterVariant = {
    Value: string
    Type: string
}

/// <summary>
/// Represents a reference to another dictionary source.
/// </summary>
type DictionaryReference = {
    IndexNumber: string
    Type: string
    Volume: int option
    Page: int option
}

/// <summary>
/// Represents a query code used for searching (e.g., SKIP, Four Corner).
/// </summary>
type QueryCode = {
    Value: string
    Type: string
    SkipMisclassification: string option
}

/// <summary>
/// Represents a reading for a character (on, kun, pinyin, etc.).
/// </summary>
type CharacterReading = {
    Value: string
    Type: string
}

/// <summary>
/// Represents a meaning for a character in a given language.
/// </summary>
type CharacterMeaning = {
    Value: string
    LanguageCode: string
}

/// <summary>
/// Represents a complete kanji character entry including codepoints, radicals, readings, and meanings.
/// </summary>
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

/// <summary>
/// Represents a CJK radical value including both radical and unified ideograph forms.
/// </summary>
type CJKRadicalValue = {
    RadicalCharacter: Rune
    UnifiedIdeographCharacter: Rune
}

/// <summary>
/// Represents a CJK radical entry including standard and optional simplified forms.
/// </summary>
type CJKRadical = {
    RadicalNumber: int
    Standard: CJKRadicalValue
    Simplified: CJKRadicalValue option
}
