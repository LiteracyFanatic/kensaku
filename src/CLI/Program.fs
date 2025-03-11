open System
open System.IO
open Argu
open Kensaku.Database
open Kensaku.CLI.KanjiCommand
open Kensaku.CLI.WordCommand
open Kensaku.CLI.VersionCommand
open Kensaku.CLI.LicensesCommand
open Microsoft.Data.Sqlite

type Args =
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Kanji of ParseResults<KanjiArgs>
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Word of ParseResults<WordArgs>
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Licenses
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Version

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Kanji _ -> "search for kanji"
            | Word _ -> "search for words"
            | Licenses -> "display license info"
            | Version -> "display the version info"

let getDbConnection () =
    let dbPath =
        match Environment.GetEnvironmentVariable("KENSAKU_DB_PATH") with
        | null
        | "" ->
            if OperatingSystem.IsWindows() then
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "kensaku", "kensaku.db")
            else if OperatingSystem.IsLinux() then
                "/usr/share/kensaku/kensaku.db"
            else
                failwith "Unsupported operating system"
        | path -> path

    let ctx = new SqliteConnection($"Data Source=%s{dbPath}")
    Schema.registerTypeHandlers ()
    Schema.registerRegexpFunction ctx
    ctx

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

    match results.GetSubCommand() with
    | Kanji kanjiArgs -> kanjiHandler (getDbConnection ()) kanjiArgs
    | Word wordArgs -> wordHandler (getDbConnection ()) wordArgs
    | Licenses -> licensesHandler ()
    | Version -> versionHandler ()

    0
