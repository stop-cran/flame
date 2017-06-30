using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace Flame3
{
	public partial class Plot : Control
	{
		public Plot()
		{
			ScaleX = new Scale() { MinValue = 0, MaxValue = 1, MinScreenValue = 40, Exponential = false };
			ScaleY = new Scale() { MinValue = 0, MaxValue = 1, MaxScreenValue = 20, Exponential = false };
			FocusArea = new RectangleF();
			CurveY = new List<Curve>();
			DoubleBuffered = true;

			InitializeComponent();
		}

		[Category("Appearance")]
		[DefaultValue(PlotStyle.Block)]
		public PlotStyle PlotStyle { get; set; }
		[Category("Appearance")]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public Scale ScaleX { get; private set; }
		[Category("Appearance")]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public Scale ScaleY { get; private set; }
		Point FocusLocation;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Rectangle FocusBounds
		{
			get
			{
				if (Capture)
				{
					Point pt = PointToClient(MousePosition);
					return new Rectangle(Math.Min(FocusLocation.X, pt.X),
						Math.Min(FocusLocation.Y, pt.Y),
						Math.Abs(FocusLocation.X - pt.X),
						Math.Abs(FocusLocation.Y - pt.Y));
				}
				else
					return Rectangle.Empty;
			}
		}
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PointD FocusPosition
		{
			get
			{
				Point pt = PointToClient(MousePosition);

				return new PointD() { X = ScaleX.ScreenToValue(pt.X), Y = ScaleY.ScreenToValue(pt.Y) };
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RectangleF FocusArea { get; private set; }
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AvgList CurveX { get; set; }
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AvgList NewCurveX
		{
			get
			{
				if (CurveX == null)
					CurveX = new AvgList();
				return CurveX;
			}
		}
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<Curve> CurveY { get; set; }

		public void Add(params double[] items)
		{
			if (items.Length != CurveY.Count + 1)
				throw new ArgumentException();

			for (int i = 0; i < CurveY.Count; i++)
				CurveY[i].AddAvg(items[i + 1]);

			CurveX.AddAvg(items[0]);
			Invalidate();
		}

		const int AxisWidth = 4;
		const int AxisHeight = 14;

		protected virtual void DrawAxes(PaintEventArgs pe)
		{
			pe.Graphics.DrawLines(SystemPens.ControlText,
				new Point[]{new Point(ScaleX.MinScreenValue, ScaleY.MaxScreenValue),
				new Point(ScaleX.MinScreenValue, ScaleY.MinScreenValue),
				new Point(ScaleX.MaxScreenValue, ScaleY.MinScreenValue)});
			pe.Graphics.FillPolygon(SystemBrushes.ControlText,
				new Point[]{
					new Point(ScaleX.MaxScreenValue - AxisHeight, ScaleY.MinScreenValue - AxisWidth),
					new Point(ScaleX.MaxScreenValue, ScaleY.MinScreenValue),
					new Point(ScaleX.MaxScreenValue - AxisHeight, ScaleY.MinScreenValue + AxisWidth)});
			pe.Graphics.FillPolygon(SystemBrushes.ControlText,
				new Point[]{
			        new Point(ScaleX.MinScreenValue - AxisWidth, ScaleY.MaxScreenValue + AxisHeight),
			        new Point(ScaleX.MinScreenValue, ScaleY.MaxScreenValue),
			        new Point(ScaleX.MinScreenValue + AxisWidth, ScaleY.MaxScreenValue + AxisHeight)});

			pe.Graphics.DrawString(ScaleX.MinValue.ToString(), Font, SystemBrushes.ControlText,
				new PointF(ScaleX.MinScreenValue, ScaleY.MinScreenValue + 5));

			pe.Graphics.DrawString(ScaleX.MaxValue.ToString(), Font, SystemBrushes.ControlText,
				new PointF(ScaleX.MaxScreenValue - 20, ScaleY.MinScreenValue + 5));

			using (StringFormat sf = new StringFormat())
			{
				pe.Graphics.DrawString(ScaleY.MaxValue.ToString(), Font, SystemBrushes.ControlText,
					new PointF(5, ScaleY.MaxScreenValue - 15), sf);
				pe.Graphics.DrawString(ScaleY.MinValue.ToString(), Font, SystemBrushes.ControlText,
					new PointF(5, ScaleY.MinScreenValue + 20), sf);
			}
		}

		protected virtual void DrawBlock(PaintEventArgs pe)
		{
		}

		protected virtual void DrawLine(PaintEventArgs pe)
		{
			PointF pt = new PointF();
			List<PointF> points = new List<PointF>();

			if (CurveX.Count > 1)
				foreach (Curve curve in CurveY)
				{
					points.Clear();

					for (int i = 0; i < curve.Count; i++)
					{
						pt.X = (float)ScaleX.ValueToScreen(curve.AvgList[i]);
						pt.Y = (float)ScaleY.ValueToScreen(curve[i]);
						if (pt.X >= ScaleX.MinScreenValue && pt.X <= ScaleX.MaxScreenValue &&
							pt.Y <= ScaleY.MinScreenValue && pt.Y >= ScaleY.MaxScreenValue)
							points.Add(pt);
					}

					if (points.Count > 1)
						using (Pen pen = new Pen(curve.Color, curve.Width))
							pe.Graphics.DrawCurve(pen, points.ToArray());
				}
		}

		protected virtual void DrawPoint(PaintEventArgs pe)
		{
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);

			DrawAxes(pe);

			if (!DesignMode)
				switch (PlotStyle)
				{
					case PlotStyle.Block:
						DrawBlock(pe);
						break;
					case PlotStyle.Line:
						DrawLine(pe);
						break;
					case PlotStyle.Point:
						DrawPoint(pe);
						break;
				}

			if (Capture)
				ControlPaint.DrawFocusRectangle(pe.Graphics, FocusBounds);
		}

		protected override Cursor DefaultCursor { get { return Cursors.Cross; } }

		private void Plot_SizeChanged(object sender, EventArgs e)
		{
			ScaleX.MinScreenValue = 40;
			ScaleY.MaxScreenValue = 20;
			ScaleX.MaxScreenValue = Width - ScaleX.MinScreenValue - 20;
			ScaleY.MinScreenValue = Height - ScaleY.MaxScreenValue - 20;
			Invalidate();
		}

		private void Plot_MouseDown(object sender, MouseEventArgs e)
		{
			FocusLocation = PointToClient(MousePosition);
			Capture = true;
		}

		private void Plot_MouseMove(object sender, MouseEventArgs e)
		{
			if (Capture)
				Invalidate();
		}

		private void Plot_MouseUp(object sender, MouseEventArgs e)
		{
			switch (e.Button)
			{
				case MouseButtons.Left:
					Rectangle rt = FocusBounds;

					Capture = false;

					if (rt.Width == 0 || rt.Height == 0)
					{
						ScaleX = new Scale()
						{
							MinScreenValue = ScaleX.MinScreenValue,
							MaxScreenValue = ScaleX.MaxScreenValue,
							MinValue = ScaleX.ScreenToValue(e.X + (ScaleX.MinScreenValue - ScaleX.MaxScreenValue) / 4),
							MaxValue = ScaleX.ScreenToValue(e.X + (ScaleX.MaxScreenValue - ScaleX.MinScreenValue) / 4),
							Exponential = ScaleX.Exponential
						};

						ScaleY = new Scale()
						{
							MinScreenValue = ScaleY.MinScreenValue,
							MaxScreenValue = ScaleY.MaxScreenValue,
							MinValue = ScaleY.ScreenToValue(e.Y + (ScaleY.MinScreenValue - ScaleY.MaxScreenValue) / 4),
							MaxValue = ScaleY.ScreenToValue(e.Y + (ScaleY.MaxScreenValue - ScaleY.MinScreenValue) / 4),
							Exponential = ScaleY.Exponential
						};
					}
					else
					{
						ScaleX = new Scale()
						{
							MinScreenValue = ScaleX.MinScreenValue,
							MaxScreenValue = ScaleX.MaxScreenValue,
							MinValue = ScaleX.ScreenToValue(rt.Left),
							MaxValue = ScaleX.ScreenToValue(rt.Right),
							Exponential = ScaleX.Exponential
						};

						ScaleY = new Scale()
						{
							MinScreenValue = ScaleY.MinScreenValue,
							MaxScreenValue = ScaleY.MaxScreenValue,
							MinValue = ScaleY.ScreenToValue(rt.Bottom),
							MaxValue = ScaleY.ScreenToValue(rt.Top),
							Exponential = ScaleY.Exponential
						};
					}

					Invalidate();
					break;

				case MouseButtons.Right:

					ScaleX = new Scale()
					{
						MinScreenValue = ScaleX.MinScreenValue,
						MaxScreenValue = ScaleX.MaxScreenValue,
						MinValue = ScaleX.ScreenToValue(e.X + ScaleX.MinScreenValue - ScaleX.MaxScreenValue),
						MaxValue = ScaleX.ScreenToValue(e.X + ScaleX.MaxScreenValue - ScaleX.MinScreenValue),
						Exponential = ScaleX.Exponential
					};

					ScaleY = new Scale()
					{
						MinScreenValue = ScaleY.MinScreenValue,
						MaxScreenValue = ScaleY.MaxScreenValue,
						MinValue = ScaleY.ScreenToValue(e.Y + ScaleY.MinScreenValue - ScaleY.MaxScreenValue),
						MaxValue = ScaleY.ScreenToValue(e.Y + ScaleY.MaxScreenValue - ScaleY.MinScreenValue),
						Exponential = ScaleY.Exponential
					};

					Invalidate();
					break;
			}
		}

		private void Plot_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && CurveX.Count > 0 && CurveY.Count > 0)
				if (CurveY[0].Count > 0)
				{
					double minX = CurveX[0], maxX = CurveX[0], minY = CurveY[0][0], maxY = CurveY[0][0];

					foreach (double value in CurveX)
					{
						if (value < minX)
							minX = value;
						if (value > maxY)
							maxX = value;
					}

					foreach (Curve curve in CurveY)
						foreach (double value in curve)
						{
							if (value < minY)
								minY = value;
							if (value > maxY)
								maxY = value;
						}

					if (minY < maxY && minX < maxX)
					{
						ScaleX.MinValue = minX;
						ScaleX.MaxValue = maxX;
						ScaleY.MinValue = minY;
						ScaleY.MaxValue = maxY;

						Invalidate();
					}
				}
		}
	}

	public class Scale
	{
		public double MinValue { get; set; }
		public double MaxValue { get; set; }
		public int MinScreenValue { get; set; }
		public int MaxScreenValue { get; set; }
		public bool Exponential { get; set; }

		public override string ToString()
		{
			return "{" + MinValue + " : " + MaxValue + " => " + MinScreenValue + " : " + MaxScreenValue +
				", " + (Exponential ? "Логарифмический" : "Линейный");
		}

		public double ScreenToValue(int screenValue)
		{
			return Exponential ?
				Math.Pow(MaxValue / MinValue, ((double)(screenValue - MinScreenValue)) /
				(MaxScreenValue - MinScreenValue)) * MinValue :
				MinValue + (((double)(screenValue - MinScreenValue)) / (MaxScreenValue - MinScreenValue)) * (MaxValue - MinValue);
		}

		public double ValueToScreen(double value)
		{
			double d = MinScreenValue +
				(Exponential ? Math.Log10(value / MinValue) / Math.Log10(MaxValue / MinValue) :
				(value - MinValue) / (MaxValue - MinValue)) * (MaxScreenValue - MinScreenValue);
			if (d == double.PositiveInfinity)
				return Math.Max(MaxScreenValue, MinScreenValue);
			if (d == double.NegativeInfinity)
				return Math.Min(MaxScreenValue, MinScreenValue);
			return d;
		}
	}

	[Serializable]
	public class AvgList : List<double>
	{
		public int AvgCountCur { get; private set; }
		public int AvgCount { get; set; }

		public void AddAvg(double item)
		{
			if (AvgCountCur >= AvgCount)
			{
				AvgCountCur = 0;
				Add(item);
			}
			else
			{
				int i = Count - 1;

				AvgCountCur++;
				this[i] = (this[i] * AvgCountCur + item) / (AvgCountCur + 1);
			}
		}
	}

	[Serializable]
	public class Curve : List<double>
	{
		public Color Color { get; set; }
		public int Width { get; set; }
		public AvgList AvgList { get; set; }

		public void AddAvg(double item)
		{
			if (AvgList.AvgCountCur >= AvgList.AvgCount)
				Add(item);
			else
			{
				int i = Count - 1;

				this[i] = (this[i] * AvgList.AvgCountCur + item) / (AvgList.AvgCountCur + 1);
			}
		}
	}


	public struct PointD
	{
		public double X { get; set; }
		public double Y { get; set; }
	}


	public enum PlotStyle { Block, Line, Point }
}
