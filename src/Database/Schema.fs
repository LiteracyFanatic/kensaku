module Kensaku.Database.Schema

open System
open System.IO
open System.Text
open System.Reflection
open System.Data.Common
open Dapper
open Kensaku
open Kensaku.Database.Tables

let createSchema (ctx: DbConnection) =
    let stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Database.sql.schema.sql")
    use sr = new StreamReader(stream)
    sr.ReadToEnd()
    |> ctx.Execute
    |> ignore

let getLastRowId (ctx: DbConnection) =
    ctx.QuerySingle<int>("SELECT last_insert_rowid()")

let populateKanjiElementPriorities (ctx: DbConnection) (kanjiElementId: int) (priorities: string list) =
    for p in priorities do
        let param: KanjiElementPriority = {
            KanjiElementId = kanjiElementId
            Value = p
        }
        ctx.Execute(
            "INSERT INTO KanjiElementPriorities ('KanjiElementId', Value) VALUES (@KanjiElementId, @Value)",
            param
        ) |> ignore

let populateKanjiElementInformation (ctx: DbConnection) (kanjiElementId: int) (information: string list) =
    for i in information do
        let param: KanjiElementInformation = {
            KanjiElementId = kanjiElementId
            Value = i
        }
        ctx.Execute(
            "INSERT INTO KanjiElementInformation ('KanjiElementId', Value) VALUES (@KanjiElementId, @Value)",
            param
        ) |> ignore

let populateKanjiElements (ctx: DbConnection) (entryId: int) (kanjiElements: Domain.KanjiElement list) =
    for k in kanjiElements do
        let param: KanjiElement = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
            Value = k.Value
        }
        ctx.Execute(
            "INSERT INTO KanjiElements ('EntryId', Value) VALUES (@EntryId, @Value)",
            param
        ) |> ignore
        let id = getLastRowId ctx
        populateKanjiElementPriorities ctx id k.Priority
        populateKanjiElementInformation ctx id k.Information

let populateReadingElementPriorities (ctx: DbConnection) (readingElementId: int) (priorities: string list) =
    for p in priorities do
        let param: ReadingElementPriority = {
            ReadingElementId = readingElementId
            Value = p
        }
        ctx.Execute(
            "INSERT INTO ReadingElementPriorities ('ReadingElementId', Value) VALUES (@ReadingElementId, @Value)",
            param
        ) |> ignore

let populateReadingElementInformation (ctx: DbConnection) (readingElementId: int) (information: string list) =
    for i in information do
        let param: ReadingElementInformation = {
            ReadingElementId = readingElementId
            Value = i
        }
        ctx.Execute(
            "INSERT INTO ReadingElementInformation ('ReadingElementId', Value) VALUES (@ReadingElementId, @Value)",
            param
        ) |> ignore

let populateReadingElementRestrictions (ctx: DbConnection) (readingElementId: int) (restrictions: string list) =
    for r in restrictions do
        let param: ReadingElementRestriction = {
            ReadingElementId = readingElementId
            Value = r
        }
        ctx.Execute(
            "INSERT INTO ReadingElementRestrictions ('ReadingElementId', Value) VALUES (@ReadingElementId, @Value)",
            param
        ) |> ignore

let populateReadingElements (ctx: DbConnection) (entryId: int) (readingElements: Domain.ReadingElement list) =
    for r in readingElements do
        let param: ReadingElement = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
            Value = r.Value
            IsTrueReading = r.IsTrueReading
        }
        ctx.Execute(
            "INSERT INTO ReadingElements ('EntryId', Value, 'IsTrueReading') VALUES (@EntryId, @Value, @IsTrueReading)",
            param
        ) |> ignore
        let id = getLastRowId ctx
        populateReadingElementPriorities ctx id r.Priority
        populateReadingElementInformation ctx id r.Information
        populateReadingElementRestrictions ctx id r.Restrictions

let populateAntonyms (ctx: DbConnection) (senseId: int) (antonyms: Domain.Antonym list) =
    for a in antonyms do
        let param: Antonym = {
            SenseId = senseId
            ReferenceKanjiElement = a.Kanji
            ReferenceReadingElement = a.Reading
        }
        ctx.Execute(
            "INSERT INTO Antonyms ('SenseId', 'ReferenceKanjiElement', 'ReferenceReadingElement') VALUES(@SenseId, @ReferenceKanjiElement, @ReferenceReadingElement)",
            param
        ) |> ignore

