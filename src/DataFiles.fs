module DataFiles

open System.Net.Http
open System.IO
open System.IO.Compression
open System.Threading.Tasks

let downloadGZippedResource (hc: HttpClient) (url: string) (fileName: string) =
    task {
        let! ms = hc.GetStreamAsync(url)
        use data = new GZipStream(ms, CompressionMode.Decompress)
        let fs = File.Create($"data/{fileName}")
        return! data.CopyToAsync(fs)
    }

let downloadRadicalFiles (hc: HttpClient) =
    task {
        let! stream = hc.GetStreamAsync("http://ftp.edrdg.org/pub/Nihongo/kradzip.zip")
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
    ] |> Task.WhenAll
