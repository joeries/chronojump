/*
 * This file is part of ChronoJump
 *
 * ChronoJump is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or   
 * (at your option) any later version.
 *    
 * ChronoJump is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * Copyright (C) 2019-2020   Xavier de Blas <xaviblas@gmail.com>
 */

using Gtk;
using System;
using System.Threading;

public partial class ChronoJumpWindow
{
	static ChronojumpImporter.Result importerResult;
	static Thread threadImport;
	static ChronojumpImporter chronojumpImporter;

	private void on_button_import_chronojump_session(object o, EventArgs args)
	{
		sessionLoadWindowShow(app1s_windowType.IMPORT_SESSION);
		app1s_radio_import_new_current_sensitive();
	}

	//from import session
	private void on_load_session_accepted_to_import(object o, EventArgs args)
	{
		int sourceSession = app1s_CurrentSessionId();
		string databasePath = app1s_ImportDatabasePath();
		LogB.Information (databasePath);

		Session destinationSession = currentSession;

		if (app1s_ImportToNewSession ()) {
			destinationSession = null;
		}

		importSessionFromDatabasePrepare (databasePath, sourceSession, destinationSession);
	}

	private void importSessionFromDatabasePrepare (string databasePath, int sourceSession, Session destinationSession)
	{
		string source_filename = databasePath;
		string destination_filename = Sqlite.DatabaseFilePath;

		int destinationSessionId;
		if (destinationSession == null)
			destinationSessionId = 0;
		else
			destinationSessionId = destinationSession.UniqueID;

		chronojumpImporter = new ChronojumpImporter (app1, source_filename, destination_filename, sourceSession, destinationSessionId, preferences.debugMode);

		if(destinationSessionId == 0)
		{
			app1s_NotebookPage(app1s_PAGE_IMPORT_RESULT); //import do and end page
			importSessionFromDatabasePrepare2 (new object(), new EventArgs());
		} else
		{
			string sessionName = ChronojumpImporter.GetSessionName (chronojumpImporter.SourceFile, chronojumpImporter.SourceSession);
			app1s_LabelImportSessionName(sessionName);
			app1s_LabelImportFile(chronojumpImporter.SourceFile);

			app1s_NotebookPage(app1s_PAGE_IMPORT_CONFIRM); //import confirm page
		}
	}

	private void importSessionFromDatabasePrepare2 (object o, EventArgs args)
	{
		LogB.Information("import before thread");	
		LogB.PrintAllThreads = true; //TODO: remove this

		threadImport = new Thread(new ThreadStart(importSessionFromDatabaseDo));
		GLib.Idle.Add (new GLib.IdleHandler (PulseGTKImport));

		LogB.ThreadStart(); 
		threadImport.Start(); 
	}

	//no GTK here!
	private void importSessionFromDatabaseDo()
	{
		importerResult = chronojumpImporter.import ();
	}

	private bool PulseGTKImport ()
	{
		if ( ! threadImport.IsAlive ) {
			LogB.ThreadEnding();
			importSessionFromDatabaseEnd();

			app1s_Pulse(chronojumpImporter.MessageToPulsebar);
			app1s_PulseEnd();

			LogB.ThreadEnded();
			return false;
		}

		app1s_Pulse(chronojumpImporter.MessageToPulsebar);

		Thread.Sleep (100);
		//LogB.Debug(threadImport.ThreadState.ToString());
		return true;
	}

	private void importSessionFromDatabaseEnd()
	{
		if (importerResult.success)
		{
			//update GUI if events have been added
			//1) simple jump
			createComboSelectJumps(false);
			UtilGtk.ComboUpdate(combo_result_jumps,
					SqliteJumpType.SelectJumpTypes(false, Constants.AllJumpsNameStr(), "", true), ""); //without filter, only select name
			combo_select_jumps.Active = 0;
			combo_result_jumps.Active = 0;

			createComboSelectJumpsDjOptimalFall(false);
			createComboSelectJumpsWeightFVProfile(false);
			createComboSelectJumpsEvolution(false);

			//2) reactive jump
			createComboSelectJumpsRj(false);
			UtilGtk.ComboUpdate(combo_result_jumps_rj,
					SqliteJumpType.SelectJumpRjTypes(Constants.AllJumpsNameStr(), true), ""); //without filter, only select name
			combo_select_jumps_rj.Active = 0;
			combo_result_jumps_rj.Active = 0;

			//3) simple run
			createComboSelectRuns(false);
			UtilGtk.ComboUpdate(combo_result_runs,
					SqliteRunType.SelectRunTypes(Constants.AllRunsNameStr(), true), ""); //without filter, only select name
			combo_select_runs.Active = 0;
			combo_result_runs.Active = 0;

			//4) intervallic run
			createComboSelectRunsInterval(false);
			UtilGtk.ComboUpdate(combo_result_runs_interval,
					SqliteRunIntervalType.SelectRunIntervalTypes(Constants.AllRunsNameStr(), true), ""); //without filter, only select name
			combo_select_runs_interval.Active = 0;
			combo_result_runs_interval.Active = 0;

			// TODO: we need this on encoder or is already done at reloadSession???
			//createEncoderCombos();

			// forceSensor
			fillForceSensorExerciseCombo("");

			// runEncoder
			fillRunEncoderExerciseCombo("");

			//update stats combos
			updateComboStats ();

			reloadSession ();

			//chronojumpImporter.showImportCorrectlyFinished ();
			app1s_ShowLabelImportedOk();
		} else {
			LogB.Debug ("Chronojump Importer error: ", importerResult.error);
			app1s_ShowImportError(importerResult.error);
		}
	}
}
