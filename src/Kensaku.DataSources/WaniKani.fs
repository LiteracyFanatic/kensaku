namespace Kensaku.DataSources

open System
open System.IO
open System.Text.Encodings.Web
open System.Text.Json
open System.Text.Json.Serialization

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
    metadata: {| inline_styles: bool |}
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
        | "image/svg+xml" -> Svg(JsonSerializer.Deserialize<WaniKaniSvg>(&reader, options))
        | "image/png" -> Png(JsonSerializer.Deserialize<WaniKaniPng>(&reader, options))
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
    characters: string option
    character_images: WaniKaniImage[]
    meanings:
        {|
            meaning: string
            primary: bool
            accepted_answer: bool
        |}[]
    auxiliary_meanings:
        {|
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
    meanings:
        {|
            meaning: string
            primary: bool
            accepted_answer: bool
        |}[]
    auxiliary_meanings:
        {|
            ``type``: string
            meaning: string
        |}[]
    readings:
        {|
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

module WaniKani =
    let private jsonSerializerOptions =
        JsonSerializerOptions(Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

    let getRadicals (path: string) =
        use stream = File.OpenRead(path)
        JsonSerializer.Deserialize<WaniKaniData<WaniKaniRadical> list>(stream, jsonSerializerOptions)

    let getKanji (path: string) =
        use stream = File.OpenRead(path)
        JsonSerializer.Deserialize<WaniKaniData<WaniKaniKanji> list>(stream, jsonSerializerOptions)