let populateFields (ctx: DbConnection) (senseId: int) (fields: string list) =
    for f in fields do
        let param: Field = {
            SenseId = senseId
            Value = f
        }
        ctx.Execute(
            "INSERT INTO Fields ('SenseId', Value) VALUES (@SenseId, @Value)",
            param
        ) |> ignore

let populateDialects (ctx: DbConnection) (senseId: int) (dialects: string list) =
    for d in dialects do
        let param: Dialect = {
            SenseId = senseId
            Value = d
        }
        ctx.Execute(
            "INSERT INTO Dialects ('SenseId', Value) VALUES (@SenseId, @Value)",
            param
        ) |> ignore

let populateMiscellaneousInformation (ctx: DbConnection) (senseId: int) (miscellaneousInformation: string list) =
    for m in miscellaneousInformation do
        let param: MiscellaneousInformation = {
            SenseId = senseId
            Value = m
        }
        ctx.Execute(
            "INSERT INTO MiscellaneousInformation ('SenseId', Value) VALUES (@SenseId, @Value)",
            param
        ) |> ignore

let populateAdditionalInformation (ctx: DbConnection) (senseId: int) (additionalInformation: string list) =
    for a in additionalInformation do
        let param: SenseInformation = {
            SenseId = senseId
            Value = a
        }
        ctx.Execute(
            "INSERT INTO SenseInformation ('SenseId', Value) VALUES (@SenseId, @Value)",
            param
        ) |> ignore

let populateLanguageSources (ctx: DbConnection) (senseId: int) (languageSources: Domain.LanguageSource list) =
    for l in languageSources do
        let param: LanguageSource = {
            SenseId = senseId
            Value = l.Value
            LanguageCode = l.Code
            IsPartial = l.IsPartial
            IsWasei = l.IsWasei
        }
        ctx.Execute(
            "INSERT INTO LanguageSources ('SenseId', Value, 'LanguageCode', 'IsPartial', 'IsWasei') VALUES (@SenseId, @Value, @LanguageCode, @IsPartial, @IsWasei)",
            param
        ) |> ignore

let populatePartsOfSpeech (ctx: DbConnection) (senseId: int) (partsOfSpeech: string list) =
    for p in partsOfSpeech do
        let param: PartOfSpeech = {
            SenseId = senseId
            Value = p
        }
        ctx.Execute(
            "INSERT INTO PartsOfSpeech ('SenseId', Value) VALUES (@SenseId, @Value)",
            param
        ) |> ignore

let populateGlosses (ctx: DbConnection) (senseId: int) (glosses: Domain.Gloss list) =
    for g in glosses do
        let param: Gloss = {
            SenseId = senseId
            Value = g.Value
            Language = g.LanguageCode
            Type = g.Type
        }
        ctx.Execute(
            "INSERT INTO Glosses ('SenseId', Value, 'Language', 'Type') VALUES (@SenseId, @Value, @Language, @Type)",
            param
        ) |> ignore

let populateSenseKanjiElementRestrictions (ctx: DbConnection) (senseId: int) (restrictions: string list) =
    for r in restrictions do
        let param: SenseKanjiElementRestriction = {
            SenseId = senseId
            KanjiElement = r
        }
        ctx.Execute(
            "INSERT INTO SenseKanjiElementRestrictions ('SenseId', 'KanjiElement') VALUES (@SenseId, @KanjiElement)",
            param
        ) |> ignore

let populateSenseReadingElementRestrictions (ctx: DbConnection) (senseId: int) (restrictions: string list) =
    for r in restrictions do
        let param: SenseReadingElementRestriction = {
            SenseId = senseId
            ReadingElement = r
        }
        ctx.Execute(
            "INSERT INTO SenseReadingElementRestrictions ('SenseId', 'ReadingElement') VALUES (@SenseId, @ReadingElement)",
            param
        ) |> ignore

