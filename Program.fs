open System.Net
open System.Net.Http
open System.IO
open System.IO.Compression
open System.Text
open System.Text.RegularExpressions
open Microsoft.Data.Sqlite

let downloadGZippedResource (hc: HttpClient) (url: string) (fileName: string) =
    async {
        let! ms = hc.GetStreamAsync(url) |> Async.AwaitTask
        use data = new GZipStream(ms, CompressionMode.Decompress)
        let fs = File.Create($"data/{fileName}")
        return! data.CopyToAsync(fs) |> Async.AwaitTask
    }

let downloadRadicalFiles =
    async {
        let request = WebRequest.Create("ftp://ftp.monash.edu/pub/nihongo/kradzip.zip")
        let! response = request.GetResponseAsync() |> Async.AwaitTask
        use archive = new ZipArchive(response.GetResponseStream())
        let files = [
            "kradfile"
            "kradfile2"
            "radkfile"
            "radkfile2"
        ]
        archive.Entries
        |> Seq.filter (fun x -> List.contains x.Name files)
        |> Seq.iter (fun x -> x.ExtractToFile($"data/{x.Name}"))
    }

let downloadData () =
    let dir = DirectoryInfo("data")
    dir.Create()
    dir.EnumerateFileSystemInfos()
    |> Seq.iter (fun x -> x.Delete())
    let hc = new HttpClient()
    [
        downloadGZippedResource hc "http://ftp.edrdg.org/pub/Nihongo/JMdict.gz" "JMdict.xml"
        downloadGZippedResource hc "http://ftp.monash.edu/pub/nihongo/JMnedict.xml.gz" "JMnedict.xml"
        downloadGZippedResource hc "http://www.edrdg.org/kanjidic/kanjidic2.xml.gz" "kanjidic2.xml"
        downloadRadicalFiles
    ] |> Async.Parallel

let createSchema (connection: SqliteConnection) =
    let cmd = connection.CreateCommand()
    cmd.CommandText <- File.ReadAllText("sql/schema.sql")
    cmd.ExecuteNonQuery() |> ignore

type RadkEntry = {
    Radical: char
    StrokeCount: int
    Kanji: Set<char>
}

let parseRadkFile (path: string) =
    let text = File.ReadAllText(path, Encoding.GetEncoding("EUC-JP"))
    Regex.Matches(text, @"^\$ (.) (\d).*$([^$]+)", RegexOptions.Multiline)
    |> Seq.toList
    |> List.map (fun m ->
        {
            Radical = char m.Groups.[1].Value
            StrokeCount= int m.Groups.[2].Value
            // Remove newlines and katakana middle dots
            Kanji = set m.Groups.[3].Value - set ['\n'; '\u30FB']
        }
    )

let getRadkEntries () =
    [
        "data/radkfile"
        "data/radkfile2"
    ] |> List.collect parseRadkFile
    |> List.groupBy (fun x -> x.Radical)
    |> List.map (fun (radical, pair) ->
        match pair with
        | [ a; b ] ->
            { a with Kanji = a.Kanji + b.Kanji }
        | _ ->
            failwithf "Expected exactly one entry for %c in each radk file." radical
    )

let populateTables (connection: SqliteConnection) =
    let radkEntries = getRadkEntries ()
    printfn "%A" radkEntries
    ()

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    use connection = new SqliteConnection("Data Source=data/kensaku.db")
    connection.Open()
    createSchema connection
    populateTables connection
    connection.Close()
    0
