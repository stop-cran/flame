using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	/// <summary>
	/// Двухатомная молекула
	/// </summary>
	public class Молекула
	{
		public Молекула()
		{ }

		public Молекула(Молекула m)
		{
			D = m.D;
			Вещество = m.Вещество;
			Атом = m.Атом;
			ħω = m.ħω;
			αħω = m.αħω;
			ħωвр = m.ħωвр;
			Mred = m.Mred;
			Расстояние = m.Расстояние;
			P = m.P;
			Q = m.Q;
			θ = m.θ;
			Уровни = m.Уровни;
		}

		public int Вещество { get; set; }
		public int Атом { get; set; }
		/// <summary>
		/// Приведённая масса
		/// </summary>
		public double Mred { get; set; }
		public double Расстояние { get; set; }
		/// <summary>
		/// Межъядерное расстояние
		/// </summary>
		public double D { get; set; }
		public double P { get; set; }
		public double Q { get; set; }
		public double θ { get; set; }
		/// <summary>
		/// Колебательный квант, см^-1. Екол(v) = ħω * (v + 1/2) + αħω * (v + 1/2) ^ 2
		/// </summary>
		public double ħω { get; set; }
		/// <summary>
		/// Константа ангармонизма, см^-1.
		/// </summary>
		public double αħω { get; set; }
		public double ħωвр { get; set; }
		int уровни;
		public int Уровни
		{
			get { return уровни; }
			set
			{
				уровни = value;
				X = new double[уровни];
				dXdt0 = new double[уровни];
				dXdt = new double[уровни];
			}
		}
		/// <summary>
		/// Мольные доли молекул с определённым колебательным уровнем.
		/// </summary>
		public double[] X { get; private set; }
		/// <summary>
		/// Производные мольные долей по времени молекул с определённым колебательным уровнем, без учёта влияния электрического поля.
		/// </summary>
		public double[] dXdt0 { get; private set; }
		/// <summary>
		/// Производные мольные долей по времени молекул с определённым колебательным уровнем.
		/// </summary>
		public double[] dXdt { get; private set; }

		/// <summary>
		/// Энергия колебательного уровня с квантовым числом v, эВ.
		/// </summary>
		/// <param name="v">Колебательный уровень</param>
		public double E(int v)
		{
			return 1.2399e-004 * (v + 0.5) * (ħω - αħω * (v + 0.5));
		}

		/// <summary>
		/// Задать распределение Больмана по колебательным уровням.
		/// </summary>
		/// <param name="ɣ">Мольная доля молекулы в смеси</param>
		/// <param name="T">Температура колебательного распределения</param>
		public void SetBolzmann(double ɣ, double T)
		{
			if (ɣ == 0)
				for (int v = 0; v < Уровни; v++)
					X[v] = 0;
			else
				if (T == 0)
				{
					X[0] = ɣ;
					for (int v = 1; v < Уровни; v++)
						X[v] = 0;
				}
				else
					if (double.IsInfinity(T))
						for (int v = 0; v < Уровни; v++)
							X[v] = ɣ / Уровни;
					else
					{
						double Z = 0;

						for (int v = 0; v < Уровни; v++)
							Z += (X[v] = Math.Exp(-11610d * E(v) / T));
						Z /= ɣ;
						for (int v = 0; v < Уровни; v++)
							X[v] /= Z;
					}
		}

		static protected double F(double x)
		{
			return (x > 20) ? (8 * Math.Sqrt(Math.PI / 3) * Math.Exp(7 * Math.Log(x) / 3 -
				3 * Math.Exp(2 * Math.Log(x) / 3))) : (0.5 * (3 - Math.Exp(-2 * x / 3)) * Math.Exp(-2 * x / 3));
		}

		/// <summary>
		/// Базовый метод добавляет к dXdt0[i] величину dɣdt0[Вещество] и
		/// вычисляет вклад молекулы в Engine.dΓdt0 (сумму dXdt0[i] минус Engine.dɣdt0[Вещество]).
		/// </summary>
		/// <returns></returns>
		public virtual void CalculateVKin()
		{ }

		public double CalculatedΓdt0()
		{
			int i;
			double dɣdt0 = Engine.dɣdt0[Вещество], dΓdt0 = -dɣdt0;

			if (dɣdt0 < 0)
				for (i = 0; i < Уровни; i++)
				{
					dXdt0[i] += dɣdt0 * X[i];
					dΓdt0 += dXdt0[i];
				}
			else
			{
				dXdt0[0] += dɣdt0;
				for (i = 0; i < Уровни; i++)
					dΓdt0 += dXdt0[i];
			}

			return dΓdt0;
		}

		public double CalculatedΓdt()
		{
			int i;
			double dɣdt1 = Engine.dɣdt[Вещество], dΓdt = -dɣdt1;

			if (dɣdt1 < 0)
				for (i = 0; i < Уровни; i++)
				{
					dXdt[i] += dɣdt1 * X[i];
					dΓdt += dXdt[i];
				}
			else
			{
				dXdt[0] += dɣdt1;
				for (i = 0; i < Уровни; i++)
					dΓdt += dXdt[i];
			}

			return dΓdt;
		}

		public void CopyX()
		{
			for (int i = 0; i < Уровни; i++)
				dXdt[i] = dXdt0[i];
		}

		public void TimeStep()
		{
			double dt = Engine.dt;

			for (int i = 0; i < Уровни; i++)
			{
				X[i] += dXdt[i] * dt;
				if (X[i] < Engine.ɣMin)
					X[i] = 0;
			}

			double s = X.Sum();

			if (s == 0)
				SetBolzmann(Engine.ɣ[Вещество], Engine.T);
			else
			{
				double Г = Engine.ɣ[Вещество] / s;

				for (int i = 0; i < Уровни; i++)
					X[i] *= Г;
			}
		}

		public void Reset()
		{
			for (int i = 0; i < Уровни; i++)
				dXdt[i] = dXdt0[i] = 0;
		}
	}

	public class МолекулаH2 : Молекула
	{
		public МолекулаH2(Молекула m) : base(m) { }

		public override void CalculateVKin()
		{
			int v, w;
			double k, G, N = Engine.N, T = Engine.T,
				T2 = Math.Sqrt(T / 300),	// (T/300)^( 1/2)
				T3 = Math.Pow(T / 300, -1d / 3d),	// (T/300)^(-1/3)
				T4 = Math.Sqrt(T2),		// (T/300)^( 1/4)
				kVT = 7.79e+013 * T2 * Math.Exp(-14.02 * T3),
				kVV = 2.55e+009 * T3 * Math.Exp(-0.0572 * T3);


			for (v = 1; v < Уровни; v++)
			{
				for (w = 0; w < Уровни; w++)
				{
					// VT - обмен	H2(X,v) + H2(X,w) <=> H2(X,v-1) + H2(X,w)
					k = kVT * v * Math.Exp(0.97 * T3 * (v - 1) + (0.287 / T2) * w);
					//			G = k*N*X[w]*(  X[v] - X[v-1]*Math.Exp( -1.4388*(ħω-2*αħω*v)/Tg )  );
					G = k * N * X[w] * X[v];
					dXdt0[v - 1] += G;
					dXdt0[v] -= G;
				}
			}

			for (v = 0; v < Уровни - 1; v++)
				for (w = 0; w < Уровни - 1; w++)
				{
					if (w >= v)
						continue;

					// VV - обмен H2(X,v) + H2(X,w+1) <=> H2(X,v+1) + H2(X,w)

					k = kVV * (v + 1) * (w + 1) * (1.5 - 0.5 * Math.Exp(-0.21 * T2 * (v - w))) *
						Math.Exp(0.236 * T4 * (v - w));
					//			G = k*N*( X[v]*X[w+1] - X[v+1]*X[w]*Math.Exp( 2.8775*αħω*(v-w)/Tg ) );
					G = k * N * X[v] * X[w + 1];
					dXdt0[v + 1] += G;
					dXdt0[w] += G;
					dXdt0[v] -= G;
					dXdt0[w + 1] -= G;
				}
		}
	}

	public class МолекулаN2 : Молекула
	{
		double α, Z0;

		public МолекулаN2(Молекула m)
			: base(m)
		{
			α = αħω / ħω;
			Z0 = Расстояние * Расстояние / Math.Sqrt(Mred);
		}

		public override void CalculateVKin()
		{
			int v, w;
			double k, G, T = Engine.T, T2 = Math.Sqrt(T), T13 = Math.Pow(T, -1d / 3d), N = Engine.N;
			double kVT01, kVTat01, kVV, gVT, gVV, vc, deltaVT1, deltaVT2;

			//	memset(D0dGammadt+i0,0,CE[1].V*sizeof(double));

			gVT = Math.Sqrt(θ / (8 * T));
			kVT01 = (4.5713e-012 * 6.022e+023) * Z0 * T2 * P * T;
			gVV = α * Math.Sqrt(θ / (2 * T));
			kVV = (4.5713e-012 * 6.022e+023) * Z0 * T2 * Q * T;
			kVTat01 = (T <= 2000 ? 5.64e+011 * T2 * Math.Exp(-115.50 * T13) :
				3.19e+015 * T2 * Math.Exp(-224.36 * T13));
			vc = 81.4825 - 1.6506 * T2;
			deltaVT1 = 2.4337 * T13;
			deltaVT2 = 5.34 / T2;

			for (v = 1; v < Уровни; v++)
			{

				// VT - обмен	N2(X,v) + N2(X) <=> N2(X,v-1) + N2(X)

				k = kVT01 * (v / (1 - α * v)) * F(gVT * (1 - 2 * α * v));
				G = k * N * Engine.ɣ[Вещество] * (X[v] - X[v - 1] * Math.Exp(-1.4388 * (ħω - 2 * αħω * v) / T));
				dXdt0[v - 1] += G;
				dXdt0[v] -= G;
				// VT - обмен	N2(X,v) + N => N2(X,v-1) + N
				/*		if( v<=vc )	deltaVT = deltaVT1;
						else		deltaVT = deltaVT2;
						k = kVTat01 * (  v * Math.Exp( (v-1)*deltaVT ) - (v-1) * Math.Exp( (v-2)*deltaVT ) * Math.Exp( -(3353-41.2*(v-1))/T )  );
						G = k*N*D0gamma[v] * D0gamma[CE[1].elA];
						D0dGammadt[i0+v-1] += G;
						D0dGammadt[i0+v]   -= G;
				*/
				for (w = 1; w < v; w++)
				{
					// VV - обмен N2(X,v) + N2(X,w-1) <=> N2(X,v-1) + N2(X,w)
					k = kVV * (v / (1 - α * v)) * (w / (1 - α * w)) * F(gVV * (v - w));
					G = k * N * (X[v] * X[w - 1] - X[v - 1] * X[w] * Math.Exp(2.8775 * αħω * (v - w) / T));
					dXdt0[v - 1] += G;
					dXdt0[w] += G;
					dXdt0[v] -= G;
					dXdt0[w - 1] -= G;
				}
			}
		}
	}
}
