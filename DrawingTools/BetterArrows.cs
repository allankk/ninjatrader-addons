// 
// Copyright (C) 2023, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

// For context menu to work:
using System.Windows.Controls;
using NinjaTrader.NinjaScript.DrawingTools;

#endregion

namespace NinjaTrader.NinjaScript.DrawingTools
{
	public abstract class BetterChartMarker : DrawingTool
	{
		private		Brush			areaBrush;
		[CLSCompliant(false)]
		protected	DeviceBrush		areaDeviceBrush		= new DeviceBrush();
		private		Brush			outlineBrush;
		[CLSCompliant(false)]
		protected	DeviceBrush		outlineDeviceBrush	= new DeviceBrush();

		public ChartAnchor	Anchor					{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
		[XmlIgnore]
		public Brush		AreaBrush
		{ 
			get { return areaBrush; }
			set 
			{
				areaBrush = value;
				areaDeviceBrush.Brush = value;
			}
		}

		[Browsable(false)]
		public string AreaBrushSerialize
		{
			get { return Serialize.BrushToString(AreaBrush);	}
			set { AreaBrush = Serialize.StringToBrush(value);	}
		}

		protected double BarWidth
		{
			get
			{
				if (AttachedTo != null)
				{
					ChartBars chartBars = AttachedTo.ChartObject as ChartBars;
					if (chartBars == null)
					{
						Gui.NinjaScript.IChartBars iChartBars = AttachedTo.ChartObject as Gui.NinjaScript.IChartBars;
						if (iChartBars != null)
							chartBars = iChartBars.ChartBars;
					}
					if (chartBars != null && chartBars.Properties.ChartStyle != null)
						return chartBars.Properties.ChartStyle.BarWidth;
				}
				return MinimumSize;
			}
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesOutlineBrush", GroupName = "NinjaScriptGeneral", Order = 2)]
		[XmlIgnore]
		public Brush		OutlineBrush
		{
			get { return outlineBrush; }
			set 
			{
				outlineBrush = value;
				outlineDeviceBrush.Brush = value;
			}
		}

		[Browsable(false)]
		public string OutlineBrushSerialize
		{
			get { return Serialize.BrushToString(OutlineBrush);		}
			set { OutlineBrush = Serialize.StringToBrush(value);	}
		}

		public static float MinimumSize { get { return 5f; } }

		public override IEnumerable<ChartAnchor> Anchors
		{
			get { return new[]{Anchor}; }
		}

		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!this.IsVisible)
				return;


			MinValue = Anchor.Price;
			MaxValue = Anchor.Price;
		}

		protected override void Dispose(bool disposing)
		{
			areaDeviceBrush.RenderTarget	= null;
			outlineDeviceBrush.RenderTarget	= null;
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			if (DrawingState == DrawingState.Building)
				return Cursors.Pen;
			if (DrawingState == DrawingState.Moving)
				return IsLocked ? Cursors.No : Cursors.SizeAll;
			// this is fired whenever the chart marker is selected.
			// so if the mouse is anywhere near our marker, show a moving icon only. point is already in device pixels
			// we want to check at least 6 pixels away, or by padding x 2 if its more (It could be 0 on some objects like square)
			Point anchorPointPixels = Anchor.GetPoint(chartControl, chartPanel, chartScale);
			Vector distToMouse = point - anchorPointPixels;
			return distToMouse.Length <= GetSelectionSensitivity(chartControl) ?
				IsLocked ?  Cursors.Arrow : Cursors.SizeAll : 
				null;
		}

		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			if (Anchor.IsEditing)
				return new Point[0];

			ChartPanel chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
			Point anchorPoint = Anchor.GetPoint(chartControl, chartPanel, chartScale);
			return new[]{ anchorPoint };
		}

