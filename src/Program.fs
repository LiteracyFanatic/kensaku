open System.Text

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    DataParsing.getJMnedictEntries ()
    |> Seq.toList
    |> printfn "%A"
    0
