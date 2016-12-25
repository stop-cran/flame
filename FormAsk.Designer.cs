namespace Flame3
{
	partial class FormAsk
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
			this.bnOK = new System.Windows.Forms.Button();
			this.bnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// bnOK
			// 
			this.bnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.bnOK.Location = new System.Drawing.Point(12, 12);
			this.bnOK.Name = "bnOK";
			this.bnOK.Size = new System.Drawing.Size(75, 23);
			this.bnOK.TabIndex = 0;
			this.bnOK.Text = "ОК";
			this.bnOK.UseVisualStyleBackColor = true;
			// 
			// bnCancel
			// 
			this.bnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.bnCancel.Location = new System.Drawing.Point(94, 12);
			this.bnCancel.Name = "bnCancel";
			this.bnCancel.Size = new System.Drawing.Size(75, 23);
			this.bnCancel.TabIndex = 1;
			this.bnCancel.Text = "Отмена";
			this.bnCancel.UseVisualStyleBackColor = true;
			// 
			// FormAsk
			// 
			this.AcceptButton = this.bnOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.bnCancel;
			this.ClientSize = new System.Drawing.Size(187, 48);
			this.Controls.Add(this.bnCancel);
			this.Controls.Add(this.bnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormAsk";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Продолжить расчёт?";
			this.TopMost = true;
			this.Load += new System.EventHandler(this.FormAsk_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button bnOK;
		private System.Windows.Forms.Button bnCancel;
	}
}