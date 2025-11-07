CREATE TABLE "Mitarbeiter" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Name"	TEXT NOT NULL,
	"Vorname"	TEXT,
	"Abteilung"	TEXT,
	"Telefon"	TEXT,
	PRIMARY KEY("Id" AUTOINCREMENT)
)

CREATE TABLE "Phase" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Nummer"	TEXT NOT NULL,
	"Name"	TEXT NOT NULL,
	"Dauer"	INTEGER,
	"Vorgaenger"	INTEGER,
	"ProjektId"	INTEGER NOT NULL,
	PRIMARY KEY("Id" AUTOINCREMENT),
	FOREIGN KEY("ProjektId") REFERENCES "Projekt"("Id"),
	FOREIGN KEY("Vorgaenger") REFERENCES "Phase"("Id")
)

CREATE TABLE "Projekt" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Bezeichnung"	TEXT NOT NULL UNIQUE,
	"Startdatum"	TEXT,
	"Enddatum"	TEXT,
	"MitarbeiterId"	INTEGER,
	PRIMARY KEY("Id" AUTOINCREMENT),
	FOREIGN KEY("MitarbeiterId") REFERENCES "Mitarbeiter"("Id")
)
