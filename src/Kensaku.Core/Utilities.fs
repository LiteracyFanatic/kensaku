namespace Kensaku.Core

open System.Text

[<AutoOpen>]
module Utilities =
    let internal sql (input: string) = input

    /// <summary>
    /// String utility functions.
    /// </summary>
    module String =
        /// <summary>
        /// Gets a list of runes from a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>A list of Unicode runes.</returns>
        let getRunes (input: string) = [ for rune in input.EnumerateRunes() -> rune ]

    /// <summary>
    /// Creates a rune from a string or character.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <returns>A Unicode rune.</returns>
    let inline rune input = Rune.GetRuneAt(string input, 0)
