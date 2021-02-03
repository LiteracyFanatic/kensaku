module Database

open System.IO
open System.Text
open Dapper
open System.Data.Common

let createSchema (ctx: DbConnection) =
    File.ReadAllText("sql/schema.sql")
    |> ctx.Execute
    |> ignore

let getLastRowId (ctx: DbConnection) =
    ctx.QuerySingle<int>("SELECT last_insert_rowid()")

let populateKanjiElementPriorities (ctx: DbConnection) (kanjiElementId: int) (priorities: string list) =
    for p in priorities do
        ctx.Execute(
            "INSERT INTO KanjiElementPriorities ('KanjiElementId', 'Value') VALUES (@KanjiElementId, @Value)",
            {| KanjiElementId = kanjiElementId; Value = p |}
        ) |> ignore

let populateKanjiElementInformation (ctx: DbConnection) (kanjiElementId: int) (information: string list) =
    for i in information do
        ctx.Execute(
            "INSERT INTO KanjiElementInformation ('KanjiElementId', 'Value') VALUES (@KanjiElementId, @Value)",
            {| KanjiElementId = kanjiElementId; Value = i |}
        ) |> ignore

let populateKanjiElements (ctx: DbConnection) (entryId: int) (kanjiElements: DataParsing.KanjiElement list) =
    for k in kanjiElements do
        ctx.Execute(
            "INSERT INTO KanjiElements ('EntryId', 'Value') VALUES (@EntryId, @Value)",
            {| k with EntryId = entryId |}
        ) |> ignore
        let id = getLastRowId ctx
        populateKanjiElementPriorities ctx id k.Priority
        populateKanjiElementInformation ctx id k.Information

let populateReadingElementPriorities (ctx: DbConnection) (readingElementId: int) (priorities: string list) =
    for p in priorities do
        ctx.Execute(
            "INSERT INTO ReadingElementPriorities ('ReadingElementId', 'Value') VALUES (@ReadingElementId, @Value)",
            {| ReadingElementId = readingElementId; Value = p |}
        ) |> ignore

let populateReadingElementInformation (ctx: DbConnection) (readingElementId: int) (information: string list) =
    for i in information do
        ctx.Execute(
            "INSERT INTO ReadingElementInformation ('ReadingElementId', 'Value') VALUES (@ReadingElementId, @Value)",
            {| ReadingElementId = readingElementId; Value = i |}
        ) |> ignore

let populateReadingElementRestrictions (ctx: DbConnection) (readingElementId: int) (restrictions: string list) =
    for r in restrictions do
        ctx.Execute(
            "INSERT INTO ReadingElementRestrictions ('ReadingElementId', 'Value') VALUES (@ReadingElementId, @Value)",
            {| ReadingElementId = readingElementId; Value = r |}
        ) |> ignore

let populateReadingElements (ctx: DbConnection) (entryId: int) (readingElements: DataParsing.ReadingElement list) =
    for r in readingElements do
        ctx.Execute(
            "INSERT INTO ReadingElements ('EntryId', 'Value', 'IsTrueReading') VALUES (@EntryId, @Value, @IsTrueReading)",
            {| r with EntryId = entryId |}
        ) |> ignore
        let id = getLastRowId ctx
        populateReadingElementPriorities ctx id r.Priority
        populateReadingElementInformation ctx id r.Information
        populateReadingElementRestrictions ctx id r.Restrictions

let populateAntonyms (ctx: DbConnection) (senseId: int) (antonyms: DataParsing.Antonym list) =
    for a in antonyms do
        ctx.Execute(
            "INSERT INTO Antonyms ('SenseId', 'ReferenceKanjiElement', 'ReferenceReadingElement') VALUES(@SenseId, @Kanji, @Reading)",
            {| a with SenseId = senseId |}
        ) |> ignore

let populateFields (ctx: DbConnection) (senseId: int) (fields: string list) =
    for f in fields do
        ctx.Execute(
            "INSERT INTO Fields ('SenseId', 'Value') VALUES (@SenseId, @Value)",
            {| SenseId = senseId; Value = f |}
        ) |> ignore

let populateDialects (ctx: DbConnection) (senseId: int) (dialects: string list) =
    for d in dialects do
        ctx.Execute(
            "INSERT INTO Dialects ('SenseId', 'Value') VALUES (@SenseId, @Value)",
            {| SenseId = senseId; Value = d |}
        ) |> ignore

let populateMiscellaneousInformation (ctx: DbConnection) (senseId: int) (miscellaneousInformation: string list) =
    for m in miscellaneousInformation do
        ctx.Execute(
            "INSERT INTO MiscellaneousInformation ('SenseId', 'Value') VALUES (@SenseId, @Value)",
            {| SenseId = senseId; Value = m |}
        ) |> ignore

