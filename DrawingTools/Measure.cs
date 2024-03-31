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

#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{
	public class Measure : DrawingTool
	{
		private const int 						cursorSensitivity 			= 15;
		private	ChartAnchor						editingAnchor;
		private bool							isTextCreated;
		private const float						textMargin					= 6f;
		private SharpDX.DirectWrite.TextFormat	textFormat;
		private SharpDX.DirectWrite.TextLayout	textLayout;
		private Brush							textBrush;
		private	readonly DeviceBrush			textDeviceBrush				= new DeviceBrush();
		private	readonly DeviceBrush			textBackgroundDeviceBrush	= new DeviceBrush();
		private string							yValueString;
		private string 							timeText;
		private ValueUnit						yValueDisplayUnit			= ValueUnit.Price;
		private System.Windows.Media.Brush		positiveAreaBrush;
		private System.Windows.Media.Brush		negativeAreaBrush;
		private int								areaOpacity;
		private	Gui.Tools.SimpleFont 			font;

		public override IEnumerable<ChartAnchor> Anchors { get { return new[] { StartAnchor, EndAnchor }; } }
		
		[Display(Order = 1)]
		public ChartAnchor		StartAnchor		{ get; set; }
		[Display(Order = 2)]
		public ChartAnchor		EndAnchor		{ get; set;	}
		//[Display(Order = 3)]
		//public ChartAnchor		TextAnchor		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptGeneral", Order = 2)]
		public Stroke 			LineColor		{ get; set; }

		private bool ShouldDrawText { get { return DrawingState == DrawingState.Moving || (EndAnchor != null && !EndAnchor.IsEditing); }  }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolText", GroupName = "NinjaScriptGeneral", Order = 1)]
		public Brush		 	TextColor
		{
			get { return textBrush; }
			set { textBrush = value; textDeviceBrush.Brush = value; }
		}
		
		[Browsable(false)]
		public string TextColorSerialize
		{
			get { return  Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 3)]
		public ValueUnit 	YValueDisplayUnit
		{ 
			get { return yValueDisplayUnit; }
			set { yValueDisplayUnit = value; isTextCreated = false; }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PositiveAreaColor", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush PositiveAreaBrush
		{
			get { return positiveAreaBrush; }
			set
			{
				positiveAreaBrush = value;
				if (positiveAreaBrush != null)
				{
					if (positiveAreaBrush.IsFrozen)
						positiveAreaBrush = positiveAreaBrush.Clone();
					positiveAreaBrush.Opacity = areaOpacity / 100d;
					positiveAreaBrush.Freeze();
				}
			}
		}

		[Browsable(false)]
		public string PositiveAreaBrushSerialize
		{
			get { return Serialize.BrushToString(PositiveAreaBrush); }
			set { PositiveAreaBrush = Serialize.StringToBrush(value); }
		}
	
		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral")]
		public int AreaOpacity
		{
			get { return areaOpacity; }
			set
			{
				areaOpacity = Math.Max(0, Math.Min(100, value));
				if (positiveAreaBrush != null)
				{	
					System.Windows.Media.Brush newBrush		= positiveAreaBrush.Clone();
					newBrush.Opacity	= areaOpacity / 100d;
					newBrush.Freeze();
					positiveAreaBrush			= newBrush;
				}
			}
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NegativeAreaColor", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush NegativeAreaBrush
		{
			get { return negativeAreaBrush; }
			set
			{
				negativeAreaBrush = value;
				if (negativeAreaBrush != null)
				{
					if (negativeAreaBrush.IsFrozen)
						negativeAreaBrush = negativeAreaBrush.Clone();
					negativeAreaBrush.Opacity = areaOpacity / 100d;
					negativeAreaBrush.Freeze();
				}
			}
		}

		[Browsable(false)]
		public string NegativeAreaBrushSerialize
		{
			get { return Serialize.BrushToString(NegativeAreaBrush); }
			set { NegativeAreaBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "TextColor", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush TextBrush
		{
			get { return textBrush; }
			set { textBrush = value; }
		}
	
		[Browsable(false)]
		public string TextBrushSerialize
		{	
			get { return Serialize.BrushToString(TextBrush); }
			set { TextBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolTextFont", GroupName = "NinjaScriptGeneral", Order = 4)]
		public Gui.Tools.SimpleFont Font
		{
			get { return font; }
			set
			{
				font				= value;
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			try 
			{
				if (textLayout != null)
					textLayout.Dispose();
				textFormat = null;
				// this triggers native brush disposes
				textDeviceBrush.RenderTarget = null;
				textBackgroundDeviceBrush.RenderTarget = null;
			}
			catch { }
			finally
			{
				LineColor = null;
			}
		}
		
		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:	return Cursors.Pen;
				case DrawingState.Moving:	return IsLocked ? Cursors.No : Cursors.SizeAll;
				case DrawingState.Editing:
					if (IsLocked)
						return Cursors.No;
					return editingAnchor == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
				default:
					// see if we are near an anchor right away. this is is cheap so no big deal to do often
					ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if (closest != null)
					{
						if (IsLocked)
							return Cursors.Arrow;
						return closest == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
					}
					// draw move cursor if cursor is near line path anywhere
					Point	startAnchorPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point	endAnchorPoint		= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point	txtAnchorPoint		= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Vector	startEndVector		= endAnchorPoint - startAnchorPoint;
					Vector	endToTextVector		= txtAnchorPoint - endAnchorPoint;

					//Text Outline Box Path as well
					UpdateTextLayout(chartControl, ChartPanel, chartScale);
					Point bottomLeft			= new Point(txtAnchorPoint.X - textLayout.MaxWidth - textMargin, txtAnchorPoint.Y);
					Point topLeft				= new Point(bottomLeft.X, txtAnchorPoint.Y - textLayout.MaxHeight - 2 * textMargin);
					Point topRight				= new Point(txtAnchorPoint.X, txtAnchorPoint.Y - textLayout.MaxHeight - 2 * textMargin);

					Vector txtBottomLeft		= bottomLeft - txtAnchorPoint;
					Vector bottomLeftTopLeft	= topLeft - bottomLeft;
					Vector topLeftTopRight		= topRight - topLeft;
					Vector topRightTxt			= txtAnchorPoint - topRight;

					if (MathHelper.IsPointAlongVector(point, startAnchorPoint, startEndVector, cursorSensitivity) ||
						MathHelper.IsPointAlongVector(point, endAnchorPoint, endToTextVector, cursorSensitivity) ||
						MathHelper.IsPointAlongVector(point, txtAnchorPoint, txtBottomLeft, cursorSensitivity) ||
						MathHelper.IsPointAlongVector(point, bottomLeft, bottomLeftTopLeft, cursorSensitivity) ||
						MathHelper.IsPointAlongVector(point, topLeft, topLeftTopRight, cursorSensitivity) ||
						MathHelper.IsPointAlongVector(point, topRight, topRightTxt, cursorSensitivity))
						return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
					return null;
			}
		}

		public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel	chartPanel	= chartControl.ChartPanels[chartScale.PanelIndex];
			Point		startPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		endPoint	= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

			if (!ShouldDrawText) 
				return new[] { startPoint, endPoint };
			Point textPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			return new[] { startPoint, textPoint, endPoint };
		}
		
		public override object Icon { get { return Icons.DrawRuler; } }

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return true;

			DateTime minTime = Core.Globals.MaxDate;
			DateTime maxTime = Core.Globals.MinDate;
			foreach (ChartAnchor anchor in Anchors)
			{
				if (anchor.Time < minTime)
					minTime = anchor.Time;
				if (anchor.Time > maxTime)
					maxTime = anchor.Time;
			}

			// did we go through visible time at all?
			if ((minTime <= lastTimeOnChart) || (minTime <= firstTimeOnChart && maxTime >= firstTimeOnChart))
				return true;
			return false;
		}

		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!IsVisible)
				return;

			MinValue = Anchors.Select(a => a.Price).Min();
			MaxValue = Anchors.Select(a => a.Price).Max();
		}

		public override void OnBarsChanged()
		{
			isTextCreated = false;
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
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
						
						// give text anchor something to start with right away so we dont try to render it
						// with uninitialized values as well
						dataPoint.CopyDataValues(EndAnchor);
					}
					/*else if (TextAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(TextAnchor);
						TextAnchor.IsEditing = false;
					}*/
					
					// is initial building done (all anchors set)
					if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
					{
						DrawingState = DrawingState.Normal;
						IsSelected = false; 
					}
					break;
				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					// see if they clicked near a point to edit, if so start editing
					editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if (editingAnchor != null)
					{
						editingAnchor.IsEditing = true;
						DrawingState = DrawingState.Editing;
					}
					else if (editingAnchor == null || IsLocked)
					{
						// or if they didnt click particulary close to either, move (they still clicked close to our line)
						// set it to moving even if locked so we know to change cursor
						if (GetCursor(chartControl, chartPanel, chartScale, point) != null)
							DrawingState = DrawingState.Moving;
						else
							IsSelected = false;
					}
					break;
			}
		}
		
		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			//if (IsLocked && DrawingState != DrawingState.Building)
				//return;
			
			if (DrawingState == DrawingState.Building)
			{
				// start anchor will not be editing here because we start building as soon as user clicks, which
				// plops down a start anchor right away. check these seperately because they will both initially
				// be in edit mode.
				if (EndAnchor.IsEditing)
				{
					dataPoint.CopyDataValues(EndAnchor);
					isTextCreated = false;
				}
				//else if (TextAnchor.IsEditing)
					//dataPoint.CopyDataValues(TextAnchor);
			}
			else if (DrawingState == DrawingState.Editing && editingAnchor != null)
			{
				dataPoint.CopyDataValues(editingAnchor);
				if (editingAnchor == StartAnchor || editingAnchor == EndAnchor)
					isTextCreated = false;
			}
			else if (DrawingState == DrawingState.Moving)
			{
				foreach (ChartAnchor anchor in Anchors)
					anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint,chartControl, chartPanel, chartScale, this);
				// dont forget to update delta to last used
				if (textLayout != null)
					textLayout.Dispose();
				textLayout = null;
			}
		}

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
				return;

			// simply end whatever was editing / moving
			DrawingState = DrawingState.Normal;
			if (editingAnchor != null)
				editingAnchor.IsEditing = false;
			editingAnchor = null;
		}
		
		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			LineColor.RenderTarget				= RenderTarget;

			// first of all, turn on anti-aliasing to smooth out our line
			RenderTarget.AntialiasMode			= SharpDX.Direct2D1.AntialiasMode.PerPrimitive;

			ChartPanel panel					= chartControl.ChartPanels[chartScale.PanelIndex];
			
			// draw a line from start measure point to end measure point.
			Point lineStartPoint 				= StartAnchor.GetPoint(chartControl, panel, chartScale);
			Point lineEndPoint					= EndAnchor.GetPoint(chartControl, panel, chartScale);
		
			// align to full pixel to avoid unneeded aliasing
			double strokePixAdjust				= (LineColor.Width % 2).ApproxCompare(0) == 0 ? 0.5d : 0d;
			Vector strokePixAdjustVec			= new Vector(strokePixAdjust, strokePixAdjust);

			SharpDX.Vector2 endVec				= (lineEndPoint + strokePixAdjustVec).ToVector2();
			SharpDX.Direct2D1.Brush tmpBrush	= IsInHitTest ? chartControl.SelectionBrush : LineColor.BrushDX;
			SharpDX.Direct2D1.Brush tmpBrushText = IsInHitTest ? chartControl.SelectionBrush : LineColor.BrushDX;


			// Warning:  Brush objects must be disposed of after they have been used
			SharpDX.Direct2D1.Brush positiveAreaBrushDx = positiveAreaBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush negativeAreaBrushDx = negativeAreaBrush.ToDxBrush(RenderTarget);
			//SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,	SharpDX.Color.DodgerBlue);
			
			if (lineStartPoint.Y < lineEndPoint.Y) 
			{
				tmpBrush = negativeAreaBrush.Clone().ToDxBrush(RenderTarget);
				tmpBrushText = negativeAreaBrush.Clone().ToDxBrush(RenderTarget);
			} else {
				tmpBrush = positiveAreaBrush.Clone().ToDxBrush(RenderTarget);
				tmpBrushText = positiveAreaBrush.Clone().ToDxBrush(RenderTarget);
			}
			
			tmpBrush.Opacity = 0.3f;
			tmpBrushText.Opacity = 1f;
			
			// line from first anchor to second
			// VERTICAL LINE
			SharpDX.Vector2 vert1 = new SharpDX.Vector2((float) ((lineStartPoint.X+lineEndPoint.X)/2), (float) lineStartPoint.Y);
			SharpDX.Vector2 vert2 = new SharpDX.Vector2((float) ((lineStartPoint.X+lineEndPoint.X)/2), (float) lineEndPoint.Y);
			RenderTarget.DrawLine(vert1, vert2, tmpBrush, LineColor.Width, LineColor.StrokeStyle);

			// HORIZONTAL LINE
			SharpDX.Vector2 hor1 = new SharpDX.Vector2((float) lineStartPoint.X, (float) (lineStartPoint.Y+lineEndPoint.Y)/2);
			SharpDX.Vector2 hor2 = new SharpDX.Vector2((float) lineEndPoint.X, (float) (lineStartPoint.Y+lineEndPoint.Y)/2);
			RenderTarget.DrawLine(hor1, hor2, tmpBrush, LineColor.Width, LineColor.StrokeStyle);
			
			// DIAGONAL LINE
			//RenderTarget.DrawLine((lineStartPoint + strokePixAdjustVec).ToVector2(), endVec, tmpBrush, LineColor.Width, LineColor.StrokeStyle);
			
			SharpDX.RectangleF rectt = new SharpDX.RectangleF((float)lineStartPoint.X, (float)lineStartPoint.Y, (float)(lineEndPoint.X - lineStartPoint.X), (float)(lineEndPoint.Y - lineStartPoint.Y));

			// The FillRectangle() method is used to "Paint" the area of a Rectangle
			if (lineStartPoint.Y < lineEndPoint.Y) 
			{
				RenderTarget.FillRectangle(rectt, negativeAreaBrushDx);
			} else {
				RenderTarget.FillRectangle(rectt, positiveAreaBrushDx);
			}
			
			UpdateTextLayout(chartControl, ChartPanel, chartScale);
			textDeviceBrush.RenderTarget			= RenderTarget;
			// Text rec uses same settings as mini data box
			textBackgroundDeviceBrush.Brush			= Application.Current.FindResource("ChartControl.DataBoxBackground") as Brush;
			textBackgroundDeviceBrush.RenderTarget	= RenderTarget;
				
			Brush borderBrush						= Application.Current.FindResource("BorderThinBrush") as Brush;
			object thicknessResource				= Application.Current.FindResource("BorderThinThickness");
			double thickness						= thicknessResource as double? ?? 1;
			Stroke textBorderStroke					= new Stroke(borderBrush ?? LineColor.Brush, DashStyleHelper.Solid,Convert.ToSingle(thickness)) { RenderTarget = RenderTarget };
				
			Point			textEndPoint			= EndAnchor.GetPoint(chartControl, panel, chartScale);
			SharpDX.Vector2 textEndVec				= (textEndPoint + strokePixAdjustVec).ToVector2();
				
			float adjustY;
				
			if (lineStartPoint.Y < lineEndPoint.Y) 
			{
				adjustY = textLayout.MaxHeight + textMargin + 15;	
			} else
			{
				adjustY = -15;
			}
				
			// second to third anchor point line:
			//RenderTarget.DrawLine(endVec, textEndVec, LineColor.BrushDX, LineColor.Width, LineColor.StrokeStyle);
				
			/* old text rectangle */
				
			float				rectPixAdjust		= (float)(strokePixAdjust / 2f);
			SharpDX.RectangleF	rect				= new SharpDX.RectangleF((float)((textEndPoint.X+lineStartPoint.X)/2 - textLayout.MaxWidth - textMargin + rectPixAdjust + (textLayout.MaxWidth / 2) - 10),
																				(float)(textEndPoint.Y - textLayout.MaxHeight - textMargin + rectPixAdjust + adjustY),
																				textLayout.MaxWidth + textMargin * 3f + 15, textLayout.MaxHeight + textMargin * 2f);

			// color text rectangle
			tmpBrush.Opacity = 1f;
			
			RenderTarget.FillRectangle(rect, tmpBrush);
			
			positiveAreaBrushDx.Dispose();
			negativeAreaBrushDx.Dispose();
			tmpBrush.Dispose();
				
			if (textBackgroundDeviceBrush.BrushDX != null && !IsInHitTest)
				//RenderTarget.FillRectangle(rect, textBackgroundDeviceBrush.BrushDX);
				//RenderTarget.DrawRectangle(rect, textBorderStroke.BrushDX, textBorderStroke.Width, textBorderStroke.StrokeStyle);
			
			if (textDeviceBrush.BrushDX != null && !IsInHitTest)
				RenderTarget.DrawTextLayout(new SharpDX.Vector2((float)(rect.X + textMargin + strokePixAdjust + 10), (float)(rect.Y + textMargin + strokePixAdjust)), textLayout, textDeviceBrush.BrushDX);
		}
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description				= @"Modified ruler tool to display values dynamically";
				Name					= "Measure";
				//Name					= Custom.Resource.NinjaScriptDrawingToolRuler;
				DrawingState			= DrawingState.Building;
				StartAnchor				= new ChartAnchor { IsEditing = true, DrawingTool = this };
				EndAnchor				= new ChartAnchor { IsEditing = true, DrawingTool = this };
				//TextAnchor				= new ChartAnchor { IsEditing = true, DrawingTool = this };
				StartAnchor.DisplayName	= Custom.Resource.NinjaScriptDrawingToolAnchorStart;
				EndAnchor.DisplayName	= Custom.Resource.NinjaScriptDrawingToolAnchorEnd;
				//TextAnchor.DisplayName	= Custom.Resource.NinjaScriptDrawingToolAnchorText;
				Font					= new Gui.Tools.SimpleFont() { Size = 14 };
				LineColor				= new Stroke(Brushes.DarkGray, DashStyleHelper.Solid, 1f, 50);
				PositiveAreaBrush		= System.Windows.Media.Brushes.LightSeaGreen;
				NegativeAreaBrush 		= System.Windows.Media.Brushes.Crimson;
				AreaOpacity				= 10;
				TextColor				= Application.Current.FindResource("ChartControl.DataBoxForeground") as Brush ?? Brushes.CornflowerBlue;
				
			}
			else if (State == State.Terminated)
			{
				// release any device resources
				Dispose();
			}
		}
		
		private void UpdateTextLayout(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			if (isTextCreated && textLayout != null && !textLayout.IsDisposed)
				return;
			
			if (textFormat != null && !textFormat.IsDisposed)
				textFormat.Dispose();
			if (textLayout != null && !textLayout.IsDisposed)
				textLayout.Dispose();
		
			ChartBars chartBars = GetAttachedToChartBars();
			
			// bars can be null while chart is initializing
			if (chartBars == null)
				return;

			double yDiffPrice	= AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EndAnchor.Price - StartAnchor.Price);
			double yDiffTicks	= yDiffPrice / AttachedTo.Instrument.MasterInstrument.TickSize;

			switch (YValueDisplayUnit)
			{
				case ValueUnit.Price	: yValueString = chartBars.Bars.Instrument.MasterInstrument.FormatPrice(yDiffPrice); break;
				case ValueUnit.Currency	: 
					yValueString = AttachedTo.Instrument.MasterInstrument.InstrumentType == InstrumentType.Forex
						? Core.Globals.FormatCurrency((int)(yDiffTicks) * Account.All[0].ForexLotSize * (AttachedTo.Instrument.MasterInstrument.TickSize * AttachedTo.Instrument.MasterInstrument.PointValue))
						: Core.Globals.FormatCurrency((int)(yDiffTicks) * (AttachedTo.Instrument.MasterInstrument.TickSize * AttachedTo.Instrument.MasterInstrument.PointValue)); 
					break;
				case ValueUnit.Percent	: yValueString = (yDiffPrice / AttachedTo.Instrument.MasterInstrument.RoundToTickSize(StartAnchor.Price)).ToString("P", Core.Globals.GeneralOptions.CurrentCulture); break;
				case ValueUnit.Ticks	: yValueString = yDiffTicks.ToString("F0"); break;
				case ValueUnit.Pips		:
					// show tenth pips (if available)
					double pips = Math.Abs(yDiffTicks/10);
					char decimalChar = Char.Parse(Core.Globals.GeneralOptions.CurrentCulture.NumberFormat.NumberDecimalSeparator);
					yValueString = Int32.Parse(pips.ToString("F1").Split(decimalChar)[1]) > 0 ? pips.ToString("F1").Replace(decimalChar, '\'') : pips.ToString("F0");
					break;
			}
		
			TimeSpan timeDiff = EndAnchor.Time - StartAnchor.Time;
			// trim off millis/ticks, match NT7 time formatting
			timeDiff = new TimeSpan(timeDiff.Days, timeDiff.Hours, timeDiff.Minutes, timeDiff.Seconds);

			bool isMultiDay = Math.Abs(timeDiff.TotalHours) >= 24;

			if (chartBars.Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Day)
			{
				int timeDiffDay = Math.Abs(timeDiff.Days);
				timeText = timeDiffDay > 1 ? Math.Abs(timeDiff.Days) + " " + Custom.Resource.Days  : Math.Abs(timeDiff.Days) + " " + Custom.Resource.Day;
			}
			else
			{
				timeText	= isMultiDay ? string.Format("{0}\n{1,25}", 
											string.Format(Custom.Resource.NinjaScriptDrawingToolRulerDaysFormat, Math.Abs(timeDiff.Days)),
											timeDiff.Subtract(new TimeSpan(timeDiff.Days, 0, 0, 0)).Duration().ToString()) : timeDiff.Duration().ToString();
			}

			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			int startIdx = chartBars.GetBarIdxByX(chartControl, (int)(startPoint.X));
			int endIdx = chartBars.GetBarIdxByX(chartControl, (int)endPoint.X);
			int numBars = endIdx - startIdx;
			//test
			

			//test
			

			//SimpleFont wpfFont			= chartControl.Properties.LabelFont ?? new SimpleFont() { Bold = true };
			//SimpleFont wpfFont			= new SimpleFont() { Bold = true };
			SimpleFont wpfFont				= Font;
			textFormat					= wpfFont.ToDirectWriteTextFormat();
			textFormat.TextAlignment	= SharpDX.DirectWrite.TextAlignment.Leading;
			textFormat.WordWrapping		= SharpDX.DirectWrite.WordWrapping.NoWrap;

			// format text to our text rectangle bounds (it will wrap to these constraints), nt7 format
			// NOTE: Environment.NewLine doesnt work right here
			string text = string.Format("{0}\n{1,-11}{2,-11}\n{3,-11}{4,-11}\n{5,-10}{6,-10}",
				AttachedTo.DisplayName, 
				Custom.Resource.NinjaScriptDrawingToolRulerNumberBarsText, numBars,
				Custom.Resource.NinjaScriptDrawingToolRulerTimeText, timeText,
				Custom.Resource.NinjaScriptDrawingToolRulerYValueText, yValueString);
			// give big values for max width/height, we will trim to actual used
			textLayout				= new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, 600, 600); 
			// use measured max width/height
			textLayout.MaxWidth		= textLayout.Metrics.Width;
			textLayout.MaxHeight	= textLayout.Metrics.Height;
			isTextCreated			= true;
		}
	}
}
