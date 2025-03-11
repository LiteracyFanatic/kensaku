namespace CLI

open System
open System.Text
open System.Text.Json
open System.Text.Json.Serialization

type RuneJsonConverter() =
    inherit JsonConverter<Rune>()

    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        let value = reader.GetString()

        if value.Length = 1 then
            Rune(value[0])
        else
            raise (JsonException("Expected a single character"))

    override this.Write(writer: Utf8JsonWriter, value: Rune, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())
