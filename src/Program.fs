open System.Text

[<EntryPoint>]
let main argv =
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    DataParsing.getJmdictEntries ()
    |> Seq.take 10
    |> Seq.toList
    |> printfn "%A"
    0
