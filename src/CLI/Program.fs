open System.Text
open System.Text.RegularExpressions
open System.Reflection
open System.Linq
open Microsoft.Data.Sqlite
open Kensaku
open Kensaku.Core.Kanji
open Argu

type KanjiArgs =
    | Strokes of int
    | Min_Strokes of int
    | Max_Strokes of int
    | Include_Stroke_Miscounts
    | Radicals of string
    | Skip_Code of string
    | Sh_Code of string
    | Four_Corner_Code of string
    | Deroo_Code of string
    | Reading of string
    | Meaning of string
    | Nanori of string
    | Common_Only
    | Pattern of string
    | [<MainCommand; Last>] Kanji of string list
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Strokes _ -> "search for kanji with the given number of strokes"
            | Min_Strokes _ -> "search for kanji with at least the given number of strokes"
            | Max_Strokes _ -> "search for kanji with at most the given number of strokes"
            | Include_Stroke_Miscounts -> "include kanji which are commonly mistaken to have the given number of strokes"
            | Radicals _ -> "search for kanji containing the given radicals"
            | Skip_Code _ -> "search for kanji with the given SKIP code"
            | Sh_Code _ -> """search for kanji with the given descriptor code from "The Kanji Dictionary" (Tuttle 1996) by Spahn and Hadamitzky"""
            | Four_Corner_Code _ -> "search for kanji with the given Four Corner code"
            | Deroo_Code _ -> """search for kanji with the given code from "2001 Kanji" (Bonjinsha) by Father Joseph De Roo"""
            | Reading _ -> "search for kanji with the given reading"
            | Meaning _ -> "search for kanji with a meaning that matches the given regular expression"
            | Nanori _ -> "search for kanji with the given reading when used in names"
            | Common_Only -> "include only the 2,500 most common kanji in the search"
            | Pattern _ -> "search for kanji that appear in words matching the given pattern"
            | Kanji _ -> "show info for the given kanji"

let makeWordList (words: string list) =
    let sb = StringBuilder()
    let separator =
        if words.Length > 2 then
            ", "
        else
            " "
    for i, word in List.indexed words do
        if i = words.Length - 1 then
            sb.Append($"or %s{word}") |> ignore
        else
            sb.Append($"%s{word}%s{separator}") |> ignore
    sb.ToString()

let tryParseSkipCode (code: string) =
    if Regex.IsMatch(code, "^\d-\d{1,2}-\d{1,2}$") then
        Some (SkipCode code)
    else
        None

let tryParseShCode (code: string) =
    if Regex.IsMatch(code, "^\d{1,2}\p{IsBasicLatin}\d{1,2}\.\d{1,2}$") then
        Some (ShDescCode code)
    else
        None

let tryParseFourCornerCode (code: string) =
    if Regex.IsMatch(code, "^\d{4}\.\d$") then
        Some (FourCornerCode code)
    else
        None

let tryParseDeRooCode (code: string) =
    if Regex.IsMatch(code, "^\d{3,4}$") then
        Some (DeRooCode code)
    else
        None

let postProcessSkipCode =
    tryParseSkipCode
    >> Option.defaultWith (fun () -> failwith "Invalid SKIP code")

let postProcessShCode =
    tryParseShCode
    >> Option.defaultWith (fun () -> failwith "Invalid SH code")
let postProcessFourCornerCode =
    tryParseFourCornerCode
    >> Option.defaultWith (fun () -> failwith "Invalid Four Corner code")
let postProcessDeRooCode =
    tryParseDeRooCode
    >> Option.defaultWith (fun () -> failwith "Invalid De Roo code")

let validateCodeArgs (args: ParseResults<KanjiArgs>) =
    let n =
        [
            args.Contains(Skip_Code)
            args.Contains(Sh_Code)
            args.Contains(Four_Corner_Code)
            args.Contains(Deroo_Code)
        ] |> List.filter id
        |> List.length
    if n > 1 then
        let codeArgNames =
            [
                args.Parser.GetArgumentCaseInfo(Skip_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Sh_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Four_Corner_Code).Name.Value
                args.Parser.GetArgumentCaseInfo(Deroo_Code).Name.Value
            ] |> makeWordList
        args.Raise($"Only one of %s{codeArgNames} can be used")

