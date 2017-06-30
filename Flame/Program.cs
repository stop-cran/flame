using System;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace KryptonForm
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			ThreadPool.QueueUserWorkItem(CloseLoop);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}

		[DllImport("user32.dll")]
		extern static IntPtr FindWindow(IntPtr winClass, IntPtr winCaption);
		[DllImport("user32.dll")]
		extern static IntPtr SendMessage(IntPtr hWnd, uint Msg, short wParam, int lParam);


		static void CloseLoop(object state)
		{
			IntPtr handle, winCaption = Marshal.StringToHGlobalAnsi("Unlicensed application");
			DateTime dt = DateTime.Now;

			for (int i = 0; i < 2 && (DateTime.Now - dt).Seconds < 10; )
			{
				Thread.Sleep(100);
				handle = FindWindow(IntPtr.Zero, winCaption);
				if (handle != IntPtr.Zero)
				{
					SendMessage(handle, 0x0010, 0, 0);
					i++;
				}
			}

			Marshal.FreeHGlobal(winCaption);
		}
	}
}