module Kensaku.Core.Kanji

open System.Text
open System.Data.Common
open Dapper
open Kensaku
open Kensaku.Database

type KeyRadical =
    | KanjiX of int
    | Nelson of int
    with
        member this.Value =
            match this with
            | KanjiX i -> i
            | Nelson i -> i

type CharacterCode =
    | SkipCode of string
    | ShDescCode of string
    | FourCornerCode of string
    | DeRooCode of string
    with
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
    CharacterCode: CharacterCode option
    CharacterReading: string option
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
    Nanori: string option
    CommonOnly: bool
    Pattern: string option
    KeyRadical: int option
}
with
    static member FromQuery(query: GetKanjiQuery) =
        {
            MinStrokeCount = query.MinStrokeCount
            MaxStrokeCount = query.MaxStrokeCount
            IncludeStrokeMiscounts = query.IncludeStrokeMiscounts
            CharacterCode = query.CharacterCode |> Option.map (fun cc -> cc.Value)
            CharacterReading = query.CharacterReading
            Nanori = query.Nanori
            CommonOnly = query.CommonOnly
            Pattern = query.Pattern
            KeyRadical = query.KeyRadical |> Option.map (fun kr -> kr.Value)
        }

type SkipMisclassification =
    | Position of string
    | StrokeCount of string
    | StrokeAndPosition of string
    | StrokeDifference of string
    with
        static member Create(misclassificationType: string, skipCode: string) =
            match misclassificationType with
            | "posn" -> Position skipCode
            | "stroke_count" -> StrokeCount skipCode
            | "stroke_and_posn" -> StrokeAndPosition skipCode
            | "stroke_diff" -> StrokeDifference skipCode
            | _ -> failwith $"Invalid SKIP misclassification type: %s{misclassificationType}"

type GetKanjiQueryResult = {
    Id: int
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
        KanjiX: int
        Nelson: int option
    |}
    DictionaryReferences: Tables.CharacterDictionaryReference list
    Variants: Tables.CharacterVariant list
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

let makeCharacterCodeCondition (characterCode: CharacterCode option) =
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

let makePatternCondition (pattern: string option) =
    match pattern with
    | Some pattern ->
        match pattern.IndexOf("_") with
        | -1 -> failwith $"Invalid pattern: %s{pattern}"
        | i ->
            sql $"
            c.Value in (
                select substr(ke.Value, %i{i + 1}, %i{i + 1})
                from KanjiElements as ke
                where ke.Value like @Pattern
            )"
    | None -> "true"

