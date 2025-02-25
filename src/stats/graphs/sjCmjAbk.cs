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
 * Copyright (C) 2004-2017   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Data;
using Gtk;
using System.Collections; //ArrayList
using System.Drawing;
using System.Drawing.Imaging;
using Mono.Unix;


public class GraphSjCmjAbk : StatSjCmjAbk
{
	protected string operation;

	//for simplesession
	GraphSerie serieHeight;
	GraphSerie serieTv;
	
	public GraphSjCmjAbk (StatTypeStruct myStatTypeStruct)
	{
		completeConstruction (myStatTypeStruct, treeview);
		
		this.dataColumns = 2; //for Simplesession
		
		if (statsJumpsType == 2) {
			this.operation = "MAX";
		} else {
			this.operation = "AVG";
		}

		CurrentGraphData.WindowTitle = Catalog.GetString("Chronojump graph");
		//title is shown on the graph except it's a report, then title will be on the html
		if(myStatTypeStruct.ToReport) {
			CurrentGraphData.GraphTitle = "";
		} else {
			//CurrentGraphData.GraphTitle = this.ToString();
			CurrentGraphData.GraphTitle = "";
		}
		
		if(sessions.Count == 1) {
			//four series, the four columns
			serieTv = new GraphSerie();
			serieHeight = new GraphSerie();
				
			serieTv.Title = translateYesNo("TF");
			serieHeight.Title = translateYesNo("Height");
			
			serieTv.IsLeftAxis = true;
			serieHeight.IsLeftAxis = false;

			CurrentGraphData.LabelLeft = translateYesNo("TF") + "(s)";
			CurrentGraphData.LabelRight = translateYesNo("Height") + "(cm)";
		} else {
			for(int i=0; i < sessions.Count ; i++) {
				string [] stringFullResults = sessions[i].ToString().Split(new char[] {':'});
				CurrentGraphData.XAxisNames.Add(stringFullResults[1].ToString());
			}
			if(heightPreferred) {
				CurrentGraphData.LabelLeft = translateYesNo("Height") + "(cm)";
			} else {
				CurrentGraphData.LabelLeft = translateYesNo("TF") + "(s)";
			}
			CurrentGraphData.LabelRight = "";
		}

		//CurrentGraphData.GraphType = myStatTypeStruct.GraphType;
		//CurrentGraphData.GraphPalette = myStatTypeStruct.GraphPalette;
		//CurrentGraphData.GraphTransposed = myStatTypeStruct.GraphTransposed;
	}

	protected override void printData (string [] statValues) 
	{
		//values are recorded for calculating later AVG and SD
		recordStatValues(statValues);

		if(sessions.Count == 1) {
			int i=0;
			bool foundAVG = false;
			//we need to save this transposed
			foreach (string myValue in statValues) {
				if(i == 0) {
					//don't plot AVG and SD rows
					if( myValue == Catalog.GetString("AVG")) 
						foundAVG = true;
					else
						CurrentGraphData.XAxisNames.Add(myValue);
				} else if(i == 1) {
					if(foundAVG)
						serieHeight.Avg = Convert.ToDouble(myValue);
					else
						serieHeight.SerieData.Add(myValue);
				} else if(i == 2) {
					if(foundAVG)
						serieTv.Avg = Convert.ToDouble(myValue);
					else
						serieTv.SerieData.Add(myValue);
				}

				if(foundAVG && i == dataColumns) {
					//add created series to GraphSeries ArrayList
					//check don't do it two times
					if(GraphSeries.Count == 0) {
						GraphSeries.Add(serieTv);
						GraphSeries.Add(serieHeight);
					}
					return;
				}

				i++;
			}
		} else {
			GraphSerie mySerie = new GraphSerie();
			mySerie.IsLeftAxis = true;

			int i=0;
			foreach (string myValue in statValues) {
				if( myValue == Catalog.GetString("SD") ) 
					return;

				if(i == 0) 
					mySerie.Title = myValue;
				else if( i == sessions.Count + 1 ) { //eg, for 2 sessions: [(0)person name, (1)sess1, (2)sess2, (3)AVG]
					if(myValue != "-")
						mySerie.Avg = Convert.ToDouble(myValue);
				} else 
					mySerie.SerieData.Add(myValue);
				
				i++;
			}
			GraphSeries.Add(mySerie);
		}
	}
}
