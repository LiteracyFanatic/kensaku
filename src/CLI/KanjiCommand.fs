module Kensaku.CLI.KanjiCommand

open System
open System.Data.Common
open System.Text
open System.Text.RegularExpressions
open Argu
open CLI
open CLI.Formatting
open Kensaku
open Kensaku.Core.Kanji
open Spectre.Console

type KanjiArgs =
    | Strokes of int
    | Min_Strokes of int
    | Max_Strokes of int
    | Include_Stroke_Miscounts
    | Radicals of string list
    | Skip_Code of string
    | Sh_Code of string
    | Four_Corner_Code of string
    | Deroo_Code of string
    | Reading of string
    | Meaning of string
    | Nanori of string
    | Common_Only
    | Pattern of string
    | [<MainCommand; Last>] Kanji of string list
    | Format of Format
    | No_Pager

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Strokes _ -> "search for kanji with the given number of strokes"
            | Min_Strokes _ -> "search for kanji with at least the given number of strokes"
            | Max_Strokes _ -> "search for kanji with at most the given number of strokes"
            | Include_Stroke_Miscounts ->
                "include kanji which are commonly mistaken to have the given number of strokes"
            | Radicals _ ->
                "search for kanji containing the given radicals. CJK, Kangxi, and WaniKani radical names are supported as well, so both 東 and \"east\" are valid."
            | Skip_Code _ -> "search for kanji with the given SKIP code"
            | Sh_Code _ ->
                """search for kanji with the given descriptor code from "The Kanji Dictionary" (Tuttle 1996) by Spahn and Hadamitzky"""
            | Four_Corner_Code _ -> "search for kanji with the given Four Corner code"
            | Deroo_Code _ ->
                """search for kanji with the given code from "2001 Kanji" (Bonjinsha) by Father Joseph De Roo"""
            | Reading _ -> "search for kanji with the given reading"
            | Meaning _ -> "search for kanji with a meaning that matches the given regular expression"
            | Nanori _ -> "search for kanji with the given reading when used in names"
            | Common_Only -> "include only the 2,500 most common kanji in the search"
            | Pattern _ -> "search for kanji that appear in words matching the given pattern"
            | Kanji _ -> "show info for the given kanji"
            | Format _ -> "output format"
            | No_Pager -> "do not use a pager"

let makeWordList (words: string list) =
    let sb = StringBuilder()
    let separator = if words.Length > 2 then ", " else " "

    for i, word in List.indexed words do
        if i = words.Length - 1 then
            sb.Append($"or %s{word}") |> ignore
        else
            sb.Append($"%s{word}%s{separator}") |> ignore

    sb.ToString()

let tryParseSkipCode (code: string) =
    if Regex.IsMatch(code, "^\d-\d{1,2}-\d{1,2}$") then
        Some(SkipCode code)
    else
        None

let tryParseShCode (code: string) =
    if Regex.IsMatch(code, "^\d{1,2}\p{IsBasicLatin}\d{1,2}\.\d{1,2}$") then
        Some(ShDescCode code)
    else
        None

let tryParseFourCornerCode (code: string) =
    if Regex.IsMatch(code, "^\d{4}\.\d$") then
        Some(FourCornerCode code)
    else
        None

let tryParseDeRooCode (code: string) =
    if Regex.IsMatch(code, "^\d{3,4}$") then
        Some(DeRooCode code)
    else
        None

let postProcessSkipCode =
    tryParseSkipCode >> Option.defaultWith (fun () -> failwith "Invalid SKIP code")

let postProcessShCode =
    tryParseShCode >> Option.defaultWith (fun () -> failwith "Invalid SH code")

let postProcessFourCornerCode =
    tryParseFourCornerCode
    >> Option.defaultWith (fun () -> failwith "Invalid Four Corner code")

let postProcessDeRooCode =
    tryParseDeRooCode
    >> Option.defaultWith (fun () -> failwith "Invalid De Roo code")

let validateCodeArgs (args: ParseResults<KanjiArgs>) =
    let n =
        [
            args.Contains(Skip_Code)
            args.Contains(Sh_Code)
            args.Contains(Four_Corner_Code)
            args.Contains(Deroo_Code)
        ]
        |> List.filter id
        |> List.length

    if n > 1 then
        let codeArgNames =
            [
                args.Parser.GetArgumentCaseInfo(Skip_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Sh_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Four_Corner_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Deroo_Code).Name.Value
            ]
            |> makeWordList

        args.Raise($"Only one of %s{codeArgNames} can be used")

