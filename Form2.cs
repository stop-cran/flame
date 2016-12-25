using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Flame3
{
	public partial class Form2 : Form
	{
		public Form2()
		{
			InitializeComponent();
			plot1.CurveY.Add(new Curve() { Color = Color.Red, Width = 1, AvgList = plot1.CurveX });
			plot1.Add(0, 0);
			plot1.Add(0.05, 0.0025);
			plot1.Add(0.1, 0.01);
			plot1.Add(0.2, 0.04);
			plot1.Add(0.3, 0.09);
			plot1.Add(0.4, 0.16);
		}
	}
}
