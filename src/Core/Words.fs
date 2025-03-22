module Kensaku.Core.Words

open System.Data.Common
open Dapper
open Kensaku
open Kensaku.Database
open Kensaku.Domain

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

let getReadings (kanji: KanjiElement) (readings: ReadingElement list) =
    readings
    |> List.filter (fun re -> re.Restrictions.IsEmpty || List.contains kanji.Value re.Restrictions)

let getPrimaryAndAlternateForms (word: GetWordQueryResult) =
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

    kanjiReadingPairs.Head, (kanjiReadingPairs.Tail @ falseEntries)

let getIdsForWordLiterals (word: string) (ctx: DbConnection) =
    ctx.Query<int>(
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
    |> Seq.toList

let getKanjiElements (entryId: int) (ctx: DbConnection) =
    ctx.Query<Tables.KanjiElement>(
        sql
            """
        select ke.*
        from KanjiElements as ke
        where ke.EntryId = @EntryId
        order by ke.Id""",
        {| EntryId = entryId |}
    )
    |> Seq.map (fun ke ->
        let information =
            ctx.Query<string>(
                sql
                    """
                select kei.Value
                from KanjiElementInformation as kei
                where kei.KanjiElementId = @KanjiElementId""",
                {| KanjiElementId = ke.Id |}
            )
            |> Seq.toList

        let priority =
            ctx.Query<string>(
                sql
                    """
                select kep.Value
                from KanjiElementPriorities as kep
                where kep.KanjiElementId = @KanjiElementId""",
                {| KanjiElementId = ke.Id |}
            )
            |> Seq.toList

        {
            Value = ke.Value
            Information = information
            Priority = priority
        })
    |> Seq.toList

let getReadingElements (entryId: int) (ctx: DbConnection) =
    ctx.Query<Tables.ReadingElement>(
        sql
            """
        select re.*
        from ReadingElements as re
        where re.EntryId = @EntryId
        order by re.Id""",
        {| EntryId = entryId |}
    )
    |> Seq.map (fun re ->
        let restrictions =
            ctx.Query<string>(
                sql
                    """
                select rer.Value
                from ReadingElementRestrictions as rer
                where rer.ReadingElementId = @ReadingElementId""",
                {| ReadingElementId = re.Id |}
            )
            |> Seq.toList

        let information =
            ctx.Query<string>(
                sql
                    """
                select rei.Value
                from ReadingElementInformation as rei
                where rei.ReadingElementId = @ReadingElementId""",
                {| ReadingElementId = re.Id |}
            )
            |> Seq.toList

        let priority =
            ctx.Query<string>(
                sql
                    """
                select rep.Value
                from ReadingElementPriorities as rep
                where rep.ReadingElementId = @ReadingElementId""",
                {| ReadingElementId = re.Id |}
            )
            |> Seq.toList

        {
            Value = re.Value
            IsTrueReading = re.IsTrueReading
            Restrictions = restrictions
            Information = information
            Priority = priority
        })
    |> Seq.toList

let getKanjiRestrictions (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select sker.KanjiElement
        from SenseKanjiElementRestrictions as sker
        where sker.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getReadingRestrictions (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select srer.ReadingElement
        from SenseReadingElementRestrictions as srer
        where srer.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getPartsOfSpeech (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select pos.Value
        from PartsOfSpeech as pos
        where pos.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getCrossReferences (senseId: int) (ctx: DbConnection) =
    ctx.Query<Tables.SenseCrossReference>(
        sql
            """
        select scr.*
        from SenseCrossReferences as scr
        where scr.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.map (fun scf -> {
        Kanji = scf.ReferenceKanjiElement
        Reading = scf.ReferenceReadingElement
        Index = scf.ReferenceSense
    })
    |> Seq.toList

let getAntonyms (senseId: int) (ctx: DbConnection) =
    ctx.Query<Tables.Antonym>(
        sql
            """
        select a.*
        from Antonyms as a
        where a.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList
    |> List.map (fun a ->
        ({
            Kanji = a.ReferenceKanjiElement
            Reading = a.ReferenceReadingElement
        }
        : Antonym))

let getFields (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select f.Value
        from Fields as f
        where f.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getMiscellaneousInformation (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select mi.Value
        from MiscellaneousInformation as mi
        where mi.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getAdditionalInformation (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select sai.Value
        from SenseInformation as sai
        where sai.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getLanguageSources (senseId: int) (ctx: DbConnection) =
    ctx.Query<Tables.LanguageSource>(
        sql
            """
        select ls.*
        from LanguageSources as ls
        where ls.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.map (fun ls -> {
        Value = ls.Value
        Code = ls.LanguageCode
        IsPartial = ls.IsPartial
        IsWasei = ls.IsWasei
    })
    |> Seq.toList

let getDialects (senseId: int) (ctx: DbConnection) =
    ctx.Query<string>(
        sql
            """
        select d.Value
        from Dialects as d
        where d.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.toList

let getGlosses (senseId: int) (ctx: DbConnection) =
    ctx.Query<Tables.Gloss>(
        sql
            """
        select g.*
        from Glosses as g
        where g.SenseId = @SenseId""",
        {| SenseId = senseId |}
    )
    |> Seq.map (fun g -> {
        Value = g.Value
        LanguageCode = g.Language
        Type = g.Type
    })
    |> Seq.toList

let getSenses (entryId: int) (ctx: DbConnection) : Sense list =
    ctx.Query<Tables.Sense>(
        sql
            """
        select s.*
        from Senses as s
        where s.EntryId = @EntryId""",
        {| EntryId = entryId |}
    )
    |> Seq.map (fun s ->
        let result: Sense = {
            KanjiRestrictions = getKanjiRestrictions s.Id ctx
            ReadingRestrictions = getReadingRestrictions s.Id ctx
            PartsOfSpeech = getPartsOfSpeech s.Id ctx
            CrossReferences = getCrossReferences s.Id ctx
            Antonyms = getAntonyms s.Id ctx
            Fields = getFields s.Id ctx
            MiscellaneousInformation = getMiscellaneousInformation s.Id ctx
            AdditionalInformation = getAdditionalInformation s.Id ctx
            LanguageSources = getLanguageSources s.Id ctx
            Dialects = getDialects s.Id ctx
            Glosses = getGlosses s.Id ctx
        }

        result)
    |> Seq.toList

let getTranslations (entryId: int) (ctx: DbConnection) =
    ctx.Query<Tables.Translation>(
        sql
            """
        select t.*
        from Translations as t
        where t.EntryId = @EntryId""",
        {| EntryId = entryId |}
    )
    |> Seq.map (fun t ->
        let nameTypes =
            ctx.Query<string>(
                sql
                    """
                select nt.Value
                from NameTypes as nt
                where nt.TranslationId = @TranslationId""",
                {| TranslationId = t.Id |}
            )
            |> Seq.toList

        let crossReferences =
            ctx.Query<Tables.TranslationCrossReference>(
                sql
                    """
                select tcr.*
                from TranslationCrossReferences as tcr
                where tcr.TranslationId = @TranslationId""",
                {| TranslationId = t.Id |}
            )
            |> Seq.map (fun tcr -> {
                Kanji = tcr.ReferenceKanjiElement
                Reading = tcr.ReferenceReadingElement
                Index = tcr.ReferenceTranslation
            })
            |> Seq.toList

        let contents =
            ctx.Query<Tables.TranslationContent>(
                sql
                    """
                select tc.*
                from TranslationContents as tc
                where tc.TranslationId = @TranslationId""",
                {| TranslationId = t.Id |}
            )
            |> Seq.map (fun tc ->
                {
                    Value = tc.Value
                    LanguageCode = tc.Language
                }
                : TranslationContents)
            |> Seq.toList

        {
            NameTypes = nameTypes
            CrossReferences = crossReferences
            Contents = contents
        })
    |> Seq.toList

let getWordsByIds (ids: int list) (ctx: DbConnection) =
    ids
    |> List.map (fun id -> {
        Id = id
        KanjiElements = getKanjiElements id ctx
        ReadingElements = getReadingElements id ctx
        Senses = getSenses id ctx
        Translations = getTranslations id ctx
    })

let getWordLiterals (word: string) (ctx: DbConnection) =
    let ids = getIdsForWordLiterals word ctx
    let words = getWordsByIds ids ctx
    words