let validateStrokeArgs (args: ParseResults<KanjiArgs>) =
    if
        args.Contains(Strokes)
        && (args.Contains(Min_Strokes) || args.Contains(Max_Strokes))
    then
        let strokeArgName = args.Parser.GetArgumentCaseInfo(Strokes).Name.Value

        let minMaxArgNames =
            [
                args.Parser.GetArgumentCaseInfo(Min_Strokes).Name.Value
                args.Parser.GetArgumentCaseInfo(Max_Strokes).Name.Value
            ]
            |> makeWordList

        args.Raise($"%s{strokeArgName} can not be used with %s{minMaxArgNames}")

let isSearchOption (arg: KanjiArgs) =
    match arg with
    | Strokes _
    | Min_Strokes _
    | Max_Strokes _
    | Include_Stroke_Miscounts
    | Radicals _
    | Skip_Code _
    | Sh_Code _
    | Four_Corner_Code _
    | Deroo_Code _
    | Reading _
    | Meaning _
    | Nanori _
    | Common_Only
    | Pattern _
    | Kanji _ -> true
    | Format _
    | No_Pager -> false

let getSearchOptions (args: ParseResults<KanjiArgs>) =
    args.GetAllResults()
    |> List.filter isSearchOption

let validateAtLeastOneArg (args: ParseResults<KanjiArgs>) =
    let searchOptions = getSearchOptions args
    if searchOptions.Length = 0 then
        args.Raise("You must specify at least one search option")

let validateNoOtherSearchOptionsWithLiteralKanji (args: ParseResults<KanjiArgs>) =
    let searchOptions = getSearchOptions args
    if args.Contains(Kanji) && searchOptions.Length > 1 then
        args.Raise("You can not use other search options when passing a literal kanji")

let validateKanjiArgs (args: ParseResults<KanjiArgs>) =
    validateCodeArgs args
    validateStrokeArgs args
    validateAtLeastOneArg args
    validateNoOtherSearchOptionsWithLiteralKanji args

let kanjiHandler (ctx: DbConnection) (args: ParseResults<KanjiArgs>) =
    validateKanjiArgs args

    let kanji =
        match args.TryGetResult(Kanji) with
        | Some kanji ->
            let runes = kanji |> List.collect String.getRunes
            getKanjiLiterals runes ctx
        | None ->
            let minStrokeCount, maxStrokeCount =
                match args.TryGetResult Strokes with
                | Some n -> Some n, Some n
                | None -> args.TryGetResult Min_Strokes, args.TryGetResult Max_Strokes

            let searchRadicals, searchRadicalMeanings =
                args.TryGetResult Radicals
                |> Option.defaultValue []
                |> List.partition (String.forall isJapanese)

            let radicalNames = getRadicalNames ctx

            for searchRadicalMeaning in searchRadicalMeanings do
                let recognizedName =
                    radicalNames
                    |> List.exists _.Equals(searchRadicalMeaning, StringComparison.OrdinalIgnoreCase)

                if not recognizedName then
                    args.Raise($"Could not find a radical named \"%s{searchRadicalMeaning}\"")

            let query = {
                MinStrokeCount = minStrokeCount
                MaxStrokeCount = maxStrokeCount
                IncludeStrokeMiscounts = args.Contains Include_Stroke_Miscounts
                SearchRadicals = List.map rune searchRadicals
                SearchRadicalMeanings = searchRadicalMeanings
                CharacterCode =
                    args.TryPostProcessResult(Skip_Code, postProcessSkipCode)
                    |> Option.orElseWith (fun () -> args.TryPostProcessResult(Sh_Code, postProcessShCode))
                    |> Option.orElseWith (fun () ->
                        args.TryPostProcessResult(Four_Corner_Code, postProcessFourCornerCode))
                    |> Option.orElseWith (fun () -> args.TryPostProcessResult(Deroo_Code, postProcessDeRooCode))
                CharacterReading = args.TryGetResult Reading
                CharacterMeaning = args.TryGetResult Meaning
                Nanori = args.TryGetResult Nanori
                CommonOnly = args.Contains Common_Only
                Pattern = args.TryGetResult Pattern
                KeyRadical = None
            }

            getKanji query ctx

    match kanji with
    | [] -> ()
    | _ ->
        match args.TryGetResult(Format) |> Option.defaultValue Format.Text with
        | Format.Text ->
            let console = StringWriterAnsiConsole()
            for i in 0..kanji.Length-1 do
                printKanji console kanji[i]
                if i < kanji.Length-1 then
                    console.WriteLine()
                    (console :> IAnsiConsole).Write(Rule())
                    console.WriteLine()
            let text = console.ToString()
            if args.Contains(No_Pager) then
                printf "%s" text
            else
                toPager text
        | Format.Json ->
            kanji
            |> toJson
            |> printfn "%s"
