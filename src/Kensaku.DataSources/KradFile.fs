namespace Kensaku.DataSources

open System.IO
open System.Text
open System.Threading

module private KradFile =
    open System.Text.RegularExpressions

    let parseReplacements (text: string) =
        Regex.Matches(text, @"# (.) U\+([a-zA-Z0-9]{4,6})")
        |> Seq.map (fun m -> rune m.Groups[1].Value, hexStringToRune m.Groups[2].Value)
        |> Map.ofSeq

[<AbstractClass; Sealed>]
type KradFile =
    static member ParseReplacementsAsync(stream: Stream, ?encoding: Encoding, ?ct: CancellationToken) =
        task {
            let encoding = defaultArg encoding Encoding.UTF8
            let ct = defaultArg ct CancellationToken.None
            use sr = new StreamReader(stream, encoding)
            let! text = sr.ReadToEndAsync(ct)
            return KradFile.parseReplacements text |> Map.toSeq |> dict
        }

    static member ParseReplacementsAsync(path: string, ?encoding: Encoding, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        KradFile.ParseReplacementsAsync(stream, ?encoding = encoding, ?ct = ct)
