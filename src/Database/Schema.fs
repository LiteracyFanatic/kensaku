module Kensaku.Database.Schema

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Reflection
open System.Data.Common
open Dapper
open Microsoft.Data.Sqlite
open Kensaku
open Kensaku.Database.Tables

let createSchema (ctx: DbConnection) =
    let stream =
        Assembly.GetExecutingAssembly().GetManifestResourceStream("Database.sql.schema.sql")

    use sr = new StreamReader(stream)
    sr.ReadToEnd() |> ctx.Execute |> ignore

let getLastRowId (ctx: DbConnection) =
    ctx.QuerySingle<int>(sql "select last_insert_rowid()")

let populateEquivalentCharacters (ctx: DbConnection) (variantGroups: Set<Rune> list) =
    use transaction = ctx.BeginTransaction()

    for group in variantGroups do
        let param = {| Group = group |> Set.toList |> List.map string |> String.concat "" |}

        ctx.Execute(sql "insert into EquivalentCharacters ('Characters') values (@Group)", param)
        |> ignore

    transaction.Commit()

let populateKanjiElementPriorities (ctx: DbConnection) (kanjiElementId: int) (priorities: string list) =
    for p in priorities do
        let param: KanjiElementPriority = {
            KanjiElementId = kanjiElementId
            Value = p
        }

        ctx.Execute(
            sql
                "insert into KanjiElementPriorities ('KanjiElementId', Value) values (@KanjiElementId, @Value) on conflict do nothing",
            param
        )
        |> ignore

let populateKanjiElementInformation (ctx: DbConnection) (kanjiElementId: int) (information: string list) =
    for i in information do
        let param: KanjiElementInformation = {
            KanjiElementId = kanjiElementId
            Value = i
        }

        ctx.Execute(
            sql
                "insert into KanjiElementInformation ('KanjiElementId', Value) values (@KanjiElementId, @Value) on conflict do nothing",
            param
        )
        |> ignore

let populateKanjiElements (ctx: DbConnection) (entryId: int) (kanjiElements: Domain.KanjiElement list) =
    for k in kanjiElements do
        let param: KanjiElement = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
            Value = k.Value
        }

        let id =
            ctx.ExecuteScalar<int>(
                sql
                    "
                insert into KanjiElements ('EntryId', Value)
                values (@EntryId, @Value)
                on conflict do update
                set EntryId = excluded.EntryId, Value = excluded.Value
                returning Id",
                param
            )

        populateKanjiElementPriorities ctx id k.Priority
        populateKanjiElementInformation ctx id k.Information

let populateReadingElementPriorities (ctx: DbConnection) (readingElementId: int) (priorities: string list) =
    for p in priorities do
        let param: ReadingElementPriority = {
            ReadingElementId = readingElementId
            Value = p
        }

        ctx.Execute(
            sql
                "insert into ReadingElementPriorities ('ReadingElementId', Value) values (@ReadingElementId, @Value) on conflict do nothing",
            param
        )
        |> ignore

let populateReadingElementInformation (ctx: DbConnection) (readingElementId: int) (information: string list) =
    for i in information do
        let param: ReadingElementInformation = {
            ReadingElementId = readingElementId
            Value = i
        }

        ctx.Execute(
            sql
                "insert into ReadingElementInformation ('ReadingElementId', Value) values (@ReadingElementId, @Value) on conflict do nothing",
            param
        )
        |> ignore

let populateReadingElementRestrictions (ctx: DbConnection) (readingElementId: int) (restrictions: string list) =
    for r in restrictions do
        let param: ReadingElementRestriction = {
            ReadingElementId = readingElementId
            Value = r
        }

        ctx.Execute(
            sql
                "insert into ReadingElementRestrictions ('ReadingElementId', Value) values (@ReadingElementId, @Value) on conflict do nothing",
            param
        )
        |> ignore

