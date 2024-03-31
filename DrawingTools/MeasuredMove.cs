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
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
#endregion

namespace NinjaTrader.NinjaScript.DrawingTools
{
	public abstract class MeasuredLevels : PriceLevelContainer
	{
		protected	const	int 			CursorSensitivity		= 15;

		protected			ChartAnchor 	editingAnchor;
		
		[Display(ResourceType=typeof(Custom.Resource), Name = "NinjaScriptDrawingToolFibonacciLevelsBaseAnchorLineStroke", GroupName = "NinjaScriptLines", Order = 1)]
		public Stroke 		AnchorLineStroke 	{ get; set; }

		// fib tools have a start and end at very least
		[Display(Order = 1)]
		public ChartAnchor 	StartAnchor 		{ get; set; }
		[Display(Order = 2)]
		public ChartAnchor 	EndAnchor 			{ get; set; }

		public override IEnumerable<ChartAnchor>	Anchors				{ get { return new[] { StartAnchor, EndAnchor }; } }
		public override bool						SupportsAlerts		{ get { return true; } }

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			if (PriceLevels == null || PriceLevels.Count == 0)
				yield break;
			foreach (PriceLevel priceLevel in PriceLevels)
			{
				yield return new AlertConditionItem
				{
					Name					= priceLevel.Name,
					ShouldOnlyDisplayName	= true,
					// stuff our actual price level in the tag so we can easily find it in the alert callback
					Tag						= priceLevel,
				};
			}
		}
	}
	
	/// <summary>
	/// Represents an interface that exposes information regarding a Fibonacci Extensions IDrawingTool.
	/// </summary>
	//public class MeasuredMoves : FibonacciRetracements 
	public class MeasuredMoves : FibonacciRetracements 
	{


		Point anchorExtensionPoint;

		[Display(Order = 3)]
		public ChartAnchor ExtensionAnchor { get; set; }

		[Display(Order = 4)]
		public ChartAnchor AdjustAnchorLeft { get; set; }
		
		[Display(Order = 5)]
		public ChartAnchor AdjustAnchorRight { get; set; }

		//protected bool CheckAlertRetracementLine(Condition condition, Point lineStartPoint, Point lineEndPoint,
													//ChartControl chartControl, ChartScale chartScale, ChartAlertValue[] values);

		public override IEnumerable<ChartAnchor> Anchors { get { return new[] { StartAnchor, EndAnchor, ExtensionAnchor, AdjustAnchorLeft, AdjustAnchorRight }; } }
		
		protected new Tuple<Point, Point> GetPriceLevelLinePoints(PriceLevel priceLevel, ChartControl chartControl, ChartScale chartScale, bool isInverted)
		{
			ChartPanel chartPanel		= chartControl.ChartPanels[PanelIndex];
			Point anchorStartPoint 		= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point anchorEndPoint 		= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			double totalPriceRange		= EndAnchor.Price - StartAnchor.Price;
			// dont forget user could start/end draw backwards
			double anchorMinX 		= Math.Min(anchorStartPoint.X, anchorEndPoint.X);
			double anchorMaxX 		= Math.Max(anchorStartPoint.X, anchorEndPoint.X);
			double lineStartX		= IsExtendedLinesLeft ? chartPanel.X : anchorMinX;
			double lineEndX 		= IsExtendedLinesRight ? chartPanel.X + chartPanel.W : anchorMaxX;
			double levelY			= priceLevel.GetY(chartScale, ExtensionAnchor.Price, totalPriceRange, isInverted);
			return new Tuple<Point, Point>(new Point(lineStartX, levelY), new Point(lineEndX, levelY));
		}
		
		private new void DrawPriceLevelText(ChartPanel chartPanel, ChartScale chartScale, double minX, double maxX, double y, double price, PriceLevel priceLevel)
		{
			if (TextLocation == TextLocation.Off || priceLevel == null || priceLevel.Stroke == null || priceLevel.Stroke.BrushDX == null)
				return;

			// make a rectangle that sits right at our line, depending on text alignment settings
			SimpleFont wpfFont = chartPanel.ChartControl.Properties.LabelFont ?? new SimpleFont();
			SharpDX.DirectWrite.TextFormat textFormat = wpfFont.ToDirectWriteTextFormat();
			textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
			textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;

			string str = GetPriceString(price, chartPanel);

			// when using extreme alignments, give a few pixels of padding on the text so we dont end up right on the edge
			const double edgePadding = 3f;
			float layoutWidth = (float)Math.Abs(maxX - minX); // always give entire available width for layout
			// dont use max x for max text width here, that can break inside left/right when extended lines are on
			SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, str, textFormat, layoutWidth, textFormat.FontSize);

			double drawAtX = minX;

			if (IsExtendedLinesLeft && TextLocation == TextLocation.ExtremeLeft)
				drawAtX = chartPanel.X + edgePadding;
			else if (IsExtendedLinesRight && TextLocation == TextLocation.ExtremeRight)
				drawAtX = chartPanel.X + chartPanel.W - textLayout.Metrics.Width;
			else
			{
				if (TextLocation == TextLocation.InsideLeft || TextLocation == TextLocation.ExtremeLeft)
					drawAtX = minX <= maxX ? minX - 1 : maxX - 1;
				else
					drawAtX = minX > maxX ? minX - textLayout.Metrics.Width : maxX - textLayout.Metrics.Width;
			}

			// we also move our y value up by text height so we draw label above line like NT7.
			RenderTarget.DrawTextLayout(new SharpDX.Vector2((float)drawAtX, (float)(y - textFormat.FontSize - edgePadding)), textLayout, priceLevel.Stroke.BrushDX, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

			textFormat.Dispose();
			textLayout.Dispose();
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			if (DrawingState != DrawingState.Normal)
				return base.GetCursor(chartControl, chartPanel, chartScale, point);

			// draw move cursor if cursor is near line path anywhere
			Point startAnchorPixelPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);

			ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, CursorSensitivity, point);
			if (closest != null)
			{
				// show arrow until they try to move it
				if (IsLocked)
					return Cursors.Arrow;
				return closest == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
			}

			// for extensions, we want to see if the cursor along the following lines (represented as vectors):
			// start -> end, end -> ext, ext start -> ext end. Also supp line
			Point	endAnchorPixelPoint			= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point	extPixelPoint				= ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point   suppLeftPoint				= AdjustAnchorLeft.GetPoint(chartControl, chartPanel, chartScale);
			Point   suppRightPoint				= AdjustAnchorRight.GetPoint(chartControl, chartPanel, chartScale);
			Tuple<Point, Point> extYLinePoints	= GetTranslatedExtensionYLine(chartControl, chartScale);

			Vector startEndVec	= endAnchorPixelPoint - startAnchorPixelPoint;
			Vector endExtVec	= extPixelPoint - endAnchorPixelPoint;
			Vector extYVec		= extYLinePoints.Item2 - extYLinePoints.Item1;
			Vector suppVec		= suppRightPoint - suppLeftPoint;
			// need to have an actual point to run vector along, so glue em together here
			if (new[] {	new Tuple<Vector, Point>(startEndVec, startAnchorPixelPoint), 
						new Tuple<Vector, Point>(endExtVec, endAnchorPixelPoint), 
						new Tuple<Vector, Point>(extYVec, extYLinePoints.Item1),
						new Tuple<Vector, Point>(suppVec, suppLeftPoint)}
					.Any(chkTup => MathHelper.IsPointAlongVector(point, chkTup.Item2, chkTup.Item1, CursorSensitivity)))
				return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
			return null;
		}

		private Point GetEndLineMidpoint(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel chartPanel	= chartControl.ChartPanels[PanelIndex];
			Point endPoint 			= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point startPoint 		= ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale);
			return new Point((endPoint.X + startPoint.X) / 2, (endPoint.Y + startPoint.Y) / 2);
		}

		public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			Point[] pts = base.GetSelectionPoints(chartControl, chartScale);
			if (!ExtensionAnchor.IsEditing || !EndAnchor.IsEditing)
			{
				// match NT7, show 3 points along ext based on actually drawn line
				Tuple<Point, Point> extYLine = GetTranslatedExtensionYLine(chartControl, chartScale);	
				Point midExtYPoint = extYLine.Item1 + (extYLine.Item2 - extYLine.Item1) / 2;

				ChartPanel	chartPanel	= chartControl.ChartPanels[chartScale.PanelIndex];
				Point adjustLeftPoint = AdjustAnchorLeft.GetPoint(chartControl, chartPanel, chartScale);
				Point adjustRightPoint = AdjustAnchorRight.GetPoint(chartControl, chartPanel, chartScale);
				
				return pts.Union(new[]{extYLine.Item1, extYLine.Item2, midExtYPoint, adjustLeftPoint, adjustRightPoint}).ToArray();
			}
			return pts;
		}

		private string GetPriceString(double price, ChartPanel chartPanel)
		{
			// note, dont use MasterInstrument.FormatPrice() as it will round value to ticksize which we do not want
			string priceStr = price.ToString(Core.Globals.GetTickFormatString(AttachedTo.Instrument.MasterInstrument.TickSize));
			return priceStr;
		}

		private Tuple<Point, Point> GetTranslatedExtensionYLine(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel chartPanel	= chartControl.ChartPanels[PanelIndex];
			Point extPoint 			= ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point startPoint 		= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint			= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

			Point ext2Point			= new Point(extPoint.X + (endPoint.X - startPoint.X), extPoint.Y + (endPoint.Y - startPoint.Y));

			return new Tuple<Point, Point>(new Point(ext2Point.X, ext2Point.Y), new Point(extPoint.X, anchorExtensionPoint.Y));
		}

		public override object Icon { 
			get 
			{ 
				string uniCodeArrow = "\u21C8";            
    			return uniCodeArrow;
				//return Icons.DrawArc; 
			} 
		}

		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
		{
			PriceLevel priceLevel = conditionItem.Tag as PriceLevel;
			if (priceLevel == null)
				return false;
			ChartPanel chartPanel		= chartControl.ChartPanels[PanelIndex];
			Tuple<Point, Point>	plp		= GetPriceLevelLinePoints(priceLevel, chartControl, chartScale, false);
			Point anchorStartPoint 		= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point extensionPoint	 	= ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale);
			// note these points X will be based on start/end, so move to our extension 
			Vector vecToExtension		= extensionPoint - anchorStartPoint;
			Point adjStartPoint			= plp.Item1 + vecToExtension;
			Point adjEndPoint			= plp.Item2 + vecToExtension;
			return CheckAlertRetracementLine(condition, adjStartPoint, adjEndPoint, chartControl, chartScale, values);
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			// because we have a third anchor we need to do some extra stuff here
			
			switch (DrawingState)
			{
				case DrawingState.Building:
					if (StartAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(StartAnchor);
						// give end anchor something to start with so we dont try to render it with bad values right away
						dataPoint.CopyDataValues(EndAnchor);
						StartAnchor.IsEditing = false;
					}
					else if (EndAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(EndAnchor);
						EndAnchor.IsEditing = false;

						// give extension anchor something nearby to start with
						dataPoint.CopyDataValues(ExtensionAnchor);

						// Initial values for supp anchors
						Point nullPoint = new Point(0, 0);
						ChartAnchor nullAnchor = new ChartAnchor();
						nullAnchor.UpdateFromPoint(nullPoint, chartControl, chartScale);

						nullAnchor.CopyDataValues(AdjustAnchorLeft);
						nullAnchor.CopyDataValues(AdjustAnchorRight);
					}
					else if (ExtensionAnchor.IsEditing)
					{
						Print("ext building");
						dataPoint.CopyDataValues(ExtensionAnchor);

						// side values
						Tuple<Point, Point> extLinePoints	= GetTranslatedExtensionYLine(chartControl, chartScale);

						Point leftPoint = new Point(extLinePoints.Item1.X - 15, extLinePoints.Item1.Y);
						Point rightPoint = new Point(extLinePoints.Item1.X + 15, extLinePoints.Item1.Y);

						ChartAnchor	leftAnchor			= new ChartAnchor();
						leftAnchor.UpdateFromPoint(leftPoint, chartControl, chartScale);

						ChartAnchor	rightAnchor			= new ChartAnchor();
						rightAnchor.UpdateFromPoint(rightPoint, chartControl, chartScale);

						leftAnchor.CopyDataValues(AdjustAnchorLeft);
						rightAnchor.CopyDataValues(AdjustAnchorRight);

						AdjustAnchorLeft.MoveAnchor(AdjustAnchorLeft, leftAnchor, chartControl, chartPanel, chartScale, this);
						AdjustAnchorRight.MoveAnchor(AdjustAnchorRight, rightAnchor, chartControl, chartPanel, chartScale, this);

						AdjustAnchorLeft.IsEditing = false;
						AdjustAnchorRight.IsEditing = false;
						ExtensionAnchor.IsEditing = false;
					}
					
					// is initial building done (all anchors set)
					if (Anchors.All(a => !a.IsEditing))
					{
						DrawingState 	= DrawingState.Normal;
						IsSelected 		= false; 
					}
					break;
				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					// first try base mouse down
					base.OnMouseDown(chartControl, chartPanel, chartScale, dataPoint);
					if (DrawingState != DrawingState.Normal)
						break;
					// now check if they clicked along extension fibs Y line and correctly select if so
					Tuple<Point, Point> extYLinePoints	= GetTranslatedExtensionYLine(chartControl, chartScale);
					Vector extYVec						= extYLinePoints.Item2 - extYLinePoints.Item1;
					Point pointDeviceY = new Point(point.X, ConvertToVerticalPixels(chartControl, chartPanel, point.Y));
					// need to have an actual point to run vector along, so glue em together here
					if (MathHelper.IsPointAlongVector(pointDeviceY, extYLinePoints.Item1, extYVec, CursorSensitivity))
						DrawingState = DrawingState.Moving;
					else
						IsSelected = false;

					break;
			}
		}
		
		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (IsLocked && DrawingState != DrawingState.Building)
				return;


			Point moveToPoint 					= dataPoint.GetPoint(chartControl, chartPanel, chartScale);
			Tuple<Point, Point> extLinePoints	= GetTranslatedExtensionYLine(chartControl, chartScale);
			Point leftMax 						= new Point(extLinePoints.Item1.X - 15, extLinePoints.Item1.Y);
			Point rightMax 						= new Point(extLinePoints.Item1.X + 15, extLinePoints.Item1.Y);

			switch (DrawingState)
			{
				case DrawingState.Building:
					if (StartAnchor.IsEditing)
						dataPoint.CopyDataValues(StartAnchor);
					if (EndAnchor.IsEditing)
						dataPoint.CopyDataValues(EndAnchor);
					if (ExtensionAnchor.IsEditing)
						dataPoint.CopyDataValues(ExtensionAnchor);

						AdjustAnchorRight.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
						AdjustAnchorLeft.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);

					break;
				case DrawingState.Editing:
					if (StartAnchor.IsEditing)
					{
						StartAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
						AdjustAnchorRight.MoveAnchor(dataPoint, InitialMouseDownAnchor, chartControl, chartPanel, chartScale, this);
						AdjustAnchorLeft.MoveAnchor(dataPoint, InitialMouseDownAnchor, chartControl, chartPanel, chartScale, this);
					}
					if (EndAnchor.IsEditing)
					{
						EndAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
						AdjustAnchorRight.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
						AdjustAnchorLeft.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					}
					if (ExtensionAnchor.IsEditing)
					{
						ExtensionAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
						AdjustAnchorRight.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
						AdjustAnchorLeft.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					}
					if (AdjustAnchorLeft.IsEditing && moveToPoint.X <= leftMax.X)
					{
						Point initPoint = AdjustAnchorRight.GetPoint(chartControl, chartPanel, chartScale);
						AdjustAnchorLeft.UpdateXFromPoint(dataPoint.GetPoint(chartControl, chartPanel, chartScale), chartControl, chartScale);
					}
					if (AdjustAnchorRight.IsEditing && moveToPoint.X >= rightMax.X)
					{
						AdjustAnchorRight.UpdateXFromPoint(dataPoint.GetPoint(chartControl, chartPanel, chartScale), chartControl, chartScale);
					}
					break;
				case DrawingState.Moving:
					StartAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					EndAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					ExtensionAnchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);

					AdjustAnchorRight.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					AdjustAnchorLeft.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);

					break;
			}
		}
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				AnchorLineStroke 			= new Stroke(Brushes.Orange, DashStyleHelper.Solid, 3f, 50);
				Name 						= "Measured Move";
				StartAnchor					= new ChartAnchor { IsEditing = true, DrawingTool = this };
				ExtensionAnchor				= new ChartAnchor { IsEditing = true, DrawingTool = this };
				EndAnchor					= new ChartAnchor { IsEditing = true, DrawingTool = this };
				AdjustAnchorLeft			= new ChartAnchor { IsEditing = true, DrawingTool = this };
				AdjustAnchorRight			= new ChartAnchor { IsEditing = true, DrawingTool = this };
				//StartAnchor.DisplayName		= Custom.Resource.NinjaScriptDrawingToolAnchorStart;
				//EndAnchor.DisplayName		= Custom.Resource.NinjaScriptDrawingToolAnchorEnd;
				//ExtensionAnchor.DisplayName	= Custom.Resource.NinjaScriptDrawingToolAnchorExtension;
			}
			else if (State == State.Configure)
			{
				if (PriceLevels.Count == 0)
				{
					PriceLevels.Add(new PriceLevel(0,		Brushes.Orange));
					PriceLevels.Add(new PriceLevel(100,		Brushes.Orange));
				}
			}
			else if (State == State.Terminated)
				Dispose();
		}
		
		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			// nothing is drawn yet
			if (Anchors.All(a => a.IsEditing)) 
				return;
			
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			// get x distance of the line, this will be basis for our levels
			// unless extend left/right is also on
			ChartPanel chartPanel			= chartControl.ChartPanels[PanelIndex];
			Point anchorStartPoint 			= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point anchorEndPoint 			= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			
			anchorExtensionPoint			= ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale);
			AnchorLineStroke.RenderTarget	= RenderTarget;
			
			// align to full pixel to avoid unneeded aliasing
			double strokePixAdj			= (AnchorLineStroke.Width % 2.0).ApproxCompare(0) == 0 ? 0.5d : 0d;
			Vector pixelAdjustVec		= new Vector(strokePixAdj, strokePixAdj);

			SharpDX.Vector2 startVec	= (anchorStartPoint + pixelAdjustVec).ToVector2();
			SharpDX.Vector2 endVec		= (anchorEndPoint + pixelAdjustVec).ToVector2();
			RenderTarget.DrawLine(startVec, endVec, AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

			// supp lines
			RenderTarget.DrawLine(new SharpDX.Vector2((float)startVec.X - 15, (float)startVec.Y), new SharpDX.Vector2((float)startVec.X + 15, (float)startVec.Y), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
			RenderTarget.DrawLine(new SharpDX.Vector2((float)endVec.X - 15, (float)endVec.Y), new SharpDX.Vector2((float)endVec.X + 15, (float)endVec.Y), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
			
			// is second anchor set yet? check both so we correctly redraw during extension anchor editing
			if (ExtensionAnchor.IsEditing && EndAnchor.IsEditing)
				return;
			
			SharpDX.Vector2			extVector	= anchorExtensionPoint.ToVector2();
			//SharpDX.Direct2D1.Brush	tmpBrush	= IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX;
			//RenderTarget.DrawLine(endVec, extVector, tmpBrush, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
	
			if (PriceLevels == null || !PriceLevels.Any())
				return;

			SetAllPriceLevelsRenderTarget();

			double minLevelY		= float.MaxValue;
			double maxLevelY		= float.MinValue;
			double minLevelX		= float.MaxValue;
			double maxLevelX		= float.MinValue;
			Point lastStartPoint	= new Point(0, 0);
			Stroke lastStroke		= null;

			int count = 0;
			foreach (PriceLevel priceLevel in PriceLevels.Where(pl => pl.IsVisible && pl.Stroke != null).OrderBy(pl => pl.Value))
			{
				Tuple<Point, Point>	plp		= GetPriceLevelLinePoints(priceLevel, chartControl, chartScale, false);
				// note these points X will be based on start/end, so move to our extension
				Vector vecToExtension		= anchorExtensionPoint - anchorStartPoint;
				Point startTranslatedToExt	= plp.Item1 + vecToExtension;
				Point endTranslatedToExt	= plp.Item2 + vecToExtension;
				
				// dont nuke extended X if extend left/right is on
				double startX 				= IsExtendedLinesLeft ? plp.Item1.X : startTranslatedToExt.X;
				double endX 				= IsExtendedLinesRight ? plp.Item2.X : endTranslatedToExt.X;
				Point adjStartPoint			= new Point(startX, plp.Item1.Y);
				Point adjEndPoint			= new Point(endX, plp.Item2.Y);

				// align to full pixel to avoid unneeded aliasing
				double plPixAdjust			=	(priceLevel.Stroke.Width % 2.0).ApproxCompare(0) == 0 ? 0.5d : 0d;
				Vector plPixAdjustVec		= new Vector(plPixAdjust, plPixAdjust);
				
				// don't hit test on the price level line & text (match NT7 here), but do keep track of the min/max y
				Point startPoint	= adjStartPoint + plPixAdjustVec;
				Point endPoint		= adjEndPoint + plPixAdjustVec;
				
				// PRICELINE LINES
				//RenderTarget.DrawLine(startPoint.ToVector2(), endPoint.ToVector2(), priceLevel.Stroke.BrushDX, priceLevel.Stroke.Width, priceLevel.Stroke.StrokeStyle);

				if (lastStroke == null)
					lastStroke = new Stroke();

				//if (lastStroke == null)
				//	lastStroke = new Stroke();
				//else if (!IsInHitTest)
				//{
				//	SharpDX.RectangleF borderBox = new SharpDX.RectangleF((float)lastStartPoint.X, (float)lastStartPoint.Y,
				//		(float)(endPoint.X - lastStartPoint.X), (float)(endPoint.Y - lastStartPoint.Y));

				//	RenderTarget.FillRectangle(borderBox, lastStroke.BrushDX);
				//}
				priceLevel.Stroke.CopyTo(lastStroke);
				lastStartPoint		= startPoint;
				minLevelY			= Math.Min(adjStartPoint.Y, minLevelY);
				maxLevelY			= Math.Max(adjStartPoint.Y, maxLevelY);

				count++;
			}

			if (!IsInHitTest)
				foreach (PriceLevel priceLevel in PriceLevels.Where(pl => pl.IsVisible && pl.Stroke != null).OrderBy(pl => pl.Value))
				{
					Tuple<Point, Point>	plp		= GetPriceLevelLinePoints(priceLevel, chartControl, chartScale, false);
					// note these points X will be based on start/end, so move to our extension
					Vector vecToExtension		= anchorExtensionPoint - anchorStartPoint;
					Point startTranslatedToExt	= plp.Item1 + vecToExtension;
					
					// dont nuke extended X if extend left/right is on
					double startX 				= IsExtendedLinesLeft ? plp.Item1.X : startTranslatedToExt.X;
					Point adjStartPoint			= new Point(startX, plp.Item1.Y);

					double extMinX = anchorExtensionPoint.X;
					double extMaxX = anchorExtensionPoint.X + anchorEndPoint.X - anchorStartPoint.X; // actual width of lines before extension

					double totalPriceRange	= EndAnchor.Price - StartAnchor.Price;
					double price			= priceLevel.GetPrice(ExtensionAnchor.Price, totalPriceRange, false);
					//DrawPriceLevelText(chartPanel, chartScale, extMinX, extMaxX, adjStartPoint.Y, price, priceLevel);
				}

			Point diffPoint = new Point(extVector.X + (anchorEndPoint.X - anchorStartPoint.X), extVector.Y + (anchorEndPoint.Y - anchorStartPoint.Y));

			if (count > 0)
			{
				RenderTarget.DrawLine(new SharpDX.Vector2((float)extVector.X, (float)extVector.Y), new SharpDX.Vector2((float)diffPoint.X, (float)diffPoint.Y), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

				// supplementary horizontal lines
				RenderTarget.DrawLine(new SharpDX.Vector2((float)extVector.X - 15, (float)extVector.Y), new SharpDX.Vector2((float)extVector.X + 15, (float)extVector.Y), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

				Point leftAdjustPoint 		= AdjustAnchorLeft.GetPoint(chartControl, chartPanel, chartScale);
				//leftAdjustPoint.Y = diffPoint.Y;
				Point rightAdjustPoint 	= AdjustAnchorRight.GetPoint(chartControl, chartPanel, chartScale);

				if (leftAdjustPoint.X < 2 && leftAdjustPoint.Y < 2 && rightAdjustPoint.X < 2 && rightAdjustPoint.Y < 2) {
					RenderTarget.DrawLine(new SharpDX.Vector2((float)diffPoint.X - 15, (float)diffPoint.Y), new SharpDX.Vector2((float)diffPoint.X + 15, (float)diffPoint.Y), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
				}


				//rightAdjustPoint.Y = diffPoint.Y;
				SharpDX.Vector2 leftAdjustVec	= (leftAdjustPoint + pixelAdjustVec).ToVector2();
				SharpDX.Vector2 rightAdjustVec	= (rightAdjustPoint + pixelAdjustVec).ToVector2();
				//SharpDX.Vector2 leftAdjustVec	= (leftAdjustPoint).ToVector2();
				//SharpDX.Vector2 rightAdjustVec	= (rightAdjustPoint).ToVector2();
				RenderTarget.DrawLine(leftAdjustVec, rightAdjustVec, AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

				// adjusted supp
				//RenderTarget.DrawLine(new SharpDX.Vector2((float)diffPoint.X - 15, (float)diffPoint.Y), new SharpDX.Vector2((float)diffPoint.X + 15, (float)diffPoint.Y), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);


			}

		}
	}


	public static partial class Draw
	{
		private static T MeasuredMoveCore<T>(NinjaScriptBase owner, bool isAutoScale, string tag,
			int startBarsAgo, DateTime startTime, double startY, 
			int endBarsAgo, DateTime endTime, double endY, bool isGlobal, string templateName) where T : FibonacciLevels
		{
			if (owner == null)
				throw new ArgumentException("owner");
			if (startTime == Core.Globals.MinDate && endTime == Core.Globals.MinDate && startBarsAgo == int.MinValue && endBarsAgo == int.MinValue)
				throw new ArgumentException("bad start/end date/time");

			if (string.IsNullOrWhiteSpace(tag))
				throw new ArgumentException("tag cant be null or empty");

			if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
				tag = string.Format("{0}{1}", GlobalDrawingToolManager.GlobalDrawingToolTagPrefix, tag);

			T fibBase = DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) as T;
			if (fibBase == null)
				return null;

			DrawingTool.SetDrawingToolCommonValues(fibBase, tag, isAutoScale, owner, isGlobal);

			// dont nuke existing anchor refs 
			ChartAnchor		startAnchor	= DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
			ChartAnchor		endAnchor	= DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);

			startAnchor.CopyDataValues(fibBase.StartAnchor);
			endAnchor.CopyDataValues(fibBase.EndAnchor);
			fibBase.SetState(State.Active);
			return fibBase;
		}

		// extensions has third anchor, so provide an extra base drawing function for it
		private static MeasuredMoves MeasuredMovesCore(NinjaScriptBase owner, bool isAutoScale, string tag,
			int startBarsAgo, DateTime startTime, double startY, 
			int endBarsAgo, DateTime endTime, double endY,
			int extensionBarsAgo, DateTime extensionTime, double extensionY, bool isGlobal, string templateName)
		{
			MeasuredMoves	fibExt		= MeasuredMoveCore<MeasuredMoves>(owner, isAutoScale, tag, startBarsAgo, 
																					startTime, startY, endBarsAgo, endTime, endY, isGlobal, templateName);

			ChartAnchor			extAnchor	= DrawingTool.CreateChartAnchor(owner, extensionBarsAgo, extensionTime, extensionY);
			extAnchor.CopyDataValues(fibExt.ExtensionAnchor);
			return fibExt;
		}


		/// <summary>
		/// Draws a fibonacci extension.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="extensionBarsAgo">The extension bars ago.</param>
		/// <param name="extensionY">The y value of the 3rd anchor point</param>
		/// <returns></returns>
		public static MeasuredMoves MeasuredMoves(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, 
			double startY, int endBarsAgo, double endY, int extensionBarsAgo, double extensionY)
		{
			return MeasuredMovesCore(owner, isAutoScale, tag, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, 
				Core.Globals.MinDate, endY, extensionBarsAgo, Core.Globals.MinDate, extensionY, false, null);
		}

		/// <summary>
		/// Draws a fibonacci extension.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startTime">The starting time where the draw object will be drawn.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endTime">The end time where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="extensionTime">The time of the 3rd anchor point</param>
		/// <param name="extensionY">The y value of the 3rd anchor point</param>
		/// <returns></returns>
		public static MeasuredMoves MeasuredMoves(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, 
			double startY, DateTime endTime, double endY, DateTime extensionTime, double extensionY)
		{
			return MeasuredMovesCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, 
				endTime, endY, int.MinValue, extensionTime, extensionY, false, null);
		}

		/// <summary>
		/// Draws a fibonacci extension.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startTime">The starting time where the draw object will be drawn.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endTime">The end time where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="extensionTime">The time of the 3rd anchor point</param>
		/// <param name="extensionY">The y value of the 3rd anchor point</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static MeasuredMoves MeasuredMoves(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, 
			double startY, DateTime endTime, double endY, DateTime extensionTime, double extensionY, bool isGlobal, string templateName)
		{
			return MeasuredMovesCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, 
				endTime, endY, int.MinValue, extensionTime, extensionY, isGlobal, templateName);
		}

		/// <summary>
		/// Draws a fibonacci extension.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="extensionBarsAgo">The extension bars ago.</param>
		/// <param name="extensionY">The y value of the 3rd anchor point</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		public static MeasuredMoves MeasuredMoves(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, 
			double startY, int endBarsAgo, double endY, int extensionBarsAgo, double extensionY, bool isGlobal, string templateName)
		{
			return MeasuredMovesCore(owner, isAutoScale, tag, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, 
				Core.Globals.MinDate, endY, extensionBarsAgo, Core.Globals.MinDate, extensionY, isGlobal, templateName);
		}
	}
}
