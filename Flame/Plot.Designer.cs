namespace Flame3
{
    partial class Plot
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.SuspendLayout();
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
			// 
			// Plot
			// 
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Plot_MouseMove);
			this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.Plot_MouseDoubleClick);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Plot_MouseDown);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Plot_MouseUp);
			this.SizeChanged += new System.EventHandler(this.Plot_SizeChanged);
			this.ResumeLayout(false);

        }

        #endregion

		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
    }
}