let populateReadingElements (ctx: DbConnection) (entryId: int) (readingElements: Domain.ReadingElement list) =
    for r in readingElements do
        let param: ReadingElement = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
            Value = r.Value
            IsTrueReading = r.IsTrueReading
        }

        let id =
            ctx.ExecuteScalar<int>(
                sql
                    "
                insert into ReadingElements ('EntryId', Value, 'IsTrueReading')
                values (@EntryId, @Value, @IsTrueReading)
                on conflict do update
                set EntryId = excluded.EntryId, Value = excluded.Value, 'IsTrueReading' = excluded.'IsTrueReading'
                returning Id",
                param
            )

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
            sql
                "insert into Antonyms ('SenseId', 'ReferenceKanjiElement', 'ReferenceReadingElement') values(@SenseId, @ReferenceKanjiElement, @ReferenceReadingElement)",
            param
        )
        |> ignore

let populateFields (ctx: DbConnection) (senseId: int) (fields: string list) =
    for f in fields do
        let param: Field = {
            SenseId = senseId
            Value = f
        }

        ctx.Execute(sql "insert into Fields ('SenseId', Value) values (@SenseId, @Value)", param)
        |> ignore

let populateDialects (ctx: DbConnection) (senseId: int) (dialects: string list) =
    for d in dialects do
        let param: Dialect = {
            SenseId = senseId
            Value = d
        }

        ctx.Execute(sql "insert into Dialects ('SenseId', Value) values (@SenseId, @Value)", param)
        |> ignore

let populateMiscellaneousInformation (ctx: DbConnection) (senseId: int) (miscellaneousInformation: string list) =
    for m in miscellaneousInformation do
        let param: MiscellaneousInformation = {
            SenseId = senseId
            Value = m
        }

        ctx.Execute(sql "insert into MiscellaneousInformation ('SenseId', Value) values (@SenseId, @Value)", param)
        |> ignore

let populateAdditionalInformation (ctx: DbConnection) (senseId: int) (additionalInformation: string list) =
    for a in additionalInformation do
        let param: SenseInformation = {
            SenseId = senseId
            Value = a
        }

        ctx.Execute(sql "insert into SenseInformation ('SenseId', Value) values (@SenseId, @Value)", param)
        |> ignore

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
            sql
                "insert into LanguageSources ('SenseId', Value, 'LanguageCode', 'IsPartial', 'IsWasei') values (@SenseId, @Value, @LanguageCode, @IsPartial, @IsWasei)",
            param
        )
        |> ignore

let populatePartsOfSpeech (ctx: DbConnection) (senseId: int) (partsOfSpeech: string list) =
    for p in partsOfSpeech do
        let param: PartOfSpeech = {
            SenseId = senseId
            Value = p
        }

        ctx.Execute(sql "insert into PartsOfSpeech ('SenseId', Value) values (@SenseId, @Value)", param)
        |> ignore

let populateGlosses (ctx: DbConnection) (senseId: int) (glosses: Domain.Gloss list) =
    for g in glosses do
        let param: Gloss = {
            SenseId = senseId
            Value = g.Value
            Language = g.LanguageCode
            Type = g.Type
        }

        ctx.Execute(
            sql "insert into Glosses ('SenseId', Value, 'Language', 'Type') values (@SenseId, @Value, @Language, @Type)",
            param
        )
        |> ignore

let populateSenseKanjiElementRestrictions (ctx: DbConnection) (senseId: int) (restrictions: string list) =
    for r in restrictions do
        let param: SenseKanjiElementRestriction = {
            SenseId = senseId
            KanjiElement = r
        }

        ctx.Execute(
            sql "insert into SenseKanjiElementRestrictions ('SenseId', 'KanjiElement') values (@SenseId, @KanjiElement)",
            param
        )
        |> ignore

let populateSenseReadingElementRestrictions (ctx: DbConnection) (senseId: int) (restrictions: string list) =
    for r in restrictions do
        let param: SenseReadingElementRestriction = {
            SenseId = senseId
            ReadingElement = r
        }

        ctx.Execute(
            sql
                "insert into SenseReadingElementRestrictions ('SenseId', 'ReadingElement') values (@SenseId, @ReadingElement)",
            param
        )
        |> ignore

let populateSenseCrossReferences (ctx: DbConnection) (senseId: int) (crossReferences: Domain.CrossReference list) =
    for c in crossReferences do
        let param: SenseCrossReference = {
            SenseId = senseId
            ReferenceKanjiElement = c.Kanji
            ReferenceReadingElement = c.Reading
            ReferenceSense = c.Index
        }

        ctx.Execute(
            sql
                "insert into SenseCrossReferences ('SenseId', 'ReferenceKanjiElement', 'ReferenceReadingElement', 'ReferenceSense') values (@SenseId, @ReferenceKanjiElement, @ReferenceReadingElement, @ReferenceSense)",
            param
        )
        |> ignore

