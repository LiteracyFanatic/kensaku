namespace Kensaku.DataSources

open System.Text

type CJKRadicalValue = {
    RadicalCharacter: Rune
    UnifiedIdeographCharacter: Rune
}

type CJKRadical = {
    RadicalNumber: int
    Standard: CJKRadicalValue
    Simplified: CJKRadicalValue option
}

module Unicode =
    open System
    open System.Collections.Generic
    open System.Globalization
    open System.IO
    open System.Text.RegularExpressions

    let private hexStringToInt (hexString: string) =
        Int32.Parse(hexString, NumberStyles.HexNumber)

    let private hexStringToRune (hexString: string) =
        Rune(Int32.Parse(hexString, NumberStyles.HexNumber))

    [<RequireQualifiedAccess>]
    module EquivalentUnifiedIdeograph =
        let getEquivalentCharacters (path: string) =
            let groups = ResizeArray<Set<Rune>>()

            for line in File.ReadAllLines(path) do
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

    [<RequireQualifiedAccess>]
    module CJKRadicals =
        let getCJKRadicals (path: string) =
            let radicals = Dictionary<int, CJKRadical>()

            for line in File.ReadAllLines(path) do
                let m = Regex.Match(line, @"(\d+'?); (.{4}); (.{4})")

                if m.Success then
                    let n = int (m.Groups[1].Value.Replace("'", ""))

                    let radicalValue = {
                        RadicalCharacter = hexStringToRune (m.Groups[2].Value)
                        UnifiedIdeographCharacter = hexStringToRune (m.Groups[3].Value)
                    }

                    if m.Groups[1].Value.EndsWith("'") then
                        radicals[n] <- { radicals[n] with Simplified = Some radicalValue }
                    else
                        radicals[n] <- {
                            RadicalNumber = n
                            Standard = radicalValue
                            Simplified = None
                        }

            radicals |> Seq.map (_.Value) |> Seq.toList

    [<RequireQualifiedAccess>]
    module DerivedName =
        let private replace (pattern: string) (replacement: string) (input: string) =
            Regex.Replace(input, pattern, replacement)

        let getDerivedRadicalNames (path: string) =
            File.ReadAllLines(path)
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