let populateAdditionalInformation (ctx: DbConnection) (senseId: int) (additionalInformation: string list) =
    for a in additionalInformation do
        ctx.Execute(
            "INSERT INTO SenseInformation ('SenseId', 'Value') VALUES (@SenseId, @Value)",
            {| SenseId = senseId; Value = a |}
        ) |> ignore

let populateLanguageSources (ctx: DbConnection) (senseId: int) (languageSources: DataParsing.LanguageSource list) =
    for l in languageSources do
        ctx.Execute(
            "INSERT INTO LanguageSources ('SenseId', 'Value', 'LanguageCode', 'IsPartial', 'IsWasei') VALUES (@SenseId, @Value, @Code, @IsPartial, @IsWasei)",
            {| l with SenseId = senseId |}
        ) |> ignore

let populatePartsOfSpeech (ctx: DbConnection) (senseId: int) (partsOfSpeech: string list) =
    for p in partsOfSpeech do
        ctx.Execute(
            "INSERT INTO PartsOfSpeech ('SenseId', 'Value') VALUES (@SenseId, @Value)",
            {| SenseId = senseId; Value = p |}
        ) |> ignore

let populateGlosses (ctx: DbConnection) (senseId: int) (glosses: DataParsing.Gloss list) =
    for g in glosses do
        ctx.Execute(
            "INSERT INTO Glosses ('SenseId', 'Value', 'Language', 'Type') VALUES (@SenseId, @Value, @LanguageCode, @Type)",
            {| g with SenseId = senseId |}
        ) |> ignore

let populateSenseKanjiElementRestrictions (ctx: DbConnection) (senseId: int) (restrictions: string list) =
    for r in restrictions do
        ctx.Execute(
            "INSERT INTO SenseKanjiElementRestrictions ('SenseId', 'KanjiElement') VALUES (@SenseId, @KanjiElement)",
            {| SenseId = senseId; KanjiElement = r |}
        ) |> ignore

let populateSenseReadingElementRestrictions (ctx: DbConnection) (senseId: int) (restrictions: string list) =
    for r in restrictions do
        ctx.Execute(
            "INSERT INTO SenseReadingElementRestrictions ('SenseId', 'ReadingElement') VALUES (@SenseId, @ReadingElement)",
            {| SenseId = senseId; ReadingElement = r |}
        ) |> ignore

let populateSenseCrossReferences (ctx: DbConnection) (senseId: int) (crossReferences: DataParsing.CrossReference list) =
    for c in crossReferences do
        ctx.Execute(
            "INSERT INTO SenseCrossReferences ('SenseId', 'ReferenceKanjiElement', 'ReferenceReadingElement', 'ReferenceSense') VALUES (@SenseId, @Kanji, @Reading, @Index)",
            {| c with SenseId = senseId |}
        ) |> ignore

let populateSenses (ctx: DbConnection) (entryId: int) (senses: DataParsing.Sense list) =
    for s in senses do
        ctx.Execute(
            "INSERT INTO Senses ('EntryId') VALUES (@EntryId)",
            {| EntryId = entryId |}
        ) |> ignore
        let id = getLastRowId ctx
        populateAntonyms ctx id s.Antonyms
        populateFields ctx id s.Fields
        populateDialects ctx id s.Dialects
        populateMiscellaneousInformation ctx id s.MiscellaneousInformation
        populateAdditionalInformation ctx id s.AdditionalInformation
        populateLanguageSources ctx id s.LanguageSources
        populatePartsOfSpeech ctx id s.PartsOfSpeech
        populateGlosses ctx id s.Glosses
        populateSenseKanjiElementRestrictions ctx id s.KanjiRestrictions
        populateSenseReadingElementRestrictions ctx id s.ReadingRestrictions
        populateSenseCrossReferences ctx id s.CrossReferences

let populateJMdictEntries (ctx: DbConnection) (jMdictEntries: DataParsing.JMdictEntry seq) =
    use transaction = ctx.BeginTransaction()
    for entry in jMdictEntries do
        ctx.Execute(
            "INSERT INTO Entries ('Id', 'IsProperName') VALUES (@Id, @IsProperName)",
            entry
        ) |> ignore
        let id = getLastRowId ctx
        populateKanjiElements ctx id entry.KanjiElements
        populateReadingElements ctx id entry.ReadingElements
        populateSenses ctx id entry.Senses
    transaction.Commit()

let populateNameTypes (ctx: DbConnection) (translationId: int) (nameTypes: string list) =
    for n in nameTypes do
        ctx.Execute(
            "INSERT INTO NameTypes ('TranslationId', 'Value') VALUES (@TranslationId, @Value)",
            {| TranslationId = translationId; Value = n |}
        ) |> ignore

