namespace Flame3
{
	partial class Form2
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.plot1 = new Flame3.Plot();
			this.SuspendLayout();
			// 
			// plot1
			// 
			this.plot1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.plot1.Location = new System.Drawing.Point(0, 0);
			this.plot1.Name = "plot1";
			this.plot1.PlotStyle = Flame3.PlotStyle.Line;
			this.plot1.ScaleX.Exponential = false;
			this.plot1.ScaleX.MaxScreenValue = 680;
			this.plot1.ScaleX.MaxValue = 1;
			this.plot1.ScaleX.MinScreenValue = 40;
			this.plot1.ScaleX.MinValue = 0;
			this.plot1.ScaleY.Exponential = false;
			this.plot1.ScaleY.MaxScreenValue = 20;
			this.plot1.ScaleY.MaxValue = 1;
			this.plot1.ScaleY.MinScreenValue = 429;
			this.plot1.ScaleY.MinValue = 0;
			this.plot1.Size = new System.Drawing.Size(740, 469);
			this.plot1.TabIndex = 0;
			this.plot1.Text = "plot1";
			// 
			// Form2
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(740, 469);
			this.Controls.Add(this.plot1);
			this.Name = "Form2";
			this.Text = "Form2";
			this.ResumeLayout(false);

		}

		#endregion

		private Plot plot1;
	}
}