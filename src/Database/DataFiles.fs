module Kensaku.DataFiles

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Net.Http
open System.IO
open System.IO.Compression
open System.Threading.Tasks
open System.Text.Encodings.Web

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
        let files = [
            "kradfile"
            "kradfile2"
            "radkfile"
            "radkfile2"
        ]
        archive.Entries
        |> Seq.filter (fun x -> List.contains x.Name files)
        |> Seq.iter (fun x -> x.ExtractToFile(Path.Combine(dir, x.Name), true))
    }

type WaniKaniData<'T> = {
    id: int
    object: string
    url: string
    data_updated_at: DateTime
    data: 'T
}

type WaniKaniCollection<'T> = {
    object: string
    url: string
    pages: {|
        per_page: int
        next_url: string option
        previous_url: string option
    |}
    total_count: int
    data_updated_at: DateTime
    data: WaniKaniData<'T>[]
}

type WaniKaniSvg = {
    url: string
    metadata: {|
        inline_styles: bool
    |}
    content_type: string
}

type WaniKaniPng = {
    url: string
    metadata: {|
        color: string
        dimensions: string
        style_name: string
    |}
    content_type: string
}

[<JsonConverter(typeof<WaniKaniImageJsonConverter>)>]
type WaniKaniImage =
    | Svg of WaniKaniSvg
    | Png of WaniKaniPng

and WaniKaniImageJsonConverter() =
    inherit JsonConverter<WaniKaniImage>()
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        // Clone reader so we don't change its position
        let mutable readerClone = reader
        let doc = JsonDocument.ParseValue(&readerClone).RootElement
        match doc.GetProperty("content_type").GetString() with
        | "image/svg+xml" -> Svg (JsonSerializer.Deserialize<WaniKaniSvg>(&reader, options))
        | "image/png" -> Png (JsonSerializer.Deserialize<WaniKaniPng>(&reader, options))
        | contentType -> raise (JsonException($"Unrecognized content_type: \"%s{contentType}\""))

    override this.Write(writer: Utf8JsonWriter, value: WaniKaniImage, options: JsonSerializerOptions) =
        writer.WriteStartObject()
        match value with
        | Svg svg ->
            writer.WriteString(nameof svg.url, svg.url)
            writer.WritePropertyName(nameof svg.metadata)
            writer.WriteStartObject()
            writer.WriteBoolean(nameof svg.metadata.inline_styles, svg.metadata.inline_styles)
            writer.WriteEndObject()
            writer.WriteString(nameof svg.content_type, svg.content_type)
        | Png png ->
            writer.WriteString(nameof png.url, png.url)
            writer.WritePropertyName(nameof png.metadata)
            writer.WriteStartObject()
            writer.WriteString(nameof png.metadata.color, png.metadata.color)
            writer.WriteString(nameof png.metadata.dimensions, png.metadata.dimensions)
            writer.WriteString(nameof png.metadata.style_name, png.metadata.style_name)
            writer.WriteEndObject()
            writer.WriteString(nameof png.content_type, png.content_type)
        writer.WriteEndObject()

type WaniKaniRadical = {
    created_at: DateTime
    level: int
    slug: string
    hidden_at: DateTime option
    document_url: string
    characters: string
    character_images: WaniKaniImage[]
    meanings: {|
        meaning: string
        primary: bool
        accepted_answer: bool
    |}[]
    auxiliary_meanings: {|
        ``type``: string
        meaning: string
    |}[]
    amalgamation_subject_ids: int[]
    meaning_mnemonic: string
    lesson_position: int
    spaced_repetition_system_id: int
}

type WaniKaniKanji = {
    created_at: DateTime
    level: int
    slug: string
    hidden_at: DateTime option
    document_url: string
    characters: string
    meanings: {|
        meaning: string
        primary: bool
        accepted_answer: bool
    |}[]
    auxiliary_meanings: {|
        ``type``: string
        meaning: string
    |}[]
    readings: {|
        ``type``: string
        primary: bool
        reading: string
        accepted_answer: bool
    |}[]
    component_subject_ids: int[]
    amalgamation_subject_ids: int[]
    visually_similar_subject_ids: int[]
    meaning_mnemonic: string
    meaning_hint: string
    lesson_position: int
    spaced_repetition_system_id: int
}

let depaginateAsync (f: string -> Task<WaniKaniCollection<'T>>) (url: string) =
    let rec loop (acc) (url: string) =
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
    depaginateAsync (fun url ->
        task {
            use req = new HttpRequestMessage(HttpMethod.Get, url)
            req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", apiKey)
            req.Headers.Add("Wanikani-Revision", "20170710")
            use! response = hc.SendAsync(req)
            response.EnsureSuccessStatusCode() |> ignore
            use! stream = response.Content.ReadAsStreamAsync()
            return! JsonSerializer.DeserializeAsync<WaniKaniCollection<'T>>(stream)
        }) url

let getWaniKaniRadicalsAsync (apiKey: string) (hc: HttpClient) =
    getWaniKanjiCollectionAsync<WaniKaniRadical> "https://api.wanikani.com/v2/subjects?types=radical" apiKey hc

let getWaniKaniKanjiAsync (apiKey: string) (hc: HttpClient) =
    getWaniKanjiCollectionAsync<WaniKaniKanji> "https://api.wanikani.com/v2/subjects?types=kanji" apiKey hc

let jsonSerializerOptions =
    JsonSerializerOptions(
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

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
