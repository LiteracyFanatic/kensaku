open System.Text

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    DataParsing.getJMdictEntries ()
    |> Seq.toList
    |> printfn "%A"
    0