let populateTranslationCrossReferences (ctx: DbConnection) (translationId: int) (crossReferences: DataParsing.CrossReference list) =
    for c in crossReferences do
        ctx.Execute(
            "INSERT INTO TranslationCrossReferences ('TranslationId', 'ReferenceKanjiElement', 'ReferenceReadingElement', 'ReferenceTranslation') VALUES (@TranslationId, @Kanji, @Reading, @Index)",
            {| c with TranslationId = translationId |}
        ) |> ignore

let populateTranslationContents (ctx: DbConnection) (translationId: int) (contents: DataParsing.TranslationContents list) =
    for c in contents do
        ctx.Execute(
            "INSERT INTO TranslationContents ('TranslationId', 'Value', 'Language') VALUES (@TranslationId, @Value, @LanguageCode)",
            {| c with TranslationId = translationId |}
        ) |> ignore

let populateTranslations (ctx: DbConnection) (entryId: int) (translations: DataParsing.Translation list) =
    for t in translations do
        ctx.Execute(
            "INSERT INTO Translations ('EntryId') VALUES (@EntryId)",
            {| EntryId = entryId |}
        ) |> ignore
        let id = getLastRowId ctx
        populateNameTypes ctx id t.NameTypes
        populateTranslationCrossReferences ctx id t.CrossReferences
        populateTranslationContents ctx id t.Contents

let populateJMnedictEntries (ctx: DbConnection) (jMnedictEntries: DataParsing.JMnedictEntry seq) =
    use transaction = ctx.BeginTransaction()
    for entry in jMnedictEntries do
        ctx.Execute(
            "INSERT INTO Entries ('Id', 'IsProperName') VALUES (@Id, @IsProperName)",
            entry
        ) |> ignore
        let id = getLastRowId ctx
        populateKanjiElements ctx id entry.KanjiElements
        populateReadingElements ctx id entry.ReadingElements
        populateTranslations ctx id entry.Translations
    transaction.Commit()

let populateKanjidic2Info (ctx: DbConnection) (info: DataParsing.Kanjidic2Info) =
    use transaction = ctx.BeginTransaction()
    ctx.Execute(
        "INSERT INTO Kanjidic2Info ('FileVersion', 'DatabaseVersion', 'DateOfCreation') VALUES (@FileVersion, @DatabaseVersion, @DateOfCreation)",
        info
    ) |> ignore
    transaction.Commit()

let populateCodepoints (ctx: DbConnection) (characterId: int) (codepoints: DataParsing.CodePoint list) =
    for c in codepoints do
        ctx.Execute(
            "INSERT INTO Codepoints ('CharacterId', 'Value', 'Type') VALUES (@CharacterId, @Value, @Type)",
            {| c with CharacterId = characterId |}
        ) |> ignore

let populateKeyRadicals (ctx: DbConnection) (characterId: int) (keyRadicals: DataParsing.KeyRadical list) =
    for k in keyRadicals do
        ctx.Execute(
            "INSERT INTO KeyRadicals ('CharacterId', 'Value', 'Type') VALUES (@CharacterId, @Value, @Type)",
            {| k with CharacterId = characterId |}
        ) |> ignore

let populateStrokeMiscounts (ctx: DbConnection) (characterId: int) (strokeMiscounts: int list) =
    for s in strokeMiscounts do
        ctx.Execute(
            "INSERT INTO StrokeMiscounts ('CharacterId', 'Value') VALUES (@CharacterId, @Value)",
            {| CharacterId = characterId; Value = s |}
        ) |> ignore

let populateCharacterVariants (ctx: DbConnection) (characterId: int) (characterVariants: DataParsing.CharacterVariant list) =
    for c in characterVariants do
        ctx.Execute(
            "INSERT INTO CharacterVariants ('CharacterId', 'Value', 'Type') VALUES (@CharacterId, @Value, @Type)",
            {| c with CharacterId = characterId |}
        ) |> ignore

let populateRadicalNames (ctx: DbConnection) (characterId: int) (radicalNames: string list) =
    for r in radicalNames do
        ctx.Execute(
            "INSERT INTO RadicalNames ('CharacterId', 'Value') VALUES (@CharacterId, @Value)",
            {| CharacterId = characterId; Value = r |}
        ) |> ignore

let populateCharacterDictionaryReferences (ctx: DbConnection) (characterId: int) (references: DataParsing.DictionaryReference list) =
    for r in references do
        ctx.Execute(
            "INSERT INTO CharacterDictionaryReferences ('CharacterId', 'IndexNumber', 'Type', 'Volume', 'Page') VALUES (@CharacterId, @IndexNumber, @Type, @Volume, @Page)",
            {| r with CharacterId = characterId |}
        ) |> ignore

