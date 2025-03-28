namespace Kensaku.CLI

module Formatting =
    open System
    open System.Diagnostics
    open System.IO
    open System.Text.Encodings.Web
    open System.Text.Json
    open System.Text.Json.Serialization

    open Spectre.Console

    open Kensaku.Core
    open Kensaku.Core.Domain
    open Kensaku.Core.Kanji
    open Kensaku.Core.Words

    type Format =
        | Text
        | Json

    let jsonSerializerOptions =
        JsonSerializerOptions(Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true)

    JsonFSharpOptions.Default().AddToJsonSerializerOptions(jsonSerializerOptions)
    jsonSerializerOptions.Converters.Add(RuneJsonConverter())

    let toJson value =
        JsonSerializer.Serialize(value, jsonSerializerOptions)

    let tryGetPager () =
        if OperatingSystem.IsWindows() || Console.IsOutputRedirected then
            None
        else
            let lessArgs = "-FR"
            let moreArgs = ""

            [
                "/usr/bin/less", lessArgs
                "/bin/less", lessArgs
                "/usr/bin/more", moreArgs
                "/bin/more", moreArgs
            ]
            |> List.tryFind (fst >> File.Exists)

    let toPager (output: string) =
        match tryGetPager () with
        | None -> printf "%s" output
        | Some(command, args) ->
            try
                let psi =
                    ProcessStartInfo(
                        FileName = command,
                        Arguments = args,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    )

                use pager = Process.Start(psi)

                using pager.StandardInput (fun writer ->
                    if writer.BaseStream.CanWrite then
                        writer.Write(output))

                pager.WaitForExit()
            with ex ->
                printfn "%s" output

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

    let formatKeyRadical (keyRadical: KeyRadicalValue) =
        // This will prefer characters in the CJK Unified Ideographs block over those in the Kangxi Radicals block
        let value = keyRadical.Values |> List.sortDescending |> List.head |> string
        let meanings = keyRadical.Meanings |> String.concat ", "
        $"%i{keyRadical.Number} %s{value} (%s{meanings})"

    let printKanji (console: StringWriterAnsiConsole) (kanji: GetKanjiQueryResult) =
        console.WriteLineNonBreaking($"Kanji: %A{kanji.Value}")

        let meanings = kanji.CharacterMeanings |> String.concat ", "

        console.WriteLineNonBreaking($"Meanings: %s{meanings}")

        console.WriteLineNonBreaking("Readings:")

        let kunyomi = kanji.CharacterReadings.Kunyomi |> String.concat ", "
        console.WriteLineNonBreaking($"    Kun: %s{kunyomi}")

        let onyomi = kanji.CharacterReadings.Onyomi |> String.concat ", "
        console.WriteLineNonBreaking($"    On: %s{onyomi}")

        let nanori = kanji.Nanori |> String.concat ", "
        console.WriteLineNonBreaking($"    Nanori: %s{nanori}")

        console.WriteNonBreaking("Grade: ")

        match kanji.Grade with
        | Some grade -> console.WriteLineNonBreaking(string grade)
        | None -> console.WriteLineNonBreaking("-")

        console.WriteNonBreaking($"Stroke Count: %i{kanji.StrokeCount}")

        match kanji.StrokeMiscounts with
        | [] -> console.WriteLine()
        | miscounts ->
            miscounts
            |> List.map string
            |> List.reduce (sprintf "%s, %s")
            |> sprintf " (%s)"
            |> console.WriteLineNonBreaking


        console.WriteNonBreaking("Frequency: ")

        match kanji.Frequency with
        | Some frequency -> console.WriteLineNonBreaking(string frequency)
        | None -> console.WriteLineNonBreaking("-")


        console.WriteNonBreaking($"Key Radical: Kangxi radical %s{formatKeyRadical kanji.KeyRadicals.Kangxi}")

        match kanji.KeyRadicals.Nelson with
        | Some nelsonRadical -> console.WriteLineNonBreaking($"; Nelson radical %s{formatKeyRadical nelsonRadical}")
        | None -> console.WriteLine()

        console.WriteNonBreaking("Radicals: ")

        for radical in kanji.Radicals do
            console.WriteNonBreaking(string radical)

        console.WriteLine()

        console.WriteLineNonBreaking("Variants:")

        for variant in kanji.Variants do
            let character = variant.Character |> Option.defaultValue (rune "□")
            let variantType = printVariantType variant.Type
            console.WriteLineNonBreaking($"    %A{character} %s{variant.Value} (%s{variantType})")

        console.WriteLineNonBreaking("References:")

        for reference in kanji.DictionaryReferences do
            let dictionaryName = printReferenceType reference.Type
            console.WriteNonBreaking($"    index %s{reference.IndexNumber}")

            if reference.Page.IsSome then
                console.WriteNonBreaking($", page %i{reference.Page.Value}")

            if reference.Page.IsSome then
                console.WriteNonBreaking($", volume %i{reference.Volume.Value}")

            console.WriteLineNonBreaking($" - %s{dictionaryName}")

        console.WriteLineNonBreaking("Character Codes:")

        console.WriteNonBreaking("    SKIP: ")

        match kanji.CharacterCodes.Skip with
        | Some skip ->
            console.WriteNonBreaking(skip)

            match kanji.CharacterCodes.SkipMisclassifications with
            | [] -> console.WriteLine()
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
                |> console.WriteLineNonBreaking

        | None -> console.WriteLineNonBreaking("-")

        console.WriteNonBreaking("    SH: ")

        match kanji.CharacterCodes.ShDesc with
        | Some sh -> console.WriteLineNonBreaking(sh)
        | None -> console.WriteLineNonBreaking("-")

        console.WriteNonBreaking("    Four Corner: ")

        match kanji.CharacterCodes.FourCorner with
        | Some fourCorner -> console.WriteLineNonBreaking(fourCorner)
        | None -> console.WriteLineNonBreaking("-")

        console.WriteNonBreaking("    DeRoo: ")

        match kanji.CharacterCodes.DeRoo with
        | Some deroo -> console.WriteLineNonBreaking(deroo)
        | None -> console.WriteLineNonBreaking("-")

        console.WriteLineNonBreaking("Codepoints:")
        console.WriteLineNonBreaking($"    Unicode: %s{kanji.CodePoints.Ucs}")

        if kanji.CodePoints.Jis208.IsSome then
            console.WriteLineNonBreaking($"    JIS X 0208: %s{kanji.CodePoints.Jis208.Value}")

        if kanji.CodePoints.Jis212.IsSome then
            console.WriteLineNonBreaking($"    JIS X 0212: %s{kanji.CodePoints.Jis212.Value}")

        if kanji.CodePoints.Jis213.IsSome then
            console.WriteLineNonBreaking($"    JIS X 0213: %s{kanji.CodePoints.Jis213.Value}")

    let printWord (console: StringWriterAnsiConsole) (word: GetWordQueryResult) =
        let wordForms = getWordForms word

        console.MarkupLineNonBreaking(wordForms.Primary.ToString())

        let senses =
            word.Senses
            |> List.filter (fun sense -> sense.Glosses |> List.exists (fun gloss -> gloss.LanguageCode = "eng"))

        if senses.Length > 0 then
            console.WriteLine()

            for i in 0 .. senses.Length - 1 do
                let sense = senses[i]
                let partsOfSpeech = sense.PartsOfSpeech |> String.concat ", "
                console.MarkupLineNonBreaking($"[italic dim]%s{partsOfSpeech}[/]")
                let glosses = sense.Glosses |> List.map _.Value |> String.concat "; "
                console.MarkupNonBreaking($"%i{i + 1}. %s{glosses}")

                let kanjiRestrictions =
                    sense.KanjiRestrictions
                    |> List.map (sprintf "Only applies to %s")
                    |> String.concat ", "

                let readingRestrictions =
                    sense.ReadingRestrictions
                    |> List.map (sprintf "Only applies to %s")
                    |> String.concat ", "

                let crossReferences =
                    sense.CrossReferences |> List.map _.ToString() |> String.concat ", "

                let antonyms = sense.Antonyms |> List.map _.ToString() |> String.concat ", "
                let fields = sense.Fields |> String.concat ", "
                let miscellaneousInformation = sense.MiscellaneousInformation |> String.concat ", "
                let additionalInformation = sense.AdditionalInformation |> String.concat ", "
                let dialects = sense.Dialects |> String.concat ", "

                let languageSources =
                    sense.LanguageSources |> List.map _.ToString() |> String.concat ", "

                let details =
                    [
                        if fields.Length > 0 then
                            $"%s{fields}"
                        if antonyms.Length > 0 then
                            $"Antonyms: %s{antonyms}"
                        if miscellaneousInformation.Length > 0 then
                            miscellaneousInformation
                        if dialects.Length > 0 then
                            $"%s{dialects}"
                        if languageSources.Length > 0 then
                            languageSources
                        if crossReferences.Length > 0 then
                            crossReferences
                        if additionalInformation.Length > 0 then
                            additionalInformation
                        if kanjiRestrictions.Length > 0 then
                            kanjiRestrictions
                        if readingRestrictions.Length > 0 then
                            readingRestrictions
                    ]
                    |> String.concat ", "

                if details.Length > 0 then
                    console.MarkupNonBreaking($" [dim]%s{details}[/]")

                console.WriteLine()

                if i < senses.Length - 1 then
                    console.WriteLine()

        let translations = word.Translations

        if translations.Length > 0 then
            console.WriteLine()

            for i in 0 .. translations.Length - 1 do
                let translation = translations[i]
                let nameTypes = String.concat ", " translation.NameTypes
                console.MarkupLineNonBreaking($"[italic dim]%s{nameTypes}[/]")
                let contents = translation.Contents |> List.map _.Value |> String.concat "; "

                let crossReferences =
                    translation.CrossReferences |> List.map _.ToString() |> String.concat ", "

                console.MarkupNonBreaking($"%i{i + 1 + senses.Length}. %s{contents}")

                if crossReferences.Length > 0 then
                    console.MarkupNonBreaking($" [dim]%s{crossReferences}[/]")

                console.WriteLine()

                if i < translations.Length - 1 then
                    console.WriteLine()

        let otherForms = wordForms.Alternate |> Seq.map _.ToString() |> String.concat ", "

        if otherForms.Length > 0 then
            console.WriteLine()
            console.MarkupLineNonBreaking("Other Forms")
            console.MarkupLineNonBreaking(otherForms)

        let kanjiNotes =
            word.KanjiElements
            |> List.map (fun ke -> {
                ke with
                    Information = ke.Information |> List.filter (fun i -> i <> "search-only kanji form")
            })
            |> List.filter (fun ke -> ke.Information.Length > 0)
            |> List.map (fun ke ->
                let info = ke.Information |> String.concat ", "
                $"%s{ke.Value}: %s{info}")

        let readingNotes =
            word.ReadingElements
            |> List.map (fun re -> {
                re with
                    Information = re.Information |> List.filter (fun i -> i <> "search-only kana form")
            })
            |> List.filter (fun re -> re.Information.Length > 0)
            |> List.map (fun re ->
                let info = re.Information |> String.concat ", "
                $"%s{re.Value}: %s{info}")

        let notes = List.append kanjiNotes readingNotes

        if readingNotes.Length > 0 then
            console.WriteLine()
            console.MarkupLineNonBreaking("Notes")

            for note in notes do
                console.MarkupLineNonBreaking(note)
