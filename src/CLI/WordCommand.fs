module Kensaku.CLI.WordCommand

open System
open System.Data.Common
open Argu
open CLI
open CLI.Formatting
open Kensaku.Core.Words
open Spectre.Console

type WordArgs =
    | Reading of string
    | Meaning of string
    | Pattern of string
    | [<MainCommand; Last>] Word of string
    | Format of Format
    | No_Pager

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Reading _ -> "search for words with the given reading"
            | Meaning _ -> "search for words with a meaning that matches the given regular expression"
            | Pattern _ -> "search for words matching the given pattern"
            | Word _ -> "show info for the given word"
            | Format _ -> "output format"
            | No_Pager -> "do not use a pager"

let isSearchOption (arg: WordArgs) =
    match arg with
    | Reading _
    | Meaning _
    | Pattern _
    | Word _ -> true
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

let validateWordArgs (args: ParseResults<WordArgs>) =
    validateAtLeastOneArg args
    validateNoOtherSearchOptionsWithLiteralWord args

let wordHandler (ctx: DbConnection) (args: ParseResults<WordArgs>) =
    validateWordArgs args

    let words =
        match args.TryGetResult(Word) with
        | Some words -> getWordLiterals words ctx
        | None ->
            let query = {
                Reading = args.TryGetResult Reading
                Meaning = args.TryGetResult Meaning
                Pattern = args.TryGetResult Pattern
            }
            getWordsByQuery query ctx

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
