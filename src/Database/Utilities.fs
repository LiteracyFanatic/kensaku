[<AutoOpen>]
module Kensaku.Utilities

open System.Text
open System.Xml.Linq

module Option =
    let collect f = Option.map f >> Option.flatten

type XAttribute with
    member this.TryGetValue() =
        this |> Option.ofObj |> Option.map (_.Value)

let isHiragana (c: char) = c >= '\u3040' && c <= '\u309f'

let isKatakana (c: char) = c >= '\u30a0' && c <= '\u30ff'

let isKana (c: char) = isHiragana c || isKatakana c

let isKanji (c: char) =
    (c >= '\u4e00' && c <= '\u9fcf')
    || (c >= '\uf900' && c <= '\ufaff')
    || (c >= '\u3400' && c <= '\u4dbf')

let isJapanese (c: char) = isKana c || isKanji c

let inline rune input = Rune.GetRuneAt(string input, 0)

let sql (input: string) = input
