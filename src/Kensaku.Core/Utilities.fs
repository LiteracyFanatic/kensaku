namespace Kensaku.Core

open System.Text

[<AutoOpen>]
module Utilities =
    let internal sql (input: string) = input

    module String =
        let getRunes (input: string) = [ for rune in input.EnumerateRunes() -> rune ]

    let inline rune input = Rune.GetRuneAt(string input, 0)