let populateSenses (ctx: DbConnection) (entryId: int) (senses: Domain.Sense list) =
    for s in senses do
        let param: Sense = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
        }

        ctx.Execute(sql "insert into Senses ('EntryId') values (@EntryId)", param)
        |> ignore

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

        ctx.Execute(sql "insert into Entries ('Id', 'IsProperName') values (@Id, @IsProperName)", param)
        |> ignore

        populateKanjiElements ctx entry.Id entry.KanjiElements
        populateReadingElements ctx entry.Id entry.ReadingElements
        populateSenses ctx entry.Id entry.Senses

    transaction.Commit()

let populateNameTypes (ctx: DbConnection) (translationId: int) (nameTypes: string list) =
    for n in nameTypes do
        let param: NameType = {
            TranslationId = translationId
            Value = n
        }

        ctx.Execute(sql "insert into NameTypes ('TranslationId', Value) values (@TranslationId, @Value)", param)
        |> ignore

let populateTranslationCrossReferences
    (ctx: DbConnection)
    (translationId: int)
    (crossReferences: Domain.CrossReference list)
    =
    for c in crossReferences do
        let param: TranslationCrossReference = {
            TranslationId = translationId
            ReferenceKanjiElement = c.Kanji
            ReferenceReadingElement = c.Reading
            ReferenceTranslation = c.Index
        }

        ctx.Execute(
            sql
                "insert into TranslationCrossReferences ('TranslationId', 'ReferenceKanjiElement', 'ReferenceReadingElement', 'ReferenceTranslation') values (@TranslationId, @ReferenceKanjiElement, @ReferenceReadingElement, @ReferenceTranslation)",
            param
        )
        |> ignore

let populateTranslationContents (ctx: DbConnection) (translationId: int) (contents: Domain.TranslationContents list) =
    for c in contents do
        let param: TranslationContent = {
            TranslationId = translationId
            Value = c.Value
            Language = c.LanguageCode
        }

        ctx.Execute(
            sql
                "insert into TranslationContents ('TranslationId', Value, 'Language') values (@TranslationId, @Value, @Language)",
            param
        )
        |> ignore

let populateTranslations (ctx: DbConnection) (entryId: int) (translations: Domain.Translation list) =
    for t in translations do
        let param: Translation = {
            Id = Unchecked.defaultof<_>
            EntryId = entryId
        }

        ctx.Execute(sql "insert into Translations ('EntryId') values (@EntryId)", param)
        |> ignore

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
            sql "insert into Entries ('Id', 'IsProperName') values (@Id, @IsProperName) on conflict do nothing",
            param
        )
        |> ignore

        populateKanjiElements ctx entry.Id entry.KanjiElements
        populateReadingElements ctx entry.Id entry.ReadingElements
        populateTranslations ctx entry.Id entry.Translations

    transaction.Commit()

let populateKanjidic2Info (ctx: DbConnection) (info: Domain.Kanjidic2Info) =
    use transaction = ctx.BeginTransaction()

    let param: Kanjidic2Info = {
        FileVersion = info.FileVersion
        DatabaseVersion = info.DatabaseVersion
        DateOfCreation = info.DateOfCreation
    }

    ctx.Execute(
        sql
            "insert into Kanjidic2Info ('FileVersion', 'DatabaseVersion', 'DateOfCreation') values (@FileVersion, @DatabaseVersion, @DateOfCreation)",
        param
    )
    |> ignore

    transaction.Commit()

let populateCodepoints (ctx: DbConnection) (characterId: int) (codepoints: Domain.CodePoint list) =
    for c in codepoints do
        let param: Codepoint = {
            CharacterId = characterId
            Value = c.Value
            Type = c.Type
        }

        ctx.Execute(
            sql "insert into CodePoints ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
            param
        )
        |> ignore

let populateKeyRadicals (ctx: DbConnection) (characterId: int) (keyRadicals: Domain.KeyRadical list) =
    for k in keyRadicals do
        let param: KeyRadical = {
            CharacterId = characterId
            Value = k.Value
            Type = k.Type
        }

        ctx.Execute(
            sql "insert into KeyRadicals ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
            param
        )
        |> ignore

