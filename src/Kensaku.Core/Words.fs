namespace Kensaku.Core

module Words =
    open System.Threading.Tasks

    open Dapper

    open Kensaku.Core.Domain

    type GetWordQueryResult = {
        Id: int
        KanjiElements: KanjiElement list
        ReadingElements: ReadingElement list
        Senses: Sense list
        Translations: Translation list
    }

    type EntryLabel = {
        Kanji: string option
        Reading: string
    } with

        override this.ToString() =
            this.Kanji
            |> Option.map (fun k -> $"%s{k} 【{this.Reading}】")
            |> Option.defaultValue this.Reading

    type WordForms = {
        Primary: EntryLabel
        Alternate: EntryLabel seq
    }

    let private getReadings (kanji: KanjiElement) (readings: ReadingElement list) =
        readings
        |> List.filter (fun re -> re.Restrictions.IsEmpty || List.contains kanji.Value re.Restrictions)

    let getWordForms (word: GetWordQueryResult) =
        let trueReadings =
            word.ReadingElements
            |> List.filter (fun re ->
                re.IsTrueReading
                && re.Information |> List.contains "search-only kana form" |> not)

        let falseEntries =
            word.ReadingElements
            |> List.filter (fun re -> re.IsTrueReading |> not)
            |> List.map (fun re -> {
                Kanji = None
                Reading = re.Value
            })

        let nonSearchKanji =
            word.KanjiElements
            |> List.filter (fun ke -> ke.Information |> List.contains "search-only kanji form" |> not)

        let kanjiReadingPairs =
            match nonSearchKanji with
            | [] ->
                trueReadings
                |> List.map (fun re -> {
                    Kanji = None
                    Reading = re.Value
                })
            | _ ->
                nonSearchKanji
                |> List.collect (fun ke -> getReadings ke trueReadings |> List.map (fun re -> ke, re))
                |> List.map (fun (ke, re) -> {
                    Kanji = Some ke.Value
                    Reading = re.Value
                })

        {
            Primary = kanjiReadingPairs.Head
            Alternate = (kanjiReadingPairs.Tail @ falseEntries)
        }

    let private getIdsForWordLiteralsAsync (word: string) (ctx: KensakuConnection) =
        ctx.QueryAsync<int>(
            sql
                """
            select EntryId
            from KanjiElements as ke
            where ke.Value = @Word

            union

            select EntryId
            from ReadingElements as re
            where re.Value = @Word""",
            {| Word = word |}
        )

    let private getKanjiElementsAsync (entryId: int) (ctx: KensakuConnection) =
        task {
            let! kanjiElements =
                ctx.QueryAsync<Tables.KanjiElement>(
                    sql
                        """
                select ke.*
                from KanjiElements as ke
                where ke.EntryId = @EntryId
                order by ke.Id""",
                    {| EntryId = entryId |}
                )

            return!
                kanjiElements
                |> Seq.map (fun ke ->
                    task {
                        let! information =
                            ctx.QueryAsync<string>(
                                sql
                                    """
                            select kei.Value
                            from KanjiElementInformation as kei
                            where kei.KanjiElementId = @KanjiElementId""",
                                {| KanjiElementId = ke.Id |}
                            )

                        let! priority =
                            ctx.QueryAsync<string>(
                                sql
                                    """
                            select kep.Value
                            from KanjiElementPriorities as kep
                            where kep.KanjiElementId = @KanjiElementId""",
                                {| KanjiElementId = ke.Id |}
                            )

                        return {
                            Value = ke.Value
                            Information = information |> Seq.toList
                            Priority = priority |> Seq.toList
                        }
                    })
                |> Task.WhenAll
        }

    let private getReadingElementsAsync (entryId: int) (ctx: KensakuConnection) =
        task {
            let! readingElements =
                ctx.QueryAsync<Tables.ReadingElement>(
                    sql
                        """
                select re.*
                from ReadingElements as re
                where re.EntryId = @EntryId
                order by re.Id""",
                    {| EntryId = entryId |}
                )

            return!
                readingElements
                |> Seq.map (fun re ->
                    task {
                        let! restrictions =
                            ctx.QueryAsync<string>(
                                sql
                                    """
                            select rer.Value
                            from ReadingElementRestrictions as rer
                            where rer.ReadingElementId = @ReadingElementId""",
                                {| ReadingElementId = re.Id |}
                            )

                        let! information =
                            ctx.QueryAsync<string>(
                                sql
                                    """
                            select rei.Value
                            from ReadingElementInformation as rei
                            where rei.ReadingElementId = @ReadingElementId""",
                                {| ReadingElementId = re.Id |}
                            )

                        let! priority =
                            ctx.QueryAsync<string>(
                                sql
                                    """
                            select rep.Value
                            from ReadingElementPriorities as rep
                            where rep.ReadingElementId = @ReadingElementId""",
                                {| ReadingElementId = re.Id |}
                            )

                        return {
                            Value = re.Value
                            IsTrueReading = re.IsTrueReading
                            Restrictions = restrictions |> Seq.toList
                            Information = information |> Seq.toList
                            Priority = priority |> Seq.toList
                        }
                    })
                |> Task.WhenAll
        }

    let private getKanjiRestrictionsAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
            select sker.KanjiElement
            from SenseKanjiElementRestrictions as sker
            where sker.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getReadingRestrictionsAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
            select srer.ReadingElement
            from SenseReadingElementRestrictions as srer
            where srer.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getPartsOfSpeechAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
            select pos.Value
            from PartsOfSpeech as pos
            where pos.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getCrossReferencesAsync (senseId: int) (ctx: KensakuConnection) =
        task {
            let! crossReferences =
                ctx.QueryAsync<Tables.SenseCrossReference>(
                    sql
                        """
                select scr.*
                from SenseCrossReferences as scr
                where scr.SenseId = @SenseId""",
                    {| SenseId = senseId |}
                )

            return
                crossReferences
                |> Seq.map (fun scf -> {
                    Kanji = scf.ReferenceKanjiElement
                    Reading = scf.ReferenceReadingElement
                    Index = scf.ReferenceSense
                })
        }

    let private getAntonymsAsync (senseId: int) (ctx: KensakuConnection) =
        task {
            let! antonyms =
                ctx.QueryAsync<Tables.Antonym>(
                    sql
                        """
                select a.*
                from Antonyms as a
                where a.SenseId = @SenseId""",
                    {| SenseId = senseId |}
                )

            return
                antonyms
                |> Seq.map (fun a ->
                    ({
                        Kanji = a.ReferenceKanjiElement
                        Reading = a.ReferenceReadingElement
                    }
                    : Antonym))
        }

    let private getFieldsAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
            select f.Value
            from Fields as f
            where f.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getMiscellaneousInformationAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
                select mi.Value
                from MiscellaneousInformation as mi
                where mi.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getAdditionalInformationAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
                select sai.Value
                from SenseInformation as sai
                where sai.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getLanguageSourcesAsync (senseId: int) (ctx: KensakuConnection) =
        task {
            let! languageSources =
                ctx.QueryAsync<Tables.LanguageSource>(
                    sql
                        """
                select ls.*
                from LanguageSources as ls
                where ls.SenseId = @SenseId""",
                    {| SenseId = senseId |}
                )

            return
                languageSources
                |> Seq.map (fun ls -> {
                    Value = ls.Value
                    Code = ls.LanguageCode
                    IsPartial = ls.IsPartial
                    IsWasei = ls.IsWasei
                })
        }

    let private getDialectsAsync (senseId: int) (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(
            sql
                """
                select d.Value
                from Dialects as d
                where d.SenseId = @SenseId""",
            {| SenseId = senseId |}
        )

    let private getGlossesAsync (senseId: int) (ctx: KensakuConnection) =
        task {
            let! glosses =
                ctx.QueryAsync<Tables.Gloss>(
                    sql
                        """
                select g.*
                from Glosses as g
                where g.SenseId = @SenseId""",
                    {| SenseId = senseId |}
                )

            return
                glosses
                |> Seq.map (fun g -> {
                    Value = g.Value
                    LanguageCode = g.Language
                    Type = g.Type
                })
                |> Seq.toList
        }

    let private getSensesAsync (entryId: int) (ctx: KensakuConnection) =
        task {
            let! senses =
                ctx.QueryAsync<Tables.Sense>(
                    sql
                        """
                select s.*
                from Senses as s
                where s.EntryId = @EntryId""",
                    {| EntryId = entryId |}
                )

            return!
                senses
                |> Seq.map (fun s ->
                    task {
                        let! kanjiRestrictions = getKanjiRestrictionsAsync s.Id ctx
                        let! readingRestrictions = getReadingRestrictionsAsync s.Id ctx
                        let! partsOfSpeech = getPartsOfSpeechAsync s.Id ctx
                        let! crossReferences = getCrossReferencesAsync s.Id ctx
                        let! antonyms = getAntonymsAsync s.Id ctx
                        let! fields = getFieldsAsync s.Id ctx
                        let! miscInfo = getMiscellaneousInformationAsync s.Id ctx
                        let! additionalInfo = getAdditionalInformationAsync s.Id ctx
                        let! languageSources = getLanguageSourcesAsync s.Id ctx
                        let! dialects = getDialectsAsync s.Id ctx
                        let! glosses = getGlossesAsync s.Id ctx

                        return {
                            KanjiRestrictions = kanjiRestrictions |> Seq.toList
                            ReadingRestrictions = readingRestrictions |> Seq.toList
                            PartsOfSpeech = partsOfSpeech |> Seq.toList
                            CrossReferences = crossReferences |> Seq.toList
                            Antonyms = antonyms |> Seq.toList
                            Fields = fields |> Seq.toList
                            MiscellaneousInformation = miscInfo |> Seq.toList
                            AdditionalInformation = additionalInfo |> Seq.toList
                            LanguageSources = languageSources |> Seq.toList
                            Dialects = dialects |> Seq.toList
                            Glosses = glosses
                        }
                    })
                |> Task.WhenAll
        }

    let private getTranslationsAsync (entryId: int) (ctx: KensakuConnection) =
        task {
            let! translations =
                ctx.QueryAsync<Tables.Translation>(
                    sql
                        """
                select t.*
                from Translations as t
                where t.EntryId = @EntryId""",
                    {| EntryId = entryId |}
                )

            return!
                translations
                |> Seq.map (fun t ->
                    task {
                        let! nameTypes =
                            ctx.QueryAsync<string>(
                                sql
                                    """
                        select nt.Value
                        from NameTypes as nt
                        where nt.TranslationId = @TranslationId""",
                                {| TranslationId = t.Id |}
                            )

                        let! crossReferences =
                            ctx.QueryAsync<Tables.TranslationCrossReference>(
                                sql
                                    """
                        select tcr.*
                        from TranslationCrossReferences as tcr
                        where tcr.TranslationId = @TranslationId""",
                                {| TranslationId = t.Id |}
                            )

                        let! contents =
                            ctx.QueryAsync<Tables.TranslationContent>(
                                sql
                                    """
                        select tc.*
                        from TranslationContents as tc
                        where tc.TranslationId = @TranslationId""",
                                {| TranslationId = t.Id |}
                            )

                        return {
                            NameTypes = nameTypes |> Seq.toList
                            CrossReferences =
                                crossReferences
                                |> Seq.map (fun tcr -> {
                                    Kanji = tcr.ReferenceKanjiElement
                                    Reading = tcr.ReferenceReadingElement
                                    Index = tcr.ReferenceTranslation
                                })
                                |> Seq.toList
                            Contents =
                                contents
                                |> Seq.map (fun tc -> {
                                    TranslationContents.Value = tc.Value
                                    LanguageCode = tc.Language
                                })
                                |> Seq.toList
                        }
                    })
                |> Task.WhenAll
        }

    let private getWordsByIdsAsync (ids: int seq) (ctx: KensakuConnection) =
        task {
            return!
                ids
                |> Seq.map (fun id ->
                    task {
                        let! kanjiElements = getKanjiElementsAsync id ctx
                        let! readingElements = getReadingElementsAsync id ctx
                        let! senses = getSensesAsync id ctx
                        let! translations = getTranslationsAsync id ctx

                        return {
                            Id = id
                            KanjiElements = kanjiElements |> Seq.toList
                            ReadingElements = readingElements |> Seq.toList
                            Senses = senses |> Seq.toList
                            Translations = translations |> Seq.toList
                        }
                    })
                |> Task.WhenAll
        }

    let getWordLiteralsAsync (word: string) (ctx: KensakuConnection) =
        task {
            let! ids = getIdsForWordLiteralsAsync word ctx
            return! getWordsByIdsAsync ids ctx
        }
