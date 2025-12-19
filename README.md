	CREATE TABLE "Employee" (
		"Id"	INTEGER NOT NULL UNIQUE,
		"LastName"	TEXT NOT NULL,
		"FirstName"	TEXT,
		"Department"	TEXT,
		"Phone"	TEXT,
		PRIMARY KEY("Id" AUTOINCREMENT)
	);

	CREATE TABLE "Phase" (
		"Id"	INTEGER NOT NULL UNIQUE,
		"Number"	TEXT NOT NULL,
		"Name"	TEXT NOT NULL,
		"Duration"	INTEGER,
		"ProjectId"	INTEGER NOT NULL,
		PRIMARY KEY("Id" AUTOINCREMENT),
		FOREIGN KEY("ProjectId") REFERENCES "Project"("Id")
	);

	CREATE TABLE "Predecessor" (
		"PhaseId"	INTEGER NOT NULL,
		"PredecessorId"	INTEGER NOT NULL,
		PRIMARY KEY("PredecessorId","PhaseId"),
		FOREIGN KEY("PhaseId") REFERENCES "Phase"("Id"),
		FOREIGN KEY("PredecessorId") REFERENCES "Phase"("Id"),
		CHECK("PhaseId" <> "PredecessorId")
	);
	
	CREATE TABLE "Project" (
		"Id"	INTEGER NOT NULL UNIQUE,
		"Title"	TEXT NOT NULL UNIQUE,
		"StartDate"	TEXT,
		"EndDate"	TEXT,
		"EmployeeId"	INTEGER,
		PRIMARY KEY("Id" AUTOINCREMENT),
		FOREIGN KEY("EmployeeId") REFERENCES "Employee"("Id")
	);
