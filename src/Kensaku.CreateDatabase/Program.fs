open FSharp.Control
open System
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Text
open System.Text.Encodings.Web
open System.Text.Json
open System.Threading.Tasks

open Kensaku.Core
open Kensaku.DataSources

let downloadGZippedResourceAsync (hc: HttpClient) (url: string) (path: string) =
    task {
        use! ms = hc.GetStreamAsync(url)
        use data = new GZipStream(ms, CompressionMode.Decompress)
        use fs = File.Create(path)
        do! data.CopyToAsync(fs)
    }

let downloadJMdictAsync (hc: HttpClient) =
    downloadGZippedResourceAsync hc "http://ftp.edrdg.org/pub/Nihongo/JMdict.gz"

let downloadJMnedictAsync (hc: HttpClient) =
    downloadGZippedResourceAsync hc "http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz"

let downloadKanjidic2Async (hc: HttpClient) =
    downloadGZippedResourceAsync hc "http://ftp.edrdg.org/pub/Nihongo/kanjidic2.xml.gz"

let downloadRadicalFilesAsync (hc: HttpClient) (dir: string) =
    task {
        use! stream = hc.GetStreamAsync("http://ftp.edrdg.org/pub/Nihongo/kradzip.zip")
        use archive = new ZipArchive(stream)
        let files = [ "kradfile"; "kradfile2"; "radkfile"; "radkfile2" ]

        archive.Entries
        |> Seq.filter (fun x -> List.contains x.Name files)
        |> Seq.iter (fun x -> x.ExtractToFile(Path.Combine(dir, x.Name), true))
    }

let downloadCJKRadicalsFileAsync (hc: HttpClient) (path: string) =
    task {
        use! stream = hc.GetStreamAsync("https://www.unicode.org/Public/UCD/latest/ucd/CJKRadicals.txt")
        use fs = File.Create(path)
        do! stream.CopyToAsync(fs)
    }

let downloadEquivalentUnifiedIdeographFileAsync (hc: HttpClient) (path: string) =
    task {
        use! stream = hc.GetStreamAsync("https://www.unicode.org/Public/UCD/latest/ucd/EquivalentUnifiedIdeograph.txt")
        use fs = File.Create(path)
        do! stream.CopyToAsync(fs)
    }

let downloadDerivedNameFileAsync (hc: HttpClient) (path: string) =
    task {
        use! stream = hc.GetStreamAsync("https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedName.txt")
        use fs = File.Create(path)
        do! stream.CopyToAsync(fs)
    }

