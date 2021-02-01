open System.Text

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    DataParsing.getKanjidic2Info ()
    |> printfn "%A"
    DataParsing.getKanjidic2Entries ()
    |> printfn "%A"
    0
