/*
 * This file is part of ChronoJump
 *
 * ChronoJump is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or   
 *    (at your option) any later version.
 *    
 * ChronoJump is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 *    GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * Copyright (C) 2019   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
//using System.Data;
using System.Collections;
using System.IO; //DirectoryInfo
using Mono.Data.Sqlite;
using System.Text.RegularExpressions; //Regex

class SqliteRunEncoder : Sqlite
{
	private static string table = Constants.RunEncoderTable;

	public SqliteRunEncoder() {
	}

	~SqliteRunEncoder() {}

	/*
	 * create and initialize tables
	 */

	protected internal static void createTable()
	{
		dbcmd.CommandText =
			"CREATE TABLE " + table + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"personID INT, " +
			"sessionID INT, " +
			"exerciseID INT, " + //right now all will be exercise 0, until we have a clear idea of what exercises could be done and how can affect measurements
			"device TEXT, " +
			"distance INT, " +
			"temperature INT, " +
			"filename TEXT, " +
			"url TEXT, " +		//URL of data files. stored as relative
			"datetime TEXT, " + 	//2019-07-11_15-01-44
			"comments TEXT, " +
			"videoURL TEXT)";	//URL of video of signals. stored as relative
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
	}

	public static int Insert (bool dbconOpened, string insertString)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "INSERT INTO " + table +
				" (uniqueID, personID, sessionID, exerciseID, device, distance, temperature, filename, url, dateTime, comments, videoURL)" +
				" VALUES " + insertString;
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery(); //TODO uncomment this again

		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		closeIfNeeded(dbconOpened);

		return myLast;
	}

	public static void Update (bool dbconOpened, string updateString)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "UPDATE " + table + " SET " + updateString;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}

	public static void UpdateComments (bool dbconOpened, int uniqueID, string comments)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "UPDATE " + table + " SET comments = \"" + comments + "\"" +
			" WHERE uniqueID = " + uniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}

	/* right now unused
	public static void DeleteSQLAndFile (bool dbconOpened, int uniqueID)
	{
		RunEncoder fs = (RunEncoder) Select (dbconOpened, uniqueID, -1, -1)[0];
		DeleteSQLAndFile (dbconOpened, fs);
	}
	*/
	public static void DeleteSQLAndFiles (bool dbconOpened, RunEncoder re)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "DELETE FROM " + table + " WHERE uniqueID = " + re.UniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);

		//delete the files
		Util.FileDelete(re.FullURL);

		if(re.FullVideoURL != "")
			Util.FileDelete(re.FullVideoURL);
	}

	public static ArrayList Select (bool dbconOpened, int uniqueID, int personID, int sessionID)
	{
		openIfNeeded(dbconOpened);

		string selectStr = "SELECT " + table + ".*, " + Constants.RunEncoderExerciseTable + ".Name FROM " + table + ", " + Constants.RunEncoderExerciseTable;
		string whereStr = " WHERE " + table + ".exerciseID = " + Constants.RunEncoderExerciseTable + ".UniqueID ";

		string uniqueIDStr = "";
		if(uniqueID != -1)
			uniqueIDStr = " AND " + table + ".uniqueID = " + uniqueID;

		string personIDStr = "";
		if(personID != -1)
			personIDStr = " AND " + table + ".personID = " + personID;

		string sessionIDStr = "";
		if(sessionID != -1)
			sessionIDStr = " AND " + table + ".sessionID = " + sessionID;

		dbcmd.CommandText = selectStr + whereStr + uniqueIDStr + personIDStr + sessionIDStr + " Order BY " + table + ".uniqueID";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList(1);
		RunEncoder fs;

		while(reader.Read()) {
			fs = new RunEncoder (
					Convert.ToInt32(reader[0].ToString()),	//uniqueID
					Convert.ToInt32(reader[1].ToString()),	//personID
					Convert.ToInt32(reader[2].ToString()),	//sessionID
					Convert.ToInt32(reader[3].ToString()),	//exerciseID
					(RunEncoder.Devices) Enum.Parse(
						typeof(RunEncoder.Devices), reader[4].ToString()), 	//device
					Convert.ToInt32(reader[5].ToString()),	//distance
					Convert.ToInt32(reader[6].ToString()),	//temperature
					reader[7].ToString(),			//filename
					Util.MakeURLabsolute(fixOSpath(reader[8].ToString())),	//url
					reader[9].ToString(),			//datetime
					reader[10].ToString(),			//comments
					reader[11].ToString(),			//videoURL
					reader[12].ToString()			//exerciseName
					);
			array.Add(fs);
		}

		reader.Close();
		closeIfNeeded(dbconOpened);

		return array;
	}

	public static ArrayList SelectRowsOfAnExercise(bool dbconOpened, int exerciseID)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "select count(*), " +
			Constants.PersonTable + ".name, " +
			Constants.SessionTable + ".name, " +
			Constants.SessionTable + ".date " +
			" FROM " + table + ", " + Constants.PersonTable + ", " + Constants.SessionTable +
			" WHERE exerciseID == " + exerciseID +
			" AND " + Constants.PersonTable + ".uniqueID == " + table + ".personID " +
		        " AND " + Constants.SessionTable + ".uniqueID == " + table + ".sessionID " +
			" GROUP BY sessionID, personID";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList();
		int count = 0;
		while(reader.Read()) {
			array.Add(new string [] {
					count.ToString(),
					reader[0].ToString(), //count
					reader[1].ToString(), //person name
					reader[2].ToString(), //session name
					reader[3].ToString()  //session date
			});
			count ++;
		}

		reader.Close();
		closeIfNeeded(dbconOpened);

		return array;
	}

	public static ArrayList SelectSessionOverviewSets (bool dbconOpened, int sessionID)
	{
		if(! dbconOpened)
			Sqlite.Open();

		dbcmd.CommandText =
			"SELECT person77.uniqueID, person77.name, person77.sex, runEncoderExercise.name, COUNT(*)" +
			" FROM person77, personSession77, runEncoderExercise, runEncoder" +
			" WHERE person77.uniqueID == runEncoder.personID AND personSession77.personID == runEncoder.personID AND personSession77.sessionID == runEncoder.sessionID AND runEncoderExercise.uniqueID==runEncoder.exerciseID AND runEncoder.sessionID == " + sessionID +
			" GROUP BY runEncoder.personID, exerciseID" +
			" ORDER BY person77.name";

		LogB.SQL(dbcmd.CommandText.ToString());

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList();
		while(reader.Read())
		{
			string [] s = {
				reader[0].ToString(), 	//personID
				reader[1].ToString(), 	//person name
				reader[2].ToString(), 	//person sex
				reader[3].ToString(), 	//exercise name
				reader[4].ToString()	//sets count
			};
			array.Add (s);
		}

		reader.Close();
		if(! dbconOpened)
			Sqlite.Close();

		return array;
	}

	protected internal static void import_from_1_70_to_1_71() //database is opened
	{
		//LogB.PrintAllThreads = true; //TODO: remove this
		LogB.Information("at import_from_1_70_to_1_71()");

		string raceAnalyzerDir = Util.GetRunEncoderDir();
		if(Sqlite.UpdatingDBFrom == Sqlite.UpdatingDBFromEnum.IMPORTED_SESSION)
			raceAnalyzerDir = Path.Combine(Util.GetDatabaseTempImportDir(), "raceAnalyzer");

		if(! Directory.Exists(raceAnalyzerDir))
		{
			LogB.Information("nothing to import");
			//LogB.PrintAllThreads = false; //TODO: remove this
			return;
		}

		bool importedSomething = false;
		DirectoryInfo [] sessions = new DirectoryInfo(raceAnalyzerDir).GetDirectories();
		foreach (DirectoryInfo session in sessions) //session.Name will be the UniqueID
		{
			FileInfo[] files = session.GetFiles();
			foreach (FileInfo file in files)
			{
				//in dir there are .csv and .png, take only the .csv
				if(Util.GetExtension(file.Name) != ".csv")
					continue;

				string fileWithoutExtension = Util.RemoveExtension(Util.GetLastPartOfPath(file.Name));
				RunEncoderLoadTryToAssignPersonAndComment relt =
					new RunEncoderLoadTryToAssignPersonAndComment(true, fileWithoutExtension, Convert.ToInt32(session.Name));

				Person p = relt.GetPerson();
				if(p.UniqueID == -1)
					continue;

				if(! Util.IsNumber(session.Name, false))
					continue;

				string parsedDate = UtilDate.ToFile(DateTime.MinValue);
				Match match = Regex.Match(file.Name, @"(\d+-\d+-\d+_\d+-\d+-\d+)");
				if(match.Groups.Count == 2)
					parsedDate = match.Value;

				//filename will be this
				string myFilename = p.UniqueID + "_" + p.Name + "_" + parsedDate + ".csv";
				//try to move the file
				try{
					File.Move(file.FullName, Util.GetRunEncoderSessionDir(Convert.ToInt32(session.Name)) + Path.DirectorySeparatorChar + myFilename);
				} catch {
					//if cannot, then use old filename
					myFilename = file.FullName;
				}

				int exerciseID = 0; //initial import with all exercises as 0 (because exercises are not yet defined)
				int distance = 99; //mark to know at import that this have to be changed
				int temperature = 25;
				RunEncoder runEncoder = new RunEncoder(-1, p.UniqueID, Convert.ToInt32(session.Name), exerciseID,
						RunEncoder.Devices.MANUAL, distance, temperature,
						myFilename,
						Util.MakeURLrelative(Util.GetRunEncoderSessionDir(Convert.ToInt32(session.Name))),
						parsedDate, relt.Comment,
						"", ""); //import without video and without name on comment

				runEncoder.InsertSQL(true);
				importedSomething = true;
			}
		}

		//need to create an exercise to assign to the imported files
		if(importedSomething)
			SqliteRunEncoderExercise.Insert(true, 0, "Sprint", "");

		LogB.Information("end of import_from_1_70_to_1_71()");
		//LogB.PrintAllThreads = false; //TODO: remove this
	}

}

