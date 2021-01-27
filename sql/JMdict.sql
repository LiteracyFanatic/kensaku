PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
DROP TABLE IF EXISTS "Entries";
CREATE TABLE "Entries" (
	"id"	INTEGER NOT NULL UNIQUE,
	PRIMARY KEY("id")
);
DROP TABLE IF EXISTS "CrossReferences";
CREATE TABLE "CrossReferences" (
	"senseId"	INTEGER NOT NULL,
	"referenceKanjiElementId"	INTEGER,
	"referenceReadingElementId"	INTEGER,
	"referenceSenseId"	INTEGER,
	FOREIGN KEY("senseId") REFERENCES "Senses"("id"),
	FOREIGN KEY("referenceSenseId") REFERENCES "Senses"("id"),
	FOREIGN KEY("referenceReadingElementId") REFERENCES "ReadingElements"("id"),
	FOREIGN KEY("referenceKanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "Fields";
CREATE TABLE "Fields" (
	"entity"	TEXT NOT NULL UNIQUE,
	"text"	TEXT NOT NULL,
	PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "MiscellaneousInformation";
CREATE TABLE "MiscellaneousInformation" (
	"entity"	TEXT NOT NULL UNIQUE,
	"text"	TEXT NOT NULL,
	PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "PartsOfSpeech";
CREATE TABLE "PartsOfSpeech" (
	"entity"	TEXT NOT NULL UNIQUE,
	"text"	TEXT NOT NULL,
	PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "Senses_Fields";
CREATE TABLE "Senses_Fields" (
	"senseId"	INTEGER NOT NULL,
	"fieldEntity"	TEXT NOT NULL,
	FOREIGN KEY("fieldEntity") REFERENCES "Fields"("entity"),
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Senses_PartsOfSpeech";
CREATE TABLE "Senses_PartsOfSpeech" (
	"sensesId"	INTEGER NOT NULL,
	"partsOfSpeechEntity"	TEXT NOT NULL,
	FOREIGN KEY("partsOfSpeechEntity") REFERENCES "PartsOfSpeech"("entity"),
	FOREIGN KEY("sensesId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Senses_Dialects";
CREATE TABLE "Senses_Dialects" (
	"senseId"	INTEGER NOT NULL,
	"dialectEntity"	TEXT NOT NULL,
	FOREIGN KEY("senseId") REFERENCES "Senses"("id"),
	FOREIGN KEY("dialectEntity") REFERENCES "Dialects"("entity")
);
DROP TABLE IF EXISTS "SenseInformation";
CREATE TABLE "SenseInformation" (
	"senseId"	INTEGER NOT NULL,
	"text"	TEXT NOT NULL,
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "KanjiElementInformation";
CREATE TABLE "KanjiElementInformation" (
	"entity"	TEXT NOT NULL UNIQUE,
	"text"	TEXT NOT NULL,
	CONSTRAINT "KanjiElementInfo_PK" PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "KanjiElements_KanjiElementInformation";
CREATE TABLE "KanjiElements_KanjiElementInformation" (
	"kanjiElementId"	INTEGER NOT NULL,
	"kanjiElementInformationEntity"	TEXT NOT NULL,
	FOREIGN KEY("kanjiElementInformationEntity") REFERENCES "KanjiElementInformation"("entity"),
	FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "KanjiElements";
CREATE TABLE "KanjiElements" (
	"id"	INTEGER NOT NULL UNIQUE,
	"entryId"	INTEGER NOT NULL,
	"text"	TEXT NOT NULL,
	"priority"	TEXT,
	PRIMARY KEY("id" AUTOINCREMENT),
	FOREIGN KEY("entryId") REFERENCES "Entries"("id")
);
DROP TABLE IF EXISTS "ReadingElements";
CREATE TABLE "ReadingElements" (
	"id"	INTEGER NOT NULL UNIQUE,
	"entryId"	INTEGER NOT NULL,
	"text"	TEXT NOT NULL,
	"trueReading"	INTEGER NOT NULL,
	"priority"	TEXT,
	FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
	PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "ReadingElementInformation";
CREATE TABLE "ReadingElementInformation" (
	"entity"	TEXT NOT NULL UNIQUE,
	"text"	TEXT NOT NULL,
	PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "ReadingElements_ReadingElementInformation";
CREATE TABLE "ReadingElements_ReadingElementInformation" (
	"readingElementId"	INTEGER NOT NULL,
	"readingElementInformationEntity"	TEXT NOT NULL,
	FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id"),
	FOREIGN KEY("readingElementInformationEntity") REFERENCES "ReadingElementInformation"("entity")
);
DROP TABLE IF EXISTS "Senses";
CREATE TABLE "Senses" (
	"id"	INTEGER NOT NULL UNIQUE,
	"entryId"	INTEGER NOT NULL,
	FOREIGN KEY("entryId") REFERENCES "Entries"("id"),
	PRIMARY KEY("id" AUTOINCREMENT)
);
DROP TABLE IF EXISTS "Sense_KanjiElement_Restrictions";
CREATE TABLE "Sense_KanjiElement_Restrictions" (
	"senseId"	INTEGER NOT NULL,
	"kanjiElementId"	INTEGER NOT NULL,
	FOREIGN KEY("senseId") REFERENCES "Senses"("id"),
	FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "ReadingElement_KanjiElement_Restrictions";
CREATE TABLE "ReadingElement_KanjiElement_Restrictions" (
	"readingElementId"	INTEGER NOT NULL,
	"kanjiElementId"	INTEGER NOT NULL,
	FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id"),
	FOREIGN KEY("kanjiElementId") REFERENCES "KanjiElements"("id")
);
DROP TABLE IF EXISTS "Sense_ReadingElement_Restrictions";
CREATE TABLE "Sense_ReadingElement_Restrictions" (
	"senseId"	INTEGER NOT NULL,
	"readingElementId"	INTEGER NOT NULL,
	FOREIGN KEY("readingElementId") REFERENCES "ReadingElements"("id"),
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Senses_MiscellaneousInformation";
CREATE TABLE "Senses_MiscellaneousInformation" (
	"senseId"	INTEGER NOT NULL,
	"miscellaneousInformationEntity"	INTEGER NOT NULL,
	FOREIGN KEY("miscellaneousInformationEntity") REFERENCES "MiscellaneousInformation"("entity"),
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Dialects";
CREATE TABLE "Dialects" (
	"entity"	TEXT NOT NULL UNIQUE,
	"text"	TEXT NOT NULL,
	PRIMARY KEY("entity")
);
DROP TABLE IF EXISTS "LanguageSources";
CREATE TABLE "LanguageSources" (
	"senseId"	INTEGER NOT NULL,
	"text"	TEXT,
	"languageCode"	TEXT NOT NULL,
	"partial"	INTEGER NOT NULL,
	"wasei"	INTEGER NOT NULL,
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Glosses";
CREATE TABLE "Glosses" (
	"senseId"	INTEGER NOT NULL,
	"text"	TEXT NOT NULL,
	"language"	TEXT NOT NULL,
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DROP TABLE IF EXISTS "Antonyms";
CREATE TABLE "Antonyms" (
	"senseId"	INTEGER NOT NULL,
	"referenceKanjiElementId"	INTEGER,
	"referenceReadingElementId"	INTEGER,
	FOREIGN KEY("referenceKanjiElementId") REFERENCES "KanjiElements"("id"),
	FOREIGN KEY("referenceReadingElementId") REFERENCES "ReadingElements"("id"),
	FOREIGN KEY("senseId") REFERENCES "Senses"("id")
);
DELETE FROM sqlite_sequence;
INSERT INTO sqlite_sequence VALUES('KanjiElements',0);
INSERT INTO sqlite_sequence VALUES('ReadingElements',0);
INSERT INTO sqlite_sequence VALUES('Senses',0);
COMMIT;
