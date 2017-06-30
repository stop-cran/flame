using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	public class Вещество
	{
		enum TermDynData { None, Cp, H }
		const double _T0 = 2.98e-6;
		/// <summary>
		/// Степени _T0
		/// </summary>
		static readonly double[] T0 = new double[9];
		/// <summary>
		/// Степени температуры/10000
		/// </summary>
		static readonly double[] T = new double[9];
		static double _T, lnT;

		static Вещество()
		{
			T0[0] = T[0] = 1;
			for (int i = 1; i < T0.Length; i++)
				T0[i] = T0[i - 1] * _T0;
		}

		public Вещество()
		{
			Significant = true;
			UserSignificant = true;
		}

		/// <summary>
		/// Cp = Cpa[0] + Cpa[1]*T + Cpa[2]*T^2 + Cpa[3]*T^3 + Cpa[4]*T^4 + Cpa[5]*T^5
		/// </summary>
		double[] cpa;
		/// <summary>
		/// H = Ha[0] + Ha[1]*T^-1 + Ha[2]*T + Ha[3]*T^2 + Ha[4]*T^3 + Ha[5]*T^4 + Ha[6]*T^5 + Ha[7]*T^6 + Ha[8]*T^7 + Ha[9]*T^8
		/// </summary>
		double[] ha;
		TermDynData флаг_Cp = TermDynData.None;

		public double σRot { get; set; }
		public Сечение CrossSection { get; private set; }
		public Сечение NewCrossSection
		{
			get
			{
				if (CrossSection == null)
					CrossSection = new Сечение() { M = M };
				return CrossSection;
			}
		}

		public void SetCp(params double[] cp)
		{
			флаг_Cp = TermDynData.Cp;

			if (cp.Length != 6)
				throw new ArgumentException("Число коэффициентов должно быть равно 10");

			cpa = cp;
		}

		public void SetH(params double[] h)
		{
			флаг_Cp = TermDynData.H;

			if (h.Length != 10)
				throw new ArgumentException("Число коэффициентов должно быть равно 10");

			ha = h;
		}

		public void SetGibbs(params double[] gibbsK)
		{
			if (gibbsK.Length != 7)
				throw new ArgumentException("Число коэффициентов должно быть равно 7");

			GibbsK = gibbsK;
			HasGibbs = true;
		}

		public int Номер { get; set; }
		public double Hform { get; set; }
		public double Hform0K { get; set; }
		public int M { get; set; }
		public int Z { get; set; }
		public double Cp { get; set; }
		public double H { get; set; }
		public double Gibbs { get; set; }
		double[] GibbsK { get; set; }
		public bool HasGibbs { get; set; }
		public string Формула { get; set; }
		public bool Significant { get; set; }
		public bool UserSignificant { get; set; }
		public bool NewUserSignificant { get; set; }
		public double Derivative { get; set; }
		public double AbsDerivative { get; set; }
		public double OscillationFactor { get; private set; }
		public void CalcOscillationFactor()
		{
			OscillationFactor = Math.Abs(Derivative) / AbsDerivative;
			Derivative = 0;
			AbsDerivative = 0;
		}

		[Optimized]
		public static void OnTChanged()
		{
			_T = Engine.T * 1e-4;
			for (int i = 1; i < 9; i++)
				T[i] = T[i - 1] * _T;
			lnT = Math.Log(_T);
		}

		[Optimized]
		public void CalculateTermDynData()
		{
			switch (флаг_Cp)
			{
				case TermDynData.Cp:
					Cp = cpa[0] + cpa[1] * _T + cpa[2] * T[2] + cpa[3] * T[3] + cpa[4] * T[4] + cpa[5] * T[5];
					H = Hform + cpa[0] * (_T - T0[1]) + cpa[1] * (T[2] - T0[2]) / 2 + cpa[2] * (T[3] - T0[3]) / 3 +
						cpa[3] * (T[4] - T0[4]) / 4 + cpa[5] * (T[6] - T0[6]) / 6;
					break;

				case TermDynData.H:
					H = Hform + ha[0] + ha[1] / _T + ha[2] * _T + ha[3] * T[2] + ha[4] * T[3] + ha[5] * T[4] + ha[6] * T[5] + ha[7] * T[6] +
						ha[8] * T[7] + ha[9] * T[8];
					Cp = -ha[1] / T[2] + ha[2] + 2 * ha[3] * _T + 3 * ha[4] * T[2] + 4 * ha[5] * T[3] + 5 * ha[6] * T[4] + 6 * ha[7] * T[5] +
						7 * ha[8] * T[6] + 8 * ha[9] * T[7];
					break;
			}

			if (HasGibbs)
				Gibbs = GibbsK[0] + GibbsK[1] * lnT + GibbsK[2] / T[2] + GibbsK[3] / _T +
				   GibbsK[4] * _T + GibbsK[5] * T[2] + GibbsK[6] * T[3];
		}
	}
}
