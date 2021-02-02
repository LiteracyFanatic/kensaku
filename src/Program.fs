open System.Text
open System.IO
open Microsoft.Data.Sqlite

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    File.Delete("data/kensaku.db")
    use connection = new SqliteConnection("Data Source=data/kensaku.db")
    connection.Open()
    Database.createSchema connection
    Database.populateTables connection
    connection.Close()
    0
