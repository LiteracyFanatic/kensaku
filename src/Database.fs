module Database

open System
open System.IO
open System.Text
open Microsoft.Data.Sqlite

let createSchema (connection: SqliteConnection) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- File.ReadAllText("sql/schema.sql")
    cmd.ExecuteNonQuery() |> ignore

let getLastRowId (connection: SqliteConnection) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "SELECT last_insert_rowid()"
    cmd.ExecuteScalar() :?> Int64 |> int

let populateKanjiElementPriorities (connection: SqliteConnection) (kanjiElementId: int) (priorities: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO KanjiElementPriorities ('kanjiElementId', 'value') VALUES (@kanjiElementId, @value)"
    let kanjiElementIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@kanjiElementId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for p in priorities do
        kanjiElementIdParameter.Value <- kanjiElementId
        valueParameter.Value <- p
        cmd.ExecuteNonQuery() |> ignore

let populateKanjiElementInformation (connection: SqliteConnection) (kanjiElementId: int) (information: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO KanjiElementInformation ('kanjiElementId', 'value') VALUES (@kanjiElementId, @value)"
    let kanjiElementIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@kanjiElementId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for i in information do
        kanjiElementIdParameter.Value <- kanjiElementId
        valueParameter.Value <- i
        cmd.ExecuteNonQuery() |> ignore

let populateKanjiElements (connection: SqliteConnection) (entryId: int) (kanjiElements: DataParsing.KanjiElement list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO KanjiElements ('entryId', 'value') VALUES (@entryId, @value)"
    let entryIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@entryId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for k in kanjiElements do
        entryIdParameter.Value <- entryId
        valueParameter.Value <- k.Value
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateKanjiElementPriorities connection id k.Priority
        populateKanjiElementInformation connection id k.Information

let populateReadingElementPriorities (connection: SqliteConnection) (readingElementId: int) (priorities: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO ReadingElementPriorities ('readingElementId', 'value') VALUES (@readingElementId, @value)"
    let readingElementIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@readingElementId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for p in priorities do
        readingElementIdParameter.Value <- readingElementId
        valueParameter.Value <- p
        cmd.ExecuteNonQuery() |> ignore

let populateReadingElementInformation (connection: SqliteConnection) (readingElementId: int) (information: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO ReadingElementInformation ('readingElementId', 'value') VALUES (@readingElementId, @value)"
    let readingElementIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@readingElementId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for i in information do
        readingElementIdParameter.Value <- readingElementId
        valueParameter.Value <- i
        cmd.ExecuteNonQuery() |> ignore

let populateReadingElementRestrictions (connection: SqliteConnection) (readingElementId: int) (restrictions: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO ReadingElementRestrictions ('readingElementId', 'value') VALUES (@readingElementId, @value)"
    let readingElementIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@readingElementId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for r in restrictions do
        readingElementIdParameter.Value <- readingElementId
        valueParameter.Value <- r
        cmd.ExecuteNonQuery() |> ignore

let populateReadingElements (connection: SqliteConnection) (entryId: int) (readingElements: DataParsing.ReadingElement list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO ReadingElements ('entryId', 'value', 'isTrueReading') VALUES (@entryId, @value, @isTrueReading)"
    let entryIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@entryId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let isTrueReadingParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isTrueReading"))
    for r in readingElements do
        entryIdParameter.Value <- entryId
        valueParameter.Value <- r.Value
        isTrueReadingParameter.Value <- r.IsTrueReading
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateReadingElementPriorities connection id r.Priority
        populateReadingElementInformation connection id r.Information
        populateReadingElementRestrictions connection id r.Restrictions

let populateAntonyms (connection: SqliteConnection) (senseId: int) (antonyms: DataParsing.Antonym list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Antonyms ('senseId', 'referenceKanjiElement', 'referenceReadingElement') VALUES
    (@senseId, @referenceKanjiElement, @referenceReadingElement)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let referenceKanjiElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceKanjiElement"))
    let referenceReadingElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceReadingElement"))
    for a in antonyms do
        senseIdParameter.Value <- senseId
        match a.Kanji with
        | Some k -> referenceKanjiElementParameter.Value <- k
        | None -> referenceKanjiElementParameter.Value <- DBNull.Value
        match a.Reading with
        | Some r -> referenceReadingElementParameter.Value <- r
        | None -> referenceReadingElementParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateFields (connection: SqliteConnection) (senseId: int) (fields: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Fields ('senseId', 'value') VALUES (@senseId, @value)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for f in fields do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- f
        cmd.ExecuteNonQuery() |> ignore

let populateDialects (connection: SqliteConnection) (senseId: int) (dialects: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Dialects ('senseId', 'value') VALUES (@senseId, @value)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for d in dialects do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- d
        cmd.ExecuteNonQuery() |> ignore

let populateMiscellaneousInformation (connection: SqliteConnection) (senseId: int) (miscellaneousInformation: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO MiscellaneousInformation ('senseId', 'value') VALUES (@senseId, @value)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for m in miscellaneousInformation do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- m
        cmd.ExecuteNonQuery() |> ignore

let populateAdditionalInformation (connection: SqliteConnection) (senseId: int) (additionalInformation: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO SenseInformation ('senseId', 'value') VALUES (@senseId, @value)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for a in additionalInformation do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- a
        cmd.ExecuteNonQuery() |> ignore

let populateLanguageSources (connection: SqliteConnection) (senseId: int) (languageSources: DataParsing.LanguageSource list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO LanguageSources ('senseId', 'value', 'languageCode', 'isPartial', 'isWasei') VALUES
    (@senseId, @value, @code, @isPartial, @isWasei)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let codeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@code"))
    let isPartialParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isPartial"))
    let isWaseiParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isWasei"))
    for l in languageSources do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- l.Value
        codeParameter.Value <- l.Code
        isPartialParameter.Value <- l.IsPartial
        isWaseiParameter.Value <- l.IsWasei
        cmd.ExecuteNonQuery() |> ignore

let populatePartsOfSpeech (connection: SqliteConnection) (senseId: int) (partsOfSpeech: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO PartsOfSpeech ('senseId', 'value') VALUES (@senseId, @value)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for p in partsOfSpeech do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- p
        cmd.ExecuteNonQuery() |> ignore

let populateGlosses (connection: SqliteConnection) (senseId: int) (glosses: DataParsing.Gloss list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Glosses ('senseId', 'value', 'language', 'type') VALUES
    (@senseId, @value, @language, @type)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let languageParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@language"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    for g in glosses do
        senseIdParameter.Value <- senseId
        valueParameter.Value <- g.Value
        languageParameter.Value <- g.LanguageCode
        match g.Type with
        | Some t -> typeParameter.Value <- t
        | None -> typeParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateSenseKanjiElementRestrictions (connection: SqliteConnection) (senseId: int) (restrictions: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO SenseKanjiElementRestrictions ('senseId', 'kanjiElement') VALUES (@senseId, @kanjiElement)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let kanjiElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@kanjiElement"))
    for r in restrictions do
        senseIdParameter.Value <- senseId
        kanjiElementParameter.Value <- r
        cmd.ExecuteNonQuery() |> ignore

let populateSenseReadingElementRestrictions (connection: SqliteConnection) (senseId: int) (restrictions: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO SenseReadingElementRestrictions ('senseId', 'readingElement') VALUES (@senseId, @readingElement)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let readingElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@readingElement"))
    for r in restrictions do
        senseIdParameter.Value <- senseId
        readingElementParameter.Value <- r
        cmd.ExecuteNonQuery() |> ignore

let populateSenseCrossReferences (connection: SqliteConnection) (senseId: int) (crossReferences: DataParsing.CrossReference list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO SenseCrossReferences ('senseId', 'referenceKanjiElement', 'referenceReadingElement', 'referenceSense') VALUES
    (@senseId, @referenceKanjiElement, @referenceReadingElement, @referenceSense)"
    let senseIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@senseId"))
    let referenceKanjiElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceKanjiElement"))
    let referenceReadingElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceReadingElement"))
    let referenceSenseParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceSense"))
    for c in crossReferences do
        senseIdParameter.Value <- senseId
        match c.Kanji with
        | Some k -> referenceKanjiElementParameter.Value <- k
        | None -> referenceKanjiElementParameter.Value <- DBNull.Value
        match c.Reading with
        | Some r -> referenceReadingElementParameter.Value <- r
        | None -> referenceReadingElementParameter.Value <- DBNull.Value
        match c.Index with
        | Some s -> referenceSenseParameter.Value <- s
        | None -> referenceSenseParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateSenses (connection: SqliteConnection) (entryId: int) (senses: DataParsing.Sense list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Senses ('entryId') VALUES (@entryId)"
    let entryIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@entryId"))
    for s in senses do
        entryIdParameter.Value <- entryId
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateAntonyms connection id s.Antonyms
        populateFields connection id s.Fields
        populateDialects connection id s.Dialects
        populateMiscellaneousInformation connection id s.MiscellaneousInformation
        populateAdditionalInformation connection id s.AdditionalInformation
        populateLanguageSources connection id s.LanguageSources
        populatePartsOfSpeech connection id s.PartsOfSpeech
        populateGlosses connection id s.Glosses
        populateSenseKanjiElementRestrictions connection id s.KanjiRestrictions
        populateSenseReadingElementRestrictions connection id s.ReadingRestrictions
        populateSenseCrossReferences connection id s.CrossReferences

let populateJMdictEntries (connection: SqliteConnection) (jMdictEntries: DataParsing.JMdictEntry seq) =
    use transation = connection.BeginTransaction()
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Entries ('id', 'isProperName') VALUES (@id, @isProperName)"
    let id = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@id"))
    let isProperName = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isProperName"))
    for entry in jMdictEntries do
        id.Value <- entry.Id
        isProperName.Value <- entry.IsProperName
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateKanjiElements connection id entry.KanjiElements
        populateReadingElements connection id entry.ReadingElements
        populateSenses connection id entry.Senses
    transation.Commit()

let populateNameTypes (connection: SqliteConnection) (translationId: int) (nameTypes: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO NameTypes ('translationId', 'value') VALUES (@translationId, @value)"
    let translationIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@translationId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for n in nameTypes do
        translationIdParameter.Value <- translationId
        valueParameter.Value <- n
        cmd.ExecuteNonQuery() |> ignore

let populateTranslationCrossReferences (connection: SqliteConnection) (translationId: int) (crossReferences: DataParsing.CrossReference list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO TranslationCrossReferences ('translationId', 'referenceKanjiElement', 'referenceReadingElement', 'referenceTranslation') VALUES
    (@translationId, @referenceKanjiElement, @referenceReadingElement, @referenceTranslation)"
    let translationIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@translationId"))
    let referenceKanjiElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceKanjiElement"))
    let referenceReadingElementParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceReadingElement"))
    let referenceTranslationParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@referenceTranslation"))
    for c in crossReferences do
        translationIdParameter.Value <- translationId
        match c.Kanji with
        | Some k -> referenceKanjiElementParameter.Value <- k
        | None -> referenceKanjiElementParameter.Value <- DBNull.Value
        match c.Reading with
        | Some r -> referenceReadingElementParameter.Value <- r
        | None -> referenceReadingElementParameter.Value <- DBNull.Value
        match c.Index with
        | Some s -> referenceTranslationParameter.Value <- s
        | None -> referenceTranslationParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateTranslationContents (connection: SqliteConnection) (translationId: int) (contents: DataParsing.TranslationContents list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO TranslationContents ('translationId', 'value', 'language') VALUES (@translationId, @value, @language)"
    let translationIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@translationId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let languageParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@language"))
    for c in contents do
        translationIdParameter.Value <- translationId
        valueParameter.Value <- c.Value
        languageParameter.Value <- c.LanguageCode
        cmd.ExecuteNonQuery() |> ignore

let populateTranslations (connection: SqliteConnection) (entryId: int) (translations: DataParsing.Translation list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Translations ('entryId') VALUES (@entryId)"
    let entryIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@entryId"))
    for t in translations do
        entryIdParameter.Value <- entryId
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateNameTypes connection id t.NameTypes
        populateTranslationCrossReferences connection id t.CrossReferences
        populateTranslationContents connection id t.Contents

let populateJMnedictEntries (connection: SqliteConnection) (jMnedictEntries: DataParsing.JMnedictEntry seq) =
    use transation = connection.BeginTransaction()
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Entries ('id', 'isProperName') VALUES (@id, @isProperName)"
    let id = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@id"))
    let isProperName = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isProperName"))
    for entry in jMnedictEntries do
        id.Value <- entry.Id
        isProperName.Value <- entry.IsProperName
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateKanjiElements connection id entry.KanjiElements
        populateReadingElements connection id entry.ReadingElements
        populateTranslations connection id entry.Translations
    transation.Commit()

let populateKanjidic2Info (connection: SqliteConnection) (info: DataParsing.Kanjidic2Info) =
    use transation = connection.BeginTransaction()
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Kanjidic2Info ('fileVersion', 'databaseVersion', 'dateOfCreation') VALUES
    (@fileVersion, @databaseVersion, @dateOfCreation)"
    let fileVersionParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@fileVersion"))
    let databaseVersionParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@databaseVersion"))
    let dateOfCreationParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@dateOfCreation"))
    fileVersionParameter.Value <- info.FileVersion
    databaseVersionParameter.Value <- info.DatabaseVersion
    dateOfCreationParameter.Value <- info.DateOfCreation
    cmd.ExecuteNonQuery() |> ignore
    transation.Commit()

let populateCodepoints (connection: SqliteConnection) (characterId: int) (codepoints: DataParsing.CodePoint list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Codepoints ('characterId', 'value', 'type') VALUES (@characterId, @value, @type)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    for c in codepoints do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- c.Value
        typeParameter.Value <- c.Type
        cmd.ExecuteNonQuery() |> ignore

let populateKeyRadicals (connection: SqliteConnection) (characterId: int) (keyRadicals: DataParsing.KeyRadical list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO KeyRadicals ('characterId', 'value', 'type') VALUES (@characterId, @value, @type)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    for k in keyRadicals do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- k.Value
        typeParameter.Value <- k.Type
        cmd.ExecuteNonQuery() |> ignore

let populateStrokeMiscounts (connection: SqliteConnection) (characterId: int) (strokeMiscounts: int list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO StrokeMiscounts ('characterId', 'value') VALUES (@characterId, @value)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for s in strokeMiscounts do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- s
        cmd.ExecuteNonQuery() |> ignore

let populateCharacterVariants (connection: SqliteConnection) (characterId: int) (characterVariants: DataParsing.CharacterVariant list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO CharacterVariants ('characterId', 'value', 'type') VALUES (@characterId, @value, @type)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    for c in characterVariants do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- c.Value
        typeParameter.Value <- c.Type
        cmd.ExecuteNonQuery() |> ignore

let populateRadicalNames (connection: SqliteConnection) (characterId: int) (radicalNames: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO RadicalNames ('characterId', 'value') VALUES (@characterId, @value)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for r in radicalNames do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- r
        cmd.ExecuteNonQuery() |> ignore

let populateCharacterDictionaryReferences (connection: SqliteConnection) (characterId: int) (references: DataParsing.DictionaryReference list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO CharacterDictionaryReferences
    ('characterId', 'indexNumber', 'type', 'volume', 'page') VALUES
    (@characterId, @indexNumber, @type, @volume, @page)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let indexNumberParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@indexNumber"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    let volumeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@volume"))
    let pageParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@page"))
    for r in references do
        characterIdParameter.Value <- characterId
        indexNumberParameter.Value <- r.IndexNumber
        typeParameter.Value <- r.Type
        match r.Volume with
        | Some v -> volumeParameter.Value <- v
        | None -> volumeParameter.Value <- DBNull.Value
        match r.Page with
        | Some p -> pageParameter.Value <- p
        | None -> pageParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateCharacterQueryCodes (connection: SqliteConnection) (characterId: int) (queryCodes: DataParsing.QueryCode list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO CharacterQueryCodes ('characterId', 'value', 'type', 'skipMisclassification') VALUES
    (@characterId, @value, @type, @skipMisclassification)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    let skipMisclassificationParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@skipMisclassification"))
    for q in queryCodes do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- q.Value
        typeParameter.Value <- q.Type
        match q.SkipMisclassification with
        | Some s -> skipMisclassificationParameter.Value <- s
        | None -> skipMisclassificationParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateCharacterReadings (connection: SqliteConnection) (characterId: int) (readings: DataParsing.CharacterReading list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO CharacterReadings ('characterId', 'value', 'type', 'isJouyou', 'onType') VALUES
    (@characterId, @value, @type, @isJouyou, @onType)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let typeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@type"))
    let isJouyouParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isJouyou"))
    let onTypeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@onType"))
    for r in readings do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- r.Value
        typeParameter.Value <- r.Type
        isJouyouParameter.Value <- r.IsJouyou
        match r.OnType with
        | Some o -> onTypeParameter.Value <- o
        | None -> onTypeParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore

let populateCharacterMeanings (connection: SqliteConnection) (characterId: int) (meanings: DataParsing.CharacterMeaning list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO CharacterMeanings ('characterId', 'value', 'language')
    VALUES (@characterId, @value, @language)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let languageParam = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@language"))
    for m in meanings do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- m.Value
        languageParam.Value <- m.LanguageCode
        cmd.ExecuteNonQuery() |> ignore

let populateNanori (connection: SqliteConnection) (characterId: int) (nanori: string list) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Nanori ('characterId', 'value') VALUES (@characterId, @value)"
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    for n in nanori do
        characterIdParameter.Value <- characterId
        valueParameter.Value <- n
        cmd.ExecuteNonQuery() |> ignore

let populateKanjidic2Entries (connection: SqliteConnection) (characters: DataParsing.Character seq) =
    use transation = connection.BeginTransaction()
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Characters ('value', 'grade', 'strokeCount', 'frequency', 'isRadical', 'oldJlptLevel')
    VALUES (@value, @grade, @strokeCount, @frequency, @isRadical, @oldJlptLevel)"
    let valueParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let gradeParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@grade"))
    let strokeCountParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@strokeCount"))
    let frequencyParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@frequency"))
    let isRadicalParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@isRadical"))
    let oldJlptLevelParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@oldJlptLevel"))
    for c in characters do
        valueParameter.Value <- string c.Value
        match c.Grade with
        | Some g -> gradeParameter.Value <- g
        | None -> gradeParameter.Value <- DBNull.Value
        strokeCountParameter.Value <- c.StokeCount
        match c.Frequency with
        | Some f -> frequencyParameter.Value <- f
        | None -> frequencyParameter.Value <- DBNull.Value
        isRadicalParameter.Value <- c.IsRadical
        match c.OldJlptLevel with
        | Some o -> oldJlptLevelParameter.Value <- o
        | None -> oldJlptLevelParameter.Value <- DBNull.Value
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateCodepoints connection id c.CodePoints
        populateKeyRadicals connection id c.KeyRadicals
        populateStrokeMiscounts connection id c.StrokeMiscounts
        populateCharacterVariants connection id c.Variants
        populateRadicalNames connection id c.RadicalNames
        populateCharacterDictionaryReferences connection id c.DictionaryReferences
        populateCharacterQueryCodes connection id c.QueryCodes
        populateCharacterReadings connection id c.Readings
        populateCharacterMeanings connection id c.Meanings
        populateNanori connection id c.Nanori
    transation.Commit()

let getCharacterId (connection: SqliteConnection) (character: Rune) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "SELECT id FROM Characters WHERE value = @character"
    let characterParam = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@character"))
    characterParam.Value <- string character
    cmd.ExecuteScalar() :?> Int64 |> int

let populateCharactersRadicals (connection: SqliteConnection) (radicalId: int) (characters: Set<Rune>) =
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO Characters_Radicals ('characterId', 'radicalId') VALUES (@characterId, @radicalId)"
    let radicalIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@radicalId"))
    let characterIdParameter = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@characterId"))
    for c in characters do
        radicalIdParameter.Value <- radicalId
        characterIdParameter.Value <- getCharacterId connection c
        cmd.ExecuteNonQuery() |> ignore

let populateRadicals (connection: SqliteConnection) (radkEntries: DataParsing.RadkEntry list) =
    use transaction = connection.BeginTransaction()
    use cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO RADICALS ('value', 'strokeCount') VALUES (@value, @strokeCount)"
    let value = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let strokeCount = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@strokeCount"))
    for entry in radkEntries do
        value.Value <- string entry.Radical
        strokeCount.Value <- entry.StrokeCount
        cmd.ExecuteNonQuery() |> ignore
        let id = getLastRowId connection
        populateCharactersRadicals connection id entry.Kanji
    transaction.Commit()

let populateTables (connection: SqliteConnection) =
    let jMdictEntries = DataParsing.getJMdictEntries()
    populateJMdictEntries connection jMdictEntries
    let jMnedictEntries = DataParsing.getJMnedictEntries()
    populateJMnedictEntries connection jMnedictEntries
    let kanjidic2Info = DataParsing.getKanjidic2Info ()
    populateKanjidic2Info connection kanjidic2Info
    let kanjidic2Entries = DataParsing.getKanjidic2Entries ()
    populateKanjidic2Entries connection kanjidic2Entries
    let radkEntries = DataParsing.getRadkEntries ()
    populateRadicals connection radkEntries
