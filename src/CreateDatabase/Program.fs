open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open System.Text
open Microsoft.Data.Sqlite
open Kensaku
open System.Data.Common

let downloadDataAsync () =
    task {
        let apiKey = System.Environment.GetEnvironmentVariable("WANI_KANI_API_KEY")

        if String.IsNullOrEmpty(apiKey) then
            failwith "The environment variable WANI_KANI_API_KEY must be defined"

        let dir = "data"
        Directory.CreateDirectory(dir) |> ignore
        let hc = new HttpClient()

        let! _ =
            Task.WhenAll(
                [
                    DataFiles.downloadCJKRadicalsFileAsync hc (Path.Combine(dir, "CJKRadicals.txt"))
                    DataFiles.downloadEquivalentUnifiedIdeographFileAsync
                        hc
                        (Path.Combine(dir, "EquivalentUnifiedIdeograph.txt"))
                    DataFiles.downloadDerivedNameFileAsync hc (Path.Combine(dir, "DerivedName.txt"))
                    DataFiles.downloadJMdictAsync hc (Path.Combine(dir, "JMdict.xml"))
                    DataFiles.downloadJMnedictAsync hc (Path.Combine(dir, "JMnedict.xml"))
                    DataFiles.downloadKanjidic2Async hc (Path.Combine(dir, "kanjidic2.xml"))
                    DataFiles.downloadRadicalFilesAsync hc dir
                    DataFiles.downloadWaniKaniRadicalsAsync apiKey hc (Path.Combine(dir, "wani-kani-radicals.json"))
                    DataFiles.downloadWaniKaniKanjiAsync apiKey hc (Path.Combine(dir, "wani-kani-kanji.json"))
                ]
            )

        return ()
    }

let populateTables (ctx: DbConnection) =
    let equivalentCharacters =
        DataParsing.getEquivalentCharacters "data/EquivalentUnifiedIdeograph.txt"

    let getVariants = DataParsing.getIdeographicVariants equivalentCharacters
    let cjkRadicals = DataParsing.getCJKRadicals "data/CJKRadicals.txt"
    Database.Schema.populateCJKRadicals ctx getVariants cjkRadicals
    let jMdictEntries = DataParsing.getJMdictEntries "data/JMdict.xml"
    Database.Schema.populateJMdictEntries ctx jMdictEntries
    let jMnedictEntries = DataParsing.getJMnedictEntries "data/JMnedict.xml"
    Database.Schema.populateJMnedictEntries ctx jMnedictEntries
    let kanjidic2Info = DataParsing.getKanjidic2Info "data/kanjidic2.xml"
    Database.Schema.populateKanjidic2Info ctx kanjidic2Info
    let kanjidic2Entries = DataParsing.getKanjidic2Entries "data/kanjidic2.xml"
    Database.Schema.populateKanjidic2Entries ctx kanjidic2Entries
    let waniKaniRadicals = DataFiles.loadWaniKaniRadicals "data/wani-kani-radicals.json"
    let waniKaniKanji = DataFiles.loadWaniKaniKanji "data/wani-kani-kanji.json"
    Database.Schema.populateWaniKaniRadicals ctx getVariants waniKaniRadicals waniKaniKanji
    let radkEntries = DataParsing.getRadkEntries ()
    Database.Schema.populateRadicals ctx getVariants radkEntries
    let derivedRadicalNames = DataParsing.getDerivedRadicalNames "data/DerivedName.txt"
    Database.Schema.populateDerivedRadicalNames ctx derivedRadicalNames

let createDb () =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    downloadDataAsync () |> Async.AwaitTask |> Async.RunSynchronously
    use ctx = new SqliteConnection("Data Source=data/kensaku.db")
    ctx.Open()
    Database.Schema.registerTypeHandlers ()
    Database.Schema.createSchema ctx
    populateTables ctx

[<EntryPoint>]
let main argv =
    createDb ()
    0
