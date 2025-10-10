namespace Kensaku.CLI


module RadicalCommand =
    open Argu
    open Spectre.Console

    open Kensaku.CLI.Formatting
    open Kensaku.Core
    open Kensaku.Core.Radicals

    type RadicalArgs =
        | Strokes of int
        | Min_Strokes of int
        | Max_Strokes of int
        | Number of int
        | Name of string
        | Meaning of string
        | [<MainCommand; Last>] Radical of string list
        | Format of Format
        | No_Pager

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Strokes _ -> "search for a radical with the given number of strokes"
                | Min_Strokes _ -> "search for radicals with at least the given number of strokes"
                | Max_Strokes _ -> "search for radicals with at most the given number of strokes"
                | Number _ -> "search for a radical by its number"
                | Name _ -> "search for a radical by its name"
                | Meaning _ -> "search for a radical by its meaning (Unicode or WaniKani)"
                | Radical _ -> "show info for the given radical character(s)"
                | Format _ -> "output format"
                | No_Pager -> "do not use a pager"

    let validateStrokeArgs (args: ParseResults<RadicalArgs>) =
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
                |> String.concat " or "

            args.Raise($"%s{strokeArgName} can not be used with %s{minMaxArgNames}")

    let isSearchOption (arg: RadicalArgs) =
        match arg with
        | Strokes _
        | Number _
        | Name _
        | Meaning _
        | Radical _ -> true
        | Min_Strokes _
        | Max_Strokes _
        | Format _
        | No_Pager -> false

    let getSearchOptions (args: ParseResults<RadicalArgs>) =
        args.GetAllResults() |> List.filter isSearchOption

    let hasStrokeRangeOption (args: ParseResults<RadicalArgs>) =
        args.Contains(Min_Strokes) || args.Contains(Max_Strokes)

    let validateAtLeastOneArg (args: ParseResults<RadicalArgs>) =
        let searchOptions = getSearchOptions args

        let hasAny =
            searchOptions.Length > 0 || args.Contains(Strokes) || hasStrokeRangeOption args

        if not hasAny then
            args.Raise("You must specify at least one search option")

    let validateNoOtherSearchOptionsWithLiteralRadical (args: ParseResults<RadicalArgs>) =
        let searchOptions = getSearchOptions args
        let hasOtherStrokeOptions = args.Contains(Strokes) || hasStrokeRangeOption args

        if args.Contains(Radical) && (searchOptions.Length > 1 || hasOtherStrokeOptions) then
            args.Raise("You can not use other search options when passing a literal radical")

    let validateOnlyOneSearchOption (args: ParseResults<RadicalArgs>) =
        if not (args.Contains(Radical)) then
            let searchCount =
                (if args.Contains(Number) then 1 else 0)
                + (if args.Contains(Name) then 1 else 0)
                + (if args.Contains(Meaning) then 1 else 0)
                + (if args.Contains(Strokes) then 1 else 0)
                + (if hasStrokeRangeOption args then 1 else 0)

            if searchCount > 1 then
                args.Raise("You can only use one search option at a time")

    let validateNoUnrecognizedOptions (args: ParseResults<RadicalArgs>) =
        match args.TryGetResult(Radical) with
        | Some radicalList ->
            radicalList
            |> List.tryFind (fun r -> r.StartsWith("-"))
            |> Option.iter (fun r -> args.Raise($"unrecognized option: '%s{r}'"))
        | _ -> ()

    let validateRadicalArgs (args: ParseResults<RadicalArgs>) =
        validateStrokeArgs args
        validateAtLeastOneArg args
        validateNoOtherSearchOptionsWithLiteralRadical args
        validateOnlyOneSearchOption args
        validateNoUnrecognizedOptions args

    let radicalHandler (ctx: KensakuConnection) (args: ParseResults<RadicalArgs>) =
        validateRadicalArgs args

        let radicals =
            match args.TryGetResult(Radical) with
            | Some radicals ->
                let radicalRunes =
                    radicals |> List.collect (fun s -> s.EnumerateRunes() |> Seq.toList)

                getRadicalLiteralsAsync radicalRunes ctx
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Seq.toList
            | None ->
                let minStrokeCount, maxStrokeCount =
                    match args.TryGetResult Strokes with
                    | Some n -> Some n, Some n
                    | None -> args.TryGetResult Min_Strokes, args.TryGetResult Max_Strokes

                let query = {
                    RadicalNumber = args.TryGetResult Number
                    RadicalName = args.TryGetResult Name
                    RadicalMeaning = args.TryGetResult Meaning
                    MinStrokeCount = minStrokeCount
                    MaxStrokeCount = maxStrokeCount
                }

                getRadicalsAsync query ctx
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Seq.toList

        match radicals with
        | [] -> ()
        | _ ->
            match args.TryGetResult(Format) |> Option.defaultValue Format.Text with
            | Format.Text ->
                let console = StringWriterAnsiConsole()

                for i in 0 .. radicals.Length - 1 do
                    printRadical console radicals[i]

                    if i < radicals.Length - 1 then
                        console.WriteLine()
                        (console :> IAnsiConsole).Write(Rule())
                        console.WriteLine()

                let text = console.ToString()

                if args.Contains(No_Pager) then
                    printf "%s" text
                else
                    toPager text
            | Format.Json -> radicals |> toJson |> printfn "%s"
