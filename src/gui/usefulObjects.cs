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
 * Copyright (C) 2004-2020   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Data;
using Gtk;
using System.Collections; //ArrayList
using System.Collections.Generic; //List<T>

//this file has classes to allow to pass gui objectes easily
public class ExecutingGraphData
{
	public Gtk.Button Button_cancel;
	public Gtk.Button Button_finish;
	public Gtk.Label Label_message;
	public Gtk.Label Label_event_value;
	public Gtk.Label Label_time_value;
	public Gtk.Label Label_video_feedback;
	public Gtk.ProgressBar Progressbar_event;
	public Gtk.ProgressBar Progressbar_time;
	
	public ExecutingGraphData(
			Gtk.Button Button_cancel, Gtk.Button Button_finish, 
			Gtk.Label Label_message,
			Gtk.Label Label_event_value, Gtk.Label Label_time_value,
			Gtk.Label Label_video_feedback,
			Gtk.ProgressBar Progressbar_event, Gtk.ProgressBar Progressbar_time) 
	{
		this.Button_cancel =  Button_cancel;
		this.Button_finish =  Button_finish;
		this.Label_message =  Label_message;
		this.Label_event_value =  Label_event_value;
		this.Label_time_value =  Label_time_value;
		this.Label_video_feedback = Label_video_feedback;
		this.Progressbar_event =  Progressbar_event;
		this.Progressbar_time =  Progressbar_time;
	}

	public ExecutingGraphData() {
	}
}	

public class PrepareEventGraphJumpSimple {
	//sql data of previous jumps to plot graph and show stats at bottom
	public string [] jumpsAtSQL;
	
	public double personMAXAtSQLAllSessions;
	public double personMAXAtSQL;
	public double sessionMAXAtSQL;

	public double personAVGAtSQL;
	public double sessionAVGAtSQL;

	//current data
	public double tv;
	public double tc;
	public bool djShowHeights; //if djShowHeights and is a dj, graph falling height and jump height

		
	private enum jumpVariables { HEIGHT, TVTC, TC }

	public PrepareEventGraphJumpSimple() {
	}

	public PrepareEventGraphJumpSimple(double tv, double tc, int sessionID, int personID, string table, string type, bool djShowHeights)
	{
		Sqlite.Open();

		//select data from SQL to update graph	
		jumpsAtSQL = SqliteJump.SelectJumps(true, sessionID, personID, "", type,
				Sqlite.Orders_by.ID_DESC, 10); //select only last 10

		string sqlSelect = "";
		if(tv > 0) {
			if(tc <= 0)
				sqlSelect = "100*4.9*(TV/2)*(TV/2)";
			else {
				if(djShowHeights)
					sqlSelect = "100*4.9*(TV/2)*(TV/2)";
				else
					sqlSelect = "TV"; //if tc is higher than tv it will be fixed on PrepareJumpSimpleGraph
			}
		} else
			sqlSelect = "TC";
		
		personMAXAtSQLAllSessions = SqliteSession.SelectMAXEventsOfAType(true, -1, personID, table, type, sqlSelect);
		personMAXAtSQL = SqliteSession.SelectMAXEventsOfAType(true, sessionID, personID, table, type, sqlSelect);
		sessionMAXAtSQL = SqliteSession.SelectMAXEventsOfAType(true, sessionID, -1, table, type, sqlSelect);

		personAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(true, sessionID, personID, table, type, sqlSelect);
		sessionAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(true, sessionID, -1, table, type, sqlSelect);
	
		//end of select data from SQL to update graph	
			
		this.tv = tv;
		this.tc = tc;
		this.djShowHeights = djShowHeights;
		
		Sqlite.Close();
	}

	~PrepareEventGraphJumpSimple() {}
}

public class PrepareEventGraphJumpReactive {
	public double lastTv;
	public double lastTc;
	public string tvString;
	public string tcString;

	public PrepareEventGraphJumpReactive() {
	}

	public PrepareEventGraphJumpReactive(double lastTv, double lastTc, string tvString, string tcString) {
		this.lastTv = lastTv;
		this.lastTc = lastTc;
		this.tvString = tvString;
		this.tcString = tcString;
	}

	~PrepareEventGraphJumpReactive() {}
}

public class PrepareEventGraphRunSimple {
	//sql data of previous runs to plot graph and show stats at bottom
	public string [] runsAtSQL;
	
	public double personMAXAtSQLAllSessions;
	public double personMAXAtSQL;
	public double sessionMAXAtSQL;

	public double personAVGAtSQL;
	public double sessionAVGAtSQL;
	
	public double time;
	public double speed;

	public PrepareEventGraphRunSimple() {
	}