let populateStrokeMiscounts (ctx: DbConnection) (characterId: int) (strokeMiscounts: int list) =
    for s in strokeMiscounts do
        let param: StrokeMiscount = {
            CharacterId = characterId
            Value = s
        }

        ctx.Execute(sql "insert into StrokeMiscounts ('CharacterId', Value) values (@CharacterId, @Value)", param)
        |> ignore

let populateCharacterVariants (ctx: DbConnection) (characterId: int) (characterVariants: Domain.CharacterVariant list) =
    for c in characterVariants do
        let param: CharacterVariant = {
            CharacterId = characterId
            Value = c.Value
            Type = c.Type
        }

        ctx.Execute(
            sql "insert into CharacterVariants ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
            param
        )
        |> ignore

let populateRadicalNames (ctx: DbConnection) (characterId: int) (radicalNames: string list) =
    for r in radicalNames do
        let param: RadicalName = {
            CharacterId = characterId
            Value = r
        }

        ctx.Execute(sql "insert into RadicalNames ('CharacterId', Value) values (@CharacterId, @Value)", param)
        |> ignore

let populateCharacterDictionaryReferences
    (ctx: DbConnection)
    (characterId: int)
    (references: Domain.DictionaryReference list)
    =
    for r in references do
        let param: CharacterDictionaryReference = {
            CharacterId = characterId
            IndexNumber = r.IndexNumber
            Type = r.Type
            Volume = r.Volume
            Page = r.Page
        }

        ctx.Execute(
            sql
                "insert into CharacterDictionaryReferences ('CharacterId', 'IndexNumber', 'Type', 'Volume', 'Page') values (@CharacterId, @IndexNumber, @Type, @Volume, @Page)",
            param
        )
        |> ignore

let populateCharacterQueryCodes (ctx: DbConnection) (characterId: int) (queryCodes: Domain.QueryCode list) =
    for q in queryCodes do
        let param: CharacterQueryCode = {
            CharacterId = characterId
            Value = q.Value
            Type = q.Type
            SkipMisclassification = q.SkipMisclassification
        }

        ctx.Execute(
            sql
                "insert into CharacterQueryCodes ('CharacterId', Value, 'Type', 'SkipMisclassification') values (@CharacterId, @Value, @Type, @SkipMisclassification)",
            param
        )
        |> ignore

let populateCharacterReadings (ctx: DbConnection) (characterId: int) (readings: Domain.CharacterReading list) =
    for r in readings do
        let param: CharacterReading = {
            CharacterId = characterId
            Value = r.Value
            Type = r.Type
        }

        ctx.Execute(
            sql "insert into CharacterReadings ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
            param
        )
        |> ignore

let populateCharacterMeanings (ctx: DbConnection) (characterId: int) (meanings: Domain.CharacterMeaning list) =
    for m in meanings do
        let param: CharacterMeaning = {
            CharacterId = characterId
            Value = m.Value
            Language = m.LanguageCode
        }

        ctx.Execute(
            sql
                "insert into CharacterMeanings ('CharacterId', Value, 'Language') values (@CharacterId, @Value, @Language)",
            param
        )
        |> ignore

let populateNanori (ctx: DbConnection) (characterId: int) (nanori: string list) =
    for n in nanori do
        let param: Nanori = {
            CharacterId = characterId
            Value = n
        }

        ctx.Execute(sql "insert into Nanori ('CharacterId', Value) values (@CharacterId, @Value)", param)
        |> ignore

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
            sql
                "insert into Characters (Value, 'Grade', 'StrokeCount', 'Frequency', 'IsRadical', 'OldJlptLevel') values (@Value, @Grade, @StrokeCount, @Frequency, @IsRadical, @OldJlptLevel)",
            param
        )
        |> ignore

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
    let id =
        ctx.ExecuteScalar<int>(sql "select id FROM Characters WHERE Value = @Character", {| Character = character |})

    if id = 0 then
        printfn $"%A{character} does not exist"
        None
    else
        Some id

