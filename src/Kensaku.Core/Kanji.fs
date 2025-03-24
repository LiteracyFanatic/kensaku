namespace Kensaku.Core

module Kanji =
    open System.Text
    open Dapper

    type KeyRadical =
        | Kangxi of int
        | Nelson of int

        member this.Value =
            match this with
            | Kangxi i -> i
            | Nelson i -> i

    type CharacterCode =
        | SkipCode of string
        | ShDescCode of string
        | FourCornerCode of string
        | DeRooCode of string

        member this.Value =
            match this with
            | SkipCode c -> c
            | ShDescCode c -> c
            | FourCornerCode c -> c
            | DeRooCode c -> c

    type GetKanjiQuery = {
        MinStrokeCount: int option
        MaxStrokeCount: int option
        IncludeStrokeMiscounts: bool
        SearchRadicals: Rune list
        SearchRadicalMeanings: string list
        CharacterCode: CharacterCode option
        CharacterReading: string option
        CharacterMeaning: string option
        Nanori: string option
        CommonOnly: bool
        Pattern: string option
        KeyRadical: KeyRadical option
    }

    type GetKanjiQueryParams = {
        MinStrokeCount: int option
        MaxStrokeCount: int option
        IncludeStrokeMiscounts: bool
        CharacterCode: string option
        CharacterReading: string option
        CharacterMeaning: string option
        Nanori: string option
        CommonOnly: bool
        Pattern: string option
        KeyRadical: int option
    } with

        static member FromQuery(query: GetKanjiQuery) = {
            MinStrokeCount = query.MinStrokeCount
            MaxStrokeCount = query.MaxStrokeCount
            IncludeStrokeMiscounts = query.IncludeStrokeMiscounts
            CharacterCode = query.CharacterCode |> Option.map (_.Value)
            CharacterReading = query.CharacterReading
            CharacterMeaning = query.CharacterMeaning
            Nanori = query.Nanori
            CommonOnly = query.CommonOnly
            Pattern = query.Pattern
            KeyRadical = query.KeyRadical |> Option.map (_.Value)
        }

    type SkipMisclassification =
        | Position of string
        | StrokeCount of string
        | StrokeAndPosition of string
        | StrokeDifference of string

        static member Create(misclassificationType: string, skipCode: string) =
            match misclassificationType with
            | "posn" -> Position skipCode
            | "stroke_count" -> StrokeCount skipCode
            | "stroke_and_posn" -> StrokeAndPosition skipCode
            | "stroke_diff" -> StrokeDifference skipCode
            | _ -> failwith $"Invalid SKIP misclassification type: %s{misclassificationType}"

    [<CLIMutable>]
    type CharacterVariant = {
        CharacterId: int
        Type: string
        Value: string
        Character: Rune option
    }

    type KeyRadicalValue = {
        Number: int
        Values: Rune list
        Meanings: string list
        Type: string
    }

    type GetKanjiQueryResult = {
        Value: Rune
        Grade: int option
        StrokeCount: int
        StrokeMiscounts: int list
        CharacterReadings: {|
            Kunyomi: string list
            Onyomi: string list
        |}
        CharacterMeanings: string list
        CharacterCodes: {|
            Skip: string option
            SkipMisclassifications: SkipMisclassification list
            ShDesc: string option
            FourCorner: string option
            DeRoo: string option
        |}
        Nanori: string list
        KeyRadicals: {|
            Kangxi: KeyRadicalValue
            Nelson: KeyRadicalValue option
        |}
        DictionaryReferences: Tables.CharacterDictionaryReference list
        Variants: CharacterVariant list
        CodePoints: {|
            Ucs: string
            Jis208: string option
            Jis212: string option
            Jis213: string option
        |}
        Radicals: Rune list
        Frequency: int option
        IsRadical: bool
        OldJlptLevel: int option
    }

    let private makeCharacterCodeCondition (characterCode: CharacterCode option) =
        match characterCode with
        | Some characterCode ->
            let codeType =
                match characterCode with
                | SkipCode _ -> "skip"
                | ShDescCode _ -> "sh_desc"
                | FourCornerCode _ -> "four_corner"
                | DeRooCode _ -> "deroo"

            sql $"cqc.Type = '%s{codeType}' and cqc.Value = @CharacterCode"
        | None -> "true"

    let private makePatternCondition (pattern: string option) =
        match pattern with
        | Some pattern ->
            match pattern.IndexOf("_") with
            | -1 -> failwith $"Invalid pattern: %s{pattern}"
            | i ->
                sql
                    $"
                c.Value in (
                    select substr(ke.Value, %i{i + 1}, %i{i + 1})
                    from KanjiElements as ke
                    where ke.Value like @Pattern
                )"
        | None -> "true"

    // TODO: Implement search by key radical
    let private getKanjiIds (query: GetKanjiQuery) (ctx: KensakuConnection) =
        ctx.Execute(sql "create temp table SearchRadicals (Value text not null);")
        |> ignore

        for searchRadical in query.SearchRadicals do
            ctx.Execute(
                sql "insert into SearchRadicals (Value) values (@SearchRadical);",
                {| SearchRadical = searchRadical |}
            )
            |> ignore

        ctx.Execute(sql "create temp table SearchRadicalMeanings (Value text not null);")
        |> ignore

        for searchRadicalMeaning in query.SearchRadicalMeanings do
            ctx.Execute(
                sql "insert into SearchRadicalMeanings (Value) values (@SearchRadicalMeaning);",
                {| SearchRadicalMeaning = searchRadicalMeaning |}
            )
            |> ignore

        let radicalIds =
            ctx.Query<int>(
                sql
                    """
                select rv.RadicalId
                from RadicalValues as rv
                join SearchRadicals as sr on rv.Value = sr.Value

                union

                select rm.RadicalId
                from RadicalMeanings as rm
                join SearchRadicalMeanings as srm on rm.Value like srm.Value"""
            )
            |> Seq.toList

        ctx.Execute(sql "create temp table SearchRadicalIds (Id number not null);")
        |> ignore

        for radicalId in radicalIds do
            ctx.Execute(sql "insert into SearchRadicalIds (Id) values (@RadicalId);", {| RadicalId = radicalId |})
            |> ignore

        ctx.Query<int>(
            sql
                $"""
            select x.Id
            from (
                select distinct c.*
                from Characters as c
                left join CharacterQueryCodes as cqc on cqc.CharacterId = c.Id
                left join StrokeMiscounts as sm on sm.CharacterId = c.Id
                left join CharacterReadings as cr on cr.CharacterId = c.Id and cr.Type in ('ja_on', 'ja_kun')
                left join CharacterMeanings as cm on cm.CharacterId = c.Id and cm.Language = 'en'
                left join Nanori as n on n.CharacterId = c.Id
                left join KeyRadicals as kr on kr.CharacterId = c.Id
                where true
                and (@MinStrokeCount is null or c.StrokeCount >= @MinStrokeCount or (@IncludeStrokeMiscounts and sm.Value >= @MinStrokeCount))
                and (@MaxStrokeCount is null or c.StrokeCount <= @MaxStrokeCount or (@IncludeStrokeMiscounts and sm.Value <= @MaxStrokeCount))
                and %s{makeCharacterCodeCondition query.CharacterCode}
                and (@CharacterReading is null or cr.Value = @CharacterReading)
                and (@CharacterMeaning is null or cm.Value regexp @CharacterMeaning)
                and (@Nanori is null or n.Value = @Nanori)
                and (not @CommonOnly or c.Frequency is not null)
                and %s{makePatternCondition query.Pattern}
            ) as x
            where not exists (
                select sri.Id
                from SearchRadicalIds as sri

                except

                select c_r.RadicalId
                from Characters_Radicals as c_r
                where c_r.CharacterId = x.Id
            )
            order by x.Frequency, x.Value;""",
            param = GetKanjiQueryParams.FromQuery(query)
        )
        |> Seq.toList

    let private getIdsForKanjiLiterals (kanji: Rune list) (ctx: KensakuConnection) =
        ctx.Query<int>(sql "select Id from Characters where Value in @Kanji", {| Kanji = List.map string kanji |})
        |> Seq.toList

    let private getKanjiByIds (ids: int list) (ctx: KensakuConnection) =
        let param = {| Ids = ids |}

        let characters =
            ctx.Query<Tables.Character>(sql "select * from Characters where Id in @Ids", param)
            |> Seq.toList
            |> List.map (fun x -> x.Id, x)
            |> Map.ofList

        let characterQueryCodes =
            ctx.Query<Tables.CharacterQueryCode>(
                sql "select * from CharacterQueryCodes where CharacterId in @Ids",
                param
            )
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let strokeMiscounts =
            ctx.Query<Tables.StrokeMiscount>(sql "select * from StrokeMiscounts where CharacterId in @Ids", param)
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let characterReadings =
            ctx.Query<Tables.CharacterReading>(sql "select * from CharacterReadings where CharacterId in @Ids", param)
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let nanori =
            ctx.Query<Tables.Nanori>(sql "select * from Nanori where CharacterId in @Ids", param)
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let radicals = ctx.Query<Tables.Radical>(sql "select * from Radicals") |> Seq.toList

        let radicalValues =
            ctx.Query<Tables.RadicalValue>(sql "select * from RadicalValues")
            |> Seq.toList
            |> List.groupBy _.RadicalId
            |> Map.ofList

        let radicalMeanings =
            ctx.Query<Tables.RadicalMeaning>(sql "select * from RadicalMeanings")
            |> Seq.toList
            |> List.groupBy _.RadicalId
            |> Map.ofList

        let keyRadicals =
            ctx.Query<Tables.KeyRadical>(sql "select * from KeyRadicals where CharacterId in @Ids", param)
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList
            |> Map.map (fun key v ->
                v
                |> List.map (fun kr ->
                    let r = radicals |> List.find (fun x -> x.Number = Some kr.Value)

                    let values =
                        radicalValues |> Map.tryFind r.Id |> Option.defaultValue [] |> List.map _.Value

                    let meanings =
                        radicalMeanings
                        |> Map.tryFind r.Id
                        |> Option.defaultValue []
                        |> List.map _.Value

                    {
                        Number = kr.Value
                        Values = values
                        Meanings = meanings
                        Type = kr.Type
                    }))

        let characterMeanings =
            ctx.Query<Tables.CharacterMeaning>(
                sql "select * from CharacterMeanings where CharacterId in @Ids and Language = 'en'",
                param
            )
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let characterDictionaryReferences =
            ctx.Query<Tables.CharacterDictionaryReference>(
                sql "select * from CharacterDictionaryReferences where CharacterId in @Ids",
                param
            )
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let characterVariants =
            ctx.Query<CharacterVariant>(
                sql
                    "
                select cv.*, c.Value as Character
                from CharacterVariants as cv
                left join CodePoints as cp on cp.Type = cv.Type and cp.Value = cv.Value
                left join Characters as c on c.Id = cp.CharacterId
                where cv.CharacterId in @Ids",
                param
            )
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let codepoints =
            ctx.Query<Tables.Codepoint>(sql "select * from CodePoints where CharacterId in @Ids", param)
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let radicals =
            ctx.Query<
                {|
                    CharacterId: int
                    Value: Rune
                |}
             >(
                sql
                    "
                select c_r.CharacterId, rv.Value from
                Characters_Radicals as c_r
                join Radicals as r on r.Id = c_r.RadicalId
                join RadicalValues as rv on rv.RadicalId = r.Id
                where c_r.CharacterId in @Ids",
                param
            )
            |> Seq.toList
            |> List.groupBy _.CharacterId
            |> Map.ofList

        let equivalentCharacters =
            ctx.Query<string>(sql "select Characters from EquivalentCharacters")
            |> Seq.toList
            |> List.map (String.getRunes >> set)

        let getCharacterGroup (character: Rune) =
            equivalentCharacters
            |> List.tryFind (Set.contains character)
            |> Option.defaultValue (Set.singleton character)

        ids
        |> List.map (fun id ->
            let character = characters[id]

            {
                Value = character.Value
                Grade = character.Grade
                StrokeCount = character.StrokeCount
                StrokeMiscounts =
                    strokeMiscounts
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.map (_.Value)
                CharacterReadings = {|
                    Kunyomi =
                        characterReadings
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.filter (fun x -> x.Type = "ja_kun")
                        |> List.map (_.Value)
                    Onyomi =
                        characterReadings
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.filter (fun x -> x.Type = "ja_on")
                        |> List.map (_.Value)
                |}
                CharacterMeanings =
                    characterMeanings
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.map (_.Value)
                CharacterCodes = {|
                    Skip =
                        characterQueryCodes
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "skip")
                        |> Option.map (_.Value)
                    SkipMisclassifications =
                        characterQueryCodes
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.filter _.SkipMisclassification.IsSome
                        |> List.map (fun x -> SkipMisclassification.Create(x.SkipMisclassification.Value, x.Value))
                    ShDesc =
                        characterQueryCodes
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "sh_desc")
                        |> Option.map (_.Value)
                    FourCorner =
                        characterQueryCodes
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "four_corner")
                        |> Option.map (_.Value)
                    DeRoo =
                        characterQueryCodes
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "deroo")
                        |> Option.map (_.Value)
                |}
                Nanori = nanori |> Map.tryFind id |> Option.defaultValue [] |> List.map (_.Value)
                KeyRadicals = {|
                    Kangxi =
                        keyRadicals
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.find (fun x -> x.Type = "classical")
                    Nelson =
                        keyRadicals
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "nelson_c")
                |}
                DictionaryReferences = characterDictionaryReferences |> Map.tryFind id |> Option.defaultValue []
                Variants = characterVariants |> Map.tryFind id |> Option.defaultValue []
                CodePoints = {|
                    Ucs =
                        codepoints
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.find (fun x -> x.Type = "ucs")
                        |> (_.Value)
                    Jis208 =
                        codepoints
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "jis208")
                        |> Option.map (_.Value)
                    Jis212 =
                        codepoints
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "jis212")
                        |> Option.map (_.Value)
                    Jis213 =
                        codepoints
                        |> Map.tryFind id
                        |> Option.defaultValue []
                        |> List.tryFind (fun x -> x.Type = "jis213")
                        |> Option.map (_.Value)
                |}
                Radicals =
                    radicals
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.map (_.Value)
                    |> List.groupBy getCharacterGroup
                    |> List.map (snd >> List.sortDescending >> List.head)
                Frequency = character.Frequency
                IsRadical = character.IsRadical
                OldJlptLevel = character.OldJlptLevel
            })

    let getKanji (query: GetKanjiQuery) (ctx: KensakuConnection) =
        let ids = getKanjiIds query ctx
        let kanji = getKanjiByIds ids ctx
        kanji

    let getKanjiLiterals (kanji: Rune list) (ctx: KensakuConnection) =
        let ids = getIdsForKanjiLiterals kanji ctx
        let kanji = getKanjiByIds ids ctx
        kanji

    let getRadicalNames (ctx: KensakuConnection) =
        ctx.Query<string>(sql "select rm.Value from RadicalMeanings as rm")
        |> Seq.toList