	public PrepareEventGraphRunSimple(double time, double speed, int sessionID, int personID, string table, string type) 
	{
		Sqlite.Open();
		
		//obtain data
		runsAtSQL = SqliteRun.SelectRuns(true, sessionID, personID, type,
				Sqlite.Orders_by.ID_DESC, 10); //select only last 10

		
		string sqlSelect = "distance/time";
		
		personMAXAtSQLAllSessions = SqliteSession.SelectMAXEventsOfAType(true, -1, personID, table, type, sqlSelect);
		personMAXAtSQL = SqliteSession.SelectMAXEventsOfAType(true, sessionID, personID, table, type, sqlSelect);
		sessionMAXAtSQL = SqliteSession.SelectMAXEventsOfAType(true, sessionID, -1, table, type, sqlSelect);
		
		//distancePersonAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(true, sessionID, personID, table, type, "distance");
		//distanceSessionAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(true, sessionID, -1, table, type, "distance");
		//better to know speed like:
		//SELECT AVG(distance/time) from run; than 
		//SELECT AVG(distance) / SELECT AVG(time) 
		//first is ok, because is the speed AVG
		//2nd is not good because it tries to do an AVG of all distances and times
		personAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(true, sessionID, personID, table, type, sqlSelect);
		sessionAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(true, sessionID, -1, table, type, sqlSelect);
		
		this.time = time;
		this.speed = speed;
		
		Sqlite.Close();
	}

	~PrepareEventGraphRunSimple() {}
}

public class PrepareEventGraphRunInterval {
	public double distance;
	public double lastTime;
	public string timesString;
	public double distanceTotal; //we pass this because it's dificult to calculate in runs with variable distances
	public string distancesString; //we pass this because it's dificult to calculate in runs with variable distances
	public bool startIn;
	public bool finished;

	public PrepareEventGraphRunInterval() {
	}

	public PrepareEventGraphRunInterval(double distance, double lastTime, string timesString,
			double distanceTotal, string distancesString, bool startIn, bool finished) {
		this.distance = distance;
		this.lastTime = lastTime;
		this.timesString = timesString;
		this.distanceTotal = distanceTotal;
		this.distancesString = distancesString;
		this.startIn = startIn;
		this.finished = finished;
	}

	~PrepareEventGraphRunInterval() {}
}

public class PrepareEventGraphPulse {
	public double lastTime;
	public string timesString;

	public PrepareEventGraphPulse() {
	}

	public PrepareEventGraphPulse(double lastTime, string timesString) {
		this.lastTime = lastTime;
		this.timesString = timesString;
	}

	~PrepareEventGraphPulse() {}
}

public class PrepareEventGraphReactionTime {
	//sql data of previous rts to plot graph and show stats at bottom
	public string [] rtsAtSQL;
	public double personMAXAtSQL;
	public double sessionMAXAtSQL;
	public double personMINAtSQL;
	public double sessionMINAtSQL;
	public double personAVGAtSQL;
	public double sessionAVGAtSQL;

	public double time;

	public PrepareEventGraphReactionTime() {
	}

	public PrepareEventGraphReactionTime(double time, int sessionID, int personID, string table, string type) 
	{
		Sqlite.Open();

		//obtain data
		rtsAtSQL = SqliteReactionTime.SelectReactionTimes(true, sessionID, personID, type,
				Sqlite.Orders_by.ID_DESC, 10); //select only last 10
		
		personMAXAtSQL = SqliteSession.SelectMAXEventsOfAType(
				true, sessionID, personID, table, type, "time");
		sessionMAXAtSQL = SqliteSession.SelectMAXEventsOfAType(
				true, sessionID, -1, table, type, "time");
		
		personMINAtSQL = SqliteSession.SelectMINEventsOfAType(
				true, sessionID, personID, table, type, "time");
		sessionMINAtSQL = SqliteSession.SelectMINEventsOfAType(
				true, sessionID, -1, table, type, "time");

		personAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(
				true, sessionID, personID, table, type, "time");
		sessionAVGAtSQL = SqliteSession.SelectAVGEventsOfAType(
				true, sessionID, -1, table, type, "time");
		
		Sqlite.Close();
	
		this.time = time;
	}

	~PrepareEventGraphReactionTime() {}
}

public class PrepareEventGraphMultiChronopic {
	//public double timestamp;
	public string cp1InStr;
	public string cp1OutStr;
	public string cp2InStr;
	public string cp2OutStr;
	public string cp3InStr;
	public string cp3OutStr;
	public string cp4InStr;
	public string cp4OutStr;
	public bool cp1StartedIn;
	public bool cp2StartedIn;
	public bool cp3StartedIn;
	public bool cp4StartedIn;

	public PrepareEventGraphMultiChronopic() {
	}