let populateCharacterQueryCodes (ctx: DbConnection) (characterId: int) (queryCodes: DataParsing.QueryCode list) =
    for q in queryCodes do
        ctx.Execute(
            "INSERT INTO CharacterQueryCodes ('CharacterId', 'Value', 'Type', 'SkipMisclassification') VALUES (@CharacterId, @Value, @Type, @SkipMisclassification)",
            {| q with CharacterId = characterId |}
        ) |> ignore

let populateCharacterReadings (ctx: DbConnection) (characterId: int) (readings: DataParsing.CharacterReading list) =
    for r in readings do
        ctx.Execute(
            "INSERT INTO CharacterReadings ('CharacterId', 'Value', 'Type') VALUES (@CharacterId, @Value, @Type)",
            {| r with CharacterId = characterId |}
        ) |> ignore

let populateCharacterMeanings (ctx: DbConnection) (characterId: int) (meanings: DataParsing.CharacterMeaning list) =
    for m in meanings do
        ctx.Execute(
            "INSERT INTO CharacterMeanings ('CharacterId', 'Value', 'Language') VALUES (@CharacterId, @Value, @LanguageCode)",
            {| m with CharacterId = characterId |}
        ) |> ignore

let populateNanori (ctx: DbConnection) (characterId: int) (nanori: string list) =
    for n in nanori do
        ctx.Execute(
            "INSERT INTO Nanori ('CharacterId', 'Value') VALUES (@CharacterId, @Value)",
            {| CharacterId = characterId; Value = n |}
        ) |> ignore

let populateKanjidic2Entries (ctx: DbConnection) (characters: DataParsing.Character seq) =
    use transaction = ctx.BeginTransaction()
    for c in characters do
        ctx.Execute(
            "INSERT INTO Characters ('Value', 'Grade', 'StrokeCount', 'Frequency', 'IsRadical', 'OldJlptLevel') VALUES (@Value, @Grade, @StrokeCount, @Frequency, @IsRadical, @OldJlptLevel)",
            {| c with Value = string c.Value |}
        ) |> ignore
        let id = getLastRowId ctx
        populateCodepoints ctx id c.CodePoints
        populateKeyRadicals ctx id c.KeyRadicals
        populateStrokeMiscounts ctx id c.StrokeMiscounts
        populateCharacterVariants ctx id c.Variants
        populateRadicalNames ctx id c.RadicalNames
        populateCharacterDictionaryReferences ctx id c.DictionaryReferences
        populateCharacterQueryCodes ctx id c.QueryCodes
        populateCharacterReadings ctx id c.Readings
        populateCharacterMeanings ctx id c.Meanings
        populateNanori ctx id c.Nanori
    transaction.Commit()

let getCharacterId (ctx: DbConnection) (character: Rune) =
    ctx.ExecuteScalar<int>(
        "SELECT id FROM Characters WHERE value = @Character",
        {| Character = character |}
    )

let populateCharactersRadicals (ctx: DbConnection) (radicalId: int) (characters: Set<Rune>) =
    for c in characters do
        ctx.Execute(
            "INSERT INTO Characters_Radicals ('CharacterId', 'RadicalId') VALUES (@CharacterId, @RadicalId)",
            {| RadicalId = radicalId; CharacterId = getCharacterId ctx c |}
        ) |> ignore

let populateRadicals (ctx: DbConnection) (radkEntries: DataParsing.RadkEntry list) =
    use transaction = ctx.BeginTransaction()
    for entry in radkEntries do
        ctx.Execute(
            "INSERT INTO RADICALS ('Value', 'StrokeCount') VALUES (@Radical, @StrokeCount)",
            entry
        ) |> ignore
        let id = getLastRowId ctx
        populateCharactersRadicals ctx id entry.Kanji
    transaction.Commit()

let populateTables (ctx: DbConnection) =
    let jMdictEntries = DataParsing.getJMdictEntries()
    populateJMdictEntries ctx jMdictEntries
    let jMnedictEntries = DataParsing.getJMnedictEntries()
    populateJMnedictEntries ctx jMnedictEntries
    let kanjidic2Info = DataParsing.getKanjidic2Info ()
    populateKanjidic2Info ctx kanjidic2Info
    let kanjidic2Entries = DataParsing.getKanjidic2Entries ()
    populateKanjidic2Entries ctx kanjidic2Entries
    let radkEntries = DataParsing.getRadkEntries ()
    populateRadicals ctx radkEntries
