module DataFiles

open System.Net.Http
open System.IO
open System.IO.Compression

let downloadGZippedResource (hc: HttpClient) (url: string) (fileName: string) =
    async {
        let! ms = hc.GetStreamAsync(url) |> Async.AwaitTask
        use data = new GZipStream(ms, CompressionMode.Decompress)
        let fs = File.Create($"data/{fileName}")
        return! data.CopyToAsync(fs) |> Async.AwaitTask
    }

let downloadRadicalFiles (hc: HttpClient) =
    async {
        let! stream = hc.GetStreamAsync("http://ftp.edrdg.org/pub/Nihongo/kradzip.zip") |> Async.AwaitTask
        use archive = new ZipArchive(stream)
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
        downloadGZippedResource hc "http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz" "JMnedict.xml"
        downloadGZippedResource hc "http://ftp.edrdg.org/pub/Nihongo/kanjidic2.xml.gz" "kanjidic2.xml"
        downloadRadicalFiles hc
    ] |> Async.Parallel
