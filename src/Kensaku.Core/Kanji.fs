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
    let private getKanjiIdsAsync (query: GetKanjiQuery) (ctx: KensakuConnection) =
        task {
            let! _ = ctx.ExecuteAsync(sql "create temp table SearchRadicals (Value text not null);")

            for searchRadical in query.SearchRadicals do
                let! _ =
                    ctx.ExecuteAsync(
                        sql "insert into SearchRadicals (Value) values (@SearchRadical);",
                        {| SearchRadical = searchRadical |}
                    )

                ()

            let! _ = ctx.ExecuteAsync(sql "create temp table SearchRadicalMeanings (Value text not null);")

            for searchRadicalMeaning in query.SearchRadicalMeanings do
                let! _ =
                    ctx.ExecuteAsync(
                        sql "insert into SearchRadicalMeanings (Value) values (@SearchRadicalMeaning);",
                        {| SearchRadicalMeaning = searchRadicalMeaning |}
                    )

                ()

            let! radicalIds =
                ctx.QueryAsync<int>(
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

            let! _ = ctx.ExecuteAsync(sql "create temp table SearchRadicalIds (Id number not null);")

            for radicalId in radicalIds do
                let! _ =
                    ctx.ExecuteAsync(
                        sql "insert into SearchRadicalIds (Id) values (@RadicalId);",
                        {| RadicalId = radicalId |}
                    )

                ()

            return!
                ctx.QueryAsync<int>(
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
        }

    let private getIdsForKanjiLiteralsAsync (kanji: Rune list) (ctx: KensakuConnection) =
        ctx.QueryAsync<int>(sql "select Id from Characters where Value in @Kanji", {| Kanji = List.map string kanji |})

    let private getKanjiByIdsAsync (ids: int seq) (ctx: KensakuConnection) =
        task {
            let param = {| Ids = ids |}

            let! characters = ctx.QueryAsync<Tables.Character>(sql "select * from Characters where Id in @Ids", param)
            let charactersById = characters |> Seq.map (fun x -> x.Id, x) |> Map.ofSeq

            let! characterQueryCodes =
                ctx.QueryAsync<Tables.CharacterQueryCode>(
                    sql "select * from CharacterQueryCodes where CharacterId in @Ids",
                    param
                )

            let characterQueryCodesByCharacterId =
                characterQueryCodes |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! strokeMiscounts =
                ctx.QueryAsync<Tables.StrokeMiscount>(
                    sql "select * from StrokeMiscounts where CharacterId in @Ids",
                    param
                )

            let strokeMiscountsByCharacterId =
                strokeMiscounts |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! characterReadings =
                ctx.QueryAsync<Tables.CharacterReading>(
                    sql "select * from CharacterReadings where CharacterId in @Ids",
                    param
                )

            let characterReadingsByCharacterId =
                characterReadings |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! nanori = ctx.QueryAsync<Tables.Nanori>(sql "select * from Nanori where CharacterId in @Ids", param)
            let nanoriByCharacterId = nanori |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! radicals = ctx.QueryAsync<Tables.Radical>(sql "select * from Radicals")

            let! radicalValues = ctx.QueryAsync<Tables.RadicalValue>(sql "select * from RadicalValues")
            let radicalValuesByRadicalId = radicalValues |> Seq.groupBy _.RadicalId |> Map.ofSeq

            let! radicalMeanings = ctx.QueryAsync<Tables.RadicalMeaning>(sql "select * from RadicalMeanings")

            let radicalMeaningsByRadicalId =
                radicalMeanings |> Seq.groupBy _.RadicalId |> Map.ofSeq

            let! keyRadicals =
                ctx.QueryAsync<Tables.KeyRadical>(sql "select * from KeyRadicals where CharacterId in @Ids", param)

            let keyRadicalsByCharacterId =
                keyRadicals
                |> Seq.groupBy _.CharacterId
                |> Map.ofSeq
                |> Map.map (fun key v ->
                    v
                    |> Seq.map (fun kr ->
                        let r = radicals |> Seq.find (fun x -> x.Number = Some kr.Value)

                        let values =
                            radicalValuesByRadicalId
                            |> Map.tryFind r.Id
                            |> Option.defaultValue []
                            |> Seq.map _.Value
                            |> Seq.toList

                        let meanings =
                            radicalMeaningsByRadicalId
                            |> Map.tryFind r.Id
                            |> Option.defaultValue []
                            |> Seq.map _.Value
                            |> Seq.toList

                        {
                            Number = kr.Value
                            Values = values
                            Meanings = meanings
                            Type = kr.Type
                        }))

            let! characterMeanings =
                ctx.QueryAsync<Tables.CharacterMeaning>(
                    sql "select * from CharacterMeanings where CharacterId in @Ids and Language = 'en'",
                    param
                )

            let characterMeaningsByCharacterId =
                characterMeanings |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! characterDictionaryReferences =
                ctx.QueryAsync<Tables.CharacterDictionaryReference>(
                    sql "select * from CharacterDictionaryReferences where CharacterId in @Ids",
                    param
                )

            let characterDictionaryReferencesByCharacterId =
                characterDictionaryReferences |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! characterVariants =
                ctx.QueryAsync<CharacterVariant>(
                    sql
                        "
                    select cv.*, c.Value as Character
                    from CharacterVariants as cv
                    left join CodePoints as cp on cp.Type = cv.Type and cp.Value = cv.Value
                    left join Characters as c on c.Id = cp.CharacterId
                    where cv.CharacterId in @Ids",
                    param
                )

            let characterVariantsByCharacterId =
                characterVariants |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! codepoints =
                ctx.QueryAsync<Tables.Codepoint>(sql "select * from CodePoints where CharacterId in @Ids", param)

            let codepointsByCharacterId = codepoints |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! radicals =
                ctx.QueryAsync<
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

            let radicalsByCharacterId = radicals |> Seq.groupBy _.CharacterId |> Map.ofSeq

            let! equivalentCharacters = ctx.QueryAsync<string>(sql "select Characters from EquivalentCharacters")

            let equivalentCharacters =
                equivalentCharacters |> Seq.toList |> List.map (String.getRunes >> set)

            let getCharacterGroup (character: Rune) =
                equivalentCharacters
                |> List.tryFind (Set.contains character)
                |> Option.defaultValue (Set.singleton character)

            return
                ids
                |> Seq.map (fun id ->
                    let character = charactersById[id]

                    {
                        Value = character.Value
                        Grade = character.Grade
                        StrokeCount = character.StrokeCount
                        StrokeMiscounts =
                            strokeMiscountsByCharacterId
                            |> Map.tryFind id
                            |> Option.defaultValue []
                            |> Seq.map (_.Value)
                            |> Seq.toList
                        CharacterReadings = {|
                            Kunyomi =
                                characterReadingsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.filter (fun x -> x.Type = "ja_kun")
                                |> Seq.map (_.Value)
                                |> Seq.toList
                            Onyomi =
                                characterReadingsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.filter (fun x -> x.Type = "ja_on")
                                |> Seq.map (_.Value)
                                |> Seq.toList
                        |}
                        CharacterMeanings =
                            characterMeaningsByCharacterId
                            |> Map.tryFind id
                            |> Option.defaultValue []
                            |> Seq.map (_.Value)
                            |> Seq.toList
                        CharacterCodes = {|
                            Skip =
                                characterQueryCodesByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "skip")
                                |> Option.map (_.Value)
                            SkipMisclassifications =
                                characterQueryCodesByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.filter _.SkipMisclassification.IsSome
                                |> Seq.map (fun x ->
                                    SkipMisclassification.Create(x.SkipMisclassification.Value, x.Value))
                                |> Seq.toList
                            ShDesc =
                                characterQueryCodesByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "sh_desc")
                                |> Option.map (_.Value)
                            FourCorner =
                                characterQueryCodesByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "four_corner")
                                |> Option.map (_.Value)
                            DeRoo =
                                characterQueryCodesByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "deroo")
                                |> Option.map (_.Value)
                        |}
                        Nanori =
                            nanoriByCharacterId
                            |> Map.tryFind id
                            |> Option.defaultValue []
                            |> Seq.map (_.Value)
                            |> Seq.toList
                        KeyRadicals = {|
                            Kangxi =
                                keyRadicalsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.find (fun x -> x.Type = "classical")
                            Nelson =
                                keyRadicalsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "nelson_c")
                        |}
                        DictionaryReferences =
                            characterDictionaryReferencesByCharacterId
                            |> Map.tryFind id
                            |> Option.defaultValue []
                            |> Seq.toList
                        Variants =
                            characterVariantsByCharacterId
                            |> Map.tryFind id
                            |> Option.defaultValue []
                            |> Seq.toList
                        CodePoints = {|
                            Ucs =
                                codepointsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.find (fun x -> x.Type = "ucs")
                                |> (_.Value)
                            Jis208 =
                                codepointsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "jis208")
                                |> Option.map (_.Value)
                            Jis212 =
                                codepointsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "jis212")
                                |> Option.map (_.Value)
                            Jis213 =
                                codepointsByCharacterId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.tryFind (fun x -> x.Type = "jis213")
                                |> Option.map (_.Value)
                        |}
                        Radicals =
                            radicalsByCharacterId
                            |> Map.tryFind id
                            |> Option.defaultValue []
                            |> Seq.map (_.Value)
                            |> Seq.groupBy getCharacterGroup
                            |> Seq.map (snd >> Seq.sortDescending >> Seq.head)
                            |> Seq.toList
                        Frequency = character.Frequency
                        IsRadical = character.IsRadical
                        OldJlptLevel = character.OldJlptLevel
                    })
        }

    let getKanjiAsync (query: GetKanjiQuery) (ctx: KensakuConnection) =
        task {
            let! ids = getKanjiIdsAsync query ctx
            return! getKanjiByIdsAsync ids ctx
        }

    let getKanjiLiteralsAsync (kanji: Rune list) (ctx: KensakuConnection) =
        task {
            let! ids = getIdsForKanjiLiteralsAsync kanji ctx
            return! getKanjiByIdsAsync ids ctx
        }

    let getRadicalNamesAsync (ctx: KensakuConnection) =
        ctx.QueryAsync<string>(sql "select rm.Value from RadicalMeanings as rm")