let populateSenseCrossReferences (ctx: DbConnection) (senseId: int) (crossReferences: Domain.CrossReference list) =
    for c in crossReferences do
        let param: SenseCrossReference = {
            SenseId = senseId
            ReferenceKanjiElement = c.Kanji
            ReferenceReadingElement = c.Reading
            ReferenceSense = c.Index
        }
        ctx.Execute(
            "INSERT INTO SenseCrossReferences ('SenseId', 'ReferenceKanjiElement', 'ReferenceReadingElement', 'ReferenceSense') VALUES (@SenseId, @ReferenceKanjiElement, @ReferenceReadingElement, @ReferenceSense)",
            param
        ) |> ignore

let populateSenses (ctx: DbConnection) (entryId: int) (senses: Domain.Sense list) =
    for s in senses do
        let param: Sense = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
        }
        ctx.Execute(
            "INSERT INTO Senses ('EntryId') VALUES (@EntryId)",
            param
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

let populateJMdictEntries (ctx: DbConnection) (jMdictEntries: Domain.JMdictEntry seq) =
    use transaction = ctx.BeginTransaction()
    for entry in jMdictEntries do
        let param: Entry = {
            Id = entry.Id
            IsProperName = entry.IsProperName
        }
        ctx.Execute(
            "INSERT INTO Entries ('Id', 'IsProperName') VALUES (@Id, @IsProperName)",
            param
        ) |> ignore
        let id = getLastRowId ctx
        populateKanjiElements ctx id entry.KanjiElements
        populateReadingElements ctx id entry.ReadingElements
        populateSenses ctx id entry.Senses
    transaction.Commit()

let populateNameTypes (ctx: DbConnection) (translationId: int) (nameTypes: string list) =
    for n in nameTypes do
        let param: NameType = {
            TranslationId = translationId
            Value = n
        }
        ctx.Execute(
            "INSERT INTO NameTypes ('TranslationId', Value) VALUES (@TranslationId, @Value)",
            param
        ) |> ignore

let populateTranslationCrossReferences (ctx: DbConnection) (translationId: int) (crossReferences: Domain.CrossReference list) =
    for c in crossReferences do
        let param: TranslationCrossReference = {
            TranslationId = translationId
            ReferenceKanjiElement = c.Kanji
            ReferenceReadingElement = c.Reading
            ReferenceTranslation = c.Index
        }
        ctx.Execute(
            "INSERT INTO TranslationCrossReferences ('TranslationId', 'ReferenceKanjiElement', 'ReferenceReadingElement', 'ReferenceTranslation') VALUES (@TranslationId, @ReferenceKanjiElement, @ReferenceReadingElement, @ReferenceTranslation)",
            param
        ) |> ignore

let populateTranslationContents (ctx: DbConnection) (translationId: int) (contents: Domain.TranslationContents list) =
    for c in contents do
        let param: TranslationContent = {
            TranslationId = translationId
            Value = c.Value
            Language = c.LanguageCode
        }
        ctx.Execute(
            "INSERT INTO TranslationContents ('TranslationId', Value, 'Language') VALUES (@TranslationId, @Value, @Language)",
            param
        ) |> ignore

let populateTranslations (ctx: DbConnection) (entryId: int) (translations: Domain.Translation list) =
    for t in translations do
        let param: Translation = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
        }
        ctx.Execute(
            "INSERT INTO Translations ('EntryId') VALUES (@EntryId)",
            param
        ) |> ignore
        let id = getLastRowId ctx
        populateNameTypes ctx id t.NameTypes
        populateTranslationCrossReferences ctx id t.CrossReferences
        populateTranslationContents ctx id t.Contents

let populateJMnedictEntries (ctx: DbConnection) (jMnedictEntries: Domain.JMnedictEntry seq) =
    use transaction = ctx.BeginTransaction()
    for entry in jMnedictEntries do
        let param: Entry = {
            Id = entry.Id
            IsProperName = entry.IsProperName
        }
        ctx.Execute(
            "INSERT INTO Entries ('Id', 'IsProperName') VALUES (@Id, @IsProperName)",
            param
        ) |> ignore
        let id = getLastRowId ctx
        populateKanjiElements ctx id entry.KanjiElements
        populateReadingElements ctx id entry.ReadingElements
        populateTranslations ctx id entry.Translations
    transaction.Commit()

