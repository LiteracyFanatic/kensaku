# Kensaku.DataSources

Parsers for major Japanese lexical / kanji data sets, exposed as lazy asynchronous streams so very large source files can be processed with constant / low memory usage. This project is one layer of the broader Kensaku stack and focuses strictly on transforming raw source files into strongly-typed domain models.

## Supported Data Sets
| Source | What It Contains |
| ------ | ---------------- |
| JMdict | General Japanese lexical dictionary (kanji elements, reading elements, senses) |
| JMnedict | Proper names dictionary (kanji, readings, translations) |
| Kanjidic2 | Kanji metadata: radicals, strokes, codepoints, readings, meanings |
| radkfile / radkfile2 | Radical → Kanji index |
| kradfile | Kanji → component radicals |
| Unicode ancillary files | Radical names, unified equivalents, derived radical names |
| WaniKani | Readings, meanings, mnemonics |


Download links:
- JMdict, JMnedict, Kanjidic2, radkfile, kradfile: https://www.edrdg.org/
- Equivalent Unified Ideograph: https://www.unicode.org/Public/UNIDATA/EquivalentUnifiedIdeograph.txt
- CJK Radicals: https://www.unicode.org/Public/UNIDATA/CJKRadicals.txt
- Derived Names: https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedName.txt
- WaniKani: https://docs.api.wanikani.com/
