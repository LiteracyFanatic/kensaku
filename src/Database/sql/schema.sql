PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
-- JMDict scehma (many parts shared with JMnedict schema)
DROP TABLE IF EXISTS "Entries";
CREATE TABLE "Entries" (
    "Id" INTEGER NOT NULL UNIQUE,
    "IsProperName" INTEGER NOT NULL,
    PRIMARY KEY("Id")
);
DROP TABLE IF EXISTS "SenseCrossReferences";
CREATE TABLE "SenseCrossReferences" (
    "SenseId" INTEGER NOT NULL,
    "ReferenceKanjiElement" TEXT,
    "ReferenceReadingElement" TEXT,
    "ReferenceSense" INTEGER,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "Fields";
CREATE TABLE "Fields" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "PartsOfSpeech";
CREATE TABLE "PartsOfSpeech" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "Dialects";
CREATE TABLE "Dialects" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "SenseInformation";
CREATE TABLE "SenseInformation" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "KanjiElementInformation";
CREATE TABLE "KanjiElementInformation" (
    "KanjiElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("KanjiElementId") REFERENCES "KanjiElements"("Id"),
    UNIQUE("KanjiElementId", "Value")
);
DROP TABLE IF EXISTS "KanjiElementPriorities";
CREATE TABLE "KanjiElementPriorities" (
    "KanjiElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("KanjiElementId") REFERENCES "KanjiElements"("Id"),
    UNIQUE("KanjiElementId", "Value")
);
DROP TABLE IF EXISTS "ReadingElementPriorities";
CREATE TABLE "ReadingElementPriorities" (
    "ReadingElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("ReadingElementId") REFERENCES "ReadingElements"("Id"),
    UNIQUE("ReadingElementId", "Value")
);
DROP TABLE IF EXISTS "KanjiElements";
CREATE TABLE "KanjiElements" (
    "Id" INTEGER NOT NULL UNIQUE,
    "EntryId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    PRIMARY KEY("Id" AUTOINCREMENT),
    FOREIGN KEY("EntryId") REFERENCES "Entries"("Id"),
    UNIQUE("EntryId", "Value")
);
DROP TABLE IF EXISTS "ReadingElements";
CREATE TABLE "ReadingElements" (
    "Id" INTEGER NOT NULL UNIQUE,
    "EntryId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "IsTrueReading" INTEGER NOT NULL,
    FOREIGN KEY("EntryId") REFERENCES "Entries"("Id"),
    PRIMARY KEY("Id" AUTOINCREMENT),
    UNIQUE("EntryId", "Value")
);
DROP TABLE IF EXISTS "ReadingElementInformation";
CREATE TABLE "ReadingElementInformation" (
    "ReadingElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("ReadingElementId") REFERENCES "ReadingElements"("Id"),
    UNIQUE("ReadingElementId", "Value")
);
DROP TABLE IF EXISTS "Senses";
CREATE TABLE "Senses" (
    "Id" INTEGER NOT NULL UNIQUE,
    "EntryId" INTEGER NOT NULL,
    FOREIGN KEY("EntryId") REFERENCES "Entries"("Id"),
    PRIMARY KEY("Id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "SenseKanjiElementRestrictions";
CREATE TABLE "SenseKanjiElementRestrictions" (
    "SenseId" INTEGER NOT NULL,
    "KanjiElement" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "ReadingElementRestrictions";
CREATE TABLE "ReadingElementRestrictions" (
    "ReadingElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("ReadingElementId") REFERENCES "ReadingElements"("Id")
);
DROP TABLE IF EXISTS "SenseReadingElementRestrictions";
CREATE TABLE "SenseReadingElementRestrictions" (
    "SenseId" INTEGER NOT NULL,
    "ReadingElement" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id"),
    UNIQUE("SenseId", "ReadingElement")
);
DROP TABLE IF EXISTS "MiscellaneousInformation";
CREATE TABLE "MiscellaneousInformation" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "LanguageSources";
CREATE TABLE "LanguageSources" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "LanguageCode" TEXT NOT NULL,
    "IsPartial" INTEGER NOT NULL,
    "IsWasei" INTEGER NOT NULL,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "Glosses";
CREATE TABLE "Glosses" (
    "SenseId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Language" TEXT NOT NULL,
    "Type" TEXT,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
DROP TABLE IF EXISTS "Antonyms";
CREATE TABLE "Antonyms" (
    "SenseId" INTEGER NOT NULL,
    "ReferenceKanjiElement" TEXT,
    "ReferenceReadingElement" TEXT,
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
);
-- Exclusive to JMneDict scehma
DROP TABLE IF EXISTS "Translations";
CREATE TABLE "Translations" (
    "Id" INTEGER NOT NULL UNIQUE,
    "EntryId" INTEGER NOT NULL,
    FOREIGN KEY("EntryId") REFERENCES "Entries"("Id"),
    PRIMARY KEY("Id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "NameTypes";
CREATE TABLE "NameTypes" (
    "TranslationId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("TranslationId") REFERENCES "Translations"("Id")
);
DROP TABLE IF EXISTS "TranslationCrossReferences";
CREATE TABLE "TranslationCrossReferences" (
    "TranslationId" INTEGER NOT NULL,
    "ReferenceKanjiElement" TEXT,
    "ReferenceReadingElement" TEXT,
    "ReferenceTranslation" INTEGER,
    FOREIGN KEY("TranslationId") REFERENCES "Translations"("Id")
);
DROP TABLE IF EXISTS "TranslationContents";
CREATE TABLE "TranslationContents" (
    "TranslationId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Language" TEXT NOT NULL,
    FOREIGN KEY("TranslationId") REFERENCES "Translations"("Id")
);
-- kanjidic2 schema
DROP TABLE IF EXISTS "Kanjidic2Info";
CREATE TABLE "Kanjidic2Info" (
    "FileVersion" INTEGER NOT NULL,
    "DatabaseVersion" TEXT NOT NULL,
    "DateOfCreation" TEXT NOT NULL
);
DROP TABLE IF EXISTS "Characters";
CREATE TABLE "Characters" (
    "Id" INTEGER NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Grade" INTEGER,
    "StrokeCount" INTEGER NOT NULL,
    "Frequency" INTEGER,
    "IsRadical" INTEGER NOT NULL,
    "OldJlptLevel" INTEGER,
    PRIMARY KEY("Id" AUTOINCREMENT)
);
DROP TABLE if EXISTS "CodePoints";
CREATE TABLE "CodePoints" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "KeyRadicals";
CREATE TABLE "KeyRadicals" (
    "CharacterId" INTEGER NOT NULL,
    "Value" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "StrokeMiscounts";
CREATE TABLE "StrokeMiscounts" (
    "CharacterId" INTEGER NOT NULL,
    "Value" INTEGER NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "CharacterVariants";
CREATE TABLE "CharacterVariants" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "RadicalNames";
CREATE TABLE "RadicalNames" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "CharacterDictionaryReferences";
CREATE TABLE "CharacterDictionaryReferences" (
    "CharacterId" INTEGER NOT NULL,
    "IndexNumber" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "Volume" INTEGER,
    "Page" INTEGER,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "CharacterQueryCodes";
CREATE TABLE "CharacterQueryCodes" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "SkipMisclassification" TEXT,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "CharacterReadings";
CREATE TABLE "CharacterReadings" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "CharacterMeanings";
CREATE TABLE "CharacterMeanings" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Language" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
DROP TABLE IF EXISTS "Nanori";
CREATE TABLE "Nanori" (
    "CharacterId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id")
);
-- krad schema
DROP TABLE IF EXISTS "Radicals";
CREATE TABLE "Radicals" (
    "Id" INTEGER NOT NULL UNIQUE,
    "Number" INTEGER UNIQUE,
    "StrokeCount" INTEGER NOT NULL,
    PRIMARY KEY("Id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "RadicalValues";
CREATE TABLE "RadicalValues" (
    "RadicalId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL UNIQUE,
    "Type" TEXT NOT NULL,
    FOREIGN KEY("RadicalId") REFERENCES "Radicals"("Id")
);
DROP TABLE IF EXISTS "RadicalMeanings";
CREATE TABLE "RadicalMeanings" (
    "RadicalId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    FOREIGN KEY("RadicalId") REFERENCES "Radicals"("Id"),
    UNIQUE("RadicalId", "Value")
);
DROP TABLE IF EXISTS "Characters_Radicals";
CREATE TABLE "Characters_Radicals" (
    "CharacterId" INTEGER NOT NULL,
    "RadicalId" INTEGER NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id"),
    FOREIGN KEY("RadicalId") REFERENCES "Radicals"("Id"),
    UNIQUE("CharacterId", "RadicalId")
);
DROP TABLE IF EXISTS "EquivalentCharacters";
CREATE TABLE "EquivalentCharacters" (
    "Characters" TEXT NOT NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_RadicalMeanings_RadicalId ON "RadicalMeanings" ("RadicalId");
CREATE INDEX IF NOT EXISTS idx_Characters_Radicals_CharacterId ON "Characters_Radicals" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_Characters_Radicals_RadicalId ON "Characters_Radicals" ("RadicalId");
CREATE INDEX IF NOT EXISTS idx_SenseCrossReferences_SenseId ON "SenseCrossReferences" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_Fields_SenseId ON "Fields" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_PartsOfSpeech_SenseId ON "PartsOfSpeech" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_Dialects_SenseId ON "Dialects" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_SenseInformation_SenseId ON "SenseInformation" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_KanjiElementInformation_KanjiElementId ON "KanjiElementInformation" ("KanjiElementId");
CREATE INDEX IF NOT EXISTS idx_KanjiElementPriorities_KanjiElementId ON "KanjiElementPriorities" ("KanjiElementId");
CREATE INDEX IF NOT EXISTS idx_ReadingElementPriorities_ReadingElementId ON "ReadingElementPriorities" ("ReadingElementId");
CREATE INDEX IF NOT EXISTS idx_KanjiElements_EntryId ON "KanjiElements" ("EntryId");
CREATE INDEX IF NOT EXISTS idx_KanjiElements_Value ON "KanjiElements" ("Value");
CREATE INDEX IF NOT EXISTS idx_ReadingElements_EntryId ON "ReadingElements" ("EntryId");
CREATE INDEX IF NOT EXISTS idx_ReadingElements_Value ON "ReadingElements" ("Value");
CREATE INDEX IF NOT EXISTS idx_ReadingElementInformation_ReadingElementId ON "ReadingElementInformation" ("ReadingElementId");
CREATE INDEX IF NOT EXISTS idx_Senses_EntryId ON "Senses" ("EntryId");
CREATE INDEX IF NOT EXISTS idx_SenseKanjiElementRestrictions_SenseId ON "SenseKanjiElementRestrictions" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_ReadingElementRestrictions_ReadingElementId ON "ReadingElementRestrictions" ("ReadingElementId");
CREATE INDEX IF NOT EXISTS idx_SenseReadingElementRestrictions_SenseId ON "SenseReadingElementRestrictions" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_MiscellaneousInformation_SenseId ON "MiscellaneousInformation" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_LanguageSources_SenseId ON "LanguageSources" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_Glosses_SenseId ON "Glosses" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_Antonyms_SenseId ON "Antonyms" ("SenseId");
CREATE INDEX IF NOT EXISTS idx_Translations_EntryId ON "Translations" ("EntryId");
CREATE INDEX IF NOT EXISTS idx_NameTypes_TranslationId ON "NameTypes" ("TranslationId");
CREATE INDEX IF NOT EXISTS idx_TranslationCrossReferences_TranslationId ON "TranslationCrossReferences" ("TranslationId");
CREATE INDEX IF NOT EXISTS idx_TranslationContents_TranslationId ON "TranslationContents" ("TranslationId");
CREATE INDEX IF NOT EXISTS idx_CodePoints_CharacterId ON "CodePoints" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_KeyRadicals_CharacterId ON "KeyRadicals" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_StrokeMiscounts_CharacterId ON "StrokeMiscounts" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_CharacterVariants_CharacterId ON "CharacterVariants" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_RadicalNames_CharacterId ON "RadicalNames" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_CharacterDictionaryReferences_CharacterId ON "CharacterDictionaryReferences" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_CharacterQueryCodes_CharacterId ON "CharacterQueryCodes" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_CharacterReadings_CharacterId ON "CharacterReadings" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_CharacterMeanings_CharacterId ON "CharacterMeanings" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_Nanori_CharacterId ON "Nanori" ("CharacterId");
CREATE INDEX IF NOT EXISTS idx_RadicalValues_RadicalId ON "RadicalValues" ("RadicalId");

DELETE FROM sqlite_sequence;
INSERT INTO sqlite_sequence ('name', 'seq') VALUES
    ('KanjiElements', 0),
    ('ReadingElements', 0),
    ('Senses', 0),
    ('Translations', 0),
    ('Characters', 0),
    ('Radicals', 0);
COMMIT;