	public PrepareEventGraphMultiChronopic(
			//double timestamp, 
			bool cp1StartedIn, bool cp2StartedIn, bool cp3StartedIn, bool cp4StartedIn,
			string cp1InStr, string cp1OutStr, string cp2InStr, string cp2OutStr, 
			string cp3InStr, string cp3OutStr, string cp4InStr, string cp4OutStr) {
		//this.timestamp = timestamp;
		this.cp1StartedIn = cp1StartedIn; 
		this.cp2StartedIn = cp2StartedIn; 
		this.cp3StartedIn = cp3StartedIn; 
		this.cp4StartedIn = cp4StartedIn;
		this.cp1InStr = cp1InStr;
		this.cp1OutStr = cp1OutStr;
		this.cp2InStr = cp2InStr;
		this.cp2OutStr = cp2OutStr;
		this.cp3InStr = cp3InStr;
		this.cp3OutStr = cp3OutStr;
		this.cp4InStr = cp4InStr;
		this.cp4OutStr = cp4OutStr;
	}

	~PrepareEventGraphMultiChronopic() {}
}

public class UpdateProgressBar {
	public bool IsEvent;
	public bool PercentageMode;
	public double ValueToShow;

	public UpdateProgressBar() {
	}

	public UpdateProgressBar(bool isEvent, bool percentageMode, double valueToShow) {
		this.IsEvent = isEvent;
		this.PercentageMode = percentageMode;
		this.ValueToShow = valueToShow;
	}

	~UpdateProgressBar() {}
}

//start window buttons
public class MovingStartButton
{
	public bool Moving;

	private double pos;
	private double speed;
	private int end;
	public enum Dirs { R, L }
	private Dirs dir;


	public MovingStartButton(int start, int end, Dirs dir)
	{
		pos = start;
		this.end = end;
		this.dir = dir;
		Moving = true;
	}
	
	public bool Next()
	{
		if(dir == Dirs.R) {
			if( pos >= end )
				Moving = false;
			else {
				speed = Math.Ceiling(Math.Abs(end-pos)/25.0);
				pos += speed;
			}
		} else {
			if( pos <= end )
				Moving = false;
			else {
				speed = Math.Ceiling(Math.Abs(end-pos)/25.0);
				pos -= speed;
			}
		}

		//LogB.Information("pos: " + pos + "; speed: " + speed);
		return true;
	}

	public int Pos {
		get { return Convert.ToInt32(pos); }
	}
	public int Speed {
		get { return Convert.ToInt32(speed); }
	}
}

//for animate last jump, run, pulse on a bar graph
public class MovingBar 
{
	public int X;
	public int Y; 
	/*
	 * y will go from (y + alto -speed) to (ytop)
	 * eg a vertical bar that goes from the bottom to the top, will go from:
	 * YTop 37, Y 380
	 * to
	 * YTop 37, Y 37
	 * so y will be decreasing
	 */
	
	public int Width;

	public int YTop; //stored y value
	public int AltoTop; //stored alto value
	
	public Gdk.GC Pen_bar_bg;
	public bool Simulated;
	public double Result;
	public int Step;
	public Pango.Layout Layout;
	
	public MovingBar(int x, int y, int width, int yTop, int altoTop, 
			Gdk.GC pen_bar_bg, bool simulated, double result, Pango.Layout layout) {
		this.X = x;
		this.Y = y;
		this.Width = width;
		this.YTop = yTop;
		this.AltoTop = altoTop;
		this.Pen_bar_bg = pen_bar_bg;
		this.Simulated = simulated;
		this.Result = result;
		this.Layout = layout;
	} 
	
	public void Next() {
		Step = Convert.ToInt32(Math.Ceiling((Y-YTop)/100.0));
		Y = Y - Step;
	}

	~MovingBar() {}
}

//to store the xStart and xEnd of every encoder or forceSensor capture reptition
//in order to be saved or not on clicking screen
//note every rep will be c or ec
public class RepetitionMouseLimits
{
	private List<PointStartEnd> list;
	private int current;

	public RepetitionMouseLimits()
	{
		list = new List<PointStartEnd>();
		current = 0;
	}

	public void Add (double start, double end)
	{
		PointStartEnd p = new PointStartEnd(current ++, start, end);
		list.Add(p);
		LogB.Information("Added: " + p.ToString());
	}

	public int FindBarInPixel (double pixel)
	{
		foreach(PointStartEnd p in list)
			if(pixel >= p.Start && pixel <= p.End)
				return p.Id;

		return -1;
	}

	public double GetStartOfARep(int rep)
	{
		return ((PointStartEnd) list[rep]).Start;
	}
	public double GetEndOfARep(int rep)
	{
		return ((PointStartEnd) list[rep]).End;
	}
}