let depaginateAsync (f: string -> Task<WaniKaniCollection<'T>>) (url: string) =
    let rec loop acc (url: string) =
        task {
            let! response = f url
            let newAcc = Seq.append acc response.data

            if response.pages.next_url.IsSome then
                return! loop newAcc response.pages.next_url.Value
            else
                return newAcc
        }

    task {
        let! res = loop Seq.empty url
        return Seq.toList res
    }

let getWaniKanjiCollectionAsync<'T> (url: string) (apiKey: string) (hc: HttpClient) =
    depaginateAsync
        (fun url ->
            task {
                use req = new HttpRequestMessage(HttpMethod.Get, url)
                req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", apiKey)
                req.Headers.Add("Wanikani-Revision", "20170710")
                use! response = hc.SendAsync(req)
                response.EnsureSuccessStatusCode() |> ignore
                use! stream = response.Content.ReadAsStreamAsync()
                return! JsonSerializer.DeserializeAsync<WaniKaniCollection<'T>>(stream)
            })
        url

let getWaniKaniRadicalsAsync (apiKey: string) (hc: HttpClient) =
    getWaniKanjiCollectionAsync<WaniKaniRadical> "https://api.wanikani.com/v2/subjects?types=radical" apiKey hc

let getWaniKaniKanjiAsync (apiKey: string) (hc: HttpClient) =
    getWaniKanjiCollectionAsync<WaniKaniKanji> "https://api.wanikani.com/v2/subjects?types=kanji" apiKey hc

let jsonSerializerOptions =
    JsonSerializerOptions(WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

let downloadWaniKaniRadicalsAsync (apiKey: string) (hc: HttpClient) (path: string) =
    task {
        let! radicals = getWaniKaniRadicalsAsync apiKey hc
        let serializedRadicals = JsonSerializer.Serialize(radicals, jsonSerializerOptions)
        do! File.WriteAllTextAsync(path, serializedRadicals)
    }

let downloadWaniKaniKanjiAsync (apiKey: string) (hc: HttpClient) (path: string) =
    task {
        let! kanji = getWaniKaniKanjiAsync apiKey hc
        let serializedKanji = JsonSerializer.Serialize(kanji, jsonSerializerOptions)
        do! File.WriteAllTextAsync(path, serializedKanji)
    }

let downloadDataAsync () =
    task {
        let apiKey = Environment.GetEnvironmentVariable("WANI_KANI_API_KEY")

        if String.IsNullOrEmpty(apiKey) then
            failwith "The environment variable WANI_KANI_API_KEY must be defined"

        let dir = "data"
        Directory.CreateDirectory(dir) |> ignore
        let hc = new HttpClient()

        let! _ =
            Task.WhenAll(
                [
                    downloadCJKRadicalsFileAsync hc (Path.Combine(dir, "CJKRadicals.txt"))
                    downloadEquivalentUnifiedIdeographFileAsync hc (Path.Combine(dir, "EquivalentUnifiedIdeograph.txt"))
                    downloadDerivedNameFileAsync hc (Path.Combine(dir, "DerivedName.txt"))
                    downloadJMdictAsync hc (Path.Combine(dir, "JMdict.xml"))
                    downloadJMnedictAsync hc (Path.Combine(dir, "JMnedict.xml"))
                    downloadKanjidic2Async hc (Path.Combine(dir, "kanjidic2.xml"))
                    downloadRadicalFilesAsync hc dir
                    downloadWaniKaniRadicalsAsync apiKey hc (Path.Combine(dir, "wani-kani-radicals.json"))
                    downloadWaniKaniKanjiAsync apiKey hc (Path.Combine(dir, "wani-kani-kanji.json"))
                ]
            )

        return ()
    }

let populateTables (ctx: KensakuConnection) =
    task {
        let! equivalentCharacters =
            Unicode.EquivalentUnifiedIdeograph.GetCharactersAsync("data/EquivalentUnifiedIdeograph.txt")

        equivalentCharacters
        |> Map.values
        |> Seq.distinct
        |> Seq.toList
        |> Schema.populateEquivalentCharacters ctx

        let getVariants (character: Rune) =
            equivalentCharacters
            |> Map.tryFind character
            |> Option.defaultValue (Set.singleton character)

        let! cjkRadicals = Unicode.CJKRadicals.GetCJKRadicals("data/CJKRadicals.txt")
        Schema.populateCJKRadicals ctx getVariants cjkRadicals

        JMdict.ParseEntriesAsync("data/JMdict.xml")
        |> TaskSeq.toSeq
        |> Schema.populateJMdictEntries ctx

        JMnedict.ParseEntriesAsync("data/JMnedict.xml")
        |> TaskSeq.toSeq
        |> Schema.populateJMnedictEntries ctx

        let! kanjidic2Info = Kanjidic2.ParseInfoAsync("data/kanjidic2.xml")
        Schema.populateKanjidic2Info ctx kanjidic2Info

        Kanjidic2.ParseEntriesAsync("data/kanjidic2.xml")
        |> TaskSeq.toSeq
        |> Schema.populateKanjidic2Entries ctx

        let! waniKaniRadicals = WaniKani.ParseRadicalsAsync("data/wani-kani-radicals.json")
        let! waniKaniKanji = WaniKani.ParseKanjiAsync("data/wani-kani-kanji.json")
        Schema.populateWaniKaniRadicals ctx getVariants waniKaniRadicals waniKaniKanji

        let encoding = Encoding.GetEncoding("EUC-JP")
        let! radkEntries = RadkFile.ParseEntriesAsync("data/radkfile", encoding)
        let! radk2Entries = RadkFile.ParseEntriesAsync("data/radkfile2", encoding)
        let combinedEntries = RadkFile.CombineEntries(radkEntries, radk2Entries)
        Schema.populateRadicals ctx getVariants combinedEntries

        let! derivedRadicalNames = Unicode.DerivedName.ParseDerivedRadicalNamesAsync("data/DerivedName.txt")
        Schema.populateDerivedRadicalNames ctx derivedRadicalNames
    }

let createDb () =
    task {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        do! downloadDataAsync ()
        use ctx = new KensakuConnection("Data Source=data/kensaku.db")
        do! ctx.OpenAsync()
        Schema.createSchema ctx
        do! populateTables ctx
    }

[<EntryPoint>]
let main argv =
    createDb () |> Async.AwaitTask |> Async.RunSynchronously
    0
