namespace Kensaku.DataSources

open System
open System.IO
open System.Text.Encodings.Web
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading

/// <summary>
/// Represents a WaniKani API subject wrapper object.
/// </summary>
type WaniKaniData<'T> = {
    id: int
    object: string
    url: string
    data_updated_at: DateTime
    data: 'T
}

/// <summary>
/// Represents a paged WaniKani API collection response.
/// </summary>
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

/// <summary>
/// Represents an SVG radical image served by WaniKani.
/// </summary>
type WaniKaniSvg = {
    url: string
    metadata: {| inline_styles: bool |}
    content_type: string
}

/// <summary>
/// Represents a PNG radical image served by WaniKani.
/// </summary>
type WaniKaniPng = {
    url: string
    metadata: {|
        color: string
        dimensions: string
        style_name: string
    |}
    content_type: string
}

/// <summary>
/// Union of radical image representations returned by WaniKani.
/// </summary>
[<JsonConverter(typeof<WaniKaniImageJsonConverter>)>]
type WaniKaniImage =
    | Svg of WaniKaniSvg
    | Png of WaniKaniPng

and private WaniKaniImageJsonConverter() =
    inherit JsonConverter<WaniKaniImage>()

    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        use doc = JsonDocument.ParseValue(&reader)
        let el = doc.RootElement

        match el.GetProperty("content_type").GetString() with
        | "image/svg+xml" -> Svg(JsonSerializer.Deserialize<WaniKaniSvg>(el, options))
        | "image/png" -> Png(JsonSerializer.Deserialize<WaniKaniPng>(el, options))
        | contentType -> raise (JsonException($"Unrecognized content_type: \"%s{contentType}\""))

    override this.Write(writer: Utf8JsonWriter, value: WaniKaniImage, options: JsonSerializerOptions) =
        match value with
        | Svg svg -> JsonSerializer.Serialize(writer, svg, options)
        | Png png -> JsonSerializer.Serialize(writer, png, options)

/// <summary>
/// Represents the data payload for a WaniKani radical subject.
/// </summary>
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

/// <summary>
/// Represents the data payload for a WaniKani kanji subject.
/// </summary>
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

/// <summary>
/// Provides methods to parse WaniKani radicals and kanji asynchronously.
/// </summary>
[<AbstractClass; Sealed>]
type WaniKani =
    static let jsonSerializerOptions =
        JsonSerializerOptions(Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

    /// <summary>
    /// Parses WaniKani radicals from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the WaniKani radicals data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a list of <see cref="WaniKaniData{WaniKaniRadical}"/> wrapping radical subjects.</returns>
    static member ParseRadicalsAsync(stream: Stream, ?ct: CancellationToken) =
        let ct = defaultArg ct CancellationToken.None
        JsonSerializer.DeserializeAsync<WaniKaniData<WaniKaniRadical> list>(stream, jsonSerializerOptions, ct)

    /// <summary>
    /// Parses WaniKani radicals from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path containing the WaniKani radicals data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a list of <see cref="WaniKaniData{WaniKaniRadical}"/> wrapping radical subjects.</returns>
    static member ParseRadicalsAsync(path: string, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        WaniKani.ParseRadicalsAsync(stream, ?ct = ct)

    /// <summary>
    /// Parses WaniKani kanji from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the WaniKani kanji data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a list of <see cref="WaniKaniData{WaniKaniKanji}"/> wrapping kanji subjects.</returns>
    static member ParseKanjiAsync(stream: Stream, ?ct: CancellationToken) =
        let ct = defaultArg ct CancellationToken.None
        JsonSerializer.DeserializeAsync<WaniKaniData<WaniKaniKanji> list>(stream, jsonSerializerOptions, ct)

    /// <summary>
    /// Parses WaniKani kanji from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path containing the WaniKani kanji data.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a list of <see cref="WaniKaniData{WaniKaniKanji}"/> wrapping kanji subjects.</returns>
    static member ParseKanjiAsync(path: string, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        WaniKani.ParseKanjiAsync(stream, ?ct = ct)
