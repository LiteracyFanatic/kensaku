open System.Net
open System.Net.Http
open System.IO
open System.IO.Compression
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

let makeDatabase (connection: SqliteConnection) =
    connection.Open()
    let cmd = connection.CreateCommand()
    cmd.CommandText <- File.ReadAllText("sql/JMdict.sql")
    cmd.ExecuteNonQuery() |> ignore

[<EntryPoint>]
let main argv =
    use connection = new SqliteConnection("Data Source=data/kensaku.db")
    makeDatabase connection
    0