let populateKanjidic2Info (ctx: DbConnection) (info: Domain.Kanjidic2Info) =
    use transaction = ctx.BeginTransaction()
    let param: Kanjidic2Info = {
        FileVersion = info.FileVersion
        DatabaseVersion = info.DatabaseVersion
        DateOfCreation = info.DateOfCreation
    }
    ctx.Execute(
        "INSERT INTO Kanjidic2Info ('FileVersion', 'DatabaseVersion', 'DateOfCreation') VALUES (@FileVersion, @DatabaseVersion, @DateOfCreation)",
        param
    ) |> ignore
    transaction.Commit()

let populateCodepoints (ctx: DbConnection) (characterId: int) (codepoints: Domain.CodePoint list) =
    for c in codepoints do
        let param: Codepoint = {
            CharacterId = characterId
            Value = c.Value
            Type = c.Type
        }
        ctx.Execute(
            "INSERT INTO CodePoints ('CharacterId', Value, 'Type') VALUES (@CharacterId, @Value, @Type)",
            param
        ) |> ignore

let populateKeyRadicals (ctx: DbConnection) (characterId: int) (keyRadicals: Domain.KeyRadical list) =
    for k in keyRadicals do
        let param: KeyRadical = {
            CharacterId = characterId
            Value = k.Value
            Type = k.Type
        }
        ctx.Execute(
            "INSERT INTO KeyRadicals ('CharacterId', Value, 'Type') VALUES (@CharacterId, @Value, @Type)",
            param
        ) |> ignore

let populateStrokeMiscounts (ctx: DbConnection) (characterId: int) (strokeMiscounts: int list) =
    for s in strokeMiscounts do
        let param: StrokeMiscount = {
            CharacterId = characterId
            Value = s
        }
        ctx.Execute(
            "INSERT INTO StrokeMiscounts ('CharacterId', Value) VALUES (@CharacterId, @Value)",
            param
        ) |> ignore

let populateCharacterVariants (ctx: DbConnection) (characterId: int) (characterVariants: Domain.CharacterVariant list) =
    for c in characterVariants do
        let param: CharacterVariant = {
            CharacterId = characterId
            Value = c.Value
            Type = c.Type
        }
        ctx.Execute(
            "INSERT INTO CharacterVariants ('CharacterId', Value, 'Type') VALUES (@CharacterId, @Value, @Type)",
            param
        ) |> ignore

let populateRadicalNames (ctx: DbConnection) (characterId: int) (radicalNames: string list) =
    for r in radicalNames do
        let param: RadicalName = {
            CharacterId = characterId
            Value = r
        }
        ctx.Execute(
            "INSERT INTO RadicalNames ('CharacterId', Value) VALUES (@CharacterId, @Value)",
            param
        ) |> ignore

let populateCharacterDictionaryReferences (ctx: DbConnection) (characterId: int) (references: Domain.DictionaryReference list) =
    for r in references do
        let param: CharacterDictionaryReference = {
            CharacterId = characterId
            IndexNumber = r.IndexNumber
            Type = r.Type
            Volume = r.Volume
            Page = r.Page
        }
        ctx.Execute(
            "INSERT INTO CharacterDictionaryReferences ('CharacterId', 'IndexNumber', 'Type', 'Volume', 'Page') VALUES (@CharacterId, @IndexNumber, @Type, @Volume, @Page)",
            param
        ) |> ignore

let populateCharacterQueryCodes (ctx: DbConnection) (characterId: int) (queryCodes: Domain.QueryCode list) =
    for q in queryCodes do
        let param: CharacterQueryCode = {
            CharacterId = characterId
            Value = q.Value
            Type = q.Type
            SkipMisclassification = q.SkipMisclassification
        }
        ctx.Execute(
            "INSERT INTO CharacterQueryCodes ('CharacterId', Value, 'Type', 'SkipMisclassification') VALUES (@CharacterId, @Value, @Type, @SkipMisclassification)",
            param
        ) |> ignore

let populateCharacterReadings (ctx: DbConnection) (characterId: int) (readings: Domain.CharacterReading list) =
    for r in readings do
        let param: CharacterReading = {
            CharacterId = characterId
            Value = r.Value
            Type = r.Type
        }
        ctx.Execute(
            "INSERT INTO CharacterReadings ('CharacterId', Value, 'Type') VALUES (@CharacterId, @Value, @Type)",
            param
        ) |> ignore

