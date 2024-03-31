#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

public enum ToolSelector
{
	TimeRegion,
	PriceRegion,
	FixedRectangle,
	DynamicRectangle,
	OpeningRange  // 05-22-18
}


//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
#region Notes
	/*
			This indicator features:
			1) Time regions, Price regions, fixed rectAngles, dynamic rectangles, horizontal lines, vertical lines
			2) Vertical lines can be created by using same start/end time (with show outline checked), horizontal using same price (with show outline checked)
			3) Maximum of 10 events, each can be any of the 6 types
			4) User Interface expands and collapses beased on number of events entered (Min 1, max 10) Defaults to 1 event
			5) Each event can be repeated daily or any selectable Day(s) of the week.
			6) Each event independently configurable (time/price, day, color, opacity, ouline or not)
			7) Dynamic rectangles fix to the high and low of the range over the begin/end time
			8) Fixed rectangles are set to the price input by user over the begin/end time
			9) Area outline can be turned on/off individually.
	
			Example uses:
				A) Use 3 events to create 3 colors sessions for 24 hour trading
				B) Use 1 event to mark either the opening, opening range or entire session
				c) Use events to note significant weekly events, examples Oil report, Natural gas report
				d) create time based trade or no trade zones
				e) Create up to 10 price zones
				f) create up to 10 rectangles of any size/length
				g) Create 1 or more dynamic rectangles to show price high/low over time period, such as opening range, morning session, afternoon session, evening session.
	
			Limitations:
				1) Do not select a specific day for an event then set the time to cross midnight- as indicator will limit time up to midnight
				Note to perform this function use two days and two time spans, example Monday 11:00 pm to Tuesday 3:00 am, set 1 event timespan 23:00 to 23:59:59 on Monday,
				set the second event from 00:00:00 to 03:00:00 on Tuesday.
				Note: Timespan crosssing midnight will work if done daily
				
			09/10/15 CD V1.0
	
			09/13/15 V2.0 - Code rewrite, put all elements into arrays and reduced the OBU code lines by factor of 10 to 1 at the expense of array processing in State.Configure.
	
			05/12/16 V2.1 - Adjusted for code breaking change in Beta 11 - Bool autoscale added to Draw.RegionHighlightY() 
	
			06/18/2018 V2.2 - Added Opening range tool, requires start/stop time and will draw dynamic rectangle and extend high/low lines to end of session
	
	*/
#endregion
	
	public class RepeaterV2 : Indicator, ICustomTypeDescriptor
	{
		
		#region variables
		
		private SessionIterator MySessionIterator;
		
		private int		numEvents	= 	1;			// Number of seperate events to create, minimum 1, maximum 10

		private ToolSelector	toolE1Type	= ToolSelector.TimeRegion;		// Default tool is time regionHighlight
		private ToolSelector	toolE2Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE3Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE4Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE5Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE6Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE7Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE8Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE9Type	= ToolSelector.TimeRegion;
		private ToolSelector	toolE10Type	= ToolSelector.TimeRegion;
			
		private DayOfWeek dayOfWeek;					// Need to check what day of the week
		
		private bool		intraDay		= true;		// Flag for intraday data, true means intra day data.		
		private bool		drawE1once		= true;		// Used as flag when drawing price regions
		private bool		drawE2once		= true;
		private bool		drawE3once		= true;
		private bool		drawE4once		= true;
		private bool		drawE5once		= true;
		private bool		drawE6once		= true;
		private bool		drawE7once		= true;
		private bool		drawE8once		= true;
		private bool		drawE9once		= true;
		private bool		drawE10once		= true;	
		
		private bool		showE1outline	= true;		// Turns on/off the region outline
		private bool		showE2outline	= true;
		private bool		showE3outline	= true;
		private bool		showE4outline	= true;
		private bool		showE5outline	= true;
		private bool		showE6outline	= true;
		private bool		showE7outline	= true;
		private bool		showE8outline	= true;
		private bool		showE9outline	= true;
		private bool		showE10outline	= true;
		
		private bool		dailyE1			= true;		// By default all events will be daily (IE: M - F)
		private bool		dailyE2			= true;
		private bool		dailyE3			= true;
		private bool		dailyE4			= true;
		private bool		dailyE5			= true;
		private bool		dailyE6			= true;
		private bool		dailyE7			= true;
		private bool		dailyE8			= true;
		private bool		dailyE9			= true;
		private bool		dailyE10		= true;
		
		private bool		mondayE1		= false;	//	By default specific days not selected
		private bool		mondayE2		= false;
		private bool		mondayE3		= false;
		private bool		mondayE4		= false;
		private bool		mondayE5		= false;
		private bool		mondayE6		= false;
		private bool		mondayE7		= false;
		private bool		mondayE8		= false;
		private bool		mondayE9		= false;
		private bool		mondayE10		= false;
			
		private bool		tuesdayE1		= false;	//	By default specific days not selected
		private bool		tuesdayE2		= false;
		private bool		tuesdayE3		= false;
		private bool		tuesdayE4		= false;
		private bool		tuesdayE5		= false;
		private bool		tuesdayE6		= false;
		private bool		tuesdayE7		= false;
		private bool		tuesdayE8		= false;
		private bool		tuesdayE9		= false;
		private bool		tuesdayE10		= false;
				
		private bool		wednesdayE1		= false;	//	By default specific days not selected
		private bool		wednesdayE2		= false;
		private bool		wednesdayE3		= false;
		private bool		wednesdayE4		= false;
		private bool		wednesdayE5		= false;
		private bool		wednesdayE6		= false;
		private bool		wednesdayE7		= false;
		private bool		wednesdayE8		= false;
		private bool		wednesdayE9		= false;
		private bool		wednesdayE10	= false;

		private bool		thursdayE1		= false;	//	By default specific days not selected
		private bool		thursdayE2		= false;
		private bool		thursdayE3		= false;
		private bool		thursdayE4		= false;
		private bool		thursdayE5		= false;
		private bool		thursdayE6		= false;
		private bool		thursdayE7		= false;
		private bool		thursdayE8		= false;
		private bool		thursdayE9		= false;
		private bool		thursdayE10		= false;
		
		private bool		fridayE1		= false;	//	By default specific days not selected
		private bool		fridayE2		= false;
		private bool		fridayE3		= false;
		private bool		fridayE4		= false;
		private bool		fridayE5		= false;
		private bool		fridayE6		= false;
		private bool		fridayE7		= false;
		private bool		fridayE8		= false;
		private bool		fridayE9		= false;
		private bool		fridayE10		= false;
				
		private TimeSpan	tsE1start	= new TimeSpan(00, 00, 00) ;	// time spans used for event start times
		private TimeSpan	tsE2start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE3start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE4start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE5start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE6start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE7start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE8start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE9start	= new TimeSpan(00, 00, 00) ;
		private TimeSpan	tsE10start	= new TimeSpan(00, 00, 00) ;
		
		private TimeSpan	tsE1end		= new TimeSpan(00, 00, 00) ;	// time spans used for event stop times		
		private TimeSpan	tsE2end		= new TimeSpan(00, 00, 00) ;			
		private TimeSpan	tsE3end		= new TimeSpan(00, 00, 00) ;			
		private TimeSpan	tsE4end		= new TimeSpan(00, 00, 00) ;		
		private TimeSpan	tsE5end		= new TimeSpan(00, 00, 00) ;				
		private TimeSpan	tsE6end		= new TimeSpan(00, 00, 00) ;				
		private TimeSpan	tsE7end		= new TimeSpan(00, 00, 00) ;				
		private TimeSpan	tsE8end		= new TimeSpan(00, 00, 00) ;				
		private TimeSpan	tsE9end		= new TimeSpan(00, 00, 00) ;				
		private TimeSpan	tsE10end	= new TimeSpan(00, 00, 00) ;				
		
		private TimeSpan	tsOfBar		= new TimeSpan(00, 00, 00) ;	// time span for bars
		private TimeSpan	tsMidnight	= new TimeSpan(23, 59, 59) ;	// time span to check for midnight
		
		private double		e1StartPrice 	= 0.0;						// Event start price (Y) (used for price regions and rectangles)
		private double		e2StartPrice 	= 0.0;
		private double		e3StartPrice 	= 0.0;
		private double		e4StartPrice 	= 0.0;
		private double		e5StartPrice 	= 0.0;
		private double		e6StartPrice 	= 0.0;
		private double		e7StartPrice 	= 0.0;
		private double		e8StartPrice 	= 0.0;
		private double		e9StartPrice 	= 0.0;
		private double		e10StartPrice 	= 0.0;
		
		private double		e1EndPrice		= 0.0;						// Event end price (Y) (used for price regions and rectangles)	
		private double		e2EndPrice		= 0.0;		
		private double		e3EndPrice		= 0.0;		
		private double		e4EndPrice		= 0.0;		
		private double		e5EndPrice		= 0.0;
		private double		e6EndPrice		= 0.0;		
		private double		e7EndPrice		= 0.0;		
		private double		e8EndPrice		= 0.0;		
		private double		e9EndPrice		= 0.0;		
		private double		e10EndPrice		= 0.0;			
				
		private Brush 		e1Color 	= Brushes.Gold;					// Event default colors
		private Brush 		e2Color 	= Brushes.Green;
		private Brush 		e3Color 	= Brushes.Red;
		private Brush 		e4Color 	= Brushes.Blue;
		private Brush 		e5Color 	= Brushes.Orange;
		private Brush 		e6Color 	= Brushes.Purple;
		private Brush 		e7Color 	= Brushes.Aqua;
		private Brush 		e8Color 	= Brushes.Yellow;
		private Brush 		e9Color 	= Brushes.DarkGreen;
		private Brush 		e10Color 	= Brushes.Maroon;
		
		private int			e1Opacity	= 10; 							// Event default opacity	
		private int			e2Opacity	= 10; 	
		private int			e3Opacity	= 10; 				
		private int			e4Opacity	= 10; 		
		private int			e5Opacity	= 10; 		
		private int			e6Opacity	= 10; 		
		private int			e7Opacity	= 10; 				
		private int			e8Opacity	= 10;		
		private int			e9Opacity	= 10; 			
		private int			e10Opacity	= 10; 	
		
		private int 	[] 	beginBar;									// 	Array to hold the beginning bar of event
		private bool 	[]  firstTime;									//	Array to hold bool for first time in an event
		private string 	[] 	label;										//	Array to Hold the dynamic label of each event
		private bool 	[,]	days;										//	Array to hold all bools for daily and M-f for 10 events				
		private bool 	[]	drawOnce;									//	Array to hold all the bools for drawing the price regions just once
		private bool 	[] 	showOutline;								//	Array to hold all the bools for turning on/off the object outline
		private Brush 	[] 	brushColor;									//	Array to hold all the event colors
		private int 	[] 	opacity;									//	Array to hold opacity setting for each event
		private double 	[,] startStopPrice;								//	Array to hold start and end price
		private TimeSpan	 [,] startStop;								//	Array to hold start time and end times
		private ToolSelector [] toolType;								//	Array to hold type of tool to use in each event.		
		
#endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Creates repeatable time/price zones or lines.";
				Name						= "RepeaterV2.2";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= true;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
				NumberOfEvents				= 1;
			}
			