let populateCharactersRadicals (ctx: DbConnection) (radicalId: int) (characters: Set<Rune>) =
    for c in characters do
        getCharacterId ctx c
        |> Option.iter (fun characterId ->
            ctx.Execute(
                sql
                    "insert or ignore into Characters_Radicals ('CharacterId', 'RadicalId') values (@CharacterId, @RadicalId)",
                {
                    RadicalId = radicalId
                    CharacterId = characterId
                }
            )
            |> ignore)

let populateRadicalValues (ctx: DbConnection) (radicalId: int) (radicalValues: (Rune * string) list) =
    for r, t in radicalValues do
        let param: RadicalValue = {
            RadicalId = radicalId
            Value = r
            Type = t
        }

        ctx.Execute(
            sql "insert or ignore into RadicalValues (RadicalId, Value, Type) values (@RadicalId, @Value, @Type)",
            param
        )
        |> ignore

let populateRadicalMeanings (ctx: DbConnection) (radicalId: int) (radicalMeanings: (string * string) list) =
    for m, t in radicalMeanings do
        let param: RadicalMeaning = {
            RadicalId = radicalId
            Value = m
            Type = t
        }

        ctx.Execute(
            sql "insert or ignore into RadicalMeanings (RadicalId, Value, Type) values (@RadicalId, @Value, @Type)",
            param
        )
        |> ignore

let populateCJKRadicals (ctx: DbConnection) (getVariants: Rune -> Set<Rune>) (cjkRadicals: Domain.CJKRadical list) =
    use transaction = ctx.BeginTransaction()

    for radical in cjkRadicals do
        let param: Radical = {
            Id = Unchecked.defaultof<_>
            Number = Some radical.RadicalNumber
            StrokeCount = Unchecked.defaultof<_>
        }

        ctx.Execute(sql "insert into Radicals (Number, StrokeCount) values (@Number, @StrokeCount)", param)
        |> ignore

        let id = getLastRowId ctx

        let radicalValues =
            [
                radical.Standard.RadicalCharacter
                radical.Standard.UnifiedIdeographCharacter
                match radical.Simplified with
                | Some simplified ->
                    simplified.RadicalCharacter
                    simplified.UnifiedIdeographCharacter
                | None -> ()
            ]
            |> List.map getVariants
            |> List.reduce Set.union
            |> Set.toList
            |> List.map (fun x -> x, "CJKRadicals")

        populateRadicalValues ctx id radicalValues

    transaction.Commit()

let populateRadicals (ctx: DbConnection) (getVariants: Rune -> Set<Rune>) (radkEntries: Domain.RadkEntry list) =
    use transaction = ctx.BeginTransaction()

    for entry in radkEntries do
        let param = {|
            RadicalNumber =
                match DataParsing.charToRadicalNumber.TryFind entry.Radical with
                | Some 0
                | None -> None
                | n -> n
            RadicalValue = entry.Radical
        |}

        let existingRadicalId =
            ctx.Query<int>(
                sql
                    "
                select r.Id
                from Radicals as r
                where exists (
                    select *
                    from RadicalValues as rv
                    where
                        rv.RadicalId = r.Id and (
                            rv.Value = @RadicalValue
                            or (@RadicalNumber is not null and @RadicalNumber = r.Number)
                        )
                )",
                param
            )
            |> Seq.tryHead

        let radicalId =
            match existingRadicalId with
            | Some existingRadicalId ->
                let param = {|
                    RadicalId = existingRadicalId
                    StrokeCount = entry.StrokeCount
                |}

                ctx.Execute(
                    sql
                        "
                    update Radicals
                    set StrokeCount = @StrokeCount
                    where Id = @RadicalId",
                    param
                )
                |> ignore

                entry.Radical
                |> getVariants
                |> Set.toList
                |> List.map (fun x -> x, "radk")
                |> populateRadicalValues ctx existingRadicalId

                existingRadicalId
            | None ->
                let param = {| StrokeCount = entry.StrokeCount |}

                ctx.Execute(sql "insert into Radicals (StrokeCount) values (@StrokeCount)", param)
                |> ignore

                let radicalId = getLastRowId ctx

                entry.Radical
                |> getVariants
                |> Set.toList
                |> List.map (fun x -> x, "radk")
                |> populateRadicalValues ctx radicalId

                radicalId

        populateCharactersRadicals ctx radicalId entry.Kanji

    transaction.Commit()