let populateCharacterMeanings (ctx: DbConnection) (characterId: int) (meanings: Domain.CharacterMeaning list) =
    for m in meanings do
        let param: CharacterMeaning = {
            CharacterId = characterId
            Value = m.Value
            Language = m.LanguageCode
        }
        ctx.Execute(
            "INSERT INTO CharacterMeanings ('CharacterId', Value, 'Language') VALUES (@CharacterId, @Value, @Language)",
            param
        ) |> ignore

let populateNanori (ctx: DbConnection) (characterId: int) (nanori: string list) =
    for n in nanori do
        let param: Nanori = {
            CharacterId = characterId
            Value = n
        }
        ctx.Execute(
            "INSERT INTO Nanori ('CharacterId', Value) VALUES (@CharacterId, @Value)",
            param
        ) |> ignore

let populateKanjidic2Entries (ctx: DbConnection) (characters: Domain.Character seq) =
    use transaction = ctx.BeginTransaction()
    for c in characters do
        let param: Character = {
            Id = Unchecked.defaultof<_>
            Value = c.Value
            Grade = c.Grade
            StrokeCount = c.StrokeCount
            Frequency = c.Frequency
            IsRadical = c.IsRadical
            OldJlptLevel = c.OldJlptLevel
        }
        ctx.Execute(
            "INSERT INTO Characters (Value, 'Grade', 'StrokeCount', 'Frequency', 'IsRadical', 'OldJlptLevel') VALUES (@Value, @Grade, @StrokeCount, @Frequency, @IsRadical, @OldJlptLevel)",
            param
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
        "SELECT id FROM Characters WHERE Value = @Character",
        {| Character = character |}
    )

let populateCharactersRadicals (ctx: DbConnection) (radicalId: int) (characters: Set<Rune>) =
    for c in characters do
        ctx.Execute(
            "INSERT INTO Characters_Radicals ('CharacterId', 'RadicalId') VALUES (@CharacterId, @RadicalId)",
            { RadicalId = radicalId; CharacterId = getCharacterId ctx c }
        ) |> ignore

let populateRadicals (ctx: DbConnection) (radkEntries: Domain.RadkEntry list) =
    use transaction = ctx.BeginTransaction()
    for entry in radkEntries do
        let param: Radical = {
            Id = Unchecked.defaultof<_>
            Value = entry.Radical
            StrokeCount = entry.StrokeCount
        }
        ctx.Execute(
            "INSERT INTO RADICALS (Value, 'StrokeCount') VALUES (@Value, @StrokeCount)",
            param
        ) |> ignore
        let id = getLastRowId ctx
        populateCharactersRadicals ctx id entry.Kanji
    transaction.Commit()

let populateTables (ctx: DbConnection) =
    let jMdictEntries = DataParsing.getJMdictEntries()
    populateJMdictEntries ctx jMdictEntries
    let jMnedictEntries = DataParsing.getJMnedictEntries ()
    populateJMnedictEntries ctx jMnedictEntries
    let kanjidic2Info = DataParsing.getKanjidic2Info ()
    populateKanjidic2Info ctx kanjidic2Info
    let kanjidic2Entries = DataParsing.getKanjidic2Entries ()
    populateKanjidic2Entries ctx kanjidic2Entries
    let radkEntries = DataParsing.getRadkEntries ()
    populateRadicals ctx radkEntries

type OptionHandler<'T>() =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value
        then None
        else Some (value :?> 'T)

type RuneHandler() =
    inherit SqlMapper.TypeHandler<Rune>()

    override this.SetValue(param, value) =
        param.Value <- string value

    override this.Parse(value) =
        rune value

type Int32Handler() =
    inherit SqlMapper.TypeHandler<int32>()

    override this.SetValue(param, value) =
        param.Value <- value

    override this.Parse value =
        int (value :?> int64)

type Int32OptionHandler() =
    inherit SqlMapper.TypeHandler<option<int32>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value
        then None
        else Some (int (value :?> int64))

let registerTypeHandlers () =
    SqlMapper.AddTypeHandler(OptionHandler<string>())
    SqlMapper.AddTypeHandler(RuneHandler())
    SqlMapper.AddTypeHandler(Int32Handler())
    SqlMapper.AddTypeHandler(Int32OptionHandler())
