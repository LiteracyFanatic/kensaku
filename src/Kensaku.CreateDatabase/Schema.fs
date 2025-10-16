namespace Kensaku.Core

module Schema =
    open System.IO
    open System.Reflection
    open System.Text

    open Dapper

    open Kensaku
    open Kensaku.Core
    open Kensaku.Core.Utilities

    let sql (input: string) = input

    let createSchema (ctx: KensakuConnection) =
        let stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Kensaku.CreateDatabase.sql.schema.sql")

        use sr = new StreamReader(stream)
        sr.ReadToEnd() |> ctx.Execute |> ignore

    let getLastRowId (ctx: KensakuConnection) =
        ctx.QuerySingle<int>(sql "select last_insert_rowid()")

    let populateEquivalentCharacters (ctx: KensakuConnection) (variantGroups: Set<Rune> list) =
        use transaction = ctx.BeginTransaction()

        for group in variantGroups do
            let param = {| Group = group |> Set.toList |> List.map string |> String.concat "" |}

            ctx.Execute(sql "insert into EquivalentCharacters ('Characters') values (@Group)", param)
            |> ignore

        transaction.Commit()

    let populateKanjiElementPriorities (ctx: KensakuConnection) (kanjiElementId: int) (priorities: string list) =
        for p in priorities do
            let param: Kensaku.Schema.KanjiElementPriority = {
                KanjiElementId = kanjiElementId
                Value = p
            }

            ctx.Execute(
                sql
                    "insert into KanjiElementPriorities ('KanjiElementId', Value) values (@KanjiElementId, @Value) on conflict do nothing",
                param
            )
            |> ignore

    let populateKanjiElementInformation (ctx: KensakuConnection) (kanjiElementId: int) (information: string list) =
        for i in information do
            let param: Kensaku.Schema.KanjiElementInformation = {
                KanjiElementId = kanjiElementId
                Value = i
            }

            ctx.Execute(
                sql
                    "insert into KanjiElementInformation ('KanjiElementId', Value) values (@KanjiElementId, @Value) on conflict do nothing",
                param
            )
            |> ignore

    let populateKanjiElements (ctx: KensakuConnection) (entryId: int) (kanjiElements: DataSources.KanjiElement list) =
        for k in kanjiElements do
            let param: Kensaku.Schema.KanjiElement = {
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

    let populateReadingElementPriorities (ctx: KensakuConnection) (readingElementId: int) (priorities: string list) =
        for p in priorities do
            let param: Kensaku.Schema.ReadingElementPriority = {
                ReadingElementId = readingElementId
                Value = p
            }

            ctx.Execute(
                sql
                    "insert into ReadingElementPriorities ('ReadingElementId', Value) values (@ReadingElementId, @Value) on conflict do nothing",
                param
            )
            |> ignore

    let populateReadingElementInformation (ctx: KensakuConnection) (readingElementId: int) (information: string list) =
        for i in information do
            let param: Kensaku.Schema.ReadingElementInformation = {
                ReadingElementId = readingElementId
                Value = i
            }

            ctx.Execute(
                sql
                    "insert into ReadingElementInformation ('ReadingElementId', Value) values (@ReadingElementId, @Value) on conflict do nothing",
                param
            )
            |> ignore

    let populateReadingElementRestrictions
        (ctx: KensakuConnection)
        (readingElementId: int)
        (restrictions: string list)
        =
        for r in restrictions do
            let param: Kensaku.Schema.ReadingElementRestriction = {
                ReadingElementId = readingElementId
                Value = r
            }

            ctx.Execute(
                sql
                    "insert into ReadingElementRestrictions ('ReadingElementId', Value) values (@ReadingElementId, @Value) on conflict do nothing",
                param
            )
            |> ignore

    let populateReadingElements
        (ctx: KensakuConnection)
        (entryId: int)
        (readingElements: DataSources.ReadingElement list)
        =
        for r in readingElements do
            let param: Kensaku.Schema.ReadingElement = {
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

    let populateAntonyms (ctx: KensakuConnection) (senseId: int) (antonyms: DataSources.Antonym list) =
        for a in antonyms do
            let param: Kensaku.Schema.Antonym = {
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

    let populateFields (ctx: KensakuConnection) (senseId: int) (fields: string list) =
        for f in fields do
            let param: Kensaku.Schema.Field = {
                SenseId = senseId
                Value = f
            }

            ctx.Execute(sql "insert into Fields ('SenseId', Value) values (@SenseId, @Value)", param)
            |> ignore

    let populateDialects (ctx: KensakuConnection) (senseId: int) (dialects: string list) =
        for d in dialects do
            let param: Kensaku.Schema.Dialect = {
                SenseId = senseId
                Value = d
            }

            ctx.Execute(sql "insert into Dialects ('SenseId', Value) values (@SenseId, @Value)", param)
            |> ignore

    let populateMiscellaneousInformation
        (ctx: KensakuConnection)
        (senseId: int)
        (miscellaneousInformation: string list)
        =
        for m in miscellaneousInformation do
            let param: Kensaku.Schema.MiscellaneousInformation = {
                SenseId = senseId
                Value = m
            }

            ctx.Execute(sql "insert into MiscellaneousInformation ('SenseId', Value) values (@SenseId, @Value)", param)
            |> ignore

    let populateAdditionalInformation (ctx: KensakuConnection) (senseId: int) (additionalInformation: string list) =
        for a in additionalInformation do
            let param: Kensaku.Schema.SenseInformation = {
                SenseId = senseId
                Value = a
            }

            ctx.Execute(sql "insert into SenseInformation ('SenseId', Value) values (@SenseId, @Value)", param)
            |> ignore

    let populateLanguageSources
        (ctx: KensakuConnection)
        (senseId: int)
        (languageSources: DataSources.LanguageSource list)
        =
        for l in languageSources do
            let param: Kensaku.Schema.LanguageSource = {
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

    let populatePartsOfSpeech (ctx: KensakuConnection) (senseId: int) (partsOfSpeech: string list) =
        for p in partsOfSpeech do
            let param: Kensaku.Schema.PartOfSpeech = {
                SenseId = senseId
                Value = p
            }

            ctx.Execute(sql "insert into PartsOfSpeech ('SenseId', Value) values (@SenseId, @Value)", param)
            |> ignore

    let populateGlosses (ctx: KensakuConnection) (senseId: int) (glosses: DataSources.Gloss list) =
        for g in glosses do
            let param: Kensaku.Schema.Gloss = {
                SenseId = senseId
                Value = g.Value
                Language = g.LanguageCode
                Type = g.Type
            }

            ctx.Execute(
                sql
                    "insert into Glosses ('SenseId', Value, 'Language', 'Type') values (@SenseId, @Value, @Language, @Type)",
                param
            )
            |> ignore

    let populateSenseKanjiElementRestrictions (ctx: KensakuConnection) (senseId: int) (restrictions: string list) =
        for r in restrictions do
            let param: Kensaku.Schema.SenseKanjiElementRestriction = {
                SenseId = senseId
                KanjiElement = r
            }

            ctx.Execute(
                sql
                    "insert into SenseKanjiElementRestrictions ('SenseId', 'KanjiElement') values (@SenseId, @KanjiElement)",
                param
            )
            |> ignore

    let populateSenseReadingElementRestrictions (ctx: KensakuConnection) (senseId: int) (restrictions: string list) =
        for r in restrictions do
            let param: Kensaku.Schema.SenseReadingElementRestriction = {
                SenseId = senseId
                ReadingElement = r
            }

            ctx.Execute(
                sql
                    "insert into SenseReadingElementRestrictions ('SenseId', 'ReadingElement') values (@SenseId, @ReadingElement)",
                param
            )
            |> ignore

    let populateSenseCrossReferences
        (ctx: KensakuConnection)
        (senseId: int)
        (crossReferences: DataSources.CrossReference list)
        =
        for c in crossReferences do
            let param: Kensaku.Schema.SenseCrossReference = {
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

    let populateSenses (ctx: KensakuConnection) (entryId: int) (senses: DataSources.Sense list) =
        for s in senses do
            let param: Kensaku.Schema.Sense = {
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

    let populateJMdictEntries (ctx: KensakuConnection) (jMdictEntries: DataSources.JMdictEntry seq) =
        use transaction = ctx.BeginTransaction()

        for entry in jMdictEntries do
            let param: Kensaku.Schema.Entry = {
                Id = entry.Id
            }

            ctx.Execute(sql "insert into Entries ('Id') values (@Id)", param)
            |> ignore

            populateKanjiElements ctx entry.Id entry.KanjiElements
            populateReadingElements ctx entry.Id entry.ReadingElements
            populateSenses ctx entry.Id entry.Senses

        transaction.Commit()

    let populateNameTypes (ctx: KensakuConnection) (translationId: int) (nameTypes: string list) =
        for n in nameTypes do
            let param: Kensaku.Schema.NameType = {
                TranslationId = translationId
                Value = n
            }

            ctx.Execute(sql "insert into NameTypes ('TranslationId', Value) values (@TranslationId, @Value)", param)
            |> ignore

    let populateTranslationCrossReferences
        (ctx: KensakuConnection)
        (translationId: int)
        (crossReferences: DataSources.CrossReference list)
        =
        for c in crossReferences do
            let param: Kensaku.Schema.TranslationCrossReference = {
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

    let populateTranslationContents
        (ctx: KensakuConnection)
        (translationId: int)
        (contents: DataSources.TranslationContents list)
        =
        for c in contents do
            let param: Kensaku.Schema.TranslationContent = {
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

    let populateTranslations (ctx: KensakuConnection) (entryId: int) (translations: DataSources.Translation list) =
        for t in translations do
            let param: Kensaku.Schema.Translation = {
                Id = Unchecked.defaultof<_>
                EntryId = entryId
            }

            ctx.Execute(sql "insert into Translations ('EntryId') values (@EntryId)", param)
            |> ignore

            let id = getLastRowId ctx
            populateNameTypes ctx id t.NameTypes
            populateTranslationCrossReferences ctx id t.CrossReferences
            populateTranslationContents ctx id t.Contents

    let populateJMnedictEntries (ctx: KensakuConnection) (jMnedictEntries: DataSources.JMnedictEntry seq) =
        use transaction = ctx.BeginTransaction()

        for entry in jMnedictEntries do
            let param: Kensaku.Schema.Entry = {
                Id = entry.Id
            }

            ctx.Execute(
                sql "insert into Entries ('Id') values (@Id) on conflict do nothing",
                param
            )
            |> ignore

            populateKanjiElements ctx entry.Id entry.KanjiElements
            populateReadingElements ctx entry.Id entry.ReadingElements
            populateTranslations ctx entry.Id entry.Translations

        transaction.Commit()

    let populateKanjidic2Info (ctx: KensakuConnection) (info: DataSources.Kanjidic2Info) =
        use transaction = ctx.BeginTransaction()

        let param: Kensaku.Schema.Kanjidic2Info = {
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

    let populateCodepoints (ctx: KensakuConnection) (characterId: int) (codepoints: DataSources.CodePoint list) =
        for c in codepoints do
            let param: Kensaku.Schema.Codepoint = {
                CharacterId = characterId
                Value = c.Value
                Type = c.Type
            }

            ctx.Execute(
                sql "insert into CodePoints ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
                param
            )
            |> ignore

    let populateKeyRadicals (ctx: KensakuConnection) (characterId: int) (keyRadicals: DataSources.KeyRadical list) =
        for k in keyRadicals do
            let param: Kensaku.Schema.KeyRadical = {
                CharacterId = characterId
                Value = k.Value
                Type = k.Type
            }

            ctx.Execute(
                sql "insert into KeyRadicals ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
                param
            )
            |> ignore

    let populateStrokeMiscounts (ctx: KensakuConnection) (characterId: int) (strokeMiscounts: int list) =
        for s in strokeMiscounts do
            let param: Kensaku.Schema.StrokeMiscount = {
                CharacterId = characterId
                Value = s
            }

            ctx.Execute(sql "insert into StrokeMiscounts ('CharacterId', Value) values (@CharacterId, @Value)", param)
            |> ignore

    let populateCharacterVariants
        (ctx: KensakuConnection)
        (characterId: int)
        (characterVariants: DataSources.CharacterVariant list)
        =
        for c in characterVariants do
            let param: Kensaku.Schema.CharacterVariant = {
                CharacterId = characterId
                Value = c.Value
                Type = c.Type
            }

            ctx.Execute(
                sql "insert into CharacterVariants ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
                param
            )
            |> ignore

    let populateRadicalNames (ctx: KensakuConnection) (characterId: int) (radicalNames: string list) =
        for r in radicalNames do
            let param: Kensaku.Schema.RadicalName = {
                CharacterId = characterId
                Value = r
            }

            ctx.Execute(sql "insert into RadicalNames ('CharacterId', Value) values (@CharacterId, @Value)", param)
            |> ignore

    let populateCharacterDictionaryReferences
        (ctx: KensakuConnection)
        (characterId: int)
        (references: DataSources.DictionaryReference list)
        =
        for r in references do
            let param: Kensaku.Schema.CharacterDictionaryReference = {
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

    let populateCharacterQueryCodes
        (ctx: KensakuConnection)
        (characterId: int)
        (queryCodes: DataSources.QueryCode list)
        =
        for q in queryCodes do
            let param: Kensaku.Schema.CharacterQueryCode = {
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

    let populateCharacterReadings
        (ctx: KensakuConnection)
        (characterId: int)
        (readings: DataSources.CharacterReading list)
        =
        for r in readings do
            let param: Kensaku.Schema.CharacterReading = {
                CharacterId = characterId
                Value = r.Value
                Type = r.Type
            }

            ctx.Execute(
                sql "insert into CharacterReadings ('CharacterId', Value, 'Type') values (@CharacterId, @Value, @Type)",
                param
            )
            |> ignore

    let populateCharacterMeanings
        (ctx: KensakuConnection)
        (characterId: int)
        (meanings: DataSources.CharacterMeaning list)
        =
        for m in meanings do
            let param: Kensaku.Schema.CharacterMeaning = {
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

    let populateNanori (ctx: KensakuConnection) (characterId: int) (nanori: string list) =
        for n in nanori do
            let param: Kensaku.Schema.Nanori = {
                CharacterId = characterId
                Value = n
            }

            ctx.Execute(sql "insert into Nanori ('CharacterId', Value) values (@CharacterId, @Value)", param)
            |> ignore

    let populateKanjidic2Entries (ctx: KensakuConnection) (characters: DataSources.Character seq) =
        use transaction = ctx.BeginTransaction()

        for c in characters do
            let param: Kensaku.Schema.Character = {
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

    let getCharacterId (ctx: KensakuConnection) (character: Rune) =
        let id =
            ctx.ExecuteScalar<int>(
                sql "select id FROM Characters WHERE Value = @Character",
                {| Character = character |}
            )

        if id = 0 then
            printfn $"%A{character} does not exist"
            None
        else
            Some id

    let populateCharactersRadicals (ctx: KensakuConnection) (radicalId: int) (characters: Set<Rune>) =
        for c in characters do
            getCharacterId ctx c
            |> Option.iter (fun characterId ->
                ctx.Execute(
                    sql
                        "insert or ignore into Characters_Radicals ('CharacterId', 'RadicalId') values (@CharacterId, @RadicalId)",
                    {|
                        RadicalId = radicalId
                        CharacterId = characterId
                    |}
                )
                |> ignore)

    let populateRadicalValues (ctx: KensakuConnection) (radicalId: int) (radicalValues: (Rune * string) list) =
        for r, t in radicalValues do
            let param: Kensaku.Schema.RadicalValue = {
                RadicalId = radicalId
                Value = r
                Type = t
            }

            ctx.Execute(
                sql "insert or ignore into RadicalValues (RadicalId, Value, Type) values (@RadicalId, @Value, @Type)",
                param
            )
            |> ignore

    let populateRadicalMeanings (ctx: KensakuConnection) (radicalId: int) (radicalMeanings: (string * string) list) =
        for m, t in radicalMeanings do
            let param: Kensaku.Schema.RadicalMeaning = {
                RadicalId = radicalId
                Value = m
                Type = t
            }

            ctx.Execute(
                sql "insert or ignore into RadicalMeanings (RadicalId, Value, Type) values (@RadicalId, @Value, @Type)",
                param
            )
            |> ignore

    let populateCJKRadicals
        (ctx: KensakuConnection)
        (getVariants: Rune -> Set<Rune>)
        (cjkRadicals: DataSources.CJKRadical list)
        =
        use transaction = ctx.BeginTransaction()

        for radical in cjkRadicals do
            let param: Kensaku.Schema.Radical = {
                Id = Unchecked.defaultof<_>
                Number = Some radical.RadicalNumber
                StrokeCount = Unchecked.defaultof<_>
            }

            ctx.Execute(sql "insert into Radicals (Number, StrokeCount) values (@Number, @StrokeCount)", param)
            |> ignore

            let id = getLastRowId ctx

            let radicalValues =
                [
                    if radical.Standard.RadicalCharacter.IsSome then
                        radical.Standard.RadicalCharacter.Value
                    radical.Standard.UnifiedIdeographCharacter
                    for simplified in radical.Simplified do
                        if simplified.RadicalCharacter.IsSome then
                            simplified.RadicalCharacter.Value

                        simplified.UnifiedIdeographCharacter
                ]
                |> List.map getVariants
                |> List.reduce Set.union
                |> Set.toList
                |> List.map (fun x -> x, "CJKRadicals")

            populateRadicalValues ctx id radicalValues

        transaction.Commit()

    let populateRadicals
        (ctx: KensakuConnection)
        (getVariants: Rune -> Set<Rune>)
        (radkEntries: DataSources.RadkEntry list)
        =
        use transaction = ctx.BeginTransaction()

        for entry in radkEntries do
            let param = {|
                RadicalNumber = DataSources.RadkFile.tryGetRadicalNumber entry.Radical
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
        (ctx: KensakuConnection)
        (getVariants: Rune -> Set<Rune>)
        (waniKaniRadicals: DataSources.WaniKaniData<DataSources.WaniKaniRadical> list)
        (waniKaniKanji: DataSources.WaniKaniData<DataSources.WaniKaniKanji> list)
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

    let populateDerivedRadicalNames (ctx: KensakuConnection) (derivedRadicalNames: Map<Rune, string>) =
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
