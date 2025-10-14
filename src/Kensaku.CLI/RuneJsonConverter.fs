namespace Kensaku.CLI

open System
open System.Text
open System.Text.Json
open System.Text.Json.Serialization

open Kensaku.Core.Utilities

type RuneJsonConverter() =
    inherit JsonConverter<Rune>()

    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        match reader.GetString() |> String.getRunes with
        | [ rune ] -> rune
        | _ -> raise (JsonException("Expected a single character"))

    override this.Write(writer: Utf8JsonWriter, value: Rune, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())
