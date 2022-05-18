open System.Text
open Microsoft.Data.Sqlite
open Kensaku
open Kensaku.Core.Kanji
open Argu

type KanjiArgs =
    | Strokes of int
    | Min_Strokes of int
    | Max_Strokes of int
    | Include_Stroke_Miscounts
    | Radicals of string
    | Skip_Code of string
    | Sh_Code of string
    | Four_Corner_Code of string
    | Deroo_Code of string
    | Reading of string
    | Nanori of string
    | Common_Only
    | Pattern of string
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Strokes _ -> "search for kanji with the given number of strokes"
            | Min_Strokes _ -> "search for kanji with at least the given number of strokes"
            | Max_Strokes _ -> "search for kanji with at most the given number of strokes"
            | Include_Stroke_Miscounts -> "include kanji which are commonly mistaken to have the given number of strokes"
            | Radicals _ -> "search for kanji containing the given radicals"
            | Skip_Code _ -> "search for kanji with the given SKIP code"
            | Sh_Code _ -> """search for kanji with the given descriptor code from "The Kanji Dictionary" (Tuttle 1996) by Spahn and Hadamitzky"""
            | Four_Corner_Code _ -> "search for kanji with the given Four Corner code"
            | Deroo_Code _ -> """search for kanji with the given code from "2001 Kanji" (Bonjinsha) by Father Joseph De Roo"""
            | Reading _ -> "search for kanji with the given reading"
            | Nanori _ -> "search for kanji with the given reading when used in names"
            | Common_Only -> "include only the 2,500 most common kanji in the search"
            | Pattern _ -> "search for kanji that appear in words matching the given pattern"

let makeWordList (words: string list) =
    let sb = StringBuilder()
    let separator =
        if words.Length > 2 then
            ", "
        else
            " "
    for i, word in List.indexed words do
        if i = words.Length - 1 then
            sb.Append($"or %s{word}") |> ignore
        else
            sb.Append($"%s{word}%s{separator}") |> ignore
    sb.ToString()

let validateCodeArgs (args: ParseResults<KanjiArgs>) =
    let n =
        [
            args.Contains(Skip_Code)
            args.Contains(Sh_Code)
            args.Contains(Four_Corner_Code)
            args.Contains(Deroo_Code)
        ] |> List.filter id
        |> List.length
    if n > 1 then
        let codeArgNames =
            [
                args.Parser.GetArgumentCaseInfo(Skip_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Sh_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Four_Corner_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Deroo_Code).Name.Value
            ] |> makeWordList
        args.Raise($"Only one of %s{codeArgNames} can be used")

let validateStrokeArgs (args: ParseResults<KanjiArgs>) =
    if args.Contains(Strokes) && (args.Contains(Min_Strokes) || args.Contains(Max_Strokes)) then
        let strokeArgName = args.Parser.GetArgumentCaseInfo(Strokes).Name.Value
        let minMaxArgNames =
            [
                args.Parser.GetArgumentCaseInfo(Min_Strokes).Name.Value
                args.Parser.GetArgumentCaseInfo(Max_Strokes).Name.Value
            ] |> makeWordList
        args.Raise($"%s{strokeArgName} can not be used with %s{minMaxArgNames}")

let validateAtLeastOneArg (args: ParseResults<'a>) =
    if args.GetAllResults().Length = 0 then
        args.Raise("You must specify at least one search option")

let validateKanjiArgs (args: ParseResults<KanjiArgs>) =
    validateCodeArgs args
    validateStrokeArgs args
    validateAtLeastOneArg args

let kanjiHandler (args: ParseResults<KanjiArgs>) =
    validateKanjiArgs args

    let minStrokeCount, maxStrokeCount =
        match args.TryGetResult Strokes with
        | Some n -> Some n, Some n
        | None -> args.TryGetResult Min_Strokes, args.TryGetResult Max_Strokes
    let query = {
        MinStrokeCount = minStrokeCount
        MaxStrokeCount = maxStrokeCount
        IncludeStrokeMiscounts = args.Contains Include_Stroke_Miscounts
        SearchRadicals =
            args.TryGetResult Radicals
            |> Option.map (fun radicals -> radicals.EnumerateRunes() |> Seq.toList)
            |> Option.defaultValue []
        CharacterCode =
            args.TryGetResult Skip_Code
            |> Option.map SkipCode
            |> Option.orElseWith (fun () -> args.TryGetResult Sh_Code |> Option.map ShDescCode)
            |> Option.orElseWith (fun () -> args.TryGetResult Four_Corner_Code |> Option.map FourCornerCode)
            |> Option.orElseWith (fun () -> args.TryGetResult Deroo_Code |> Option.map DeRooCode)
        CharacterReading = args.TryGetResult Reading
        Nanori = args.TryGetResult Nanori
        CommonOnly = args.Contains Common_Only
        Pattern = args.TryGetResult Pattern
        KeyRadical = None
    }
    use ctx = new SqliteConnection("Data Source=data/kensaku.db")
    let kanji = getKanji query ctx
    for k in kanji do
        printfn "%A" k

type Args =
    | [<CliPrefix(CliPrefix.None)>] Kanji of ParseResults<KanjiArgs>
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Kanji _ -> "search for kanji"

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    Database.Schema.registerTypeHandlers ()
    let parser =
        ArgumentParser.Create<Args>(
            programName = "kensaku",
            helpTextMessage = "Quick and easy search for Japanese kanji, radicals, and words",
            errorHandler = ProcessExiter(),
            usageStringCharacterWidth = 80)
    let results = parser.ParseCommandLine(argv)
    match results.GetSubCommand() with
    | Kanji kanjiArgs -> kanjiHandler kanjiArgs

    0
