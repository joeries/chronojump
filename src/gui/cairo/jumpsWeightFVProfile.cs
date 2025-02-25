
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
 *  Copyright (C) 2004-2020   Xavier de Blas <xaviblas@gmail.com> 
 *  Copyright (C) 2004-2020   Jordi Rodeiro <jordirodeiro@gmail.com> 
 */

using System;
using System.Collections.Generic; //List
using Gtk;
using Cairo;


public class JumpsWeightFVProfileGraph : CairoXY
{
	public enum ErrorAtStart { NEEDLEGPARAMS, BADLEGPARAMS, NEEDJUMPS }

	//constructor when there are no points
	public JumpsWeightFVProfileGraph (DrawingArea area, ErrorAtStart error)//, string title, string jumpType, string date)
	{
		this.area = area;

		initGraph();

		string message = "";
		if(error == ErrorAtStart.NEEDLEGPARAMS)
			message = "Need to fill person's leg parameters.";
		else if(error == ErrorAtStart.BADLEGPARAMS)
			message = "Person's leg parameters are incorrect.";
		else //if(error == ErrorAtStart.NEEDJUMPS)
			message = "Need to execute jumps SJl and/or SJ.";

		g.SetFontSize(16);
		printText(area.Allocation.Width /2, area.Allocation.Height /2, 24, textHeight, message, g, true);

		endGraph();
	}

	//regular constructor
	public JumpsWeightFVProfileGraph (
			List<PointF> point_l, double slope, double intercept,
			DrawingArea area, string title, //string jumpType,
			string date)
	{
		this.point_l = point_l;
		this.slope = slope;
		this.intercept = intercept;
		this.area = area;
		this.title = title;
		//this.jumpType = jumpType;
		this.date = date;

		outerMargins = 50; //blank space outside the axis

		xVariable = "Speed";
		yVariable = "Force";
		xUnits = "m/s";
		yUnits = "N";
	}

	public override void Do()
	{
		LogB.Information("at JumpsWeightFVProfileGraph.Do");
		initGraph();

		findPointMaximums();
		//findAbsoluteMaximums();
		paintAxisAndGrid(gridTypes.BOTH);

		plotPredictedLine(predictedLineTypes.STRAIGHT);
		plotRealPoints();

		writeTitle();

		endGraph();
	}

	protected override void writeTitle()
	{
		writeTextAtRight(-5, title, true);
		writeTextAtRight(-4, "FV Profile", false);
		//writeTextAtRight(-3, "Jumptype: " + jumpType, false);
		writeTextAtRight(-2, date, false);
	}
}
