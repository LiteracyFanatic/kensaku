open System
open System.Text
open System.IO
open Microsoft.Data.Sqlite
open Dapper

type OptionHandler<'T>() =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value
        then None
        else Some (value :?> 'T)

type RuneHandler() =
    inherit SqlMapper.TypeHandler<Rune>()

    override this.SetValue(param, value) =
        param.Value <- string value

    override this.Parse(value) =
        rune value

let registerTypeHandlers () =
    SqlMapper.AddTypeHandler(OptionHandler<string>())
    SqlMapper.AddTypeHandler(OptionHandler<int>())
    SqlMapper.AddTypeHandler(RuneHandler())

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    File.Delete("data/kensaku.db")
    registerTypeHandlers ()
    use ctx = new SqliteConnection("Data Source=data/kensaku.db")
    ctx.Open()
    Database.createSchema ctx
    Database.populateTables ctx
    ctx.Close()
    0