class SqliteRunEncoderExercise : Sqlite
{
	private static string table = Constants.RunEncoderExerciseTable;

	public SqliteRunEncoderExercise() {
	}

	~SqliteRunEncoderExercise() {}

	/*
	 * create and initialize tables
	 */

	protected internal static void createTable()
	{
		dbcmd.CommandText =
			"CREATE TABLE " + table + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"name TEXT, " +
			"description TEXT)";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
	}

	//undefined defaultAngle will be 1000
	//note execution can have a different angle than the default angle
	public static int Insert (bool dbconOpened, int uniqueID, string name, string description)
	{
		if(! dbconOpened)
			Sqlite.Open();

		string uniqueIDStr = "NULL";
		if(uniqueID != -1)
			uniqueIDStr = uniqueID.ToString();

		dbcmd.CommandText = "INSERT INTO " + table +
				" (uniqueID, name, description) VALUES (" +
				uniqueIDStr + ", \"" + name + "\", \"" + description + "\")";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		if(! dbconOpened)
			Sqlite.Close();

		return myLast;
	}

	public static void Update (bool dbconOpened, RunEncoderExercise ex)
	{
		if(! dbconOpened)
			Sqlite.Open();

		/*
		   string uniqueIDStr = "NULL";
		   if(ex.UniqueID != -1)
			   uniqueIDStr = ex.UniqueID.ToString();
		   */

		dbcmd.CommandText = "UPDATE " + table + " SET " +
			" name = \"" + ex.Name +
			"\", description = \"" + ex.Description +
			"\" WHERE uniqueID = " + ex.UniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		if(! dbconOpened)
			Sqlite.Close();
	}