		public double GetSelectionSensitivity(ChartControl chartControl)
		{
			return Math.Max(15d, 10d * (BarWidth / 5d));
		}

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return false;
			// we have a single anchor so this is pretty easy
			if (!IsAutoScale && (Anchor.Price < chartScale.MinValue || Anchor.Price > chartScale.MaxValue))
				return false;
			return Anchor.Time >= firstTimeOnChart && Anchor.Time <= lastTimeOnChart;
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:
					dataPoint.CopyDataValues(Anchor);
					Anchor.IsEditing	= false;
					DrawingState		= DrawingState.Normal;
					IsSelected			= false;
					break;
				case DrawingState.Normal:
					// make sure they clicked near us. use GetCursor incase something has more than one point, like arrows
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					if (GetCursor(chartControl, chartPanel, chartScale, point) != null)
						DrawingState = DrawingState.Moving;
					else
						IsSelected = false;
					break;
			}
		}

		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState != DrawingState.Moving || IsLocked && DrawingState != DrawingState.Building)
				return;
			dataPoint.CopyDataValues(Anchor);
		}

		public override void OnMouseUp(ChartControl control, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Editing || DrawingState == DrawingState.Moving)
				DrawingState = DrawingState.Normal;
		}
	}

	public abstract class BetterArrowMarkerBase : BetterChartMarker
	{
        protected ChartControl myChartControl;
        protected MenuItem myMenuItem1;
        protected MenuItem myMenuItem2;
        protected MenuItem myMenuItem3;
        protected MenuItem myMenuItem4;
        protected MenuItem myMenuItem5;

		[XmlIgnore]
		[Browsable(false)]
		public bool		IsUpArrow	{ get; protected set; }

		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			if (Anchor.IsEditing)
				return new Point[0];
			ChartPanel panel			= chartControl.ChartPanels[chartScale.PanelIndex];
			Point pixelPointArrowTop	= Anchor.GetPoint(chartControl, panel, chartScale);
			return new [] { new Point(pixelPointArrowTop.X, pixelPointArrowTop.Y) };
		}

		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState != DrawingState.Moving || IsLocked)
				return;

			// this is reversed, we're pulling into arrow
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
			Anchor.UpdateFromPoint(new Point(point.X, point.Y), chartControl, chartScale);
		}

        private void ChartControl_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
			// When we close, we need to remove every item that we might have created if it exists.

            if(myChartControl.ContextMenu.Items.Contains(myMenuItem1)) 
                myChartControl.ContextMenu.Items.Remove(myMenuItem1);
            if(myChartControl.ContextMenu.Items.Contains(myMenuItem2)) 
                myChartControl.ContextMenu.Items.Remove(myMenuItem2);
            if(myChartControl.ContextMenu.Items.Contains(myMenuItem3)) 
                myChartControl.ContextMenu.Items.Remove(myMenuItem3);
            if(myChartControl.ContextMenu.Items.Contains(myMenuItem4)) 
                myChartControl.ContextMenu.Items.Remove(myMenuItem4);
            if(myChartControl.ContextMenu.Items.Contains(myMenuItem5)) 
                myChartControl.ContextMenu.Items.Remove(myMenuItem5);
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
			AreaBrush = Brushes.SeaGreen;
            OutlineBrush = Brushes.SeaGreen;
        }
        private void MyMenuItem2_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.Crimson;
            OutlineBrush = Brushes.Crimson;
        }
        private void MyMenuItem3_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.DarkGray;
            OutlineBrush = Brushes.DarkGray;
        }
        private void MyMenuItem4_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.DodgerBlue;
            OutlineBrush = Brushes.DodgerBlue;
        }
        private void MyMenuItem5_Click(object sender, RoutedEventArgs e)
        {
			AreaBrush = Brushes.Violet;
            OutlineBrush = Brushes.Violet;
        }

        private void AddMenuHandlers(ChartControl chartControl)
		{
			if (chartControl == null)
				return;
			
			myChartControl = chartControl;
			
			myChartControl.ContextMenuOpening += ChartControl_ContextMenuOpening;
            myChartControl.ContextMenuClosing += ChartControl_ContextMenuClosing;
			
			myMenuItem1 = new MenuItem { Header = "Green" };
			myMenuItem2 = new MenuItem { Header = "Red" };
			myMenuItem3 = new MenuItem { Header = "Gray" };
			myMenuItem4 = new MenuItem { Header = "Blue" };
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
			
            myChartControl.ContextMenuOpening -= ChartControl_ContextMenuOpening;
            myChartControl.ContextMenuClosing -= ChartControl_ContextMenuClosing;
			
			if(myChartControl.ContextMenu.Items.Contains(myMenuItem1))
            {
                myMenuItem1.Click -= MyMenuItem1_Click;
                myChartControl.ContextMenu.Items.Remove(myMenuItem1);
            }
        }

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{

            if (myChartControl == null)
				AddMenuHandlers(chartControl);

			if (Anchor.IsEditing)
				return;

			areaDeviceBrush.RenderTarget = RenderTarget;
			outlineDeviceBrush.RenderTarget = RenderTarget;

			ChartPanel panel			= chartControl.ChartPanels[chartScale.PanelIndex];
			Point pixelPoint			= Anchor.GetPoint(chartControl, panel, chartScale);
			SharpDX.Vector2 endVector	= pixelPoint.ToVector2();

			// the geometry is created with 0,0 as point origin, and pointing UP by default.
			// so translate & rotate as needed
			SharpDX.Matrix3x2 transformMatrix;
			if (!IsUpArrow)
			{
				// Flip it around. beware due to our translation we rotate on origin
				transformMatrix = /*SharpDX.Matrix3x2.Scaling(arrowScale, arrowScale) **/ SharpDX.Matrix3x2.Rotation(MathHelper.DegreesToRadians(180), SharpDX.Vector2.Zero) * SharpDX.Matrix3x2.Translation(endVector);
			}
			else 
				transformMatrix = /*SharpDX.Matrix3x2.Scaling(arrowScale, arrowScale) **/ SharpDX.Matrix3x2.Translation(endVector);

			RenderTarget.AntialiasMode	= SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			RenderTarget.Transform		= transformMatrix;
			
            float barWidth = 6f;
			float arrowHeight		= barWidth * 5f;
			float arrowPointHeight	= barWidth + 2f;
			float arrowStemWidth	= 1f;

			SharpDX.Direct2D1.PathGeometry arrowPathGeometry = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
			SharpDX.Direct2D1.GeometrySink geometrySink = arrowPathGeometry.Open();
			geometrySink.BeginFigure(SharpDX.Vector2.Zero, SharpDX.Direct2D1.FigureBegin.Filled);

			geometrySink.AddLine(new SharpDX.Vector2(barWidth, arrowPointHeight));
			geometrySink.AddLine(new SharpDX.Vector2(arrowStemWidth, arrowPointHeight));
			geometrySink.AddLine(new SharpDX.Vector2(arrowStemWidth, arrowHeight));
			geometrySink.AddLine(new SharpDX.Vector2(-arrowStemWidth, arrowHeight));
			geometrySink.AddLine(new SharpDX.Vector2(-arrowStemWidth, arrowPointHeight));
			geometrySink.AddLine(new SharpDX.Vector2(-barWidth, arrowPointHeight));

			geometrySink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
			geometrySink.Close(); // note this calls dispose for you. but not the other way around

			SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : areaDeviceBrush.BrushDX;
			if (tmpBrush != null)
				RenderTarget.FillGeometry(arrowPathGeometry, tmpBrush);
			tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineDeviceBrush.BrushDX;
			if (tmpBrush != null)
				RenderTarget.DrawGeometry(arrowPathGeometry, tmpBrush);
			arrowPathGeometry.Dispose();
			RenderTarget.Transform				= SharpDX.Matrix3x2.Identity;
		}
	}

	/// <summary>
	/// Represents an interface that exposes information regarding an Arrow Down IDrawingTool.
	/// </summary>
	public class BetterArrowDown : BetterArrowMarkerBase
	{
		public override object Icon { get { return Gui.Tools.Icons.DrawArrowDown; } }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Anchor	= new ChartAnchor
				{
					DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
					IsEditing	= true,
				};
				Name				= "Better Arrow Down";
				AreaBrush			= Brushes.Crimson;
				OutlineBrush		= Brushes.Crimson;
				IsUpArrow			= false;
			}
			else if (State == State.Terminated)
				Dispose();
		}


	}

	/// <summary>
	/// Represents an interface that exposes information regarding an Arrow Up IDrawingTool.
	/// </summary>
	public class BetterArrowUp : BetterArrowMarkerBase
	{
		public override object Icon { get { return Gui.Tools.Icons.DrawArrowUp; } }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Anchor	= new ChartAnchor
				{
					DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
					IsEditing	= true,
				};
				Name				= "Better Arrow Up";
				AreaBrush			= Brushes.SeaGreen;
				OutlineBrush		= Brushes.SeaGreen;
				IsUpArrow			= true;
			}
			else if (State == State.Terminated)
				Dispose();
		}
	}

	public static partial class Draw
	{
		// this function does all the actual instance creation and setup
		private static T BetterChartMarkerCore<T>(NinjaScriptBase owner, string tag, bool isAutoScale, 
										int barsAgo, DateTime time, double yVal, Brush brush, bool isGlobal, string templateName) where T : BetterChartMarker
		{
			if (owner == null)
				throw new ArgumentException("owner");
			if (time == Core.Globals.MinDate && barsAgo == int.MinValue)
				throw new ArgumentException("bad start/end date/time");
			if (yVal.ApproxCompare(double.MinValue) == 0 || yVal.ApproxCompare(double.MaxValue) == 0)
				throw new ArgumentException("bad Y value");

			if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
				tag = string.Format("{0}{1}", GlobalDrawingToolManager.GlobalDrawingToolTagPrefix, tag);

			T chartMarkerT = DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) as T;
			
			if (chartMarkerT == null)
				return default(T);

			DrawingTool.SetDrawingToolCommonValues(chartMarkerT, tag, isAutoScale, owner, isGlobal);
			
			// dont nuke existing anchor refs 

			//int				currentBar		= DrawingTool.GetCurrentBar(owner);
			//ChartControl	chartControl	= DrawingTool.GetOwnerChartControl(owner);
			//ChartBars		chartBars		= (owner as Gui.NinjaScript.IChartBars).ChartBars;

			ChartAnchor anchor = DrawingTool.CreateChartAnchor(owner, barsAgo, time, yVal);
			anchor.CopyDataValues(chartMarkerT.Anchor);

			// dont forget to set anchor as not editing or else it wont be drawn
			chartMarkerT.Anchor.IsEditing = false;

			// can be null when loaded from templateName
			if (brush != null)
				chartMarkerT.AreaBrush = brush;

			chartMarkerT.SetState(State.Active);
			return chartMarkerT;
		}

		// arrow down
		/// <summary>
		/// Draws an arrow pointing down.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <returns></returns>
		public static BetterArrowDown BetterArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
		{
			return BetterChartMarkerCore<BetterArrowDown>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
		}

		/// <summary>
		/// Draws an arrow pointing down.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="time"> The time the object will be drawn at.</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <returns></returns>
		public static BetterArrowDown BetterArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
		{
			return BetterChartMarkerCore<BetterArrowDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null);
		}

		/// <summary>
		/// Draws an arrow pointing down.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
		/// <returns></returns>
		public static BetterArrowDown BetterArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
		{
			return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
				BetterChartMarkerCore<BetterArrowDown>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null));
		}

		/// <summary>
		/// Draws an arrow pointing down.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="time"> The time the object will be drawn at.</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
		/// <returns></returns>
		public static BetterArrowDown BetterArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
		{
			return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
				BetterChartMarkerCore<BetterArrowDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null));
		}

		/// <summary>
		/// Draws an arrow pointing down.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static BetterArrowDown BetterArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
		{
			return BetterChartMarkerCore<BetterArrowDown>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, null, isGlobal, templateName);
		}

		/// <summary>
		/// Draws an arrow pointing down.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="time"> The time the object will be drawn at.</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static BetterArrowDown BetterArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
		{
			return BetterChartMarkerCore<BetterArrowDown>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
		}

		// arrow up
		/// <summary>
		/// Draws an arrow pointing up.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <returns></returns>
		public static BetterArrowUp BetterArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
		{
			return BetterChartMarkerCore<BetterArrowUp>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
		}

		/// <summary>
		/// Draws an arrow pointing up.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="time"> The time the object will be drawn at.</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <returns></returns>
		public static BetterArrowUp BetterArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
		{
			return BetterChartMarkerCore<BetterArrowUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null);
		}

		/// <summary>
		/// Draws an arrow pointing up.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
		/// <returns></returns>
		public static BetterArrowUp BetterArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
		{
			return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
				BetterChartMarkerCore<BetterArrowUp>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null));
		}

		/// <summary>
		/// Draws an arrow pointing up.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="time"> The time the object will be drawn at.</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
		/// <returns></returns>
		public static BetterArrowUp BetterArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
		{
			return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
				BetterChartMarkerCore<BetterArrowUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null));
		}

		/// <summary>
		/// Draws an arrow pointing up.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static BetterArrowUp BetterArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
		{
			return BetterChartMarkerCore<BetterArrowUp>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, null, isGlobal, templateName);
		}

		/// <summary>
		/// Draws an arrow pointing up.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="time"> The time the object will be drawn at.</param>
		/// <param name="y">The y value or Price for the object</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static BetterArrowUp BetterArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
		{
			return BetterChartMarkerCore<BetterArrowUp>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
		}
	}
}
