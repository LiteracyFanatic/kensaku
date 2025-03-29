namespace Kensaku.CLI

[<AutoOpen>]
module Utilities =
    module Char =
        let isJapanese (c: char) =
            // Hiragana
            c >= '\u3040' && c <= '\u309f'
            // Katakana
            || (c >= '\u30a0' && c <= '\u30ff')
            // CJK Unified Ideographs
            || (c >= '\u4e00' && c <= '\u9fcf')
            // CJK Compatibility Ideographs
            || (c >= '\uf900' && c <= '\ufaff')
            // CJK Unified Ideographs Extension A
            || (c >= '\u3400' && c <= '\u4dbf')
            // CJK Radicals Supplement
            || (c >= '\u2E80' && c <= '\u2eff')
            // Kangxi Radicals
            || (c >= '\u2f00' && c <= '\u2fdf')
