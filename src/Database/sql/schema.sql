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
    FOREIGN KEY("KanjiElementId") REFERENCES "KanjiElements"("Id")
);
DROP TABLE IF EXISTS "KanjiElementPriorities";
CREATE TABLE "KanjiElementPriorities" (
    "KanjiElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("KanjiElementId") REFERENCES "KanjiElements"("Id")
);
DROP TABLE IF EXISTS "ReadingElementPriorities";
CREATE TABLE "ReadingElementPriorities" (
    "ReadingElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("ReadingElementId") REFERENCES "ReadingElement"("Id")
);
DROP TABLE IF EXISTS "KanjiElements";
CREATE TABLE "KanjiElements" (
    "Id" INTEGER NOT NULL UNIQUE,
    "EntryId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    PRIMARY KEY("Id" AUTOINCREMENT),
    FOREIGN KEY("EntryId") REFERENCES "Entries"("Id")
);
DROP TABLE IF EXISTS "ReadingElements";
CREATE TABLE "ReadingElements" (
    "Id" INTEGER NOT NULL UNIQUE,
    "EntryId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    "IsTrueReading" INTEGER NOT NULL,
    FOREIGN KEY("EntryId") REFERENCES "Entries"("Id"),
    PRIMARY KEY("Id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "ReadingElementInformation";
CREATE TABLE "ReadingElementInformation" (
    "ReadingElementId" INTEGER NOT NULL,
    "Value" TEXT NOT NULL,
    FOREIGN KEY("ReadingElementId") REFERENCES "ReadingElements"("Id")
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
    FOREIGN KEY("SenseId") REFERENCES "Senses"("Id")
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
    "Value" TEXT NOT NULL UNIQUE,
    "StrokeCount" INTEGER NOT NULL,
    PRIMARY KEY("Id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "Characters_Radicals";
CREATE TABLE "Characters_Radicals" (
    "CharacterId" INTEGER NOT NULL,
    "RadicalId" INTEGER NOT NULL,
    FOREIGN KEY("CharacterId") REFERENCES "Characters"("Id"),
    FOREIGN KEY("RadicalId") REFERENCES "Radicals"("Id")
);

DELETE FROM sqlite_sequence;
INSERT INTO sqlite_sequence ('name', 'seq') VALUES
    ('KanjiElements', 0),
    ('ReadingElements', 0),
    ('Senses', 0),
    ('Translations', 0),
    ('Characters', 0),
    ('Radicals', 0);
COMMIT;
