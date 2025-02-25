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
 * Copyright (C) 2017-2020   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using Gtk;
using Glade;
using System.Collections.Generic; //List<T>


public partial class ChronoJumpWindow
{
	[Widget] Gtk.TextView textview_encoder_analyze_triggers;
	TriggerList triggerListEncoder;

	// start of encoder ------------->

	private void showEncoderAnalyzeTriggersAndTab()
	{
		triggerListEncoder.Print();
		if(triggerListEncoder.Count() > 0)
		{
			//fill textview
			TextBuffer tb1 = new TextBuffer (new TextTagTable());
			tb1.Text = triggerListEncoder.ToString();
			textview_encoder_analyze_triggers.Buffer = tb1;
		}

		if(radio_encoder_analyze_individual_current_set.Active && triggerListEncoder.Count() > 0)
			showEncoderAnalyzeTriggerTab(true);
		else
			showEncoderAnalyzeTriggerTab(false);
	}
	
	private void showEncoderAnalyzeTriggerTab(bool show)
	{
		if(show)
			notebook_analyze_results.GetNthPage(2).Show();
		else
			notebook_analyze_results.GetNthPage(2).Hide();
	}

	// <--------------- end of encoder
	// start of race analyzer ------------->

	[Widget] Gtk.TextView textview_run_encoder_triggers;
	TriggerList triggerListRunEncoder;

	private void showRaceAnalyzerTriggers()
	{
		triggerListRunEncoder.Print();
		if(triggerListRunEncoder.Count() > 0)
		{
			//fill textview
			TextBuffer tb1 = new TextBuffer (new TextTagTable());
			tb1.Text = triggerListRunEncoder.ToString();
			textview_run_encoder_triggers.Buffer = tb1;
		} else {
			TextBuffer tb1 = new TextBuffer (new TextTagTable());
			tb1.Text = "";
			textview_run_encoder_triggers.Buffer = tb1;
		}
	}
}
