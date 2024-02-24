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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BarSizeCounter : Indicator
	{
		private	Gui.Tools.SimpleFont textFont;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "BarSizeCounter";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				IsChartOnly									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				PaintPriceMarkers							= false;
				ShowPercent									= false;
				CountDown									= true;
				DistanceText								= 10;
				textFont									= new Gui.Tools.SimpleFont("Arial", 11);
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			double periodValue 	= (BarsPeriod.BarsPeriodType == BarsPeriodType.Tick) ? BarsPeriod.Value : BarsPeriod.BaseBarsPeriodValue;
			double tickCount 	= ShowPercent ? CountDown ? (1 - Bars.PercentComplete) : Bars.PercentComplete : CountDown ? periodValue - Bars.TickCount : Bars.TickCount;
			string tickMsg		= ShowPercent ? tickCount.ToString("P0") : tickCount.ToString();

			string tick1 = (BarsPeriod.BarsPeriodType == BarsPeriodType.Tick 
						|| ((BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Tick) ? ((CountDown 
										? NinjaTrader.Custom.Resource.TickCounterTicksRemaining + tickMsg : NinjaTrader.Custom.Resource.TickCounterTickCount + tickMsg))
										: NinjaTrader.Custom.Resource.TickCounterBarError);
			
			
			if (CurrentBars[0] < 1)
                return;


			string rangeValue = (High[0] - Low[0]).ToString();
			string rangeTickValue = ((High[0] - Low[0]) / TickSize).ToString();
			
			string displayText = "Points: " + rangeValue + "\nTicks: " + rangeTickValue;
		
			Draw.Text(this, "tag1", displayText, -DistanceText, Close[0], ChartControl.Properties.ChartText);	
			Draw.Text(this, "tag1", false, displayText, -DistanceText, Close[0], 0, ChartControl.Properties.ChartText, textFont, TextAlignment.Right, Brushes.Transparent, Brushes.Transparent, 100);	
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
		public bool CountDown
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "ShowPercent", Order = 2, GroupName = "NinjaScriptParameters")]
		public bool ShowPercent
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "Text Distance From Bar", Order = 3, GroupName = "NinjaScriptParameters")]
		public int DistanceText
		{ get; set; }
		
		[Display(Name = "Text Font", Description= "Select font, style, size to display on chart", GroupName= "NinjaScriptParameters", Order= 5)]
		public Gui.Tools.SimpleFont TextFont
		{
			get { return textFont; }
			set { textFont = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BarSizeCounter[] cacheBarSizeCounter;
		public BarSizeCounter BarSizeCounter(bool countDown, bool showPercent, int distanceText)
		{
			return BarSizeCounter(Input, countDown, showPercent, distanceText);
		}

		public BarSizeCounter BarSizeCounter(ISeries<double> input, bool countDown, bool showPercent, int distanceText)
		{
			if (cacheBarSizeCounter != null)
				for (int idx = 0; idx < cacheBarSizeCounter.Length; idx++)
					if (cacheBarSizeCounter[idx] != null && cacheBarSizeCounter[idx].CountDown == countDown && cacheBarSizeCounter[idx].ShowPercent == showPercent && cacheBarSizeCounter[idx].DistanceText == distanceText && cacheBarSizeCounter[idx].EqualsInput(input))
						return cacheBarSizeCounter[idx];
			return CacheIndicator<BarSizeCounter>(new BarSizeCounter(){ CountDown = countDown, ShowPercent = showPercent, DistanceText = distanceText }, input, ref cacheBarSizeCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BarSizeCounter BarSizeCounter(bool countDown, bool showPercent, int distanceText)
		{
			return indicator.BarSizeCounter(Input, countDown, showPercent, distanceText);
		}

		public Indicators.BarSizeCounter BarSizeCounter(ISeries<double> input , bool countDown, bool showPercent, int distanceText)
		{
			return indicator.BarSizeCounter(input, countDown, showPercent, distanceText);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BarSizeCounter BarSizeCounter(bool countDown, bool showPercent, int distanceText)
		{
			return indicator.BarSizeCounter(Input, countDown, showPercent, distanceText);
		}

		public Indicators.BarSizeCounter BarSizeCounter(ISeries<double> input , bool countDown, bool showPercent, int distanceText)
		{
			return indicator.BarSizeCounter(input, countDown, showPercent, distanceText);
		}
	}
}

#endregion
