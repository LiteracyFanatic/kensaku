namespace Kensaku.DataSources

open System.Text
open System.Threading

/// <summary>
/// Represents the radical glyph (if any) and its unified ideograph code point.
/// </summary>
type CJKRadicalValue = {
    RadicalCharacter: Rune option
    UnifiedIdeographCharacter: Rune
}

/// <summary>
/// Represents a CJK radical with its standard form and optional simplified derivatives.
/// </summary>
type CJKRadical = {
    RadicalNumber: int
    Standard: CJKRadicalValue
    Simplified: CJKRadicalValue list
}

module Unicode =
    open System
    open System.Collections.Generic
    open System.IO
    open System.Text.RegularExpressions

    module private EquivalentUnifiedIdeograph =
        let parseEquivalentCharacters (text: string) =
            let groups = ResizeArray<Set<Rune>>()

            for line in text.ReplaceLineEndings().Split(Environment.NewLine) do
                let m =
                    Regex.Match(
                        line,
                        @"^(?<in1>\w{4})(?:\.\.(?<in2>\w{4}))? +; +(?<out>\w{4,6}) +# +(?:\[(?<n>\d)\])? +(?<description>.+)"
                    )

                if m.Success then
                    let in1 = hexStringToInt (m.Groups["in1"].Value)
                    let out = hexStringToInt (m.Groups["out"].Value)

                    let newGroup =
                        match m.Groups["in2"].Value, m.Groups["n"].Value with
                        | "", "" -> [ in1; out ]
                        | _, "" -> failwith "in2 was present but n was not"
                        | "", _ -> failwith "n was present but in2 was not"
                        | in2, _ -> out :: [ in1 .. (hexStringToInt in2) ]
                        |> List.map Rune
                        |> Set.ofList

                    match Seq.tryFindIndex (fun g -> (Set.intersect newGroup g).Count > 0) groups with
                    | Some i -> groups[i] <- groups[i] + newGroup
                    | None -> groups.Add(newGroup)

            groups
            |> Seq.collect (fun group -> group |> Seq.map (fun c -> c, group))
            |> Map.ofSeq

    /// <summary>
    /// Provides methods to parse equivalent unified ideographs asynchronously.
    /// </summary>
    [<AbstractClass; Sealed>]
    type EquivalentUnifiedIdeograph =
        /// <summary>
        /// Parses the equivalent unified ideographs from a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream containing the equivalent unified ideographs data.</param>
        /// <param name="encoding">Optional encoding of the stream. Defaults to UTF-8.</param>
        /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a map from each <see cref="Rune"/> to the set of equivalent unified <see cref="Rune"/> values containing it.</returns>
        static member GetCharactersAsync(stream: Stream, ?encoding: Encoding, ?ct: CancellationToken) =
            task {
                let encoding = defaultArg encoding Encoding.UTF8
                let ct = defaultArg ct CancellationToken.None
                use sr = new StreamReader(stream, encoding)
                let! text = sr.ReadToEndAsync(ct)
                return EquivalentUnifiedIdeograph.parseEquivalentCharacters text
            }

        /// <summary>
        /// Parses the equivalent unified ideographs from a file path asynchronously.
        /// </summary>
        /// <param name="path">The file path containing the equivalent unified ideographs data.</param>
        /// <param name="encoding">Optional encoding of the file. Defaults to UTF-8.</param>
        /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a map from each <see cref="Rune"/> to the set of equivalent unified <see cref="Rune"/> values containing it.</returns>
        static member GetCharactersAsync(path: string, ?encoding: Encoding, ?ct: CancellationToken) =
            let stream = File.OpenRead(path)
            EquivalentUnifiedIdeograph.GetCharactersAsync(stream, ?encoding = encoding, ?ct = ct)

    module private CJKRadicals =
        let parseCJKRadicals (text: string) =
            let radicals = Dictionary<int, CJKRadical>()

            for line in text.ReplaceLineEndings().Split(Environment.NewLine) do
                let m = Regex.Match(line, @"(\d+'{0,3}); (.{4})?; (.{4})")

                if m.Success then
                    let n = int (m.Groups[1].Value.Replace("'", ""))

                    let radicalValue = {
                        RadicalCharacter =
                            if m.Groups[2].Success then
                                m.Groups[2].Value |> hexStringToRune |> Some
                            else
                                None
                        UnifiedIdeographCharacter = hexStringToRune (m.Groups[3].Value)
                    }

                    let numberOfApostrophes = m.Groups[1].Value |> Seq.filter ((=) '\'') |> Seq.length

                    // 1 apostrophe indicates a Chinese simplified form. 2 or 3 apostrophes indicates a non-Chinese simplified form.
                    match numberOfApostrophes with
                    | 0 ->
                        radicals[n] <- {
                            RadicalNumber = n
                            Standard = radicalValue
                            Simplified = []
                        }
                    | 1 -> ()
                    | 2
                    | 3 -> radicals[n] <- { radicals[n] with Simplified = radicals[n].Simplified @ [ radicalValue ] }
                    | _ -> failwithf "Unexpected number of apostrophes in radical number %d: %d" n numberOfApostrophes

            radicals |> Seq.map (_.Value) |> Seq.toList

    /// <summary>
    /// Provides methods to parse CJK radicals asynchronously.
    /// </summary>
    [<AbstractClass; Sealed>]
    type CJKRadicals =
        /// <summary>
        /// Parses the CJK radicals from a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream containing the CJK radicals data.</param>
        /// <param name="encoding">Optional encoding of the stream. Defaults to UTF-8.</param>
        /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a list of parsed <see cref="CJKRadical"/> values.</returns>
        static member GetCJKRadicals(stream: Stream, ?encoding: Encoding, ?ct: CancellationToken) =
            task {
                let encoding = defaultArg encoding Encoding.UTF8
                let ct = defaultArg ct CancellationToken.None
                use sr = new StreamReader(stream, encoding)
                let! text = sr.ReadToEndAsync(ct)
                return CJKRadicals.parseCJKRadicals text
            }

        /// <summary>
        /// Parses the CJK radicals from a file path asynchronously.
        /// </summary>
        /// <param name="path">The file path containing the CJK radicals data.</param>
        /// <param name="encoding">Optional encoding of the file. Defaults to UTF-8.</param>
        /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a list of parsed <see cref="CJKRadical"/> values.</returns>
        static member GetCJKRadicals(path: string, ?encoding: Encoding, ?ct: CancellationToken) =
            let stream = File.OpenRead(path)
            CJKRadicals.GetCJKRadicals(stream, ?encoding = encoding, ?ct = ct)

    module private DerivedName =
        let private replace (pattern: string) (replacement: string) (input: string) =
            Regex.Replace(input, pattern, replacement)

        let parseDerivedRadicalNames (text: string) =
            text.ReplaceLineEndings().Split(Environment.NewLine)
            |> Array.choose (fun line ->
                let m = Regex.Match(line, @"^(\w{4,6}) +; (?:CJK|KANGXI) RADICAL (.+)")

                if m.Success then
                    let radical = hexStringToRune (m.Groups[1].Value)

                    let name =
                        m.Groups[2].Value
                        |> replace "^(C-|J-)?SIMPLIFIED " ""
                        |> replace "((?<!(SMALL|LAME)) (ONE|TWO|THREE|FOUR))$" ""

                    Some(radical, name.ToLowerInvariant())
                else
                    None)
            |> Map.ofArray

    /// <summary>
    /// Provides methods to parse derived radical names asynchronously.
    /// </summary>
    [<AbstractClass; Sealed>]
    type DerivedName =
        /// <summary>
        /// Parses the derived radical names from a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream containing the derived radical names data.</param>
        /// <param name="encoding">Optional encoding of the stream. Defaults to UTF-8.</param>
        /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a map from radical <see cref="Rune"/> values to their derived names.</returns>
        static member ParseDerivedRadicalNamesAsync(stream: Stream, ?encoding: Encoding, ?ct: CancellationToken) =
            task {
                let encoding = defaultArg encoding Encoding.UTF8
                let ct = defaultArg ct CancellationToken.None
                use sr = new StreamReader(stream, encoding)
                let! text = sr.ReadToEndAsync(ct)
                return DerivedName.parseDerivedRadicalNames text
            }

        /// <summary>
        /// Parses the derived radical names from a file path asynchronously.
        /// </summary>
        /// <param name="path">The file path containing the derived radical names data.</param>
        /// <param name="encoding">Optional encoding of the file. Defaults to UTF-8.</param>
        /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that produces a map from radical <see cref="Rune"/> values to their derived names.</returns>
        static member ParseDerivedRadicalNamesAsync(path: string, ?encoding: Encoding, ?ct: CancellationToken) =
            let stream = File.OpenRead(path)
            DerivedName.ParseDerivedRadicalNamesAsync(stream, ?encoding = encoding, ?ct = ct)
