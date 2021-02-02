PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
-- JMDict scehma (many parts shared with JMnedict schema)
DROP TABLE IF EXISTS "Entries";
CREATE TABLE "Entries" (
    "id" INTEGER NOT NULL UNIQUE,
    "isProperName" INTEGER NOT NULL,
    PRIMARY KEY("id")
);
DROP TABLE IF EXISTS "SenseCrossReferences";
CREATE TABLE "SenseCrossReferences" (
    "senseId" INTEGER NOT NULL,
    "referenceKanjiElement" TEXT,
    "referenceReadingElement" TEXT,
    "referenceSense" INTEGER,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Fields";
CREATE TABLE "Fields" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "PartsOfSpeech";
CREATE TABLE "PartsOfSpeech" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Dialects";
CREATE TABLE "Dialects" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "SenseInformation";
CREATE TABLE "SenseInformation" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "KanjiElementInformation";
CREATE TABLE "KanjiElementInformation" (
    "kanjiElementId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "KanjiElementPriorities";
CREATE TABLE "KanjiElementPriorities" (
    "kanjiElementId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "ReadingElementPriorities";
CREATE TABLE "ReadingElementPriorities" (
    "readingElementId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "ReadingElement"("id")
);
DROP TABLE IF EXISTS "KanjiElements";
CREATE TABLE "KanjiElements" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    PRIMARY KEY("id" AUTOINCREMENT),
    FOREIGN KEY("entryId") REFERENCES "Entries"("id")
);
DROP TABLE IF EXISTS "ReadingElements";
CREATE TABLE "ReadingElements" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "isTrueReading" INTEGER NOT NULL,
    FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "ReadingElementInformation";
CREATE TABLE "ReadingElementInformation" (
    "readingElementId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id")
);
DROP TABLE IF EXISTS "Senses";
CREATE TABLE "Senses" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "SenseKanjiElementRestrictions";
CREATE TABLE "SenseKanjiElementRestrictions" (
    "senseId" INTEGER NOT NULL,
    "kanjiElement" INTEGER NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "ReadingElementRestrictions";
CREATE TABLE "ReadingElementRestrictions" (
    "readingElementId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id")
);
DROP TABLE IF EXISTS "SenseReadingElementRestrictions";
CREATE TABLE "SenseReadingElementRestrictions" (
    "senseId" INTEGER NOT NULL,
    "readingElement" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "MiscellaneousInformation";
CREATE TABLE "MiscellaneousInformation" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "LanguageSources";
CREATE TABLE "LanguageSources" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT,
    "languageCode" TEXT NOT NULL,
    "isPartial" INTEGER NOT NULL,
    "isWasei" INTEGER NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Glosses";
CREATE TABLE "Glosses" (
    "senseId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "language" TEXT NOT NULL,
    "type" TEXT,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Antonyms";
CREATE TABLE "Antonyms" (
    "senseId" INTEGER NOT NULL,
    "referenceKanjiElement" INTEGER,
    "referenceReadingElement" INTEGER,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
-- Exclusive to JMneDict scehma
DROP TABLE IF EXISTS "Translations";
CREATE TABLE "Translations" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "NameTypes";
CREATE TABLE "NameTypes" (
    "translationId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("translationId") REFERENCES "Translations"("id")
);
DROP TABLE IF EXISTS "TranslationCrossReferences";
CREATE TABLE "TranslationCrossReferences" (
    "translationId" INTEGER NOT NULL,
    "referenceKanjiElement" TEXT,
    "referenceReadingElement" TEXT,
    "referencetranslation" INTEGER,
    FOREIGN KEY("translationId") REFERENCES "Translations"("id")
);
DROP TABLE IF EXISTS "TranslationContents";
CREATE TABLE "TranslationContents" (
    "translationId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "language" TEXT NOT NULL,
    FOREIGN KEY("translationId") REFERENCES "Translations"("id")
);
-- kanjidic2 schema
DROP TABLE IF EXISTS "Kanjidic2Info";
CREATE TABLE "Kanjidic2Info" (
    "fileVersion" INTEGER NOT NULL,
    "databaseVersion" TEXT NOT NULL,
    "dateOfCreation" TEXT NOT NULL
);
DROP TABLE IF EXISTS "Characters";
CREATE TABLE "Characters" (
    "id" INTEGER NOT NULL UNIQUE,
    "value" TEXT NOT NULL,
    "grade" INTEGER,
    "strokeCount" INTEGER NOT NULL,
    "frequency" INTEGER,
    "isRadical" INTEGER NOT NULL,
    "oldJlptLevel" INTEGER,
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE if EXISTS "Codepoints";
CREATE TABLE "Codepoints" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "type" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "KeyRadicals";
CREATE TABLE "KeyRadicals" (
    "characterId" INTEGER NOT NULL,
    "value" INTEGER NOT NULL,
    "type" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "StrokeMiscounts";
CREATE TABLE "StrokeMiscounts" (
    "characterId" INTEGER NOT NULL,
    "value" INTEGER NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "CharacterVariants";
CREATE TABLE "CharacterVariants" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "type" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "RadicalNames";
CREATE TABLE "RadicalNames" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "CharacterDictionaryReferences";
CREATE TABLE "CharacterDictionaryReferences" (
    "characterId" INTEGER NOT NULL,
    "indexNumber" TEXT NOT NULL,
    "type" TEXT NOT NULL,
    "volume" INTEGER,
    "page" INTEGER,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "CharacterQueryCodes";
CREATE TABLE "CharacterQueryCodes" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "type" TEXT NOT NULL,
    "skipMisclassification" TEXT,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "CharacterReadings";
CREATE TABLE "CharacterReadings" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "type" TEXT NOT NULL,
    "isJouyou" INTEGER NOT NULL,
    "onType" TEXT,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "CharacterMeanings";
CREATE TABLE "CharacterMeanings" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    "language" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "Nanori";
CREATE TABLE "Nanori" (
    "characterId" INTEGER NOT NULL,
    "value" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
-- krad schema
DROP TABLE IF EXISTS "Radicals";
CREATE TABLE "Radicals" (
    "id" INTEGER NOT NULL UNIQUE,
    "value" TEXT NOT NULL UNIQUE,
    "strokeCount" INTEGER NOT NULL,
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "Characters_Radicals";
CREATE TABLE "Characters_Radicals" (
    "characterId" INTEGER NOT NULL,
    "radicalId" INTEGER NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id"),
    FOREIGN KEY("radicalId") REFERENCES "Radicals"("id")
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