#region State-Configure
			
			else if (State == State.Configure)
			{
				beginBar 		= new int 	[11] ;
				firstTime		= new bool 	[11] {true, true, true, true, true, true, true, true, true, true, true} ;  // only using 1 - 10 to keep it clear! 
				label			= new string[11] ;
				drawOnce		= new bool 	[11] {true, true, true, true, true, true, true, true, true, true, true} ;
				showOutline		= new bool 	[11] ;
				brushColor		= new Brush [11] ;
				opacity			= new int 	[11] ;				
				startStopPrice	= new double[11,3] ; 
				days			= new bool 	[11,7] ;				
				startStop		= new TimeSpan 	[11,3] ;
				toolType		= new ToolSelector [11] ;				

				if (DailyE1)
				{
					mondayE1 	= false;		// cycle through and reset if Daily now selected
					tuesdayE1 	= false;
					wednesdayE1	= false;
					thursdayE1 	= false;
					fridayE1	= false;
				}
				else
				{
					if (tsE1start > tsE1end)	// if !daily and time start > time to end then must set span to midnight
					{
						tsE1end = tsMidnight; 	// Since not daily then force to midnight
					}
				}
				
				if (dailyE2 && numEvents >1)
				{
					mondayE2 	= false;
					tuesdayE2 	= false;
					wednesdayE2	= false;
					thursdayE2 	= false;
					fridayE2	= false;
				}
				else
				{
					if (tsE2start > tsE2end)
					{
						tsE2end = tsMidnight;
					}
				}
				
				if (dailyE3 && numEvents >2)
				{
					mondayE3 	= false;
					tuesdayE3 	= false;
					wednesdayE3	= false;
					thursdayE3 	= false;
					fridayE3	= false;
				}	
				else
				{
					if (tsE3start > tsE3end)
					{
						tsE3end = tsMidnight;
					}
				}
				
				if (dailyE4 && numEvents >3)
				{
					mondayE4 	= false;
					tuesdayE4 	= false;
					wednesdayE4	= false;
					thursdayE4 	= false;
					fridayE4	= false;
				}	
				else
				{
					if (tsE4start > tsE4end)
					{
						tsE4end = tsMidnight;
					}
				}
				
				if (dailyE5 && numEvents >4)
				{
					mondayE5 	= false;
					tuesdayE5 	= false;
					wednesdayE5	= false;
					thursdayE5 	= false;
					fridayE5	= false;
				}
				else
				{
					if (tsE5start > tsE5end)
					{
						tsE5end = tsMidnight;
					}
				}	
				
				if (dailyE6 && numEvents >5)
				{
					mondayE6 	= false;
					tuesdayE6 	= false;
					wednesdayE6	= false;
					thursdayE6 	= false;
					fridayE6	= false;					
				}
				else
				{
					if (tsE6start > tsE6end)
					{
						tsE6end = tsMidnight;
					}
				}
				
				if (dailyE7 && numEvents >6)
				{
					mondayE7 	= false;
					tuesdayE7 	= false;
					wednesdayE7	= false;
					thursdayE7 	= false;
					fridayE7	= false;				
				}
				else
				{
					if (tsE7start > tsE7end)
					{
						tsE7end = tsMidnight;
					}
				}
				
				if (dailyE8 && numEvents >7)
				{
					mondayE8 	= false;
					tuesdayE8 	= false;
					wednesdayE8	= false;
					thursdayE8 	= false;
					fridayE8	= false;
				}
				else
				{
					if (tsE8start > tsE8end)
					{
						tsE8end = tsMidnight;
					}
				}
				
				if (dailyE9 && numEvents >8)
				{
					mondayE9 	= false;
					tuesdayE9 	= false;
					wednesdayE9	= false;
					thursdayE9 	= false;
					fridayE9	= false;
				}
				else
				{
					if (tsE9start > tsE9end)
					{
						tsE9end = tsMidnight;
					}
				}
				if (dailyE10 && numEvents >9)
				{
					mondayE10 	= false;
					tuesdayE10	= false;
					wednesdayE10= false;
					thursdayE10	= false;
					fridayE10	= false;
				}
				else
				{
					if (tsE10start > tsE10end)
					{
						tsE10end = tsMidnight;
					}					
				}
				
				days[1,1] = dailyE1;				// Fit all the daily and day bools into array for processing in OBU
				days[1,2] = mondayE1;
				days[1,3] = tuesdayE1;
				days[1,4] = wednesdayE1;
				days[1,5] = thursdayE1;
				days[1,6] = fridayE1;
				days[2,1] = dailyE2;
				days[2,2] = mondayE2;
				days[2,3] = tuesdayE2;
				days[2,4] = wednesdayE2;
				days[2,5] = thursdayE2;
				days[2,6] = fridayE2;
				days[3,1] = dailyE3;
				days[3,2] = mondayE3;
				days[3,3] = tuesdayE3;
				days[3,4] = wednesdayE3;
				days[3,5] = thursdayE3;
				days[3,6] = fridayE3;				
				days[4,1] = dailyE4;
				days[4,2] = mondayE4;
				days[4,3] = tuesdayE4;
				days[4,4] = wednesdayE4;
				days[4,5] = thursdayE4;
				days[4,6] = fridayE4;				
				days[5,1] = dailyE5;
				days[5,2] = mondayE5;
				days[5,3] = tuesdayE5;
				days[5,4] = wednesdayE5;
				days[5,5] = thursdayE5;
				days[5,6] = fridayE5;
				days[6,1] = dailyE6;
				days[6,2] = mondayE6;
				days[6,3] = tuesdayE6;
				days[6,4] = wednesdayE6;
				days[6,5] = thursdayE6;
				days[6,6] = fridayE6;	
				days[7,1] = dailyE7;
				days[7,2] = mondayE7;
				days[7,3] = tuesdayE7;
				days[7,4] = wednesdayE7;
				days[7,5] = thursdayE7;
				days[7,6] = fridayE7;	
				days[8,1] = dailyE8;
				days[8,2] = mondayE8;
				days[8,3] = tuesdayE8;
				days[8,4] = wednesdayE8;
				days[8,5] = thursdayE8;
				days[8,6] = fridayE8;
				days[9,1] = dailyE9;
				days[9,2] = mondayE9;
				days[9,3] = tuesdayE9;
				days[9,4] = wednesdayE9;
				days[9,5] = thursdayE9;
				days[9,6] = fridayE9;
				days[10,1] = dailyE10;
				days[10,2] = mondayE10;
				days[10,3] = tuesdayE10;
				days[10,4] = wednesdayE10;
				days[10,5] = thursdayE10;
				days[10,6] = fridayE10;	
				
				toolType[1] = toolE1Type;			// Fit all the tool types into array for OBU processing
				toolType[2] = toolE2Type;
				toolType[3] = toolE3Type;
				toolType[4] = toolE4Type;
				toolType[5] = toolE5Type;
				toolType[6] = toolE6Type;
				toolType[7] = toolE7Type;
				toolType[8] = toolE8Type;
				toolType[9] = toolE9Type;
				toolType[10] = toolE10Type;				
				
				showOutline[1] = showE1outline;		// Fit all the outline bools into array for OBU processing
				showOutline[2] = showE2outline;
				showOutline[3] = showE3outline;
				showOutline[4] = showE4outline;
				showOutline[5] = showE5outline;
				showOutline[6] = showE6outline;
				showOutline[7] = showE7outline;
				showOutline[8] = showE8outline;
				showOutline[9] = showE9outline;
				showOutline[10] = showE10outline;
				
				startStop[1,1] = tsE1start;			// fit the start and end timespans into array for OBU processing
				startStop[2,1] = tsE2start;
				startStop[3,1] = tsE3start;
				startStop[4,1] = tsE4start;
				startStop[5,1] = tsE5start;
				startStop[6,1] = tsE6start;
				startStop[7,1] = tsE7start;
				startStop[8,1] = tsE8start;
				startStop[9,1] = tsE9start;
				startStop[10,1] = tsE10start;
				startStop[1,2] = tsE1end;
				startStop[2,2] = tsE2end;
				startStop[3,2] = tsE3end;
				startStop[4,2] = tsE4end;
				startStop[5,2] = tsE5end;
				startStop[6,2] = tsE6end;
				startStop[7,2] = tsE7end;
				startStop[8,2] = tsE8end;
				startStop[9,2] = tsE9end;
				startStop[10,2] = tsE10end;				
				
				brushColor[1] = e1Color;			//	Fit colors into array for OBU processing
				brushColor[2] = e2Color;
				brushColor[3] = e3Color;
				brushColor[4] = e4Color;
				brushColor[5] = e5Color;
				brushColor[6] = e6Color;
				brushColor[7] = e7Color;
				brushColor[8] = e8Color;
				brushColor[9] = e9Color;
				brushColor[10] = e10Color;
				
				startStopPrice[1,1] = E1StartPrice;	// Fit start and stop prices for OBU processing
				startStopPrice[2,1] = E2StartPrice;
				startStopPrice[3,1] = E3StartPrice;
				startStopPrice[4,1] = E4StartPrice;
				startStopPrice[5,1] = E5StartPrice;
				startStopPrice[6,1] = E6StartPrice;
				startStopPrice[7,1] = E7StartPrice;
				startStopPrice[8,1] = E8StartPrice;
				startStopPrice[9,1] = E9StartPrice;
				startStopPrice[10,1] = E10StartPrice;
				startStopPrice[1,2] = E1EndPrice;
				startStopPrice[2,2] = E2EndPrice;
				startStopPrice[3,2] = E3EndPrice;
				startStopPrice[4,2] = E4EndPrice;
				startStopPrice[5,2] = E5EndPrice;
				startStopPrice[6,2] = E6EndPrice;
				startStopPrice[7,2] = E7EndPrice;
				startStopPrice[8,2] = E8EndPrice;
				startStopPrice[9,2] = E9EndPrice;
				startStopPrice[10,2] = E10EndPrice;
			
				opacity[1] = E1Opacity;				// Fit opacity into array for OBU processing
				opacity[2] = E2Opacity;
				opacity[3] = E3Opacity;
				opacity[4] = E4Opacity;
				opacity[5] = E5Opacity;
				opacity[6] = E6Opacity;
				opacity[7] = E7Opacity;
				opacity[8] = E8Opacity;
				opacity[9] = E9Opacity;
				opacity[10] = E10Opacity;
					
			}
			else if (State == State.Historical)
			{
				MySessionIterator = new SessionIterator(Bars);
			}
			
