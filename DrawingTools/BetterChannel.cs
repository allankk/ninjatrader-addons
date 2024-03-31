// 
// Copyright (C) 2023, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

// For context menu to work:
using System.Windows.Controls;
using NinjaTrader.NinjaScript.DrawingTools;

// Get templates
using System.IO;
#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{
	/// <summary>
	/// Represents an interface that exposes information regarding a Trend Channel IDrawingTool.
	/// </summary>
	public class BetterChannel : PriceLevelContainer
	{
		private				int									areaOpacity;
		private				Brush								areaBrush;	
		private	readonly	DeviceBrush							areaDeviceBrush				= new DeviceBrush();
		private	const		double								cursorSensitivity			= 15;
		private				ChartAnchor							editingAnchor;
		private				SharpDX.Direct2D1.PathGeometry		fillMainGeometry;
		private				SharpDX.Vector2[]					fillMainFig;
		private				SharpDX.Direct2D1.PathGeometry		fillLeftGeometry;
		private				SharpDX.Vector2[]					fillLeftFig;
		private				SharpDX.Direct2D1.PathGeometry		fillRightGeometry;
		private				SharpDX.Vector2[]					fillRightFig;
		private				bool								isReadyForMovingSecondLeg;
		private				bool								updateEndAnc;
		private 			ChartControl						myChartControl;
		private				int									priceLevelOpacity;
		private 			MenuItem 							myMenuItem1;
		private 			MenuItem 							myMenuItem2;
		private 			MenuItem 							myMenuItem3;
		private 			MenuItem 							myMenuItem4;
		private 			MenuItem 							myMenuItem5;

		public override object Icon { get { return Gui.Tools.Icons.DrawTrendChannel; } }

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
		public Brush AreaBrush
		{
			get { return areaBrush; }
			set { areaBrush = value.ToFrozenBrush(); }
		}

		[Browsable(false)]
		public string AreaBrushSerialize
		{
			get { return Serialize.BrushToString(AreaBrush); }
			set { AreaBrush = Serialize.StringToBrush(value); }
		}

		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
		public int AreaOpacity
		{
			get { return areaOpacity; }
			set
			{
				int newOpacity = Math.Max(0, Math.Min(100, value));
				if (newOpacity != areaOpacity)
				{
					areaOpacity = newOpacity;
					areaDeviceBrush.Brush = null;
				}
			}
		}

		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
		public int PriceLevelOpacity
		{
			get { return priceLevelOpacity; }
			set { priceLevelOpacity = Math.Max(0, Math.Min(100, value)); }
		}

		public override IEnumerable<ChartAnchor> Anchors { get { return new[] { TrendStartAnchor, TrendEndAnchor, ParallelMidAnchor }; } }

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines")]
		public bool IsExtendedLinesRight { get; set; }
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines")]
		public bool IsExtendedLinesLeft { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolTrendChannelTrendStroke", GroupName = "NinjaScriptLines", Order = 1)]
		public Stroke Stroke { get; set; }

		[Display(Order = 10), ExcludeFromTemplate]
		public ChartAnchor TrendEndAnchor { get; set; }

		[Display(Order = 0), ExcludeFromTemplate]
		public ChartAnchor TrendStartAnchor { get; set; }

		[Display(Order = 25), ExcludeFromTemplate]
		public ChartAnchor ParallelMidAnchor { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolTrendChannelParallelStroke", GroupName = "NinjaScriptLines", Order = 2)]
		public Stroke ParallelStroke { get; set; }

		public override bool SupportsAlerts { get { return true; } }

		public override void CopyTo(NinjaScript ninjaScript)
		{
			base.CopyTo(ninjaScript);
			BetterChannel tc = ninjaScript as BetterChannel;
			if (tc != null)
				tc.isReadyForMovingSecondLeg = isReadyForMovingSecondLeg;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (areaDeviceBrush != null)
				areaDeviceBrush.RenderTarget = null;

			if (fillLeftGeometry != null)
				fillLeftGeometry.Dispose();
			if (fillMainGeometry != null)
				fillMainGeometry.Dispose();
			if (fillRightGeometry != null)
				fillRightGeometry.Dispose();
		}

		protected override void OnStateChange()
		{
			switch (State)
			{
				case State.SetDefaults:
					Description						= @"Better Channel";
					Name							= "Better Channel";
					DrawingState					= DrawingState.Building;
					TrendStartAnchor				= new ChartAnchor { IsEditing = true, DrawingTool = this, IsBrowsable = true, DisplayName = Custom.Resource.NinjaScriptDrawingToolTrendChannelStart1AnchorDisplayName };
					TrendEndAnchor					= new ChartAnchor { IsEditing = true, DrawingTool = this, IsBrowsable = true, DisplayName = Custom.Resource.NinjaScriptDrawingToolTrendChannelEnd1AnchorDisplayName };
					ParallelMidAnchor				= new ChartAnchor { IsEditing = true, DrawingTool = this, IsBrowsable = true, DisplayName = Custom.Resource.NinjaScriptDrawingToolTrendChannelStart2AnchorDisplayName, Time = DateTime.MinValue };
					ParallelStroke					= new Stroke(Brushes.CornflowerBlue, DashStyleHelper.Solid, 2f, 60);
					Stroke							= new Stroke(Brushes.CornflowerBlue, DashStyleHelper.Solid, 2f, 60);
					AreaBrush						= Brushes.CornflowerBlue;
					AreaOpacity						= 4;
					PriceLevelOpacity				= 5;
					//IgnoresSnapping					= true;
					break;
				case State.Configure:
				{
					if (PriceLevels.Count == 0)
					{
						PriceLevels.Add(new PriceLevel(0,	Brushes.Transparent));
						PriceLevels.Add(new PriceLevel(50,	Brushes.Gray));
						PriceLevels.Add(new PriceLevel(100,	Brushes.Transparent));
					}
					break;
				}
				case State.Terminated:
					Dispose();
					break;
			}
		}

		private void ChartControl_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
			// When we close, we need to remove every item that we might have created if it exists.
           if(myChartControl.ContextMenu.Items.Contains(myMenuItem1)) 
		   {
				myChartControl.ContextMenu.Items.Remove(myMenuItem1);
		   }
           if(myChartControl.ContextMenu.Items.Contains(myMenuItem2)) myChartControl.ContextMenu.Items.Remove(myMenuItem2);
           if(myChartControl.ContextMenu.Items.Contains(myMenuItem3)) myChartControl.ContextMenu.Items.Remove(myMenuItem3);
           if(myChartControl.ContextMenu.Items.Contains(myMenuItem4)) myChartControl.ContextMenu.Items.Remove(myMenuItem4);
           if(myChartControl.ContextMenu.Items.Contains(myMenuItem5)) myChartControl.ContextMenu.Items.Remove(myMenuItem5);
        }

        private void ChartControl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
			if (!this.IsSelected)
				return;
			
			if (myChartControl.ContextMenu.Items.Contains(myMenuItem1) == false)
            	myChartControl.ContextMenu.Items.Add(myMenuItem1);
			if (myChartControl.ContextMenu.Items.Contains(myMenuItem2) == false)
				myChartControl.ContextMenu.Items.Add(myMenuItem2);
			if (myChartControl.ContextMenu.Items.Contains(myMenuItem3) == false)
				myChartControl.ContextMenu.Items.Add(myMenuItem3);
			if (myChartControl.ContextMenu.Items.Contains(myMenuItem4) == false)
				myChartControl.ContextMenu.Items.Add(myMenuItem4);
			if (myChartControl.ContextMenu.Items.Contains(myMenuItem5) == false)
				myChartControl.ContextMenu.Items.Add(myMenuItem5);
        }
		
        private void MyMenuItem1_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.CornflowerBlue;

			//string filePath = Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "templates","DrawingTool","BetterChannel");
			//Print("file path " + filePath);
			//XmlDocument doc = new XmlDocument();
			//doc.Load(filePath);
			//,XmlNodeList nodeList = doc.SelectNodes("/NinjaTrader/AndrewsPitchfork/CalculationMethod");
			//Print(NinjaTrader.Core.Globals.UserDataDir + @"templates\DrawingTool\BetterChannel");

			//string TemplateDirectory = NinjaTrader.Core.Globals.UserDataDir + @"templates\DrawingTool\BetterChannel";

			//string[] fileArray = Directory.GetFiles(TemplateDirectory)
			//					.Select(fileName => Path.GetFileNameWithoutExtension(fileName))
			//					.ToArray();

			//Print(fileArray);

			//foreach (string file in fileArray) 
			//{
			//	Print(file);
			//}
			
			//foreach (XmlNode node in nodeList)
			//{
			//  Print("Node Value " + node.InnerText);
			//}
        }

        private void MyMenuItem2_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.Red;
			
        }

		private void MyMenuItem3_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.Green;
        }

		private void MyMenuItem4_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.Orange;
			AreaOpacity = 6;
        }

		private void MyMenuItem5_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.DarkViolet;
        }

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			if (PriceLevels == null || PriceLevels.Count == 0)
				yield break;
			
			foreach (PriceLevel trendLevel in PriceLevels)
			{
				yield return new AlertConditionItem
				{
					Name					= trendLevel.Name,
					ShouldOnlyDisplayName	= true,
					// stuff our actual price level in the tag so we can easily find it in the alert callback
					Tag						= trendLevel,
				};
			}
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			switch (DrawingState)
			{
				case DrawingState.Building	: return Cursors.Pen;
				case DrawingState.Moving	: return IsLocked ? Cursors.No : Cursors.SizeAll;
				case DrawingState.Editing	:
					if (editingAnchor == null)
						return null;
					return IsLocked ? Cursors.No : editingAnchor == TrendStartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
				default:

					Point startAnchorPixelPoint		= TrendStartAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point midParAnchorPoint			= ParallelMidAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point endAnchorPixelPoint		= TrendEndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point startAnchor2PixelPoint	= new Point(midParAnchorPoint.X - ((endAnchorPixelPoint.X - startAnchorPixelPoint.X)/2), midParAnchorPoint.Y - (endAnchorPixelPoint.Y - startAnchorPixelPoint.Y) / 2);

					ChartAnchor closest				= GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);

					if (closest != null)
					{
						return IsLocked ? Cursors.Arrow : (closest == TrendStartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE);
					}
					
					Point	endAnchor2PixelPoint	= startAnchor2PixelPoint + (endAnchorPixelPoint - startAnchorPixelPoint);
					Vector	totalVector				= endAnchorPixelPoint - startAnchorPixelPoint;
					Vector	totalVector2			= endAnchor2PixelPoint - startAnchor2PixelPoint;
					Point	maxPoint				= GetExtendedPoint(startAnchorPixelPoint, endAnchorPixelPoint);
					Point	maxPoint2				= GetExtendedPoint(startAnchor2PixelPoint, endAnchor2PixelPoint);
					Point	minPoint				= GetExtendedPoint(endAnchorPixelPoint, startAnchorPixelPoint);
					Point	minPoint2				= GetExtendedPoint(endAnchor2PixelPoint, startAnchor2PixelPoint);

					if (IsExtendedLinesLeft)
					{
						Vector vectorLeft	= minPoint - startAnchorPixelPoint;
						Vector vector2Left	= minPoint2 - startAnchor2PixelPoint;
						if (MathHelper.IsPointAlongVector(point, startAnchorPixelPoint, vectorLeft, cursorSensitivity) ||
							MathHelper.IsPointAlongVector(point, startAnchor2PixelPoint, vector2Left, cursorSensitivity))
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
					}

					if (IsExtendedLinesRight)
					{
						Vector vectorRight = maxPoint - endAnchorPixelPoint;
						Vector vector2Right = maxPoint2 - endAnchor2PixelPoint;
						if (MathHelper.IsPointAlongVector(point, endAnchorPixelPoint, vectorRight, cursorSensitivity) ||
							MathHelper.IsPointAlongVector(point, endAnchor2PixelPoint, vector2Right, cursorSensitivity))
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
					}

					if (MathHelper.IsPointAlongVector(point, startAnchorPixelPoint, totalVector, cursorSensitivity) ||
						MathHelper.IsPointAlongVector(point, startAnchor2PixelPoint, totalVector2, cursorSensitivity))
						return IsLocked ? Cursors.Arrow : Cursors.SizeAll;

					return null;
			}
		}

		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel	chartPanel	= chartControl.ChartPanels[chartScale.PanelIndex];
			Point		startPoint	= TrendStartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		endPoint	= TrendEndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		midPoint	= new Point((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
			Point		mid2Point	= ParallelMidAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		start2Point	= new Point(mid2Point.X - ((endPoint.X - startPoint.X)/2), mid2Point.Y - (endPoint.Y - startPoint.Y) / 2);
			Point		end2Point	= new Point(mid2Point.X + ((endPoint.X - startPoint.X)/2), mid2Point.Y + (endPoint.Y - startPoint.Y) / 2);
			//Point		start2Point	= ParallelMidAnchor.GetPoint(chartControl, chartPanel, chartScale);
			//Point		end2Point	= start2Point + (endPoint - startPoint);
			//Point		mid2Point	= new Point((start2Point.X + end2Point.X) / 2, (start2Point.Y + end2Point.Y) / 2);

			if (DrawingState == DrawingState.Building && !isReadyForMovingSecondLeg)
				return new[] { startPoint, midPoint, endPoint };

			return new[] { startPoint, midPoint, endPoint, start2Point, mid2Point, end2Point };
		}

		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel	panel		= chartControl.ChartPanels[PanelIndex];
			Point		startPoint	= TrendStartAnchor.GetPoint(chartControl, panel, chartScale);
			Point		endPoint	= TrendEndAnchor.GetPoint(chartControl, panel, chartScale);
			
			PriceLevel trendLevel 	= conditionItem.Tag as PriceLevel;
			Vector startDir 		= trendLevel.Value / 100 * (ParallelMidAnchor.GetPoint(chartControl, panel, chartScale) - startPoint);
			Vector lineVector 		= endPoint - startPoint;
			Point newStartPoint 	= new Point(startPoint.X + startDir.X, startPoint.Y + startDir.Y);
			Point newEndPoint 		= new Point(newStartPoint.X + lineVector.X, newStartPoint.Y + lineVector.Y);
			
			double firstBarX		= chartControl.GetXByTime(values[0].Time);
			double firstBarY		= chartScale.GetYByValue(values[0].Value);
			
			Point alertStartPoint	= newStartPoint.X <= newEndPoint.X ? newStartPoint : newEndPoint;
			Point alertEndPoint		= newEndPoint.X >= newStartPoint.X ? newEndPoint : newStartPoint;
			Point barPoint			= new Point(firstBarX, firstBarY);
			
			if (IsExtendedLinesLeft)
			{
				Point minPoint = GetExtendedPoint(alertEndPoint, alertStartPoint);
				if (minPoint.X > -1 || minPoint.Y > -1)
					alertStartPoint = minPoint;
			}

			if (IsExtendedLinesRight)
			{
				Point maxPoint = GetExtendedPoint(alertStartPoint, alertEndPoint);
				if (maxPoint.X > -1 || maxPoint.Y > -1)
					alertEndPoint = maxPoint;
			}

			if (firstBarX < alertStartPoint.X || firstBarX > alertEndPoint.X)
				return false;

			// NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
			MathHelper.PointLineLocation pointLocation = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, barPoint);
			// for vertical things, think of a vertical line rotated 90 degrees to lay flat, where it's normal vector is 'up'
			switch (condition)
			{
				case Condition.Greater		: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove;
				case Condition.GreaterEqual	: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.Less			: return pointLocation == MathHelper.PointLineLocation.RightOrBelow;
				case Condition.LessEqual	: return pointLocation == MathHelper.PointLineLocation.RightOrBelow || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.Equals		: return pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.NotEqual		: return pointLocation != MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.CrossAbove	:
				case Condition.CrossBelow	:
					Predicate<ChartAlertValue> predicate = v =>
					{
						double barX = chartControl.GetXByTime(v.Time);
						double barY = chartScale.GetYByValue(v.Value);
						Point stepBarPoint = new Point(barX, barY);
						// NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
						MathHelper.PointLineLocation ptLocation = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, stepBarPoint);
						if (condition == Condition.CrossAbove)
							return ptLocation == MathHelper.PointLineLocation.LeftOrAbove;
						return ptLocation == MathHelper.PointLineLocation.RightOrBelow;
					};
					return MathHelper.DidPredicateCross(values, predicate);
			}
			
			return false;
		}

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (Anchors.Any(a => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart))
				return true;

			ChartPanel	panel		= chartControl.ChartPanels[chartScale.PanelIndex];
			Point		startPoint	= TrendStartAnchor.GetPoint(chartControl, panel, chartScale);
			Point		endPoint	= TrendEndAnchor.GetPoint(chartControl, panel, chartScale);
			Point		startPoint2	= ParallelMidAnchor.GetPoint(chartControl, panel, chartScale);
			Point		endPoint2	= startPoint2 + (endPoint - startPoint);

			Point		maxPoint	= GetExtendedPoint(startPoint, endPoint);
			Point		maxPoint2	= GetExtendedPoint(startPoint2, endPoint2);
			Point		minPoint	= GetExtendedPoint(endPoint, startPoint);
			Point		minPoint2	= GetExtendedPoint(endPoint2, startPoint2);
			Point[]		points		= { maxPoint, maxPoint2, minPoint, minPoint2 };
			double		minX		= points.Select(p => p.X).Min();
			double		maxX		= points.Select(p => p.X).Max();

			DateTime	minTime		= chartControl.GetTimeByX((int)minX);
			DateTime	startTime	= chartControl.GetTimeByX((int)startPoint.X);
			DateTime	endTime		= chartControl.GetTimeByX((int)endPoint.X);
			DateTime	maxTime		= chartControl.GetTimeByX((int)maxX);

			// first check if any anchor is in visible range
			foreach (DateTime time in new[] { minTime, startTime, endTime, maxTime })
				if (time >= firstTimeOnChart && time <= lastTimeOnChart)
					return true;

			// check crossthrough and keep in mind the anchors could be 'backwards' 
			if ((minTime <= firstTimeOnChart && maxTime >= lastTimeOnChart) || (startTime <= firstTimeOnChart && endTime >= lastTimeOnChart)
				|| (endTime <= firstTimeOnChart && startTime >= lastTimeOnChart))
				return true;

			return false;
		}

		private ChartAnchor GetMidPointMove(ChartAnchor staticAnchor, ChartAnchor initialAnchor, ChartAnchor initialMidAnchor, ChartAnchor movedAnchor, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			Point   staticPoint  	= staticAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point   initPoint  		= initialAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point   initMidParal  	= initialMidAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point   movedPoint  	= movedAnchor.GetPoint(chartControl, chartPanel, chartScale);

			Point trendMidPoint 	= new Point((staticPoint.X + initPoint.X)/2, (staticPoint.Y + initPoint.Y)/2);
			Point newTrendMidPoint  = new Point((staticPoint.X + movedPoint.X)/2, (staticPoint.Y + movedPoint.Y)/2);

			Point paralDifference 	= new Point(initMidParal.X + (newTrendMidPoint.X - trendMidPoint.X), initMidParal.Y + (newTrendMidPoint.Y - trendMidPoint.Y));

			ChartAnchor	ret			= new ChartAnchor();
			ret.UpdateFromPoint(paralDifference, chartControl, chartScale);

			return ret;
		}

		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!IsVisible)
				return;

			if (Anchors.Any(a => !a.IsEditing))
				foreach (ChartAnchor anchor in Anchors)
				{
					MinValue = Math.Min(anchor.Price, MinValue);
					MaxValue = Math.Max(anchor.Price, MaxValue);
				}
		}

		public override void OnEdited(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, DrawingTool oldinstance)
		{
			// if user edits anchors, we need to update our parallel end
			SetParallelLine(chartControl, false);
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:
					if (TrendStartAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(TrendStartAnchor);
						dataPoint.CopyDataValues(TrendEndAnchor);
						TrendStartAnchor.IsEditing = false;
					}
					else if (TrendEndAnchor.IsEditing)
					{
						Anchor45(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(TrendEndAnchor);
						//dataPoint.CopyDataValues(TrendEndAnchor);
						TrendEndAnchor.IsEditing = false;
					}

					if (!TrendStartAnchor.IsEditing && !TrendEndAnchor.IsEditing)
						SetParallelLine(chartControl, ParallelMidAnchor.IsEditing);

					if (!isReadyForMovingSecondLeg)
					{
						// if we just plopped second line, move it. if we just finished moving it, we're done with initial building
						if (!ParallelMidAnchor.IsEditing)
							isReadyForMovingSecondLeg = true;
					}
					else 
					{
						isReadyForMovingSecondLeg	= false;
						DrawingState				= DrawingState.Normal;
						IsSelected					= false;
					}
					break;
				case DrawingState.Normal:
				case DrawingState.Moving:
					Point point		= dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					editingAnchor	= GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);

					if (editingAnchor != null)
					{
						editingAnchor.IsEditing = true;
						DrawingState			= DrawingState.Editing;
					}
					else if (editingAnchor == null || IsLocked)
					{
						if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
							IsSelected = false;
						else
							DrawingState = DrawingState.Moving;
					}
					break;
			}
		}

		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (IsLocked && DrawingState != DrawingState.Building)
				return;

			if (DrawingState == DrawingState.Building)
			{
				if (TrendEndAnchor.IsEditing)
					Anchor45(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(TrendEndAnchor);
					//dataPoint.CopyDataValues(TrendEndAnchor);
				else if (isReadyForMovingSecondLeg)
				{
					ParallelMidAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
				}
			}
			else if (DrawingState == DrawingState.Editing)
			{
				if (!TrendStartAnchor.IsEditing && !ParallelMidAnchor.IsEditing && TrendEndAnchor.IsEditing)
				{
					TrendEndAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);

					// Move the parallel mid anchor
					//Point	initPoint		= InitialMouseDownAnchor.GetPoint(chartControl, chartPanel, chartScale);
					//Point	moveToPoint		= dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					//Point   startPoint  	= TrendStartAnchor.GetPoint(chartControl, chartPanel, chartScale);
					//Point 	paralPoint		= ParallelMidAnchor.GetPoint(chartControl, chartPanel, chartScale); 

					//Point trendMidPoint 	= new Point((startPoint.X + initPoint.X)/2, (startPoint.Y + initPoint.Y)/2);
					//Point newTrendMidPoint  = new Point((startPoint.X + moveToPoint.X)/2, (startPoint.Y + moveToPoint.Y)/2);

					//Point paralDifference 	= new Point(paralPoint.X + (newTrendMidPoint.X - trendMidPoint.X), paralPoint.Y + (newTrendMidPoint.Y - trendMidPoint.Y));

					//ChartAnchor	ret			= new ChartAnchor();
					//ret.UpdateFromPoint(paralDifference, chartControl, chartScale);

					ChartAnchor ret = GetMidPointMove(TrendStartAnchor, InitialMouseDownAnchor, ParallelMidAnchor, dataPoint, chartControl, chartPanel, chartScale);
					ParallelMidAnchor.MoveAnchor(ParallelMidAnchor, ret, chartControl, chartPanel, chartScale, this);
				}

				if (!TrendEndAnchor.IsEditing && !ParallelMidAnchor.IsEditing && TrendStartAnchor.IsEditing)
				{
					TrendStartAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);

					// Move the parallel mid anchor
					//Point	initPoint		= InitialMouseDownAnchor.GetPoint(chartControl, chartPanel, chartScale);
					//Point	moveToPoint		= dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					//Point   startPoint  	= TrendEndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					//Point 	paralPoint		= ParallelMidAnchor.GetPoint(chartControl, chartPanel, chartScale); 

					//Point trendMidPoint 	= new Point((startPoint.X + initPoint.X)/2, (startPoint.Y + initPoint.Y)/2);
					//Point newTrendMidPoint  = new Point((startPoint.X + moveToPoint.X)/2, (startPoint.Y + moveToPoint.Y)/2);

					//Point paralDifference 	= new Point(paralPoint.X + (newTrendMidPoint.X - trendMidPoint.X), paralPoint.Y + (newTrendMidPoint.Y - trendMidPoint.Y));

					//ChartAnchor	ret			= new ChartAnchor();
					//ret.UpdateFromPoint(paralDifference, chartControl, chartScale);

					ChartAnchor ret = GetMidPointMove(TrendEndAnchor, InitialMouseDownAnchor, ParallelMidAnchor, dataPoint, chartControl, chartPanel, chartScale);

					ParallelMidAnchor.MoveAnchor(ParallelMidAnchor, ret, chartControl, chartPanel, chartScale, this);
				}



				if (!TrendStartAnchor.IsEditing && !TrendEndAnchor.IsEditing && ParallelMidAnchor.IsEditing)
				{
					ParallelMidAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
				}

				if (!TrendStartAnchor.IsEditing && !ParallelMidAnchor.IsEditing && !TrendEndAnchor.IsEditing)
					DrawingState = DrawingState.Moving;
			}
			else if (DrawingState == DrawingState.Moving)
			{
				foreach (ChartAnchor anchor in new[] { TrendStartAnchor, TrendEndAnchor })
					anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
				// the anchor was adjusted with MoveAnchor in drawing state building so we need to clear here and treat as a fresh move
				ParallelMidAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
			}
		}

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
				return;

			if (DrawingState == DrawingState.Editing && updateEndAnc)
				updateEndAnc = false;

			if (editingAnchor != null)
				editingAnchor.IsEditing = false;

			editingAnchor = null;

			DrawingState = DrawingState.Normal;
		}

		/// <summary>
		/// If either Shift key is engaged, returns a new ChartAnchor of the endAnchor param adjusted to the nearest 45 degree multiple
		/// </summary>
		private ChartAnchor Anchor45(ChartAnchor startAnchor, ChartAnchor endAnchor, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
				return endAnchor;

			Point	startPoint	= startAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point	endPoint	= endAnchor.GetPoint(chartControl, chartPanel, chartScale);

			double	diffX		= endPoint.X - startPoint.X;
			double	diffY		= endPoint.Y - startPoint.Y;

			double	length		= Math.Sqrt(diffX * diffX + diffY * diffY);

			double	angle		= Math.Atan2(diffY, diffX);

			double step			= Math.PI / 8;
			double targetAngle	= 0;

			if (angle > Math.PI - step || angle < -Math.PI + step)	targetAngle = Math.PI;
			else if (angle > Math.PI - step * 3)					targetAngle = Math.PI - step * 2;
			else if (angle > Math.PI - step * 5)					targetAngle = Math.PI - step * 4;
			else if (angle > Math.PI - step * 7)					targetAngle = Math.PI - step * 6;
			else if (angle < -Math.PI + step * 3)					targetAngle = -Math.PI + step * 2;
			else if (angle < -Math.PI + step * 5)					targetAngle = -Math.PI + step * 4;
			else if (angle < -Math.PI + step * 7)					targetAngle = -Math.PI + step * 6;

			Point		targetPoint = new Point(startPoint.X + Math.Cos(targetAngle) * length, startPoint.Y + Math.Sin(targetAngle) * length);
			ChartAnchor	ret			= new ChartAnchor();

			ret.UpdateFromPoint(targetPoint, chartControl, chartScale);

			if (startPoint.X == targetPoint.X)
			{
				ret.Time		= startAnchor.Time;
				ret.SlotIndex	=startAnchor.SlotIndex;
			}
			else if (startPoint.Y == targetPoint.Y)
				ret.Price = startAnchor.Price;

			return ret;
		}

		private void AddMenuHandlers(ChartControl chartControl)
		{
			if (chartControl == null)
				return;
			
			myChartControl = chartControl;
			
			myChartControl.ContextMenuOpening += ChartControl_ContextMenuOpening;
            myChartControl.ContextMenuClosing += ChartControl_ContextMenuClosing;
			
			myMenuItem1 = new MenuItem { Header = "Blue" };
			myMenuItem2 = new MenuItem { Header = "Red" };
			myMenuItem3 = new MenuItem { Header = "Green" };
			myMenuItem4 = new MenuItem { Header = "Orange" };
			myMenuItem5 = new MenuItem { Header = "Violet" };
			
			myMenuItem1.Click += MyMenuItem1_Click;
			myMenuItem2.Click += MyMenuItem2_Click;
			myMenuItem3.Click += MyMenuItem3_Click;
			myMenuItem4.Click += MyMenuItem4_Click;
			myMenuItem5.Click += MyMenuItem5_Click;
		}
		
		private void RemoveMenuHandlers()
		{
			myMenuItem1.Click -= MyMenuItem1_Click;
            myMenuItem2.Click -= MyMenuItem2_Click;
            myMenuItem3.Click -= MyMenuItem3_Click;
            myMenuItem4.Click -= MyMenuItem4_Click;
            myMenuItem5.Click -= MyMenuItem5_Click;
			
            myChartControl.ContextMenuOpening -= ChartControl_ContextMenuOpening;
            myChartControl.ContextMenuClosing -= ChartControl_ContextMenuClosing;
			
			if(myChartControl.ContextMenu.Items.Contains(myMenuItem1))
            {
                myMenuItem1.Click -= MyMenuItem1_Click;
                myChartControl.ContextMenu.Items.Remove(myMenuItem1);
            }
            
            if(myChartControl.ContextMenu.Items.Contains(myMenuItem2))
            {
                myMenuItem2.Click -= MyMenuItem2_Click;
                myChartControl.ContextMenu.Items.Remove(myMenuItem2);
            }

            if(myChartControl.ContextMenu.Items.Contains(myMenuItem2))
            {
                myMenuItem3.Click -= MyMenuItem3_Click;
                myChartControl.ContextMenu.Items.Remove(myMenuItem3);
            }

            if(myChartControl.ContextMenu.Items.Contains(myMenuItem4))
            {
                myMenuItem4.Click -= MyMenuItem4_Click;
                myChartControl.ContextMenu.Items.Remove(myMenuItem4);
            }

            if(myChartControl.ContextMenu.Items.Contains(myMenuItem5))
            {
                myMenuItem5.Click -= MyMenuItem5_Click;
                myChartControl.ContextMenu.Items.Remove(myMenuItem5);
            }
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (myChartControl == null)
				AddMenuHandlers(chartControl);

			Stroke.RenderTarget				= RenderTarget;
			ParallelStroke.RenderTarget		= RenderTarget;
			RenderTarget.AntialiasMode		= SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			
			if (!IsInHitTest && AreaBrush != null)
			{
				if (areaDeviceBrush.Brush == null)
				{
					Brush brushCopy			= areaBrush.Clone();
					brushCopy.Opacity		= areaOpacity / 100d; 
					areaDeviceBrush.Brush	= brushCopy;
				}
				areaDeviceBrush.RenderTarget	= RenderTarget;
			}
			else 
			{
				areaDeviceBrush.RenderTarget	= null;
				areaDeviceBrush.Brush			= null;
			}
			
			ChartPanel panel			= chartControl.ChartPanels[chartScale.PanelIndex];

			Point startPoint			= TrendStartAnchor.GetPoint(chartControl, panel, chartScale);
			Point endPoint				= TrendEndAnchor.GetPoint(chartControl, panel, chartScale);

			Point midPoint2				= ParallelMidAnchor.GetPoint(chartControl, panel, chartScale);
			Point startPoint2			= new Point(midPoint2.X - ((endPoint.X - startPoint.X)/2), midPoint2.Y - (endPoint.Y - startPoint.Y) / 2);
			Point endPoint2				= startPoint2 + (endPoint - startPoint);

			SharpDX.Vector2 startVec	= startPoint.ToVector2();
			SharpDX.Vector2 endVec		= endPoint.ToVector2();
			SharpDX.Vector2 startVec2	= startPoint2.ToVector2();
			SharpDX.Vector2 endVec2		= endPoint2.ToVector2();

			Point maxPoint				= GetExtendedPoint(startPoint, endPoint);
			Point maxPoint2				= ParallelMidAnchor.Time > DateTime.MinValue ? GetExtendedPoint(startPoint2, endPoint2) : new Point(-1, -1);
			Point minPoint				= GetExtendedPoint(endPoint, startPoint);
			Point minPoint2				= ParallelMidAnchor.Time > DateTime.MinValue ? GetExtendedPoint(endPoint2, startPoint2) : new Point(-1, -1);

			SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : Stroke.BrushDX;
			RenderTarget.DrawLine(startVec, endVec, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
			
			if (DrawingState == DrawingState.Building && !isReadyForMovingSecondLeg)
				return;
			
			tmpBrush = IsInHitTest ? chartControl.SelectionBrush : ParallelStroke.BrushDX;
			RenderTarget.DrawLine(startVec2, endVec2, tmpBrush, ParallelStroke.Width, ParallelStroke.StrokeStyle);

			fillMainFig			= new SharpDX.Vector2[4];
			fillMainFig[0]		= startPoint2.ToVector2();
			fillMainFig[1]		= endPoint2.ToVector2();
			fillMainFig[2]		= endPoint.ToVector2();
			fillMainFig[3]		= startPoint.ToVector2();
			fillMainGeometry	= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

			SharpDX.Direct2D1.GeometrySink geometrySinkMain = fillMainGeometry.Open();

			geometrySinkMain.BeginFigure(new SharpDX.Vector2((float)startPoint.X, (float)startPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
			geometrySinkMain.AddLines(fillMainFig);
			geometrySinkMain.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
			geometrySinkMain.Close();

			if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
				RenderTarget.FillGeometry(fillMainGeometry, areaDeviceBrush.BrushDX);

			if (IsExtendedLinesLeft)
			{
				if (minPoint.X > -1 || minPoint.Y > -1)
					RenderTarget.DrawLine(startVec, minPoint.ToVector2(), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
				if (minPoint2.X > -1 || minPoint2.Y > -1)
					RenderTarget.DrawLine(startVec2, minPoint2.ToVector2(), ParallelStroke.BrushDX, ParallelStroke.Width, ParallelStroke.StrokeStyle);

				if (minPoint2.Y > 0 && minPoint2.X < ChartPanel.X && minPoint2.Y < ChartPanel.H + ChartPanel.Y && minPoint.X > ChartPanel.X && minPoint.Y > ChartPanel.H + ChartPanel.Y
					|| minPoint.Y > 0 && minPoint.X < ChartPanel.X && minPoint.Y < ChartPanel.H + ChartPanel.Y && minPoint2.X > ChartPanel.X && minPoint2.Y > ChartPanel.H + ChartPanel.Y)
				{
					Point extLowLeftPoint	= new Point(ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					fillLeftFig				= new SharpDX.Vector2[5];
					fillLeftFig[0]			= startPoint2.ToVector2();
					fillLeftFig[1]			= minPoint2.ToVector2();
					fillLeftFig[2]			= extLowLeftPoint.ToVector2();
					fillLeftFig[3]			= minPoint.ToVector2();
					fillLeftFig[4]			= startPoint.ToVector2();
					
					if (fillLeftGeometry != null)
						fillLeftGeometry.Dispose();
					
					fillLeftGeometry	= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkLeft = fillLeftGeometry.Open();
					geometrySinkLeft.BeginFigure(new SharpDX.Vector2((float)startPoint.X, (float)startPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkLeft.AddLines(fillLeftFig);
					geometrySinkLeft.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkLeft.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
				else if (minPoint2.X > ChartPanel.X && minPoint2.Y < ChartPanel.Y && minPoint.X < ChartPanel.X && minPoint.Y < ChartPanel.H + ChartPanel.Y
						|| minPoint.X > ChartPanel.X && minPoint.Y < ChartPanel.Y && minPoint2.X < ChartPanel.X && minPoint2.Y < ChartPanel.H + ChartPanel.Y)
				{
					Point extUppLeftPoint	= new Point(ChartPanel.X, ChartPanel.Y);
					fillLeftFig				= new SharpDX.Vector2[5];
					fillLeftFig[0]			= startPoint2.ToVector2();
					fillLeftFig[1]			= minPoint2.ToVector2();
					fillLeftFig[2]			= extUppLeftPoint.ToVector2();
					fillLeftFig[3]			= minPoint.ToVector2();
					fillLeftFig[4]			= startPoint.ToVector2();
					
					if (fillLeftGeometry != null)
						fillLeftGeometry.Dispose();
					
					fillLeftGeometry	= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkLeft = fillLeftGeometry.Open();
					geometrySinkLeft.BeginFigure(new SharpDX.Vector2((float)startPoint.X, (float)startPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkLeft.AddLines(fillLeftFig);
					geometrySinkLeft.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkLeft.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
				else if (minPoint2.X < ChartPanel.W + ChartPanel.X && minPoint2.Y < ChartPanel.Y && minPoint.X > ChartPanel.W + ChartPanel.X && minPoint.Y < ChartPanel.H + ChartPanel.Y
						|| minPoint.X < ChartPanel.W + ChartPanel.X && minPoint.Y < ChartPanel.Y && minPoint2.X > ChartPanel.W + ChartPanel.X && minPoint2.Y < ChartPanel.H + ChartPanel.Y)
				{
					Point extUppRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.Y);
					fillLeftFig				= new SharpDX.Vector2[5];
					fillLeftFig[0]			= startPoint2.ToVector2();
					fillLeftFig[1]			= minPoint2.ToVector2();
					fillLeftFig[2]			= extUppRightPoint.ToVector2();
					fillLeftFig[3]			= minPoint.ToVector2();
					fillLeftFig[4]			= startPoint.ToVector2();
					
					if (fillLeftGeometry != null)
						fillLeftGeometry.Dispose();
					
					fillLeftGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkLeft = fillLeftGeometry.Open();
					geometrySinkLeft.BeginFigure(new SharpDX.Vector2((float)startPoint.X, (float)startPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkLeft.AddLines(fillLeftFig);
					geometrySinkLeft.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkLeft.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
				else if (minPoint2.Y > 0 && minPoint2.X > ChartPanel.W + ChartPanel.X && minPoint2.Y < ChartPanel.H + ChartPanel.Y && minPoint.X < ChartPanel.W + ChartPanel.X && minPoint.Y > ChartPanel.H + ChartPanel.Y
						|| minPoint.Y > 0 && minPoint.X > ChartPanel.W + ChartPanel.X && minPoint.Y < ChartPanel.H + ChartPanel.Y && minPoint2.X < ChartPanel.W + ChartPanel.X && minPoint2.Y > ChartPanel.H + ChartPanel.Y)
				{
					Point extLowRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					fillLeftFig				= new SharpDX.Vector2[5];
					fillLeftFig[0]			= startPoint2.ToVector2();
					fillLeftFig[1]			= minPoint2.ToVector2();
					fillLeftFig[2]			= extLowRightPoint.ToVector2();
					fillLeftFig[3]			= minPoint.ToVector2();
					fillLeftFig[4]			= startPoint.ToVector2();
					
					if (fillLeftGeometry != null)
						fillLeftGeometry.Dispose();
					
					fillLeftGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkLeft = fillLeftGeometry.Open();
					geometrySinkLeft.BeginFigure(new SharpDX.Vector2((float)startPoint.X, (float)startPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkLeft.AddLines(fillLeftFig);
					geometrySinkLeft.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkLeft.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
				else
				{
					Point extUppLeftPoint	= new Point(ChartPanel.X, ChartPanel.Y);
					Point extUppRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.Y);
					Point extLowLeftPoint	= new Point(ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					Point extLowRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					fillLeftFig				= new SharpDX.Vector2[4];
					fillLeftFig[0]			= startPoint2.ToVector2();
					
					if (startPoint.Y < endPoint.Y && startPoint.X < endPoint.X && endPoint2.Y > (ChartPanel.Y + ChartPanel.H) && startPoint2.X < ChartPanel.X)
						fillLeftFig[1]		= extUppLeftPoint.ToVector2();
					else if (startPoint.Y < endPoint.Y && startPoint.X > endPoint.X && endPoint2.Y > (ChartPanel.Y + ChartPanel.H) && startPoint2.X > (ChartPanel.X + ChartPanel.W))
						fillLeftFig[1]		= extUppRightPoint.ToVector2();
					else if (startPoint.Y > endPoint.Y && startPoint.X < endPoint.X && endPoint2.Y < ChartPanel.Y && startPoint2.X < ChartPanel.X)
						fillLeftFig[1]		= extLowLeftPoint.ToVector2();
					else if (startPoint.Y > endPoint.Y && startPoint.X > endPoint.X && endPoint2.Y < ChartPanel.Y && startPoint2.X > (ChartPanel.X +ChartPanel.W))
						fillLeftFig[1]		= extLowRightPoint.ToVector2();
					else
						fillLeftFig[1]		= minPoint2.ToVector2();
					
					if (startPoint.Y < endPoint.Y && startPoint.X < endPoint.X && endPoint.Y > (ChartPanel.Y + ChartPanel.H) && startPoint.X < ChartPanel.X)
						fillLeftFig[2]		= extUppLeftPoint.ToVector2();
					else if (startPoint.Y < endPoint.Y && startPoint.X > endPoint.X && endPoint.Y > (ChartPanel.Y + ChartPanel.H) && startPoint.X > (ChartPanel.X + ChartPanel.W))
						fillLeftFig[2]		= extUppRightPoint.ToVector2();
					else if (startPoint.Y > endPoint.Y && startPoint.X < endPoint.X && endPoint.Y < 0 && startPoint.X < ChartPanel.X)
						fillLeftFig[2]		= extLowLeftPoint.ToVector2();
					else if (startPoint.Y > endPoint.Y && startPoint.X > endPoint.X && endPoint.Y < ChartPanel.Y && startPoint.X > (ChartPanel.X + ChartPanel.W))
						fillLeftFig[2]		= extLowRightPoint.ToVector2();
					else
						fillLeftFig[2]		= minPoint.ToVector2();
					
					fillLeftFig[3]			= startPoint.ToVector2();
					
					if (fillLeftGeometry != null)
						fillLeftGeometry.Dispose();
					
					fillLeftGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkLeft = fillLeftGeometry.Open();
					geometrySinkLeft.BeginFigure(new SharpDX.Vector2((float)startPoint.X, (float)startPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkLeft.AddLines(fillLeftFig);
					geometrySinkLeft.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkLeft.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}

			if (IsExtendedLinesRight)
			{
				if (maxPoint.X > -1 || maxPoint.Y > -1)
					RenderTarget.DrawLine(endVec, maxPoint.ToVector2(), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
				if (maxPoint2.X > -1 || maxPoint2.Y > -1)
					RenderTarget.DrawLine(endVec2, maxPoint2.ToVector2(), ParallelStroke.BrushDX, ParallelStroke.Width, ParallelStroke.StrokeStyle);

				if (maxPoint2.Y > 0 && maxPoint2.X < ChartPanel.X && maxPoint2.Y < ChartPanel.H + ChartPanel.Y && maxPoint.X > ChartPanel.X && maxPoint.Y > ChartPanel.H + ChartPanel.Y
					|| maxPoint.Y > 0 && maxPoint.X < ChartPanel.X && maxPoint.Y < ChartPanel.H + ChartPanel.Y && maxPoint2.X > ChartPanel.X && maxPoint2.Y > ChartPanel.H + ChartPanel.Y)
				{
					Point extLowLeftPoint	= new Point(ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					fillRightFig		= new SharpDX.Vector2[5];
					fillRightFig[0]		= endPoint2.ToVector2();
					fillRightFig[1]		= maxPoint2.ToVector2();
					fillRightFig[2]		= extLowLeftPoint.ToVector2();
					fillRightFig[3]		= maxPoint.ToVector2();
					fillRightFig[4]		= endPoint.ToVector2();
					
					if (fillRightGeometry != null)
						fillRightGeometry.Dispose();
					
					fillRightGeometry	= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkRight = fillRightGeometry.Open();
					geometrySinkRight.BeginFigure(new SharpDX.Vector2((float)endPoint.X, (float)endPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkRight.AddLines(fillRightFig);
					geometrySinkRight.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkRight.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillRightGeometry, areaDeviceBrush.BrushDX);
				}
				else if (maxPoint2.X > ChartPanel.X && maxPoint2.Y < ChartPanel.Y && maxPoint.X < ChartPanel.X && maxPoint.Y < ChartPanel.H + ChartPanel.Y
						|| maxPoint.X > ChartPanel.X && maxPoint.Y < ChartPanel.Y && maxPoint2.X < ChartPanel.X && maxPoint2.Y < ChartPanel.H + ChartPanel.Y)
				{
					Point extUppLeftPoint	= new Point(ChartPanel.X, ChartPanel.Y);
					fillRightFig			= new SharpDX.Vector2[5];
					fillRightFig[0]			= endPoint2.ToVector2();
					fillRightFig[1]			= maxPoint2.ToVector2();
					fillRightFig[2]			= extUppLeftPoint.ToVector2();
					fillRightFig[3]			= maxPoint.ToVector2();
					fillRightFig[4]			= endPoint.ToVector2();
					
					if (fillRightGeometry != null)
						fillRightGeometry.Dispose();
					
					fillRightGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkRight = fillRightGeometry.Open();
					geometrySinkRight.BeginFigure(new SharpDX.Vector2((float)endPoint.X, (float)endPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkRight.AddLines(fillRightFig);
					geometrySinkRight.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkRight.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillRightGeometry, areaDeviceBrush.BrushDX);
				}
				else if (maxPoint2.X < ChartPanel.W + ChartPanel.X && maxPoint2.Y < ChartPanel.Y && maxPoint.X > ChartPanel.W + ChartPanel.X && maxPoint.Y < ChartPanel.H + ChartPanel.Y
						|| maxPoint.X < ChartPanel.W + ChartPanel.X && maxPoint.Y < ChartPanel.Y && maxPoint2.X > ChartPanel.W + ChartPanel.X && maxPoint2.Y < ChartPanel.H + ChartPanel.Y)
				{
					Point extUppRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.Y);
					fillRightFig			= new SharpDX.Vector2[5];
					fillRightFig[0]			= endPoint2.ToVector2();
					fillRightFig[1]			= maxPoint2.ToVector2();
					fillRightFig[2]			= extUppRightPoint.ToVector2();
					fillRightFig[3]			= maxPoint.ToVector2();
					fillRightFig[4]			= endPoint.ToVector2();
					
					if (fillRightGeometry != null)
						fillRightGeometry.Dispose();
					
					fillRightGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkRight = fillRightGeometry.Open();
					geometrySinkRight.BeginFigure(new SharpDX.Vector2((float)endPoint.X, (float)endPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkRight.AddLines(fillRightFig);
					geometrySinkRight.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkRight.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillRightGeometry, areaDeviceBrush.BrushDX);
				}
				else if (maxPoint2.Y > 0 && maxPoint2.X > ChartPanel.W + ChartPanel.X && maxPoint2.Y < ChartPanel.H + ChartPanel.Y && maxPoint.X < ChartPanel.W + ChartPanel.X && maxPoint.Y > ChartPanel.H + ChartPanel.Y
						|| maxPoint.Y > 0 && maxPoint.X > ChartPanel.W + ChartPanel.X && maxPoint.Y < ChartPanel.H + ChartPanel.Y && maxPoint2.X < ChartPanel.W + ChartPanel.X && maxPoint2.Y > ChartPanel.H + ChartPanel.Y)
				{
					Point extLowRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					fillRightFig			= new SharpDX.Vector2[5];
					fillRightFig[0]			= endPoint2.ToVector2();
					fillRightFig[1]			= maxPoint2.ToVector2();
					fillRightFig[2]			= extLowRightPoint.ToVector2();
					fillRightFig[3]			= maxPoint.ToVector2();
					fillRightFig[4]			= endPoint.ToVector2();
					
					if (fillRightGeometry != null)
						fillRightGeometry.Dispose();
					
					fillRightGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkRight = fillRightGeometry.Open();
					geometrySinkRight.BeginFigure(new SharpDX.Vector2((float)endPoint.X, (float)endPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkRight.AddLines(fillRightFig);
					geometrySinkRight.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkRight.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillRightGeometry, areaDeviceBrush.BrushDX);
				}
				else
				{
					Point extUppRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.Y);
					Point extUppLeftPoint	= new Point(ChartPanel.X, ChartPanel.Y);
					Point extLowRightPoint	= new Point(ChartPanel.W + ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					Point extLowLeftPoint	= new Point(ChartPanel.X, ChartPanel.H + ChartPanel.Y);
					fillRightFig			= new SharpDX.Vector2[4];
					fillRightFig[0]			= endPoint2.ToVector2();
					
					if (startPoint.Y > endPoint.Y && startPoint.X < endPoint.X && endPoint2.X > (ChartPanel.X + ChartPanel.W) && startPoint2.Y > (ChartPanel.Y + ChartPanel.H))
						fillRightFig[1]		= extUppRightPoint.ToVector2();
					else if (startPoint.Y > endPoint.Y && startPoint.X > endPoint.X && endPoint2.X < ChartPanel.X && startPoint2.Y > (ChartPanel.Y + ChartPanel.H))
						fillRightFig[1]		= extUppLeftPoint.ToVector2();
					else if (startPoint.Y < endPoint.Y && startPoint.X < endPoint.X && endPoint2.X > (ChartPanel.X + ChartPanel.W) && startPoint2.Y < ChartPanel.Y)
						fillRightFig[1]		= extLowRightPoint.ToVector2();
					else if (startPoint.Y < endPoint.Y && startPoint.X > endPoint.X && endPoint2.X < ChartPanel.X && startPoint2.Y < ChartPanel.Y)
						fillRightFig[1]		= extLowLeftPoint.ToVector2();
					else
						fillRightFig[1] = maxPoint2.ToVector2();
					
					if (startPoint.Y > endPoint.Y && startPoint.X < endPoint.X && endPoint.X > (ChartPanel.X + ChartPanel.W) && startPoint.Y > (ChartPanel.Y + ChartPanel.H))
						fillRightFig[2]		= extUppRightPoint.ToVector2();
					else if (startPoint.Y > endPoint.Y && startPoint.X > endPoint.X && endPoint.X < ChartPanel.X && startPoint.Y > (ChartPanel.Y + ChartPanel.H))
						fillRightFig[2]		= extUppLeftPoint.ToVector2();
					else if (startPoint.Y < endPoint.Y && startPoint.X < endPoint.X && endPoint.X > (ChartPanel.X + ChartPanel.W) && startPoint.Y < ChartPanel.Y)
						fillRightFig[2]		= extLowRightPoint.ToVector2();
					else if (startPoint.Y < endPoint.Y && startPoint.X > endPoint.X && endPoint.X < ChartPanel.X && startPoint.Y < ChartPanel.Y)
						fillRightFig[2]		= extLowLeftPoint.ToVector2();
					else
						fillRightFig[2]		= maxPoint.ToVector2();
					
					fillRightFig[3]			= endPoint.ToVector2();
					
					if (fillRightGeometry != null)
						fillRightGeometry.Dispose();
					
					fillRightGeometry		= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);

					SharpDX.Direct2D1.GeometrySink geometrySinkRight = fillRightGeometry.Open();
					geometrySinkRight.BeginFigure(new SharpDX.Vector2((float)endPoint.X, (float)endPoint.Y), SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySinkRight.AddLines(fillRightFig);
					geometrySinkRight.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySinkRight.Close();

					if (areaDeviceBrush != null && areaDeviceBrush.RenderTarget != null && areaDeviceBrush.BrushDX != null)
						RenderTarget.FillGeometry(fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			
			SetAllPriceLevelsRenderTarget();

			foreach (PriceLevel trendLevel in PriceLevels.Where(tl => tl.IsVisible && tl.Stroke != null))
			{
				Vector startDir = trendLevel.Value / 100 * (startPoint2 - startPoint);
				Vector lineVector = endPoint - startPoint;
				Point newStartPoint = new Point(startPoint.X + startDir.X, startPoint.Y + startDir.Y);
				Point newEndPoint = new Point(newStartPoint.X + lineVector.X, newStartPoint.Y + lineVector.Y);

				RenderTarget.DrawLine(newStartPoint.ToVector2(), newEndPoint.ToVector2(), trendLevel.Stroke.BrushDX, trendLevel.Stroke.Width, trendLevel.Stroke.StrokeStyle);

				Point maxPoint3 = GetExtendedPoint(newStartPoint, newEndPoint);
				Point minPoint3 = GetExtendedPoint(newEndPoint, newStartPoint);

				if (IsExtendedLinesLeft && (minPoint3.X > -1 || minPoint3.Y > -1))
					RenderTarget.DrawLine(newStartPoint.ToVector2(), minPoint3.ToVector2(), trendLevel.Stroke.BrushDX, trendLevel.Stroke.Width, trendLevel.Stroke.StrokeStyle);

				if (IsExtendedLinesRight && (maxPoint3.X > -1 || maxPoint3.Y > -1))
					RenderTarget.DrawLine(newEndPoint.ToVector2(), maxPoint3.ToVector2(), trendLevel.Stroke.BrushDX, trendLevel.Stroke.Width, trendLevel.Stroke.StrokeStyle);
			}
		}

		private void SetParallelLine(ChartControl chartControl, bool initialSet)
		{
			// when intial set is true, user just finished their trend line, we need to initialize
			// a parallel line somewhere, copy the first line (StartAnchor -> EndAnchor) starting where user clicked
			// as second line (Start2Anchor -> End2Anchor)
			// if initial set is false, this was called from an edit, user could have edited trend line, we need to 
			// update parallel anchors to stay parallel in price

			// NOTE: use pixel values for line time conversion but time
			// can end up non-linear which would not be correct
			if (initialSet)
			{
				if (chartControl.BarSpacingType != BarSpacingType.TimeBased)
				{
					ParallelMidAnchor.SlotIndex = TrendEndAnchor.SlotIndex;
					ParallelMidAnchor.Time = chartControl.GetTimeBySlotIndex(ParallelMidAnchor.SlotIndex);
				}
				else
					ParallelMidAnchor.Time = TrendEndAnchor.Time;

				ParallelMidAnchor.Price		= TrendEndAnchor.Price;
				ParallelMidAnchor.StartAnchor = InitialMouseDownAnchor;
			}
			else 
			{
				// if user potentially edited the trend anchors, update our price
				double priceVerticalDiff	= TrendStartAnchor.Price - ParallelMidAnchor.Price;
				ParallelMidAnchor.Price		= TrendStartAnchor.Price - priceVerticalDiff;
			}
			
			ParallelMidAnchor.IsEditing		= false;
		}
	}

	// Do not break existing code
	public class BetterLevel: PriceLevel {}

	public static partial class Draw
	{
		private static BetterChannel BetterChannelCore(NinjaScriptBase owner, string tag, bool isAutoScale,
			int anchor1BarsAgo, DateTime anchor1Time, double anchor1Y,
			int anchor2BarsAgo, DateTime anchor2Time, double anchor2Y,
			int anchor3BarsAgo, DateTime anchor3Time, double anchor3Y, bool isGlobal, string templateName)
		{
			if (owner == null)
				throw new ArgumentException("owner");

			if (string.IsNullOrWhiteSpace(tag))
				throw new ArgumentException("tag cant be null or empty");

			if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
				tag = string.Format("{0}{1}", GlobalDrawingToolManager.GlobalDrawingToolTagPrefix, tag);

			BetterChannel trendChannel = DrawingTool.GetByTagOrNew(owner, typeof(BetterChannel), tag, templateName) as BetterChannel;
			if (trendChannel == null)
				return null;

			DrawingTool.SetDrawingToolCommonValues(trendChannel, tag, isAutoScale, owner, isGlobal);

			ChartAnchor		startAnchor		= DrawingTool.CreateChartAnchor(owner, anchor1BarsAgo, anchor1Time, anchor1Y);
			ChartAnchor		endAnchor		= DrawingTool.CreateChartAnchor(owner, anchor2BarsAgo, anchor2Time, anchor2Y);
			ChartAnchor		trendAnchor		= DrawingTool.CreateChartAnchor(owner, anchor3BarsAgo, anchor3Time, anchor3Y);

			startAnchor.CopyDataValues(trendChannel.TrendStartAnchor);
			endAnchor.CopyDataValues(trendChannel.TrendEndAnchor);
			trendAnchor.CopyDataValues(trendChannel.ParallelMidAnchor);
			trendChannel.SetState(State.Active);
			return trendChannel;
		}

		/// <summary>
		/// Draws a trend channel.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
		/// <param name="anchor1Y">The y value of the 1st anchor point</param>
		/// <param name="anchor2BarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
		/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
		/// <param name="anchor3BarsAgo">The number of bars ago (x value) of the 3rd anchor point</param>
		/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
		/// <returns></returns>
		public static BetterChannel BetterChannel(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y,
												int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y)
		{
			return BetterChannelCore(owner, tag, isAutoScale,
				anchor1BarsAgo, Core.Globals.MinDate, anchor1Y,
				anchor2BarsAgo, Core.Globals.MinDate, anchor2Y,
				anchor3BarsAgo, Core.Globals.MinDate, anchor3Y, false, null);
		}

		/// <summary>
		/// Draws a trend channel.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="anchor1Time">The time of the 1st anchor point</param>
		/// <param name="anchor1Y">The y value of the 1st anchor point</param>
		/// <param name="anchor2Time">The time of the 2nd anchor point</param>
		/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
		/// <param name="anchor3Time">The time of the 3rd anchor point</param>
		/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
		/// <returns></returns>
		public static BetterChannel BetterChannel(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time,
												double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y)
		{
			return BetterChannelCore(owner, tag, isAutoScale,
				int.MinValue, anchor1Time, anchor1Y,
				int.MinValue, anchor2Time, anchor2Y,
				int.MinValue, anchor3Time, anchor3Y, false, null);
		}

		/// <summary>
		/// Draws a trend channel.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
		/// <param name="anchor1Y">The y value of the 1st anchor point</param>
		/// <param name="anchor2BarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
		/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
		/// <param name="anchor3BarsAgo">The number of bars ago (x value) of the 3rd anchor point</param>
		/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static BetterChannel BetterChannel(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y,
												int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, bool isGlobal, string templateName)
		{
			return BetterChannelCore(owner, tag, isAutoScale,
				anchor1BarsAgo, Core.Globals.MinDate, anchor1Y,
				anchor2BarsAgo, Core.Globals.MinDate, anchor2Y,
				anchor3BarsAgo, Core.Globals.MinDate, anchor3Y, isGlobal, templateName);
		}

		/// <summary>
		/// Draws a trend channel.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="anchor1Time">The time of the 1st anchor point</param>
		/// <param name="anchor1Y">The y value of the 1st anchor point</param>
		/// <param name="anchor2Time">The time of the 2nd anchor point</param>
		/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
		/// <param name="anchor3Time">The time of the 3rd anchor point</param>
		/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static BetterChannel BetterChannel(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time,
												double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, bool isGlobal, string templateName)
		{
			return BetterChannelCore(owner, tag, isAutoScale,
				int.MinValue, anchor1Time, anchor1Y,
				int.MinValue, anchor2Time, anchor2Y,
				int.MinValue, anchor3Time, anchor3Y, isGlobal, templateName);
		}
	}
}