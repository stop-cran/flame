namespace Flame3
{
	public static partial class Engine
	{
		public class CalcBase
		{
			public System.Threading.AutoResetEvent _ev;
		}


		public class CalcABCState
		{
			public int j;
			public int iBegin;
			public int iEnd;
			public double S;
			public double S1;
			public double S2;
		}
	}


	public struct НумерованноеСечение
	{
		public int Номер { get; set; }
		public double[] σ { get; set; }
		public double[] σM { get; set; }
	}


	public struct ТочкаСечения
	{
		public double ε { get; set; }
		public double σ { get; set; }
	}
}
