open Microsoft.Data.Sqlite
open Kensaku
open Kensaku.Core

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    Database.registerTypeHandlers ()
    use ctx = new SqliteConnection("Data Source=data/kensaku.db")
    let characters = getCharactersWithStrokeCount 7 ctx
    for c in characters do
        printfn "%A" c.Value
        printfn "%A" c.Grade
    0
