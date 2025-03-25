namespace Kensaku.DataSources

open System.IO
open System.Text
open System.Threading

type RadkEntry = {
    Radical: Rune
    StrokeCount: int
    Kanji: Set<Rune>
}

module RadkFile =
    open System.Text.RegularExpressions

    let private replacements =
        Map [
            '化', '\u2E85'
            '个', '\u2F09'
            '并', '\u4E37'
            '刈', '\u2E89'
            '込', '\u2ECC'
            '尚', '\u2E8C'
            '忙', '\u2E96'
            '扎', '\u2E97'
            '汁', '\u2EA1'
            '犯', '\u2EA8'
            '艾', '\u2EBE'
            '邦', '\u2ECF'
            '阡', '\u2ED9'
            '老', '\u2EB9'
            '杰', '\u2EA3'
            '礼', '\u2EAD'
            '疔', '\u2F67'
            '禹', '\u2F71'
            '初', '\u2EC2'
            '買', '\u2EB2'
            '滴', '\u5547'
        ]

    let private charToRadicalNumber =
        Map [
            rune "｜", 2 // 00FF5C | Halfwidth and Fullwidth Forms | FULLWIDTH VERTICAL LINE
            rune "ノ", 0 // 4? 0030CE | Katakana                      | KATAKANA LETTER NO
            rune "⺅", 9 // 002E85 | CJK Radicals Supplement       | CJK RADICAL PERSON
            rune "ハ", 12 // 0030CF | Katakana                      | KATAKANA LETTER HA
            rune "丷", 12 // 004E37 | CJK Unified Ideographs        | No character name available
            rune "⺉", 18 // 002E89 | CJK Radicals Supplement       | CJK RADICAL KNIFE TWO
            rune "マ", 0 // 0030DE | Katakana                      | KATAKANA LETTER MA
            rune "九", 19 // 004E5D | CJK Unified Ideographs        | CJK character Nelson  146
            rune "ユ", 0 // 0030E6 | Katakana                      | KATAKANA LETTER YU
            rune "乃", 0 // 004E43 | CJK Unified Ideographs        | CJK character Nelson  145
            rune "乞", 0 // 004E5E | CJK Unified Ideographs        | CJK character Nelson  262
            rune "⺌", 42 // 002E8C | CJK Radicals Supplement       | CJK RADICAL SMALL ONE
            rune "川", 47 // 005DDD | CJK Unified Ideographs        | CJK character Nelson 1447
            rune "已", 49 // 005DF2 | CJK Unified Ideographs        | CJK character Nelson 1461
            rune "ヨ", 58 // 0030E8 | Katakana                      | KATAKANA LETTER YO
            rune "彑", 58 // 005F51 | CJK Unified Ideographs        | No character name available
            rune "⺖", 61 // 002E96 | CJK Radicals Supplement       | CJK RADICAL HEART ONE
            rune "⺗", 61 // 002E97 | CJK Radicals Supplement       | CJK RADICAL HEART TWO
            rune "⺡", 0 // 42? 002EA1 | CJK Radicals Supplement       | CJK RADICAL WATER ONE
            rune "⺨", 94 // 002EA8 | CJK Radicals Supplement       | CJK RADICAL DOG
            rune "⺾", 140 // 002EBE | CJK Radicals Supplement       | CJK RADICAL GRASS ONE
            rune "⻏", 163 // 002ECF | CJK Radicals Supplement       | CJK RADICAL CITY
            rune "也", 0 // 004E5F | CJK Unified Ideographs        | CJK character Nelson   75
            rune "亡", 0 // 004EA1 | CJK Unified Ideographs        | CJK character Nelson  281
            rune "及", 0 // 0053CA | CJK Unified Ideographs        | CJK character Nelson  154, 157
            rune "久", 0 // 004E45 | CJK Unified Ideographs        | CJK character Nelson  153
            rune "⺹", 125 // 002EB9 | CJK Radicals Supplement       | CJK RADICAL OLD
            rune "戸", 63 // 006238 | CJK Unified Ideographs        | CJK character Nelson 1817
            rune "攵", 66 // 006535 | CJK Unified Ideographs        | No character name available
            rune "⺣", 86 // 002EA3 | CJK Radicals Supplement       | CJK RADICAL FIRE
            rune "⺭", 113 // 002EAD | CJK Radicals Supplement       | CJK RADICAL SPIRIT TWO
            rune "王", 96 // 00738B | CJK Unified Ideographs        | CJK character Nelson 2922
            rune "元", 0 // 005143 | CJK Unified Ideographs        | CJK character Nelson  275
            rune "井", 0 // 004E95 | CJK Unified Ideographs        | CJK character Nelson  165
            rune "勿", 0 // 0052FF | CJK Unified Ideographs        | CJK character Nelson  743
            rune "尤", 0 // 43? 005C24 | CJK Unified Ideographs        | CJK character Nelson  128
            rune "五", 0 // 004E94 | CJK Unified Ideographs        | CJK character Nelson   15
            rune "屯", 0 // 005C6F | CJK Unified Ideographs        | CJK character Nelson  264
            rune "巴", 0 // 005DF4 | CJK Unified Ideographs        | CJK character Nelson  263
            rune "⻂", 145 // 002EC2 | CJK Radicals Supplement       | CJK RADICAL CLOTHES
            rune "世", 0 // 004E16 | CJK Unified Ideographs        | CJK character Nelson   84,  95
            rune "巨", 0 // 005DE8 | CJK Unified Ideographs        | CJK character Nelson   19, 758
            rune "冊", 0 // 00518A | CJK Unified Ideographs        | CJK character Nelson   88
            rune "母", 80 // 006BCD | CJK Unified Ideographs        | CJK character Nelson 2466
            rune "⺲", 122 // 109? 002EB2 | CJK Radicals Supplement       | CJK RADICAL NET TWO
            rune "西", 146 // 00897F | CJK Unified Ideographs        | CJK character Nelson 4273
            rune "青", 174 // 009752 | CJK Unified Ideographs        | CJK character Nelson 5076
            rune "奄", 0 // 005944 | CJK Unified Ideographs        | CJK character Nelson 1173
            rune "岡", 0 // 005CA1 | CJK Unified Ideographs        | CJK character Nelson  621
            rune "免", 0 // 00514D | CJK Unified Ideographs        | CJK character Nelson  189, 573
            rune "斉", 210 // 006589 | CJK Unified Ideographs        | CJK character Nelson 5423
            rune "品", 0 // 0054C1 | CJK Unified Ideographs        | CJK character Nelson  889, 923
            rune "竜", 0 // 007ADC | CJK Unified Ideographs        | CJK character Nelson 3351,5440
            rune "亀", 0 // 004E80 | CJK Unified Ideographs        | CJK character Nelson 5445
            rune "啇", 0 // 005547 | CJK Unified Ideographs        | No character name available
            rune "黒", 0 // 203? 009ED2 | CJK Unified Ideographs        | CJK character Nelson 5403
            rune "無", 0 // 007121 | CJK Unified Ideographs        | CJK character Nelson 2773
            rune "歯", 0 // 006B6F | CJK Unified Ideographs        | CJK character Nelson 5428
        ]

    let tryGetRadicalNumber (radical: Rune) =
        match charToRadicalNumber.TryFind radical with
        | Some 0
        | None -> None
        | n -> n

    let internal parseEntries (text: string) =
        Regex.Matches(text, @"^\$ (.) (\d+).*$([^$]+)", RegexOptions.Multiline)
        |> Seq.toList
        |> List.map (fun m ->
            let radical = char (m.Groups[1].Value)

            {
                Radical = radical |> replacements.TryFind |> Option.defaultValue radical |> rune
                StrokeCount = int m.Groups[2].Value
                // Remove newlines and katakana middle dots
                Kanji = set (m.Groups[3].Value.EnumerateRunes()) - set [ rune '\n'; rune '\u30FB' ]
            })

    let internal combineEntries (radkFileEntries: RadkEntry list) (radkFile2Entries: RadkEntry list) =
        radkFileEntries @ radkFile2Entries
        |> List.groupBy _.Radical
        |> List.map (fun (radical, pair) ->
            match pair with
            | [ a; b ] -> { a with Kanji = a.Kanji + b.Kanji }
            | _ -> failwithf "Expected exactly one entry for %A in each radk file. Received %A." radical pair)

[<AbstractClass; Sealed>]
type RadkFile =
    static member ParseEntriesAsync(stream: Stream, ?encoding: Encoding, ?ct: CancellationToken) =
        task {
            let encoding = defaultArg encoding Encoding.UTF8
            let ct = defaultArg ct CancellationToken.None
            use sr = new StreamReader(stream, encoding)
            let! text = sr.ReadToEndAsync(ct)
            return RadkFile.parseEntries text
        }

    static member ParseEntriesAsync(path: string, ?encoding: Encoding, ?ct: CancellationToken) =
        let stream = File.OpenRead(path)
        RadkFile.ParseEntriesAsync(stream, ?encoding = encoding, ?ct = ct)

    static member CombineEntries(radkFileEntries: RadkEntry list, radkFile2Entries: RadkEntry list) =
        RadkFile.combineEntries radkFileEntries radkFile2Entries
