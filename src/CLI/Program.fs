open System.Reflection
open System.Linq
open Argu
open Kensaku.CLI.KanjiCommand

type Args =
    | [<CliPrefix(CliPrefix.None)>] Kanji of ParseResults<KanjiArgs>
    | Version

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Kanji _ -> "search for kanji"
            | Version -> "display the version info"

[<EntryPoint>]
let main argv =
    let parser =
        ArgumentParser.Create<Args>(
            programName = "kensaku",
            helpTextMessage = "Quick and easy search for Japanese kanji, radicals, and words",
            errorHandler = ProcessExiter(),
            usageStringCharacterWidth = 80
        )

    let results = parser.ParseCommandLine(argv)

    if results.Contains(Version) then
        let version =
            Assembly
                .GetEntryAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(fun a -> a.Key = "GitTag")
                .Value

        printfn "%s" version
    else
        match results.GetSubCommand() with
        | Kanji kanjiArgs -> kanjiHandler kanjiArgs
        | Version ->
            let versionOptionName = results.Parser.GetArgumentCaseInfo(Version).Name.Value
            results.Raise($"%s{versionOptionName} should be handled before evaluation of subcommands")

    0
