module Kensaku.CLI.LicensesCommand

open System.IO
open System.Reflection

let licensesHandler () =
    let stream =
        Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("CLI.NugetLicenses.txt")
    use sr = new StreamReader(stream)
    let nugetLicenses = sr.ReadToEnd().Trim()
    printfn $"kensaku is made available under the terms of the MIT License.

kensaku uses the CJKRadicals.txt, DerivedName.txt, and EquivalentUnifiedIdeograph.txt Unicode Data Files. These files are the property of the Unicode Consortium (https://www.unicode.org/), and are used in conformance with the Consortium's license (https://www.unicode.org/license.txt).

kensaku uses the JMdict, Kanjidic2, JMnedict, and Radkfile dictionary files. These files are the property of the Electronic Dictionary Research and Development Group (https://www.edrdg.org/), and are used in conformance with the Group's licence (https://www.edrdg.org/edrdg/licence.html).

kensaku uses the following packages subject to their respective licenses:

%s{nugetLicenses}"