let populateWaniKaniRadicals
    (ctx: DbConnection)
    (getVariants: Rune -> Set<Rune>)
    (waniKaniRadicals: DataFiles.WaniKaniData<DataFiles.WaniKaniRadical> list)
    (waniKaniKanji: DataFiles.WaniKaniData<DataFiles.WaniKaniKanji> list)
    =
    use transaction = ctx.BeginTransaction()

    for waniKaniRadical in waniKaniRadicals do
        match Option.map rune waniKaniRadical.data.characters with
        | Some radicalValue ->
            let param = {| RadicalValue = radicalValue |}

            let existingRadicalId =
                ctx.Query<int>(
                    sql
                        "
                    select r.Id
                    from Radicals as r
                    where exists (
                        select *
                        from RadicalValues as rv
                        where rv.RadicalId = r.Id and rv.Value = @RadicalValue
                    )",
                    param
                )
                |> Seq.tryHead

            let radicalId =
                match existingRadicalId with
                | Some existingRadicalId ->
                    radicalValue
                    |> getVariants
                    |> Set.toList
                    |> List.map (fun x -> x, "WaniKani")
                    |> populateRadicalValues ctx existingRadicalId

                    existingRadicalId
                | None ->
                    let param = {| StrokeCount = 0 |}

                    ctx.Execute(sql "insert into Radicals (StrokeCount) values (@StrokeCount)", param)
                    |> ignore

                    let radicalId = getLastRowId ctx

                    radicalValue
                    |> getVariants
                    |> Set.toList
                    |> List.map (fun x -> x, "WaniKani")
                    |> populateRadicalValues ctx radicalId

                    radicalId

            [
                yield! waniKaniRadical.data.meanings |> Array.map _.meaning.ToLowerInvariant()
                yield!
                    waniKaniRadical.data.auxiliary_meanings
                    |> Array.map _.meaning.ToLowerInvariant()
            ]
            |> List.map (fun x -> x, "WaniKani")
            |> populateRadicalMeanings ctx radicalId

            waniKaniKanji
            |> List.filter (fun x -> Array.contains x.id waniKaniRadical.data.amalgamation_subject_ids)
            |> List.map (fun x -> rune x.data.characters)
            |> Set.ofList
            |> populateCharactersRadicals ctx radicalId
        | None ->
            // Need to somehow link radicals with only images here
            ()

    transaction.Commit()

let populateDerivedRadicalNames (ctx: DbConnection) (derivedRadicalNames: Map<Rune, string>) =
    use transaction = ctx.BeginTransaction()

    for radicalValue, radicalName in Map.toList derivedRadicalNames do
        let param = {|
            RadicalValue = radicalValue
            RadicalName = radicalName
            Type = "Derived"
        |}

        ctx.Execute(
            sql
                "
            insert or ignore into RadicalMeanings (RadicalId, Value, Type)
            select rv.RadicalId, @RadicalName, @Type
            from RadicalValues as rv
            where rv.Value = @RadicalValue",
            param
        )
        |> ignore

    transaction.Commit()

type OptionHandler<'T>() =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(value :?> 'T)

type RuneHandler() =
    inherit SqlMapper.TypeHandler<Rune>()

    override this.SetValue(param, value) = param.Value <- string value

    override this.Parse(value) = rune value

type RuneOptionHandler() =
    inherit SqlMapper.TypeHandler<option<Rune>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box (string x)
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(rune (value :?> string))

type Int32Handler() =
    inherit SqlMapper.TypeHandler<int32>()

    override this.SetValue(param, value) = param.Value <- value

    override this.Parse value = int (value :?> int64)

type Int32OptionHandler() =
    inherit SqlMapper.TypeHandler<option<int32>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(int (value :?> int64))

let registerTypeHandlers () =
    SqlMapper.AddTypeHandler(OptionHandler<string>())
    SqlMapper.AddTypeHandler(RuneHandler())
    SqlMapper.AddTypeHandler(RuneOptionHandler())
    SqlMapper.AddTypeHandler(Int32Handler())
    SqlMapper.AddTypeHandler(Int32OptionHandler())

let regexpFunction (pattern: string) (input: string) =
    not (isNull input) && Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase)

let registerRegexpFunction (ctx: SqliteConnection) =
    ctx.CreateFunction("regexp", regexpFunction)
