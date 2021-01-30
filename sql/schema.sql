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
    "referenceKanjiElementId" INTEGER,
    "referenceReadingElementId" INTEGER,
    "referenceSenseId" INTEGER,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id"),
    FOREIGN KEY("referenceSenseId") REFERENCES "Senses"("id"),
    FOREIGN KEY("referenceReadingElementId") REFERENCES "ReadingElements"("id"),
    FOREIGN KEY("referenceKanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "Fields";
CREATE TABLE "Fields" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "MiscellaneousInformation";
CREATE TABLE "MiscellaneousInformation" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "PartsOfSpeech";
CREATE TABLE "PartsOfSpeech" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "Senses_Fields";
CREATE TABLE "Senses_Fields" (
    "senseId" INTEGER NOT NULL,
    "fieldEntity" TEXT NOT NULL,
    FOREIGN KEY("fieldEntity") REFERENCES "Fields"("entity"),
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Senses_PartsOfSpeech";
CREATE TABLE "Senses_PartsOfSpeech" (
    "sensesId" INTEGER NOT NULL,
    "partsOfSpeechEntity" TEXT NOT NULL,
    FOREIGN KEY("partsOfSpeechEntity") REFERENCES "PartsOfSpeech"("entity"),
    FOREIGN KEY("sensesId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Senses_Dialects";
CREATE TABLE "Senses_Dialects" (
    "senseId" INTEGER NOT NULL,
    "dialectEntity" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id"),
    FOREIGN KEY("dialectEntity") REFERENCES "Dialects"("entity")
);
DROP TABLE IF EXISTS "SenseInformation";
CREATE TABLE "SenseInformation" (
    "senseId" INTEGER NOT NULL,
    "text" TEXT NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "KanjiElementInformation";
CREATE TABLE "KanjiElementInformation" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    CONSTRAINT "KanjiElementInfo_PK" PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "KanjiElements_KanjiElementInformation";
CREATE TABLE "KanjiElements_KanjiElementInformation" (
    "kanjiElementId" INTEGER NOT NULL,
    "kanjiElementInformationEntity" TEXT NOT NULL,
    FOREIGN KEY("kanjiElementInformationEntity") REFERENCES "KanjiElementInformation"("entity"),
    FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "Priorities";
CREATE TABLE "Priorities" (
    "id" INTEGER NOT NULL UNIQUE,
    "value" TEXT NOT NULL UNIQUE,
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "KanjiElements_Priorities";
CREATE TABLE "KanjiElements_Priorities" (
    "kanjiElementId" INTEGER NOT NULL,
    "priorityId" INTEGER NOT NULL,
    FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id"),
    FOREIGN KEY("priorityId") REFERENCES "Priorities"(" id")
);
DROP TABLE IF EXISTS "ReadingElements_Priorities";
CREATE TABLE "ReadingElements_Priorities" (
    "readingElementId" INTEGER NOT NULL,
    "priorityId" INTEGER NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "REadingElement"("id"),
    FOREIGN KEY("priorityId") REFERENCES "Priorities"(" id")
);
DROP TABLE IF EXISTS "KanjiElements";
CREATE TABLE "KanjiElements" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    "text" TEXT NOT NULL,
    "priority" TEXT,
    PRIMARY KEY("id" AUTOINCREMENT),
    FOREIGN KEY("entryId") REFERENCES "Entries"("id")
);
DROP TABLE IF EXISTS "ReadingElements";
CREATE TABLE "ReadingElements" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    "text" TEXT NOT NULL,
    "isTrueReading" INTEGER NOT NULL,
    "priority" TEXT,
    FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "ReadingElementInformation";
CREATE TABLE "ReadingElementInformation" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "ReadingElements_ReadingElementInformation";
CREATE TABLE "ReadingElements_ReadingElementInformation" (
    "readingElementId" INTEGER NOT NULL,
    "readingElementInformationEntity" TEXT NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id"),
    FOREIGN KEY("readingElementInformationEntity") REFERENCES "ReadingElementInformation"("entity")
);
DROP TABLE IF EXISTS "Senses";
CREATE TABLE "Senses" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "Sense_KanjiElement_Restrictions";
CREATE TABLE "Sense_KanjiElement_Restrictions" (
    "senseId" INTEGER NOT NULL,
    "kanjiElementId" INTEGER NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id"),
    FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "ReadingElement_KanjiElement_Restrictions";
CREATE TABLE "ReadingElement_KanjiElement_Restrictions" (
    "readingElementId" INTEGER NOT NULL,
    "kanjiElementId" INTEGER NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id"),
    FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "Sense_ReadingElement_Restrictions";
CREATE TABLE "Sense_ReadingElement_Restrictions" (
    "senseId" INTEGER NOT NULL,
    "readingElementId" INTEGER NOT NULL,
    FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id"),
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Senses_MiscellaneousInformation";
CREATE TABLE "Senses_MiscellaneousInformation" (
    "senseId" INTEGER NOT NULL,
    "miscellaneousInformationEntity" INTEGER NOT NULL,
    FOREIGN KEY("miscellaneousInformationEntity") REFERENCES "MiscellaneousInformation"("entity"),
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Dialects";
CREATE TABLE "Dialects" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "LanguageSources";
CREATE TABLE "LanguageSources" (
    "senseId" INTEGER NOT NULL,
    "text" TEXT,
    "languageCode" TEXT NOT NULL,
    "isPartial" INTEGER NOT NULL,
    "isWasei" INTEGER NOT NULL,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Glosses";
CREATE TABLE "Glosses" (
    "senseId" INTEGER NOT NULL,
    "text" TEXT NOT NULL,
    "language" TEXT NOT NULL,
    "type" TEXT,
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Antonyms";
CREATE TABLE "Antonyms" (
    "senseId" INTEGER NOT NULL,
    "referenceKanjiElementId" INTEGER,
    "referenceReadingElementId" INTEGER,
    FOREIGN KEY("referenceKanjiElementId") REFERENCES "KanjiElements"("id"),
    FOREIGN KEY("referenceReadingElementId") REFERENCES "ReadingElements"("id"),
    FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
-- Exclusive to JMneDict scehma
DROP TABLE IF EXISTS "NameTypes";
CREATE TABLE "NameTypes" (
    "entity" TEXT NOT NULL UNIQUE,
    "text" TEXT NOT NULL,
    PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "Translations";
CREATE TABLE "Translations" (
    "id" INTEGER NOT NULL UNIQUE,
    "entryId" INTEGER NOT NULL,
    FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "Translations_NameTypes";
CREATE TABLE "Translations_NameTypes" (
    "translationId" INTEGER NOT NULL,
    "nameTypeEntity" TEXT NOT NULL,
    FOREIGN KEY("nameTypeEntity") REFERENCES "NameTypes"("entity"),
    FOREIGN KEY("translationId") REFERENCES "Translations"("id")
);
DROP TABLE IF EXISTS "TranslationCrossReferences";
CREATE TABLE "TranslationCrossReferences" (
    "translationId" INTEGER NOT NULL,
    "referenceKanjiElementId" INTEGER,
    "referenceReadingElementId" INTEGER,
    "referencetranslationId" INTEGER,
    FOREIGN KEY("translationId") REFERENCES "Translations"("id"),
    FOREIGN KEY("referenceTranslationId") REFERENCES "Translations"("id"),
    FOREIGN KEY("referenceReadingElementId") REFERENCES "ReadingElements"("id"),
    FOREIGN KEY("referenceKanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "TranslationContents";
CREATE TABLE "TranslationContents" (
    "translationId" INTEGER NOT NULL,
    "text" TEXT NOT NULL,
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
    "literal" TEXT NOT NULL,
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
    "id" INTEGER NOT NULL UNIQUE,
    "value" INTEGER NOT NULL,
    "type" TEXT NOT NULL,
    PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "Characters_KeyRadicals";
CREATE TABLE "Characters_KeyRadicals" (
    "characterId" INTEGER NOT NULL,
    "keyRadicalId" INTEGER NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id"),
    FOREIGN KEY("keyRadicalId") REFERENCES "KeyRadicals"("id")
);
DROP TABLE IF EXISTS "StrokeMiscounts";
CREATE TABLE "StrokeMiscounts" (
    "characterId" INTEGER NOT NULL,
    "count" INTEGER NOT NULL,
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
    "name" TEXT NOT NULL,
    FOREIGN KEY("characterId") REFERENCES "Characters"("id")
);
DROP TABLE IF EXISTS "CharacterDictionaryReferences";
CREATE TABLE "CharacterDictionaryReferences" (
    "characterId" INTEGER NOT NULL,
    "indexNumber" INTEGER NOT NULL,
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
    "type", TEXT NOT NULL,
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
    ('KeyRadicals', 0),
    ('Radicals', 0),
    ('Priorities', 0);
COMMIT;
