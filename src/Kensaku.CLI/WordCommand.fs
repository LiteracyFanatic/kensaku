namespace Kensaku.CLI


module WordCommand =
    open System

    open Argu
    open Spectre.Console

    open Kensaku.CLI.Formatting
    open Kensaku.Core
    open Kensaku.Core.Words

    type WordArgs =
        | [<MainCommand; Last>] Word of string
        | Reading of string
        | Meaning of string
        | Format of Format
        | No_Pager

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Word _ -> "show info for the given word"
                | Reading _ -> "search for words with the given reading"
                | Meaning _ -> "search for words with a meaning that matches the given regular expression"
                | Format _ -> "output format"
                | No_Pager -> "do not use a pager"

    let isSearchOption (arg: WordArgs) =
        match arg with
        | Word _
        | Reading _
        | Meaning _ -> true
        | Format _
        | No_Pager -> false

    let getSearchOptions (args: ParseResults<WordArgs>) =
        args.GetAllResults() |> List.filter isSearchOption

    let validateAtLeastOneArg (args: ParseResults<WordArgs>) =
        let searchOptions = getSearchOptions args

        if searchOptions.Length = 0 then
            args.Raise("You must specify at least one search option")

    let validateNoOtherSearchOptionsWithLiteralWord (args: ParseResults<WordArgs>) =
        let searchOptions = getSearchOptions args

        if args.Contains(Word) && searchOptions.Length > 1 then
            args.Raise("You can not use other search options when passing a literal word")

    let validateNoUnrecognizedOptions (args: ParseResults<WordArgs>) =
        match args.TryGetResult(Word) with
        | Some word when word.StartsWith("-") -> args.Raise($"unrecognized option: '%s{word}'")
        | _ -> ()

    let validateWordArgs (args: ParseResults<WordArgs>) =
        validateAtLeastOneArg args
        validateNoOtherSearchOptionsWithLiteralWord args
        validateNoUnrecognizedOptions args

    let wordHandler (ctx: KensakuConnection) (args: ParseResults<WordArgs>) =
        validateWordArgs args

        let words =
            match args.TryGetResult(Word) with
            | Some words ->
                getWordLiteralsAsync words ctx
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Seq.toList
            | None ->
                let query: GetWordsQuery = {
                    Reading = args.TryGetResult Reading
                    Meaning = args.TryGetResult Meaning
                }

                getWordsAsync query ctx
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Seq.toList

        match words with
        | [] -> ()
        | _ ->
            match args.TryGetResult(Format) |> Option.defaultValue Format.Text with
            | Format.Text ->
                let console = StringWriterAnsiConsole()

                for i in 0 .. words.Length - 1 do
                    printWord console words[i]

                    if i < words.Length - 1 then
                        console.WriteLine()
                        (console :> IAnsiConsole).Write(Rule())
                        console.WriteLine()

                let text = console.ToString()

                if args.Contains(No_Pager) then
                    printf "%s" text
                else
                    toPager text
            | Format.Json -> words |> toJson |> printfn "%s"
