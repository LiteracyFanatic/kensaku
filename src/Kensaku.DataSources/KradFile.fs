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

/// <summary>
/// Provides methods to parse replacement mappings found in the kradfile headers.
/// </summary>
[<AbstractClass; Sealed>]
type KradFile =
    /// <summary>
    /// Parses the replacement characters from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the kradfile data.</param>
    /// <param name="encoding">Optional encoding. Defaults to UTF-8.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a dictionary mapping source <see cref="Rune"/> values to replacement <see cref="Rune"/> values.</returns>
    static member ParseReplacementsAsync(stream: Stream, ?encoding: Encoding, ?ct: CancellationToken) =
        task {
            let encoding = defaultArg encoding Encoding.UTF8
            let ct = defaultArg ct CancellationToken.None
            use sr = new StreamReader(stream, encoding)
            let! text = sr.ReadToEndAsync(ct)
            return KradFile.parseReplacements text |> Map.toSeq |> dict
        }

    /// <summary>
    /// Parses the replacement characters from a file path asynchronously.
    /// </summary>
    /// <param name="path">Path to the kradfile.</param>
    /// <param name="encoding">Optional encoding. Defaults to UTF-8.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a dictionary mapping source <see cref="Rune"/> values to replacement <see cref="Rune"/> values.</returns>
    static member ParseReplacementsAsync(path: string, ?encoding: Encoding, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        KradFile.ParseReplacementsAsync(stream, ?encoding = encoding, ?ct = ct)