#endregion			
		}
		
		public override string DisplayName
		{
    		get { return "RepeaterV2.2"; } 				// to prevent showing all parameters (60+) on the chart // 05-22-18
		}

			
		protected override void OnBarUpdate()
		{
			if (!intraDay) return;  				// Only process on intraday intervals
			
			if (Bars.IsFirstBarOfSession)
			{
				MySessionIterator.GetNextSession(Time[0], true);
			}
			
			if (CurrentBar < 1) 
			{
				if (!Bars.BarsType.IsIntraday)
				{
					Draw.TextFixed (this,"text","RepeaterV2.2 only works on intra-day intervals", TextPosition.BottomRight); // 05-22-18
					intraDay = false;
				}
				return;
			}
				
			tsOfBar 	= Time[0].TimeOfDay;	// Get a timeSpan to check the time of the current bar		
			dayOfWeek 	= Time[0].DayOfWeek;	// Get the day of the week in case of individual day usage

			for (int i = 1; i <= numEvents; i++)
			{
				if (toolType[i] == ToolSelector.PriceRegion && drawOnce[i])  // If using price region tool we don't care about the time elements, so just draw.
				{
					Draw.RegionHighlightY (this, "PRevent: "+ i + " " + CurrentBar, false, startStopPrice[i,1], startStopPrice[i,2] , showOutline[i] ? brushColor[i] : Brushes.Transparent, brushColor[i], opacity[i]); //nt8b11 -autoscale
					drawOnce[i] = false; 	
				}
			
				if (toolType[i] == ToolSelector.TimeRegion || toolType[i] == ToolSelector.FixedRectangle || toolType[i] == ToolSelector.DynamicRectangle || toolType[i] == ToolSelector.OpeningRange) // 05-22-18
				{				
					
					if (days[i,1] || (days[i,2] && dayOfWeek == DayOfWeek.Monday) || (days[i,3] && dayOfWeek == DayOfWeek.Tuesday)
						|| (days[i,4] && dayOfWeek == DayOfWeek.Wednesday) || (days[i,5] && dayOfWeek == DayOfWeek.Thursday) 
						|| (days[i,6] && dayOfWeek == DayOfWeek.Friday))
					{
					
						if ((startStop[i,1] > startStop[i,2] && tsOfBar.CompareTo(startStop[i,1]) >= 0 && tsOfBar.CompareTo(startStop[i,2]) >=0 )  // If start>end, then both will be +1
							|| (startStop[i,1] > startStop[i,2] && tsOfBar.CompareTo(startStop[i,2]) <=0 && tsOfBar.CompareTo(startStop[i,1]) <=0) // If start>end, then for end both will be -1
							|| (tsOfBar.CompareTo(startStop[i,1]) >= 0 && tsOfBar.CompareTo(startStop[i,2]) <= 0 ))  // Otherwise this will be the normal path (start < end)
						{				
							if (firstTime[i])						// First time into the select time span set some values
							{
								label[i]	= "TRevent: "+ i +" "+CurrentBar; 	//  Create unique label on first date/time match
								
								if (toolType[i] == ToolSelector.FixedRectangle)
								{
									label[i]	= "FRevent: "+ i +" "+CurrentBar; 	//  Create unique label on first date/time match
								}
								
								if (toolType[i] == ToolSelector.DynamicRectangle || toolType[i] == ToolSelector.OpeningRange) // 05-22-18 
								{
									startStopPrice[i,1] = High[0];			// collect starting values if dynamic
									startStopPrice[i,2] = Low[0];			// collect starting values if dynamic
									label[i]	 	= "DRevent: "+ i +" "+CurrentBar; 	//  Create unique label on first date/time match
								}
								
								beginBar[i]		= CurrentBar;			//	Save the current bar value for start location
								firstTime[i] 	= false;				//	Set flag false so we only get the label and starting bar once per date time match.
							}
							if (toolType[i] == ToolSelector.TimeRegion)  
							{
								DateTime test = Time[0].Date + startStop[i,2];
								
								Draw.RegionHighlightX (this, label[i], Time[CurrentBar - beginBar[i]], test, showOutline[i] ? brushColor[i] : Brushes.Transparent, brushColor[i], opacity[i]);
							}
							else 	// otherwise we must be drawing a specific rectangle
							{
								if (toolType[i] == ToolSelector.DynamicRectangle || toolType[i] == ToolSelector.OpeningRange)   		// If dynamic then grab high and low  // 05-22-18
								{
									if (Low[0] < startStopPrice[i,1]) 	startStopPrice[i,1] = Low[0];	// check for higher high
									if (High[0] > startStopPrice[i,2]) 	startStopPrice[i,2] = High[0];	// check for lower low
								}
							
								Draw.Rectangle (this, label[i], false, CurrentBar - beginBar[i], startStopPrice[i,1], 0, startStopPrice[i,2], showOutline[i] ? brushColor[i] : Brushes.Transparent, brushColor[i], opacity[i]);
								
								if (toolType[i] == ToolSelector.OpeningRange)
								{
									Draw.Line(this, label[i]+"Upper", false, Time[CurrentBar - beginBar[i]], startStopPrice[i,1], MySessionIterator.ActualSessionEnd, startStopPrice[i,1], brushColor[i], DashStyleHelper.Dot, 1);
									Draw.Line(this, label[i]+"Lower", false, Time[CurrentBar - beginBar[i]], startStopPrice[i,2], MySessionIterator.ActualSessionEnd, startStopPrice[i,2], brushColor[i], DashStyleHelper.Dot, 1);
									
								}
								
							}
						}
						else		//  Else we don't match the time requirement		
						{		
							firstTime[i] 		= true;		// Reset for next occurance of 1st event (time) object		
						}
					} 	// end if (daily...
				}	// end if (Tool...	
			} //for (i=1; i 
		} 	// 	end OnBarUpdate

		
		#region Properties
		
		[Range(1, 10)]
		[Display(Name="Number Of Events", Description="Enter number of events and click to expand to number entered", Order=1, GroupName="Event")]
		[RefreshProperties(RefreshProperties.All)]
		public int NumberOfEvents
		{ 
			get {return numEvents;}
		 	set {numEvents = value; }
		}

		//=========Event 1 ================
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event1")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE1Type
		{
			get { return toolE1Type; }
			set { toolE1Type = value; }
		}
		
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event1")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE1
        {
            get { return dailyE1; }
            set { dailyE1 = value; }
        }	
		
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event1")] 
		public bool MondayE1
        {
            get { return mondayE1; }
            set { mondayE1 = value; }
        }	
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event1")] 
		public bool TuesdayE1
        {
            get { return tuesdayE1; }
            set { tuesdayE1 = value; }
        }
		
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event1")] 
		public bool WednesdayE1
        {
            get { return wednesdayE1; }
            set { wednesdayE1 = value; }
        }
		
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event1")] 
		public bool ThursdayE1
        {
            get { return thursdayE1; }
            set { thursdayE1 = value; }
        }
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event1")] 
		public bool FridayE1
        {
            get { return fridayE1; }
            set { fridayE1 = value; }
        }	
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event1")]
		public TimeSpan TSE1start
		{
			get {return tsE1start;}
		 	set {tsE1start = value; }
		}		
		[Browsable(false)]
		public string tsE1StartSerialize
        {
            get { return tsE1start.ToString(); }
            set { tsE1start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event1")]
		public TimeSpan TSE1end
		{
			get {return tsE1end;}
		 	set {tsE1end = value; }
		}
		[Browsable(false)]
		public string TSE1endSerialize
        {
            get { return tsE1end.ToString(); }
            set { tsE1end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event1")]		
		public Brush Event1Color 
		{
			get { return e1Color; }
			set { e1Color = value;; }
		}	
		[Browsable(false)]
		public string e1ColorSerializable
		{
			get { return Serialize.BrushToString(e1Color); }
			set { e1Color = Serialize.StringToBrush(value); }
		}	
		
		[Range(0, 100)]
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event1")]
		public int E1Opacity
		{ 
			get {return e1Opacity;}
		 	set {e1Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event1")]
		public double E1StartPrice
		{ 
			get {return e1StartPrice;}
		 	set {e1StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event1")]
		public double E1EndPrice
		{ 
			get {return e1EndPrice;}
		 	set {e1EndPrice = value; }
		}	
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event1")] 
		public bool ShowE1outline
        {
            get { return showE1outline; }
            set { showE1outline = value; }
        }		
		
		//===========Event 2================	
		
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event2")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE2Type
		{
			get { return toolE2Type; }
			set { toolE2Type = value; }
		}
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event2")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE2
        {
            get { return dailyE2; }
            set { dailyE2 = value; }
        }	
		
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event2")] 
		public bool MondayE2
        {
            get { return mondayE2; }
            set { mondayE2 = value; }
        }	
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event2")] 
		public bool TuesdayE2
        {
            get { return tuesdayE2; }
            set { tuesdayE2 = value; }
        }
		
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event2")] 
		public bool WednesdayE2
        {
            get { return wednesdayE2; }
            set { wednesdayE2 = value; }
        }
		
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event2")] 
		public bool ThursdayE2
        {
            get { return thursdayE2; }
            set { thursdayE2 = value; }
        }
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event2")] 
		public bool FridayE2
        {
            get { return fridayE2; }
            set { fridayE2 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event2")]
		public TimeSpan TSE2start
		{
			get {return tsE2start;}
		 	set {tsE2start = value; }
		}
		[Browsable(false)]
		public string tsE2StartSerialize
        {
            get { return tsE2start.ToString(); }
            set { tsE2start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event2")]
		public TimeSpan TSE2end
		{
			get {return tsE2end;}
		 	set {tsE2end = value; }
		}
		[Browsable(false)]
		public string TSE2endSerialize
        {
            get { return tsE2end.ToString(); }
            set { tsE2end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event2")]		
		public Brush Event2Color 
		{
			get { return e2Color; }
			set { e2Color = value;; }
		}	
		[Browsable(false)]
		public string e2ColorSerializable
		{
			get { return Serialize.BrushToString(e2Color); }
			set { e2Color = Serialize.StringToBrush(value); }
		}
		
		[Range(0, 100)]
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event2")]
		public int E2Opacity
		{ 
			get {return e2Opacity;}
		 	set {e2Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event2")]
		public double E2StartPrice
		{ 
			get {return e2StartPrice;}
		 	set {e2StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event2")]
		public double E2EndPrice
		{ 
			get {return e2EndPrice;}
		 	set {e2EndPrice = value; }
		}
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event2")] 
		public bool ShowE2outline
        {
            get { return showE2outline; }
            set { showE2outline = value; }
        }		
		
		//===========Event 3================	
		
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event3")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE3Type
		{
			get { return toolE3Type; }
			set { toolE3Type = value; }
		}
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event3")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE3
        {
            get { return dailyE3; }
            set { dailyE3 = value; }
        }	
		
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event3")] 
		public bool MondayE3
        {
            get { return mondayE3; }
            set { mondayE3 = value; }
        }	
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event3")] 
		public bool TuesdayE3
        {
            get { return tuesdayE3; }
            set { tuesdayE3 = value; }
        }
		
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event3")] 
		public bool WednesdayE3
        {
            get { return wednesdayE3; }
            set { wednesdayE3 = value; }
        }
		
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event3")] 
		public bool ThursdayE3
        {
            get { return thursdayE3; }
            set { thursdayE3 = value; }
        }
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event3")] 
		public bool FridayE3
        {
            get { return fridayE3; }
            set { fridayE3 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event3")]
		public TimeSpan TSE3start
		{
			get {return tsE3start;}
		 	set {tsE3start = value; }
		}
		[Browsable(false)]
		public string tsE3StartSerialize
        {
            get { return tsE3start.ToString(); }
            set { tsE3start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event3")]
		public TimeSpan TSE3end
		{
			get {return tsE3end;}
		 	set {tsE3end = value; }
		}
		[Browsable(false)]
		public string TSE3endSerialize
        {
            get { return tsE3end.ToString(); }
            set { tsE3end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event3")]		
		public Brush Event3Color 
		{
			get { return e3Color; }
			set { e3Color = value;; }
		}	
		[Browsable(false)]
		public string e3ColorSerializable
		{
			get { return Serialize.BrushToString(e3Color); }
			set { e3Color = Serialize.StringToBrush(value); }
		}	
		
		[Range(0, 100)]
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event3")]
		public int E3Opacity
		{ 
			get {return e3Opacity;}
		 	set {e3Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event3")]
		public double E3StartPrice
		{ 
			get {return e3StartPrice;}
		 	set {e3StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]	
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event3")]
		public double E3EndPrice
		{ 
			get {return e3EndPrice;}
		 	set {e3EndPrice = value; }
		}	

		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event3")] 
		public bool ShowE3outline
        {
            get { return showE3outline; }
            set { showE3outline = value; }
        }		
		
		//===========Event 4================	
		
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event4")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE4Type
		{
			get { return toolE4Type; }
			set { toolE4Type = value; }
		}
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event4")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE4
        {
            get { return dailyE4; }
            set { dailyE4 = value; }
        }	
			
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event4")] 
		public bool MondayE4
        {
            get { return mondayE4; }
            set { mondayE4 = value; }
        }	
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event4")] 
		public bool TuesdayE4
        {
            get { return tuesdayE4; }
            set { tuesdayE4 = value; }
        }	
		
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event4")] 
		public bool WednesdayE4
        {
            get { return wednesdayE4; }
            set { wednesdayE4 = value; }
        }
		
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event4")] 
		public bool ThursdayE4
        {
            get { return thursdayE4; }
            set { thursdayE4 = value; }
        }	
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event4")] 
		public bool FridayE4
        {
            get { return fridayE4; }
            set { fridayE4 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event4")]
		public TimeSpan TSE4start
		{
			get {return tsE4start;}
		 	set {tsE4start = value; }
		}
		
		[Browsable(false)]
		public string tsE4StartSerialize
        {
            get { return tsE4start.ToString(); }
            set { tsE4start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event4")]
		public TimeSpan TSE4end
		{
			get {return tsE4end;}
		 	set {tsE4end = value; }
		}
		
		[Browsable(false)]
		public string TSE4endSerialize
        {
            get { return tsE4end.ToString(); }
            set { tsE4end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event4")]		
		public Brush Event4Color 
		{
			get { return e4Color; }
			set { e4Color = value;; }
		}	
		[Browsable(false)]
		public string e4ColorSerializable
		{
			get { return Serialize.BrushToString(e4Color); }
			set { e4Color = Serialize.StringToBrush(value); }
		}
		
		[Range(0, 100)]	
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event4")]
		public int E4Opacity
		{ 
			get {return e4Opacity;}
		 	set {e4Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event4")]
		public double E4StartPrice
		{ 
			get {return e4StartPrice;}
		 	set {e4StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event4")]
		public double E4EndPrice
		{ 
			get {return e4EndPrice;}
		 	set {e4EndPrice = value; }
		}	
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event4")] 
		public bool ShowE4outline
        {
            get { return showE4outline; }
            set { showE4outline = value; }
        }		
		
		//===========Event 5================		
		
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event5")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE5Type
		{
			get { return toolE5Type; }
			set { toolE5Type = value; }
		}
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event5")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE5
        {
            get { return dailyE5; }
            set { dailyE5 = value; }
        }	
		
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event5")] 
		public bool MondayE5
        {
            get { return mondayE5; }
            set { mondayE5 = value; }
        }	
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event5")] 
		public bool TuesdayE5
        {
            get { return tuesdayE5; }
            set { tuesdayE5 = value; }
        }
				
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event5")] 
		public bool WednesdayE5
        {
            get { return wednesdayE5; }
            set { wednesdayE5 = value; }
        }
		
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event5")] 
		public bool ThursdayE5
        {
            get { return thursdayE5; }
            set { thursdayE5 = value; }
        }
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event5")] 
		public bool FridayE5
        {
            get { return fridayE5; }
            set { fridayE5 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event5")]
		public TimeSpan TSE5start
		{
			get {return tsE5start;}
		 	set {tsE5start = value; }
		}
		[Browsable(false)]
		public string tsE5StartSerialize
        {
            get { return tsE5start.ToString(); }
            set { tsE5start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event5")]
		public TimeSpan TSE5end
		{
			get {return tsE5end;}
		 	set {tsE5end = value; }
		}
		[Browsable(false)]
		public string TSE5endSerialize
        {
            get { return tsE5end.ToString(); }
            set { tsE5end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event5")]		
		public Brush Event5Color 
		{
			get { return e5Color; }
			set { e5Color = value;; }
		}	
		[Browsable(false)]
		public string e5ColorSerializable
		{
			get { return Serialize.BrushToString(e5Color); }
			set { e5Color = Serialize.StringToBrush(value); }
		}
		
		[Range(0, 100)]
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event5")]
		public int E5Opacity
		{ 
			get {return e5Opacity;}
		 	set {e5Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event5")]
		public double E5StartPrice
		{ 
			get {return e5StartPrice;}
		 	set {e5StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event5")]
		public double E5EndPrice
		{ 
			get {return e5EndPrice;}
		 	set {e5EndPrice = value; }
		}	
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event5")] 
		public bool ShowE5outline
        {
            get { return showE5outline; }
            set { showE5outline = value; }
        }		
		
		//===========Event 6================	
		
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event6")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE6Type
		{
			get { return toolE6Type; }
			set { toolE6Type = value; }
		}
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event6")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE6
        {
            get { return dailyE6; }
            set { dailyE6 = value; }
        }	
	
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event6")] 
		public bool MondayE6
        {
            get { return mondayE6; }
            set { mondayE6 = value; }
        }	
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event6")] 
		public bool TuesdayE6
        {
            get { return tuesdayE6; }
            set { tuesdayE6 = value; }
        }
		
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event6")] 
		public bool WednesdayE6
        {
            get { return wednesdayE6; }
            set { wednesdayE6 = value; }
        }
		
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event6")] 
		public bool ThursdayE6
        {
            get { return thursdayE6; }
            set { thursdayE6 = value; }
        }
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event6")] 
		public bool FridayE6
        {
            get { return fridayE6; }
            set { fridayE6 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event6")]
		public TimeSpan TSE6start
		{
			get {return tsE6start;}
		 	set {tsE6start = value; }
		}
		[Browsable(false)]
		public string tsE6StartSerialize
        {
            get { return tsE6start.ToString(); }
            set { tsE6start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event6")]
		public TimeSpan TSE6end
		{
			get {return tsE6end;}
		 	set {tsE6end = value; }
		}
		[Browsable(false)]
		public string TSE6endSerialize
        {
            get { return tsE6end.ToString(); }
            set { tsE6end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event6")]		
		public Brush Event6Color 
		{
			get { return e6Color; }
			set { e6Color = value;; }
		}	
		[Browsable(false)]
		public string e6ColorSerializable
		{
			get { return Serialize.BrushToString(e6Color); }
			set { e6Color = Serialize.StringToBrush(value); }
		}
		
		[Range(0, 100)]	
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event6")]
		public int E6Opacity
		{ 
			get {return e6Opacity;}
		 	set {e6Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event6")]
		public double E6StartPrice
		{ 
			get {return e6StartPrice;}
		 	set {e6StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]	
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event6")]
		public double E6EndPrice
		{ 
			get {return e6EndPrice;}
		 	set {e6EndPrice = value; }
		}	

		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event6")] 
		public bool ShowE6outline
        {
            get { return showE6outline; }
            set { showE6outline = value; }
        }
		
		//===========Event 7================	
			
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event7")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE7Type
		{
			get { return toolE7Type; }
			set { toolE7Type = value; }
		}
			
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event7")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE7
        {
            get { return dailyE7; }
            set { dailyE7 = value; }
        }	
			
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event7")] 
		public bool MondayE7
        {
            get { return mondayE7; }
            set { mondayE7 = value; }
        }
			
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event7")] 
		public bool TuesdayE7
        {
            get { return tuesdayE7; }
            set { tuesdayE7 = value; }
        }
			
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event7")] 
		public bool WednesdayE7
        {
            get { return wednesdayE7; }
            set { wednesdayE7 = value; }
        }
				
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event7")] 
		public bool ThursdayE7
        {
            get { return thursdayE7; }
            set { thursdayE7 = value; }
        }		
		
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event7")] 
		public bool FridayE7
        {
            get { return fridayE7; }
            set { fridayE7 = value; }
        }			
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event7")]
		public TimeSpan TSE7start
		{
			get {return tsE7start;}
		 	set {tsE7start = value; }
		}
		[Browsable(false)]
		public string tsE7StartSerialize
        {
            get { return tsE7start.ToString(); }
            set { tsE7start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event7")]
		public TimeSpan TSE7end
		{
			get {return tsE7end;}
		 	set {tsE7end = value; }
		}
		[Browsable(false)]
		public string TSE7endSerialize
        {
            get { return tsE7end.ToString(); }
            set { tsE7end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event7")]		
		public Brush Event7Color 
		{
			get { return e7Color; }
			set { e7Color = value;; }
		}	
		[Browsable(false)]
		public string e7ColorSerializable
		{
			get { return Serialize.BrushToString(e7Color); }
			set { e7Color = Serialize.StringToBrush(value); }
		}	
		
		[Range(0, 100)]		
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event7")]
		public int E7Opacity
		{ 
			get {return e7Opacity;}
		 	set {e7Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event7")]
		public double E7StartPrice
		{ 
			get {return e7StartPrice;}
		 	set {e7StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event7")]
		public double E7EndPrice
		{ 
			get {return e7EndPrice;}
		 	set {e7EndPrice = value; }
		}
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event7")] 
		public bool ShowE7outline
        {
            get { return showE7outline; }
            set { showE7outline = value; }
        }		
		
		//===========Event 8================	
			
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event8")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE8Type
		{
			get { return toolE8Type; }
			set { toolE8Type = value; }
		}		
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event8")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE8
        {
            get { return dailyE8; }
            set { dailyE8 = value; }
        }	
			
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event8")] 
		public bool MondayE8
        {
            get { return mondayE8; }
            set { mondayE8 = value; }
        }	
				
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event8")] 
		public bool TuesdayE8
        {
            get { return tuesdayE8; }
            set { tuesdayE8 = value; }
        }
				
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event8")] 
		public bool WednesdayE8
        {
            get { return wednesdayE8; }
            set { wednesdayE8 = value; }
        }
				
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event8")] 
		public bool ThursdayE8
        {
            get { return thursdayE8; }
            set { thursdayE8 = value; }
        }
				
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event8")] 
		public bool FridayE8
        {
            get { return fridayE8; }
            set { fridayE8 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event8")]
		public TimeSpan TSE8start
		{
			get {return tsE8start;}
		 	set {tsE8start = value; }
		}
		[Browsable(false)]
		public string tsE8StartSerialize
        {
            get { return tsE8start.ToString(); }
            set { tsE8start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event8")]
		public TimeSpan TSE8end
		{
			get {return tsE8end;}
		 	set {tsE8end = value; }
		}
		[Browsable(false)]
		public string TSE8endSerialize
        {
            get { return tsE8end.ToString(); }
            set { tsE8end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event8")]		
		public Brush Event8Color 
		{
			get { return e8Color; }
			set { e8Color = value;; }
		}	
		[Browsable(false)]
		public string e8ColorSerializable
		{
			get { return Serialize.BrushToString(e8Color); }
			set { e8Color = Serialize.StringToBrush(value); }
		}	
		
		[Range(0, 100)]
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event8")]
		public int E8Opacity
		{ 
			get {return e8Opacity;}
		 	set {e8Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event8")]
		public double E8StartPrice
		{ 
			get {return e8StartPrice;}
		 	set {e8StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]	
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event8")]
		public double E8EndPrice
		{ 
			get {return e8EndPrice;}
		 	set {e8EndPrice = value; }
		}	
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event8")] 
		public bool ShowE8outline
        {
            get { return showE8outline; }
            set { showE8outline = value; }
        }		
		
		//===========Event 9================	
			
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Event9")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE9Type
		{
			get { return toolE9Type; }
			set { toolE9Type = value; }
		}
		
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Event9")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE9
        {
            get { return dailyE9; }
            set { dailyE9 = value; }
        }		
		
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Event9")] 
		public bool MondayE9
        {
            get { return mondayE9; }
            set { mondayE9 = value; }
        }
		
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Event9")] 
		public bool TuesdayE9
        {
            get { return tuesdayE9; }
            set { tuesdayE9 = value; }
        }
			
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Event9")] 
		public bool WednesdayE9
        {
            get { return wednesdayE9; }
            set { wednesdayE9 = value; }
        }
			
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Event9")] 
		public bool ThursdayE9
        {
            get { return thursdayE9; }
            set { thursdayE9 = value; }
        }
				
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Event9")] 
		public bool FridayE9
        {
            get { return fridayE9; }
            set { fridayE9 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Event9")]
		public TimeSpan TSE9start
		{
			get {return tsE9start;}
		 	set {tsE9start = value; }
		}
		[Browsable(false)]
		public string tsE9StartSerialize
        {
            get { return tsE9start.ToString(); }
            set { tsE9start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Event9")]
		public TimeSpan TSE9end
		{
			get {return tsE9end;}
		 	set {tsE9end = value; }
		}
		[Browsable(false)]
		public string TSE9endSerialize
        {
            get { return tsE9end.ToString(); }
            set { tsE9end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Event9")]		
		public Brush Event9Color 
		{
			get { return e9Color; }
			set { e9Color = value;; }
		}		
		[Browsable(false)]
		public string e9ColorSerializable
		{
			get { return Serialize.BrushToString(e9Color); }
			set { e9Color = Serialize.StringToBrush(value); }
		}
		
		[Range(0, 100)]	
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Event9")]
		public int E9Opacity
		{ 
			get {return e9Opacity;}
		 	set {e9Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Event9")]
		public double E9StartPrice
		{ 
			get {return e9StartPrice;}
		 	set {e9StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]	
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Event9")]
		public double E9EndPrice
		{ 
			get {return e9EndPrice;}
		 	set {e9EndPrice = value; }
		}
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Event9")] 
		public bool ShowE9outline
        {
            get { return showE9outline; }
            set { showE9outline = value; }
        }
		
		//===========Event 10================	
				
		[Display(Name="Tool type", Description="Choose Draw Object", Order=1, GroupName="Last Event 10")]
		[RefreshProperties(RefreshProperties.All)]
		public ToolSelector ToolE10Type
		{
			get { return toolE10Type; }
			set { toolE10Type = value; }
		}
			
		[Display(Name="Repeats Daily", Description="Will repeat every day at time selected", Order=2, GroupName="Last Event 10")] 
		[RefreshProperties(RefreshProperties.All)]
		public bool DailyE10
        {
            get { return dailyE10; }
            set { dailyE10 = value; }
        }	
				
		[Display(Name="Repeats Mondays", Description="Will repeat every Monday at time selected", Order=3, GroupName="Last Event 10")] 
		public bool MondayE10
        {
            get { return mondayE10; }
            set { mondayE10 = value; }
        }	
				
		[Display(Name="Repeats Tuesdays", Description="Will repeat every Tuesday at time selected", Order=4, GroupName="Last Event 10")] 
		public bool TuesdayE10
        {
            get { return tuesdayE10; }
            set { tuesdayE10 = value; }
        }
			
		[Display(Name="Repeats Wednesdays", Description="Will repeat every Wednesday at time selected", Order=5, GroupName="Last Event 10")] 
		public bool WednesdayE10
        {
            get { return wednesdayE10; }
            set { wednesdayE10 = value; }
        }
				
		[Display(Name="Repeats Thursdays", Description="Will repeat every Thursday at time selected", Order=6, GroupName="Last Event 10")] 
		public bool ThursdayE10
        {
            get { return thursdayE10; }
            set { thursdayE10 = value; }
        }
				
		[Display(Name="Repeats Fridays", Description="Will repeat every Friday at time selected", Order=7, GroupName="Last Event 10")] 
		public bool FridayE10
        {
            get { return fridayE10; }
            set { fridayE10 = value; }
        }		
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="Start time", Description="HH:MM:SS", Order=8, GroupName="Last Event 10")]
		public TimeSpan TSE10start
		{
			get {return tsE10start;}
		 	set {tsE10start = value; }
		}
		[Browsable(false)]
		public string tsE10StartSerialize
        {
            get { return tsE10start.ToString(); }
            set { tsE10start = TimeSpan.Parse(value); }
        }
		
		[Range (typeof (TimeSpan), "00:00:00", "23:59:59")]
		[Display(Name="End time", Description="HH:MM:SS", Order=9, GroupName="Last Event 10")]
		public TimeSpan TSE10end
		{
			get {return tsE10end;}
		 	set {tsE10end = value; }
		}
		[Browsable(false)]
		public string TSE10endSerialize
        {
            get { return tsE10end.ToString(); }
            set { tsE10end = TimeSpan.Parse(value); }
        }
		
		[XmlIgnore]
		[Display(Name="Color", Description="Color to use", Order=12, GroupName="Last Event 10")]		
		public Brush Event10Color 
		{
			get { return e10Color; }
			set { e10Color = value;; }
		}	
		[Browsable(false)]
		public string e10ColorSerializable
		{
			get { return Serialize.BrushToString(e10Color); }
			set { e10Color = Serialize.StringToBrush(value); }
		}	
		
		[Range(0, 100)]		
		[Display(Name="Opacity", Description=" 0 to 100", Order=13, GroupName="Last Event 10")]
		public int E10Opacity
		{ 
			get {return e10Opacity;}
		 	set {e10Opacity = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="Start Price", Description="Price value to start", Order=10, GroupName="Last Event 10")]
		public double E10StartPrice
		{ 
			get {return e10StartPrice;}
		 	set {e10StartPrice = value; }
		}
		
		[Range(double.MinValue, double.MaxValue)]		
		[Display(Name="End Price", Description="Price Value to end", Order=11, GroupName="Last Event 10")]
		public double E10EndPrice
		{ 
			get {return e10EndPrice;}
		 	set {e10EndPrice = value; }
		}	
		
		[Display(Name="Show outline", Description="Sets outline of shape on or off", Order=14, GroupName="Last Event 10")] 
		public bool ShowE10outline
        {
            get { return showE10outline; }
            set { showE10outline = value; }
        }		
		
		#endregion	
		
       #region Custom Property Manipulation

        private void ModifyProperties(PropertyDescriptorCollection col)
        {

				int ne = NumberOfEvents +1	;	// Use ne to process the number of events to show, +1 offset
				if (ne <= 10) 
				{
					col.Remove(col.Find("ToolE10Type", true));
					col.Remove(col.Find("E10Opacity", true));
					col.Remove(col.Find("Event10Color", true));
					col.Remove(col.Find("TSE10end", true));
					col.Remove(col.Find("TSE10start", true));
					col.Remove(col.Find("E10StartPrice", true));
					col.Remove(col.Find("E10EndPrice", true));
					col.Remove(col.Find("DailyE10", true));
					col.Remove(col.Find("MondayE10", true));
					col.Remove(col.Find("TuesdayE10", true));
					col.Remove(col.Find("WednesdayE10", true));
					col.Remove(col.Find("ThursdayE10", true));
					col.Remove(col.Find("FridayE10", true));
					col.Remove(col.Find("ShowE10outline",true));
				}
				
				if (ne <= 9) 
				{
					col.Remove(col.Find("ToolE9Type", true));
					col.Remove(col.Find("E9Opacity", true));
					col.Remove(col.Find("Event9Color", true));
					col.Remove(col.Find("TSE9end", true));
					col.Remove(col.Find("TSE9start", true));
					col.Remove(col.Find("E9StartPrice", true));
					col.Remove(col.Find("E9EndPrice", true));
					col.Remove(col.Find("DailyE9", true));	
					col.Remove(col.Find("MondayE9", true));
					col.Remove(col.Find("TuesdayE9", true));
					col.Remove(col.Find("WednesdayE9", true));
					col.Remove(col.Find("ThursdayE9", true));
					col.Remove(col.Find("FridayE9", true));
					col.Remove(col.Find("ShowE9outline",true));
				}
				
				if (ne <= 8) 
				{
					col.Remove(col.Find("ToolE8Type", true));
					col.Remove(col.Find("E8Opacity", true));
					col.Remove(col.Find("Event8Color", true));
					col.Remove(col.Find("TSE8end", true));
					col.Remove(col.Find("TSE8start", true));
					col.Remove(col.Find("E8StartPrice", true));
					col.Remove(col.Find("E8EndPrice", true));
					col.Remove(col.Find("DailyE8", true));
					col.Remove(col.Find("MondayE8", true));
					col.Remove(col.Find("TuesdayE8", true));
					col.Remove(col.Find("WednesdayE8", true));
					col.Remove(col.Find("ThursdayE8", true));
					col.Remove(col.Find("FridayE8", true));	
					col.Remove(col.Find("ShowE8outline",true));
				}
				
				if (ne <= 7) 
				{
					col.Remove(col.Find("ToolE7Type", true));
					col.Remove(col.Find("E7Opacity", true));
					col.Remove(col.Find("Event7Color", true));
					col.Remove(col.Find("TSE7end", true));
					col.Remove(col.Find("TSE7start", true));
					col.Remove(col.Find("E7StartPrice", true));
					col.Remove(col.Find("E7EndPrice", true));
					col.Remove(col.Find("DailyE7", true));
					col.Remove(col.Find("MondayE7", true));
					col.Remove(col.Find("TuesdayE7", true));
					col.Remove(col.Find("WednesdayE7", true));
					col.Remove(col.Find("ThursdayE7", true));
					col.Remove(col.Find("FridayE7", true));	
					col.Remove(col.Find("ShowE7outline",true));
				}	
				
				if (ne <= 6) 
				{
					col.Remove(col.Find("ToolE6Type", true));
					col.Remove(col.Find("E6Opacity", true));
					col.Remove(col.Find("Event6Color", true));
					col.Remove(col.Find("TSE6end", true));
					col.Remove(col.Find("TSE6start", true));
					col.Remove(col.Find("E6StartPrice", true));
					col.Remove(col.Find("E6EndPrice", true));
					col.Remove(col.Find("DailyE6", true));
					col.Remove(col.Find("MondayE6", true));
					col.Remove(col.Find("TuesdayE6", true));
					col.Remove(col.Find("WednesdayE6", true));
					col.Remove(col.Find("ThursdayE6", true));
					col.Remove(col.Find("FridayE6", true));	
					col.Remove(col.Find("ShowE6outline",true));
				}	
				
				if (ne <= 5) 
				{
					col.Remove(col.Find("ToolE5Type", true));
					col.Remove(col.Find("E5Opacity", true));
					col.Remove(col.Find("Event5Color", true));
					col.Remove(col.Find("TSE5end", true));
					col.Remove(col.Find("TSE5start", true));
					col.Remove(col.Find("E5StartPrice", true));
					col.Remove(col.Find("E5EndPrice", true));
					col.Remove(col.Find("DailyE5", true));
					col.Remove(col.Find("MondayE5", true));
					col.Remove(col.Find("TuesdayE5", true));
					col.Remove(col.Find("WednesdayE5", true));
					col.Remove(col.Find("ThursdayE5", true));
					col.Remove(col.Find("FridayE5", true));	
					col.Remove(col.Find("ShowE5outline",true));
				}	
				
				if (ne <= 4) 
				{
					col.Remove(col.Find("ToolE4Type", true));
					col.Remove(col.Find("E4Opacity", true));
					col.Remove(col.Find("Event4Color", true));
					col.Remove(col.Find("TSE4end", true));
					col.Remove(col.Find("TSE4start", true));
					col.Remove(col.Find("E4StartPrice", true));
					col.Remove(col.Find("E4EndPrice", true));
					col.Remove(col.Find("DailyE4", true));
					col.Remove(col.Find("MondayE4", true));
					col.Remove(col.Find("TuesdayE4", true));
					col.Remove(col.Find("WednesdayE4", true));
					col.Remove(col.Find("ThursdayE4", true));
					col.Remove(col.Find("FridayE4", true));
					col.Remove(col.Find("ShowE4outline",true));
				}					
								
				if (ne <= 3) 
				{
					col.Remove(col.Find("ToolE3Type", true));
					col.Remove(col.Find("E3Opacity", true));
					col.Remove(col.Find("Event3Color", true));
					col.Remove(col.Find("TSE3end", true));
					col.Remove(col.Find("TSE3start", true));
					col.Remove(col.Find("E3StartPrice", true));
					col.Remove(col.Find("E3EndPrice", true));
					col.Remove(col.Find("DailyE3", true));
					col.Remove(col.Find("MondayE3", true));
					col.Remove(col.Find("TuesdayE3", true));
					col.Remove(col.Find("WednesdayE3", true));
					col.Remove(col.Find("ThursdayE3", true));
					col.Remove(col.Find("FridayE3", true));	
					col.Remove(col.Find("ShowE3outline",true));
				}
				
				if (ne <= 2) 
				{
					col.Remove(col.Find("ToolE2Type", true));
					col.Remove(col.Find("E2Opacity", true));
					col.Remove(col.Find("Event2Color", true));
					col.Remove(col.Find("TSE2end", true));
					col.Remove(col.Find("TSE2start", true));
					col.Remove(col.Find("E2StartPrice", true));
					col.Remove(col.Find("E2EndPrice", true));
					col.Remove(col.Find("DailyE2", true));
					col.Remove(col.Find("MondayE2", true));
					col.Remove(col.Find("TuesdayE2", true));
					col.Remove(col.Find("WednesdayE2", true));
					col.Remove(col.Find("ThursdayE2", true));
					col.Remove(col.Find("FridayE2", true));
					col.Remove(col.Find("ShowE2outline",true));
				}
				
				// =======================================
				if (ToolE1Type == ToolSelector.TimeRegion || ToolE1Type == ToolSelector.DynamicRectangle || ToolE1Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E1StartPrice", true));
					col.Remove(col.Find("E1EndPrice", true));
				}
				if (ToolE1Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE1start", true));
					col.Remove(col.Find("TSE1end", true));
					col.Remove(col.Find("DailyE1", true));
					col.Remove(col.Find("MondayE1", true));
					col.Remove(col.Find("TuesdayE1", true));
					col.Remove(col.Find("WednesdayE1", true));
					col.Remove(col.Find("ThursdayE1", true));
					col.Remove(col.Find("FridayE1", true));
				}

				
				// =======================================
				
				if (ToolE2Type == ToolSelector.TimeRegion || ToolE2Type == ToolSelector.DynamicRectangle || ToolE2Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E2StartPrice", true));
					col.Remove(col.Find("E2EndPrice", true));
				}
				if (ToolE2Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE2start", true));
					col.Remove(col.Find("TSE2end", true));
					col.Remove(col.Find("DailyE2", true));
					col.Remove(col.Find("MondayE2", true));
					col.Remove(col.Find("TuesdayE2", true));
					col.Remove(col.Find("WednesdayE2", true));
					col.Remove(col.Find("ThursdayE2", true));
					col.Remove(col.Find("FridayE2", true));
				}
				
				// =======================================
				
				if (ToolE3Type == ToolSelector.TimeRegion || ToolE3Type == ToolSelector.DynamicRectangle || ToolE3Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E3StartPrice", true));
					col.Remove(col.Find("E3EndPrice", true));
				}
				if (ToolE3Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE3start", true));
					col.Remove(col.Find("TSE3end", true));
					col.Remove(col.Find("DailyE3", true));
					col.Remove(col.Find("MondayE3", true));
					col.Remove(col.Find("TuesdayE3", true));
					col.Remove(col.Find("WednesdayE3", true));
					col.Remove(col.Find("ThursdayE3", true));
					col.Remove(col.Find("FridayE3", true));
				}
				
				// =======================================
				
				if (ToolE4Type == ToolSelector.TimeRegion || ToolE4Type == ToolSelector.DynamicRectangle || ToolE4Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E4StartPrice", true));
					col.Remove(col.Find("E4EndPrice", true));
				}
				if (ToolE4Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE4start", true));
					col.Remove(col.Find("TSE4end", true));
					col.Remove(col.Find("DailyE4", true));
					col.Remove(col.Find("MondayE4", true));
					col.Remove(col.Find("TuesdayE4", true));
					col.Remove(col.Find("WednesdayE4", true));
					col.Remove(col.Find("ThursdayE4", true));
					col.Remove(col.Find("FridayE4", true));
				}	
				
				// =======================================
				
				if (ToolE5Type == ToolSelector.TimeRegion || ToolE5Type == ToolSelector.DynamicRectangle || ToolE5Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E5StartPrice", true));
					col.Remove(col.Find("E5EndPrice", true));
				}
				if (ToolE5Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE5start", true));
					col.Remove(col.Find("TSE5end", true));
					col.Remove(col.Find("DailyE5", true));
					col.Remove(col.Find("MondayE5", true));
					col.Remove(col.Find("TuesdayE5", true));
					col.Remove(col.Find("WednesdayE5", true));
					col.Remove(col.Find("ThursdayE5", true));
					col.Remove(col.Find("FridayE5", true));
				}
				
				// =======================================
				
				if (ToolE6Type == ToolSelector.TimeRegion || ToolE6Type == ToolSelector.DynamicRectangle || ToolE6Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E6StartPrice", true));
					col.Remove(col.Find("E6EndPrice", true));
				}				
				if (ToolE6Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE6start", true));
					col.Remove(col.Find("TSE6end", true));
					col.Remove(col.Find("DailyE6", true));
					col.Remove(col.Find("MondayE6", true));
					col.Remove(col.Find("TuesdayE6", true));
					col.Remove(col.Find("WednesdayE6", true));
					col.Remove(col.Find("ThursdayE6", true));
					col.Remove(col.Find("FridayE6", true));	
				}
				
				// =======================================
				
				if (ToolE7Type == ToolSelector.TimeRegion || ToolE7Type == ToolSelector.DynamicRectangle || ToolE7Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E7StartPrice", true));
					col.Remove(col.Find("E7EndPrice", true));
				}
				if (ToolE7Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE7start", true));
					col.Remove(col.Find("TSE7end", true));
					col.Remove(col.Find("DailyE7", true));
					col.Remove(col.Find("MondayE7", true));
					col.Remove(col.Find("TuesdayE7", true));
					col.Remove(col.Find("WednesdayE7", true));
					col.Remove(col.Find("ThursdayE7", true));
					col.Remove(col.Find("FridayE7", true));		
				}								
				
				// =======================================
				
				if (ToolE8Type == ToolSelector.TimeRegion || ToolE8Type == ToolSelector.DynamicRectangle || ToolE8Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E8StartPrice", true));
					col.Remove(col.Find("E8EndPrice", true));
				}
				if (ToolE8Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE8start", true));
					col.Remove(col.Find("TSE8end", true));
					col.Remove(col.Find("DailyE8", true));
					col.Remove(col.Find("MondayE8", true));
					col.Remove(col.Find("TuesdayE8", true));
					col.Remove(col.Find("WednesdayE8", true));
					col.Remove(col.Find("ThursdayE8", true));
					col.Remove(col.Find("FridayE8", true));
				}								
				
				// =======================================
				
				if (ToolE9Type == ToolSelector.TimeRegion || ToolE9Type == ToolSelector.DynamicRectangle || ToolE9Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E9StartPrice", true));
					col.Remove(col.Find("E9EndPrice", true));
				}
				if (ToolE9Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE9start", true));
					col.Remove(col.Find("TSE9end", true));
					col.Remove(col.Find("DailyE9", true));
					col.Remove(col.Find("MondayE9", true));
					col.Remove(col.Find("TuesdayE9", true));
					col.Remove(col.Find("WednesdayE9", true));
					col.Remove(col.Find("ThursdayE9", true));
					col.Remove(col.Find("FridayE9", true));
				}								
				
				// =======================================
				
				if (ToolE10Type == ToolSelector.TimeRegion || ToolE10Type == ToolSelector.DynamicRectangle || ToolE10Type == ToolSelector.OpeningRange)
				{
					col.Remove(col.Find("E10StartPrice", true));
					col.Remove(col.Find("E10EndPrice", true));
				}
				if (ToolE10Type == ToolSelector.PriceRegion)
				{
					col.Remove(col.Find("TSE10start", true));
					col.Remove(col.Find("TSE10end", true));
					col.Remove(col.Find("DailyE10", true));
					col.Remove(col.Find("MondayE10", true));
					col.Remove(col.Find("TuesdayE10", true));
					col.Remove(col.Find("WednesdayE10", true));
					col.Remove(col.Find("ThursdayE10", true));
					col.Remove(col.Find("FridayE10", true));
				}								
				
				// =======================================	
				
				if (DailyE1)
				{
					col.Remove(col.Find("MondayE1", true));
					col.Remove(col.Find("TuesdayE1", true));
					col.Remove(col.Find("WednesdayE1", true));
					col.Remove(col.Find("ThursdayE1", true));
					col.Remove(col.Find("FridayE1", true));
				}
				
				if (DailyE2)
				{
					col.Remove(col.Find("MondayE2", true));
					col.Remove(col.Find("TuesdayE2", true));
					col.Remove(col.Find("WednesdayE2", true));
					col.Remove(col.Find("ThursdayE2", true));
					col.Remove(col.Find("FridayE2", true));
				}
				if (DailyE3)
				{
					col.Remove(col.Find("MondayE3", true));
					col.Remove(col.Find("TuesdayE3", true));
					col.Remove(col.Find("WednesdayE3", true));
					col.Remove(col.Find("ThursdayE3", true));
					col.Remove(col.Find("FridayE3", true));
				}
					
				if (DailyE4)
				{
					col.Remove(col.Find("MondayE4", true));
					col.Remove(col.Find("TuesdayE4", true));
					col.Remove(col.Find("WednesdayE4", true));
					col.Remove(col.Find("ThursdayE4", true));
					col.Remove(col.Find("FridayE4", true));				
				}
				
				if (DailyE5)
				{
					col.Remove(col.Find("MondayE5", true));
					col.Remove(col.Find("TuesdayE5", true));
					col.Remove(col.Find("WednesdayE5", true));
					col.Remove(col.Find("ThursdayE5", true));
					col.Remove(col.Find("FridayE5", true));
				}	

				if (DailyE6)
				{
					col.Remove(col.Find("MondayE6", true));
					col.Remove(col.Find("TuesdayE6", true));
					col.Remove(col.Find("WednesdayE6", true));
					col.Remove(col.Find("ThursdayE6", true));
					col.Remove(col.Find("FridayE6", true));
				}	
				
				if (DailyE7)
				{
					col.Remove(col.Find("MondayE7", true));
					col.Remove(col.Find("TuesdayE7", true));
					col.Remove(col.Find("WednesdayE7", true));
					col.Remove(col.Find("ThursdayE7", true));
					col.Remove(col.Find("FridayE7", true));
				}					
				
				if (DailyE8)
				{
					col.Remove(col.Find("MondayE8", true));
					col.Remove(col.Find("TuesdayE8", true));
					col.Remove(col.Find("WednesdayE8", true));
					col.Remove(col.Find("ThursdayE8", true));
					col.Remove(col.Find("FridayE8", true));
				}
				
				if (DailyE9)
				{
					col.Remove(col.Find("MondayE9", true));
					col.Remove(col.Find("TuesdayE9", true));
					col.Remove(col.Find("WednesdayE9", true));
					col.Remove(col.Find("ThursdayE9", true));
					col.Remove(col.Find("FridayE9", true));
				}
				
				if (DailyE10)
				{
					col.Remove(col.Find("MondayE10", true));
					col.Remove(col.Find("TuesdayE10", true));
					col.Remove(col.Find("WednesdayE10", true));
					col.Remove(col.Find("ThursdayE10", true));
					col.Remove(col.Find("FridayE10", true));
				}					
				
        }	
		
		#endregion
		
       #region ICustomTypeDescriptor Members

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(GetType());
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(GetType());
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(GetType());
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(GetType());
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(GetType());
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(GetType());
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(GetType(), editorBaseType);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(GetType(), attributes);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(GetType());
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection orig = TypeDescriptor.GetProperties(GetType(), attributes);
            PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
            orig.CopyTo(arr, 0);
            PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);

            ModifyProperties(col);
            return col;

        }

        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(GetType());
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion		
		
		
		
		


	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RepeaterV2[] cacheRepeaterV2;
		public RepeaterV2 RepeaterV2()
		{
			return RepeaterV2(Input);
		}

		public RepeaterV2 RepeaterV2(ISeries<double> input)
		{
			if (cacheRepeaterV2 != null)
				for (int idx = 0; idx < cacheRepeaterV2.Length; idx++)
					if (cacheRepeaterV2[idx] != null &&  cacheRepeaterV2[idx].EqualsInput(input))
						return cacheRepeaterV2[idx];
			return CacheIndicator<RepeaterV2>(new RepeaterV2(), input, ref cacheRepeaterV2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RepeaterV2 RepeaterV2()
		{
			return indicator.RepeaterV2(Input);
		}

		public Indicators.RepeaterV2 RepeaterV2(ISeries<double> input )
		{
			return indicator.RepeaterV2(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RepeaterV2 RepeaterV2()
		{
			return indicator.RepeaterV2(Input);
		}

		public Indicators.RepeaterV2 RepeaterV2(ISeries<double> input )
		{
			return indicator.RepeaterV2(input);
		}
	}
}

#endregion
