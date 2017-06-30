using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Flame3
{
	public partial class FormAsk : Form
	{
		public FormAsk()
		{
			InitializeComponent();
		}

		private void FormAsk_Load(object sender, EventArgs e)
		{
			ThreadPool.QueueUserWorkItem(state =>
				{
					Thread.Sleep(20000);
					Invoke((MethodInvoker)(() => DialogResult = DialogResult.OK));
				});
		}
	}
}