let validateStrokeArgs (args: ParseResults<KanjiArgs>) =
    if args.Contains(Strokes) && (args.Contains(Min_Strokes) || args.Contains(Max_Strokes)) then
        let strokeArgName = args.Parser.GetArgumentCaseInfo(Strokes).Name.Value
        let minMaxArgNames =
            [
                args.Parser.GetArgumentCaseInfo(Min_Strokes).Name.Value
                args.Parser.GetArgumentCaseInfo(Max_Strokes).Name.Value
            ] |> makeWordList
        args.Raise($"%s{strokeArgName} can not be used with %s{minMaxArgNames}")

let validateAtLeastOneArg (args: ParseResults<'a>) =
    if args.GetAllResults().Length = 0 then
        args.Raise("You must specify at least one search option")

let validateNoOtherSearchOptionsWithLiteralKanji (args: ParseResults<KanjiArgs>) =
    if args.Contains(Kanji) && args.GetAllResults().Length > 1 then
        args.Raise("You can not use other search options when passing a literal kanji")

let validateKanjiArgs (args: ParseResults<KanjiArgs>) =
    validateCodeArgs args
    validateStrokeArgs args
    validateAtLeastOneArg args
    validateNoOtherSearchOptionsWithLiteralKanji args

let printReferenceType (referenceType: string) =
    match referenceType with
    | "nelson_c" -> "Modern Reader's Japanese-English Character Dictionary edited by Andrew Nelson"
    | "nelson_n" -> "The New Nelson Japanese-English Character Dictionary edited by John Haig"
    | "halpern_njecd" -> "New Japanese-English Character Dictionary edited by Jack Halpern"
    | "halpern_kkd" -> "Kodansha Kanji Dictionary (2nd Ed. of the NJECD) edited by Jack Halpern"
    | "halpern_kkld" -> "Kanji Learners Dictionary Kodansha) edited by Jack Halpern"
    | "halpern_kkld_2ed" -> "Kanji Learners Dictionary (Kodansha), 2nd edition (2013) edited by Jack Halpern"
    | "heisig" -> "Remembering The Kanji by James Heisig"
    | "heisig6" -> "Remembering The Kanji, Sixth Ed. by James Heisig"
    | "gakken" -> "A New Dictionary of Kanji Usage (Gakken)"
    | "oneill_names" -> "Japanese Names by P.G. O'Neill"
    | "oneill_kk" -> "Essential Kanji by P.G. O'Neill"
    | "moro" -> "Daikanwajiten compiled by Morohashi"
    | "henshall" -> "A Guide To Remembering Japanese Characters by Kenneth G. Henshall"
    | "sh_kk" -> "Kanji and Kana by Spahn and Hadamitzky"
    | "sh_kk2" -> "Kanji and Kana by Spahn and Hadamitzky (2011 edition)"
    | "sakade" -> "A Guide To Reading and Writing Japanese edited by Florence Sakade"
    | "jf_cards" -> "Japanese Kanji Flashcards by Max Hodges and Tomoko Okazaki (Series 1)"
    | "henshall3" -> "A Guide To Reading and Writing Japanese 3rd edition, edited by Henshall, Seeley and De Groot"
    | "tutt_cards" -> "Tuttle Kanji Cards compiled by Alexander Kask"
    | "crowley" -> "The Kanji Way to Japanese Language Power by Dale Crowley"
    | "kanji_in_context" -> "Kanji in Context by Nishiguchi and Kono"
    | "busy_people" -> "Japanese For Busy People vols I-III, published by the AJLT"
    | "kodansha_compact" -> "Kodansha Compact Kanji Guide"
    | "maniette" -> "Les Kanjis dans la tete adapted from Heisig to French by Yves Maniette"
    | x -> x

let printVariantType (variantType: string) =
    match variantType with
    | "jis208" -> "JIS X 0208"
    | "jis212" -> "JIS X 0212"
    | "jis213" -> "JIS X 0213"
    | "deroo" -> "De Roo number"
    | "njecd" -> "Halpern NJECD index number"
    | "s_h" -> "The Kanji Dictionary (Spahn & Hadamitzky) descriptor"
    | "nelson_c" -> "Modern Reader's Japanese-English Character Dictionary edited by Andrew Nelson number"
    | "oneill" -> "Japanese Names (O'Neill) number"
    | "ucs" -> "Unicode"
    | x -> x

let printKanji (kanji: GetKanjiQueryResult) =
    let sb = StringBuilder()

    sb.AppendLine($"Kanji: %A{kanji.Value}") |> ignore

    sb.Append($"Grade: ") |> ignore
    match kanji.Grade with
    | Some grade -> sb.AppendLine(string grade) |> ignore
    | None -> sb.AppendLine("-") |> ignore

    sb.Append($"Stroke Count: %i{kanji.StrokeCount}") |> ignore
    match kanji.StrokeMiscounts with
    | [] -> sb.AppendLine() |> ignore
    | miscounts ->
        miscounts
        |> List.map string
        |> List.reduce (sprintf "%s, %s")
        |> sprintf " (%s)"
        |> sb.AppendLine
        |> ignore

    sb.Append("Frequency: ") |> ignore
    match kanji.Frequency with
    | Some frequency -> sb.AppendLine(string frequency) |> ignore
    | None -> sb.AppendLine("-") |> ignore

    sb.AppendLine($"Readings:") |> ignore
    for reading in kanji.CharacterReadings.Kunyomi do
        sb.AppendLine($"    %s{reading} (kun)") |> ignore
    for reading in kanji.CharacterReadings.Onyomi do
        sb.AppendLine($"    %s{reading} (on)") |> ignore

    sb.AppendLine($"Nanori:") |> ignore
    for reading in kanji.Nanori do
        sb.AppendLine($"    %s{reading}") |> ignore

    sb.AppendLine($"Meanings:") |> ignore
    for meaning in kanji.CharacterMeanings do
        sb.AppendLine($"    %s{meaning}") |> ignore

    sb.AppendLine("Character Codes:") |> ignore

    sb.Append($"    SKIP: ") |> ignore
    match kanji.CharacterCodes.Skip with
    | Some skip ->
        sb.Append(skip) |> ignore
        match kanji.CharacterCodes.SkipMisclassifications with
        | [] -> sb.AppendLine() |> ignore
        | misclassifications ->
            misclassifications
            |> List.map (fun misclassification ->
                match misclassification with
                | Position x -> $"%s{x} (position)"
                | StrokeCount x -> $"%s{x} (stroke count)"
                | StrokeAndPosition x -> $"%s{x} (stroke count and position)"
                | StrokeDifference x -> $"%s{x} (stroke difference)")
            |> List.reduce (sprintf "%s, %s")
            |> sprintf " (%s)"
            |> sb.AppendLine
            |> ignore
    | None -> sb.AppendLine("-") |> ignore

    sb.Append($"    SH: ") |> ignore
    match kanji.CharacterCodes.ShDesc with
    | Some sh -> sb.AppendLine(sh) |> ignore
    | None -> sb.AppendLine("-") |> ignore

    sb.Append($"    Four Corner: ") |> ignore
    match kanji.CharacterCodes.FourCorner with
    | Some fourCorner -> sb.AppendLine(fourCorner) |> ignore
    | None -> sb.AppendLine("-") |> ignore

    sb.Append($"    DeRoo: ") |> ignore
    match kanji.CharacterCodes.DeRoo with
    | Some deroo -> sb.AppendLine(deroo) |> ignore
    | None -> sb.AppendLine("-") |> ignore

    sb.Append("Radicals: ") |> ignore
    for radical in kanji.Radicals do
        sb.Append(radical) |> ignore
    sb.AppendLine() |> ignore

    sb.Append($"Key Radical: %i{kanji.KeyRadicals.KanjiX}") |> ignore
    match kanji.KeyRadicals.Nelson with
    | Some nelsonRadical -> sb.AppendLine($" %i{nelsonRadical}") |> ignore
    | None -> sb.AppendLine() |> ignore

    sb.AppendLine("References:") |> ignore
    for reference in kanji.DictionaryReferences do
        let dictionaryName = printReferenceType reference.Type
        sb.Append($"    index %s{reference.IndexNumber}") |> ignore
        if reference.Page.IsSome then
            sb.Append($", page %i{reference.Page.Value}") |> ignore
        if reference.Page.IsSome then
            sb.Append($", volume %i{reference.Volume.Value}") |> ignore
        sb.AppendLine($" - %s{dictionaryName}") |> ignore

    sb.AppendLine("Codepoints:") |> ignore
    sb.AppendLine($"    Unicode: %s{kanji.CodePoints.Ucs}") |> ignore
    if kanji.CodePoints.Jis208.IsSome then
        sb.AppendLine($"    JIS X 0208: %s{kanji.CodePoints.Jis208.Value}") |> ignore
    if kanji.CodePoints.Jis212.IsSome then
        sb.AppendLine($"    JIS X 0212: %s{kanji.CodePoints.Jis212.Value}") |> ignore
    if kanji.CodePoints.Jis213.IsSome then
        sb.AppendLine($"    JIS X 0213: %s{kanji.CodePoints.Jis213.Value}") |> ignore

    sb.AppendLine("Variants:") |> ignore
    for variant in kanji.Variants do
        let character =
            variant.Character
            |> Option.defaultValue (rune "□")
        let variantType = printVariantType variant.Type
        sb.AppendLine($"    %A{character} %s{variant.Value} (%s{variantType})") |> ignore

    sb.ToString()

let kanjiHandler (args: ParseResults<KanjiArgs>) =
    validateKanjiArgs args

    use ctx = new SqliteConnection("Data Source=data/kensaku.db")
    Database.Schema.registerTypeHandlers ()
    Database.Schema.registerRegexpFunction ctx

    let kanji =
        match args.TryGetResult(Kanji) with
        | Some kanji ->
            let runes = List.map rune kanji
            getKanjiLiterals runes ctx
        | None ->
            let minStrokeCount, maxStrokeCount =
                match args.TryGetResult Strokes with
                | Some n -> Some n, Some n
                | None -> args.TryGetResult Min_Strokes, args.TryGetResult Max_Strokes
            let query = {
                MinStrokeCount = minStrokeCount
                MaxStrokeCount = maxStrokeCount
                IncludeStrokeMiscounts = args.Contains Include_Stroke_Miscounts
                SearchRadicals =
                    args.TryGetResult Radicals
                    |> Option.map (fun radicals -> radicals.EnumerateRunes() |> Seq.toList)
                    |> Option.defaultValue []
                CharacterCode =
                    args.TryPostProcessResult(Skip_Code, postProcessSkipCode)
                    |> Option.orElseWith (fun () -> args.TryPostProcessResult(Sh_Code, postProcessShCode))
                    |> Option.orElseWith (fun () -> args.TryPostProcessResult(Four_Corner_Code, postProcessFourCornerCode))
                    |> Option.orElseWith (fun () -> args.TryPostProcessResult(Deroo_Code, postProcessDeRooCode))
                CharacterReading = args.TryGetResult Reading
                CharacterMeaning = args.TryGetResult Meaning
                Nanori = args.TryGetResult Nanori
                CommonOnly = args.Contains Common_Only
                Pattern = args.TryGetResult Pattern
                KeyRadical = None
            }
            getKanji query ctx

    kanji
    |> List.map printKanji
    |> List.reduce (sprintf "%s\n%s")
    |> printf "%s"

type Args =
    | [<CliPrefix(CliPrefix.None)>] Kanji of ParseResults<KanjiArgs>
    | Version
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Kanji _ -> "search for kanji"
            | Version -> "display the version info"

[<EntryPoint>]
let main argv =
    let parser =
        ArgumentParser.Create<Args>(
            programName = "kensaku",
            helpTextMessage = "Quick and easy search for Japanese kanji, radicals, and words",
            errorHandler = ProcessExiter(),
            usageStringCharacterWidth = 80)
    let results = parser.ParseCommandLine(argv)
    if results.Contains(Version) then
        let version =
            Assembly.GetEntryAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(fun a -> a.Key = "GitTag")
                .Value
        printfn "%s" version
    else
        match results.GetSubCommand() with
        | Kanji kanjiArgs -> kanjiHandler kanjiArgs
        | Version ->
            let versionOptionName = results.Parser.GetArgumentCaseInfo(Version).Name.Value
            results.Raise($"%s{versionOptionName} should be handled before evaluation of subcommands")

    0
