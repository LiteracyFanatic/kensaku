# kensaku

## About

検索, pronounced kensaku, means "search" or "retrieval" in Japanese. This project aims to make it easier for Japanese learners to find information about kanji, radicals, and vocabulary. It does this by bundling data from the Unicode Consortium and the Electronic Dictionary Research and Development Group into an SQLite database with an accompanying CLI tool.

## Installation

### Windows

```powershell
choco install -y kensaku
```

### Debian

Follow the instructions [here](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian?tabs=dotnet9) to install the .NET 9 runtime. Then run the following commands:

```bash
wget https://github.com/LiteracyFanatic/kensaku/releases/download/latest/kensaku.deb
sudo apt install -y ./kensaku.deb
```

### Ubuntu

Follow the instructions [here](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9) to install the .NET 9 runtime. Then run the following commands:

```bash
wget https://github.com/LiteracyFanatic/kensaku/releases/latest/download/kensaku.deb
sudo apt install -y ./kensaku.deb
```

### Fedora

```bash
wget https://github.com/LiteracyFanatic/kensaku/releases/latest/download/kensaku.rpm
sudo dnf install -y ./kensaku.rpm
```

### Arch Linux

```bash
yay -S kensaku-bin
```

### Manual Installation

Download the database and executable for your platform from the [latest release](https://github.com/LiteracyFanatic/kensaku/releases/latest/). Set the `KENSAKU_DB_PATH` environment variable to the path of the `kensaku.db` file.

## Example Usage

Let's start simple by asking kensaku to define itself: `kensaku word 検索`.

```
検索 【けんさく】

noun (common) (futsuumeishi), noun or participle which takes the aux. verb suru, transitive verb
1. looking up (e.g. a word in a dictionary); search (e.g. on the Internet); retrieval (of information); reference
```

This is a relatively short one, but many entries will have multiple meanings with notes, cross-references, and other forms of the word using different kanji. Speaking of kanji, let's see what kensaku can tell us about `賀`: `kensaku kanji 賀`.

```bash
Kanji: 賀
Meanings: congratulations, joy
Readings:
    Kun: 
    On: ガ
    Nanori: か, のり, よし, より
Grade: 4
Stroke Count: 12
Frequency: 1056
Key Radical: Kangxi radical 154 贝 (shellfish, clam, shell)
Radicals: 儿力口土目贝貝ハノ
Variants:
References:
    index 4501 - Modern Reader's Japanese-English Character Dictionary edited by Andrew Nelson
    index 5790 - The New Nelson Japanese-English Character Dictionary edited by John Haig
    index 2599 - New Japanese-English Character Dictionary edited by Jack Halpern
    index 3212 - Kodansha Kanji Dictionary (2nd Ed. of the NJECD) edited by Jack Halpern
    index 1663 - Kanji Learners Dictionary Kodansha) edited by Jack Halpern
    index 2253 - Kanji Learners Dictionary (Kodansha), 2nd edition (2013) edited by Jack Halpern
    index 868 - Remembering The Kanji by James Heisig
    index 933 - Remembering The Kanji, Sixth Ed. by James Heisig
    index 778 - A New Dictionary of Kanji Usage (Gakken)
    index 1756 - Japanese Names by P.G. O'Neill
    index 615 - Essential Kanji by P.G. O'Neill
    index 36725, page 742, volume 10 - Daikanwajiten compiled by Morohashi
    index 630 - A Guide To Remembering Japanese Characters by Kenneth G. Henshall
    index 756 - Kanji and Kana by Spahn and Hadamitzky
    index 769 - Kanji and Kana by Spahn and Hadamitzky (2011 edition)
    index 565 - A Guide To Reading and Writing Japanese edited by Florence Sakade
    index 1780 - Japanese Kanji Flashcards by Max Hodges and Tomoko Okazaki (Series 1)
    index 660 - A Guide To Reading and Writing Japanese 3rd edition, edited by Henshall, Seeley and De Groot
    index 751 - Tuttle Kanji Cards compiled by Alexander Kask
    index 1019 - Kanji in Context by Nishiguchi and Kono
    index 1728 - Kodansha Compact Kanji Guide
    index 877 - Les Kanjis dans la tete adapted from Heisig to French by Yves Maniette
Character Codes:
    SKIP: 2-5-7
    SH: 7b5.10
    Four Corner: 4680.6
    DeRoo: 1661
Codepoints:
    Unicode: 8cc0
    JIS X 0208: 1-18-76
```

Where kensaku really shines is when you need to look up a kanji from an image, video, or paper book that you can't copy and paste. OCR can be hit or miss depending on the image quality and font. Most of the time, the fastest way to find a kanji is to specify the number of strokes and some of the radicals it contains. For example to find `賀`, you might try `kensaku kanji --radicals 力 貝 --strokes 12`.

This works well when the radicals in question are also a single kanji word that you know the pronunciation of. If you don't know how to type it directly though, most software will force you to scan a table of radicals to find the one you're looking for. This can be a slow and frustrating process. kensaku aims to make this easier by allowing you to refer to radicals by their commonly used mnemonics. For example, you can search for `賀` by typing `power` and `shellfish` instead and you'll get the same result as the previous query. Both the names used by the used by Unicode and [WaniKani](https://www.wanikani.com) are supported. `kensaku kanji --radicals power shellfish --strokes 12`

Another problem that you may run in to is that many radicals are actually composed of multiple simpler radicals. Some software won't return the correct results if you use the subradicals instead of the composite radical. kensaku automatically expands composite radicals so that they also match when you search for their subradicals. kensaku also tries hard to do the right thing when given a radical-like character that isn't technically speaking a true radical. Finally, kensaku will transparently handle equivalent ideographs, characters that look the same but have different Unicode codepoints, such as ⼀ (U+2F00) and 一 (U+4E00).

Sometimes it can be hard to tell exactly how many strokes a kanji has. In these cases, you can tell kensaku to be a bit more forgiving by using the `--include-stroke-miscounts` flag. Or you can specify a range using `--min-strokes` and `--max-strokes`.

If you happen to know the reading or meaning you can use the corresponding flags to filter the results as well. The `--common-only` flag will limit the search to only the 2,500 most common kanji. For those familiar with more traditional methods of kanji lookup, SKIP, SH, Four Corner, and De Roo codes are also supported.

One last useful trick is to use the `--pattern` flag to "fill in the blank" when you come across a word with a single unknown kanji. For example, if you come across the word `祝賀`, and know how to type `祝` but not `賀`, you can search for it using `kensaku kanji --radicals power shellfish --pattern 祝_`

## Scripting

If you want to use kensaku from a shell script you can use `--format json` to get the results in a machine-readable format.

## Licenses

kensaku is made available under the terms of the MIT License. See [LICENSE](LICENSE) for details.

kensaku uses the CJKRadicals.txt, DerivedName.txt, and EquivalentUnifiedIdeograph.txt Unicode Data Files. These files are the property of the Unicode Consortium (https://www.unicode.org/), and are used in conformance with the Consortium's license (https://www.unicode.org/license.txt).

kensaku uses the JMdict, Kanjidic2, JMnedict, and Radkfile dictionary files. These files are the property of the Electronic Dictionary Research and Development Group (https://www.edrdg.org/), and are used in conformance with the Group's licence (https://www.edrdg.org/edrdg/licence.html).

See [NugetLicenses.txt](src/CLI/NugetLicenses.txt) for a list of NuGet packages used by the CLI tool and their licenses.