// TODO: Implement search by key radical
let getKanjiIds (query: GetKanjiQuery) (ctx: DbConnection) =
    ctx.Execute(
        sql "
        drop table if exists SearchRadicals;

        create table SearchRadicals (Value text not null);") |>ignore
    for searchRadical in query.SearchRadicals do
        ctx.Execute(
            sql "insert into SearchRadicals (Value) values (@SearchRadical);",
            {|
                SearchRadical = searchRadical
            |}) |> ignore
    ctx.Query<int>(
        sql $"""
        select x.Id
        from (
            select distinct c.*
            from Characters as c
            left join CharacterQueryCodes as cqc on cqc.CharacterId = c.Id
            left join StrokeMiscounts as sm on sm.CharacterId = c.Id
            left join CharacterReadings as cr on cr.CharacterId = c.Id and cr.Type in ('ja_on', 'ja_kun')
            left join Nanori as n on n.CharacterId = c.Id
            left join KeyRadicals as kr on kr.CharacterId = c.Id
            where true
            and (@MinStrokeCount is null or c.StrokeCount >= @MinStrokeCount or (@IncludeStrokeMiscounts and sm.Value >= @MinStrokeCount))
            and (@MaxStrokeCount is null or c.StrokeCount <= @MaxStrokeCount or (@IncludeStrokeMiscounts and sm.Value <= @MaxStrokeCount))
            and %s{makeCharacterCodeCondition query.CharacterCode}
            and (@CharacterReading is null or cr.Value = @CharacterReading)
            and (@Nanori is null or n.Value = @Nanori)
            and (not @CommonOnly or c.Frequency is not null)
            and %s{makePatternCondition query.Pattern}
        ) as x
        where
            not exists (
                select sr.Value
                from SearchRadicals as sr

                except

                select r.Value
                from Characters_Radicals as c_r
                join Radicals as r on r.Id = c_r.RadicalId
                where c_r.CharacterId = x.Id
            )
        order by x.Frequency, x.Value;""",
        param = GetKanjiQueryParams.FromQuery(query))
    |> Seq.toList

let getKanjiByIds (ids: int list) (ctx: DbConnection) =
    let param = {|
        Ids = ids
    |}
    let characters =
        ctx.Query<Tables.Character>(
            sql "select * from Characters where Id in @Ids",
            param)
        |> Seq.toList
        |> List.map (fun x -> x.Id, x)
        |> Map.ofList
    let characterQueryCodes =
        ctx.Query<Tables.CharacterQueryCode>(
            sql "select * from CharacterQueryCodes where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let strokeMiscounts =
        ctx.Query<Tables.StrokeMiscount>(
            sql "select * from StrokeMiscounts where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let characterReadings =
        ctx.Query<Tables.CharacterReading>(
            sql "select * from CharacterReadings where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let nanori =
        ctx.Query<Tables.Nanori>(
            sql "select * from Nanori where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let keyRadicals =
        ctx.Query<Tables.KeyRadical>(
            sql "select * from KeyRadicals where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let characterMeanings =
        ctx.Query<Tables.CharacterMeaning>(
            sql "select * from CharacterMeanings where CharacterId in @Ids and Language = 'en'",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let characterDictionaryReferences =
        ctx.Query<Tables.CharacterDictionaryReference>(
            sql "select * from CharacterDictionaryReferences where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let characterVariants =
        ctx.Query<Tables.CharacterVariant>(
            sql "select * from CharacterVariants where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let codepoints =
        ctx.Query<Tables.Codepoint>(
            sql "select * from CodePoints where CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList
    let radicals =
        ctx.Query<{| CharacterId: int; Value: Rune |}>(
            sql "
            select c_r.CharacterId, r.Value from
            Characters_Radicals as c_r
            join Radicals as r on r.Id = c_r.RadicalId
            where c_r.CharacterId in @Ids",
            param)
        |> Seq.toList
        |> List.groupBy (fun x -> x.CharacterId)
        |> Map.ofList

    ids
    |> List.map (fun id ->
        let character = characters[id]
        {
            Id = character.Id
            Value = character.Value
            Grade = character.Grade
            StrokeCount = character.StrokeCount
            StrokeMiscounts =
                strokeMiscounts
                |> Map.tryFind id
                |> Option.defaultValue []
                |> List.map (fun x -> x.Value)
            CharacterReadings = {|
                Kunyomi =
                    characterReadings
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.filter (fun x -> x.Type = "ja_kun")
                    |> List.map (fun x -> x.Value)
                Onyomi =
                    characterReadings
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.filter (fun x -> x.Type = "ja_on")
                    |> List.map (fun x -> x.Value)
            |}
            CharacterMeanings =
                characterMeanings
                |> Map.tryFind id
                |> Option.defaultValue []
                |> List.map (fun x -> x.Value)
            CharacterCodes = {|
                Skip =
                    characterQueryCodes
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "skip")
                    |> Option.map (fun x -> x.Value)
                SkipMisclassifications =
                    characterQueryCodes
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.filter (fun x -> x.SkipMisclassification.IsSome)
                    |> List.map (fun x -> SkipMisclassification.Create(x.SkipMisclassification.Value, x.Value))
                ShDesc =
                    characterQueryCodes
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "sh_desc")
                    |> Option.map (fun x -> x.Value)
                FourCorner =
                    characterQueryCodes
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "four_corner")
                    |> Option.map (fun x -> x.Value)
                DeRoo =
                    characterQueryCodes
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "deroo")
                    |> Option.map (fun x -> x.Value)
            |}
            Nanori =
                nanori
                |> Map.tryFind id
                |> Option.defaultValue []
                |> List.map (fun x -> x.Value)
            KeyRadicals = {|
                KanjiX =
                    keyRadicals
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.find (fun x -> x.Type = "classical")
                    |> fun x -> x.Value
                Nelson =
                    keyRadicals
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "nelson_c")
                    |> Option.map (fun x -> x.Value)
            |}
            DictionaryReferences =
                characterDictionaryReferences
                |> Map.tryFind id
                |> Option.defaultValue []
            Variants =
                characterVariants
                |> Map.tryFind id
                |> Option.defaultValue []
            CodePoints = {|
                Ucs =
                    codepoints
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.find (fun x -> x.Type = "ucs")
                    |> fun x -> x.Value
                Jis208 =
                    codepoints
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "jis208")
                    |> Option.map (fun x -> x.Value)
                Jis212 =
                    codepoints
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "jis212")
                    |> Option.map (fun x -> x.Value)
                Jis213 =
                    codepoints
                    |> Map.tryFind id
                    |> Option.defaultValue []
                    |> List.tryFind (fun x -> x.Type = "jis213")
                    |> Option.map (fun x -> x.Value)
            |}
            Radicals =
                radicals
                |> Map.tryFind id
                |> Option.defaultValue []
                |> List.map (fun x -> x.Value)
            Frequency = character.Frequency
            IsRadical = character.IsRadical
            OldJlptLevel = character.OldJlptLevel
        }
    )

let getKanji (query: GetKanjiQuery) (ctx: DbConnection) =
    let ids = getKanjiIds query ctx
    let kanji = getKanjiByIds ids ctx
    kanji
