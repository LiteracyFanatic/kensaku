namespace Kensaku.Core

open System.Text

[<AutoOpen>]
module Utilities =
    let internal sql (input: string) = input

    module String =
        let getRunes (input: string) = [ for rune in input.EnumerateRunes() -> rune ]

    module Char =
        let isHiragana (c: char) = c >= '\u3040' && c <= '\u309f'

        let isKatakana (c: char) = c >= '\u30a0' && c <= '\u30ff'

        let isKana (c: char) = isHiragana c || isKatakana c

        let isKanji (c: char) =
            (c >= '\u4e00' && c <= '\u9fcf')
            || (c >= '\uf900' && c <= '\ufaff')
            || (c >= '\u3400' && c <= '\u4dbf')

        let isJapanese (c: char) = isKana c || isKanji c

    let inline rune input = Rune.GetRuneAt(string input, 0)
