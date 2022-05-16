open System.Text
open Microsoft.Data.Sqlite
open Kensaku
open Kensaku.Core.Kanji

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    Database.Schema.registerTypeHandlers ()
    use ctx = new SqliteConnection("Data Source=data/kensaku.db")
    let query = {
        MinStrokeCount = Some 6
        MaxStrokeCount = Some 8
        IncludeStrokeMiscounts = true
        SearchRadicals = []
        CharacterCode = Some (SkipCode "2-1-7")
        CharacterReading = None
        Nanori = None
        CommonOnly = true
        Pattern = None
        KeyRadical = None
    }
    let kanji = getKanji query ctx
    for k in kanji do
        printfn "%A" k

    0