	public static void Delete (bool dbconOpened, int uniqueID)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "DELETE FROM " + table + " WHERE uniqueID = " + uniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}


	public static ArrayList Select (bool dbconOpened, int uniqueID, bool onlyNames)
	{
		if(! dbconOpened)
			Sqlite.Open();

		string uniqueIDStr = "";
		if(uniqueID != -1)
			uniqueIDStr = " WHERE " + table + ".uniqueID = " + uniqueID;

		if(onlyNames)
			dbcmd.CommandText = "SELECT name FROM " + table + uniqueIDStr;
		else
			dbcmd.CommandText = "SELECT * FROM " + table + uniqueIDStr;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList(1);
		RunEncoderExercise ex = new RunEncoderExercise();

		if(onlyNames) {
			while(reader.Read()) {
				ex = new RunEncoderExercise (reader[0].ToString());
				array.Add(ex);
			}
		} else {
			while(reader.Read()) {
				int angleDefault = 0;

				ex = new RunEncoderExercise (
						Convert.ToInt32(reader[0].ToString()),	//uniqueID
						reader[1].ToString(),			//name
						reader[2].ToString()			//description
						);
				array.Add(ex);
			}
		}

		reader.Close();
		if(! dbconOpened)
			Sqlite.Close();

		return array;
	}

}
