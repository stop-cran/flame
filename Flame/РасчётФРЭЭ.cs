using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Flame3
{
	public static partial class Engine
	{
		public static class РасчётФРЭЭ
		{
			static double[] EEDFold, _kappa, _d, A, B, C, C0, _F, p, q, r;

			/// <summary>
			/// Частота упругих столкновений, 1/с.
			/// </summary>
			public static double Kel { get; private set; }
			/// <summary>
			/// Шаг по шкале энергии.
			/// </summary>
			public static double Δε
			{
				get
				{
					return _Δε;
				}
				set
				{
					_Δε = value;
					εInvalidated = true;
				}
			}
			public static int εLength
			{
				get
				{
					return _εLength;
				}
				set
				{
					_εLength = value;
					εInvalidated = true;
				}
			}

			public static int εCalcLength { get; set; }
			public static int εApprFrom { get; set; }
			public static int εApprTo { get; set; }

			static double[] EEDF { get; set; }
			/// <summary>
			/// Сетка ФРЭЭ. Пока равномерная, шаг Δε, количество узлов εLength.
			/// </summary>
			public static double[] ε { get; private set; }
			static double _Δε, _εPrecision;
			static int _εLength;
			/// <summary>
			/// При следующем расчёте ФРЭЭ надо заново задать сетку.
			/// </summary>
			static bool εInvalidated { get; set; }
			public static double εPrecision
			{
				get
				{
					return _εPrecision;
				}
				set
				{
					_εPrecision = value;
					εInvalidated = true;
				}
			}

			/// <summary>
			/// Полное транспортное сечение смеси.
			/// </summary>
			public static double[] σTr { get; private set; }
			/// <summary>
			/// Сумма транспортных сечений компонент смеси делить на их молярную массу.
			/// </summary>
			public static double[] σTrM { get; private set; }

			/// <summary>
			/// Отношение величины электрического поля к его частоте, В/см*ГГц.
			/// </summary>
			public static double Eω { get; set; }

			public static void ResetEEDF()
			{
				for (int i = 0; i < ВеществаLength; i++)
					ɣToCompare[i] = 0;
			}

			const double α = 1;

			/// <summary>
			/// Истина, если медленно меняющееся поле (ν ст >> ω), иначе высокочастотное.
			/// </summary>
			public static bool DC { get; set; }

			static double lnΛ;

			static void CalcABC()
			{
				int i, j = 1;
				double d, S, S1, S2, _sqrti, _sqrth; // β = 1 - α

				dtMax = 1;
				lnΛ = Te == 0 ? 10 : 3 * Math.Log(Te * 6945308.7066610157464132630340821) - Math.Log(ɣ[Ne] * Nc);

				//for (n = 0; n < M2; n++)
				//{
				//    B0sigmaRe += ɣ[CE[n].elN] * CE[n].b * CE[n].sigmaRe;
				//}

				S = S1 = S2 = 0;
				for (i = 0; i < εCalcLength; i++) S += EEDF[i];
				states[0].S = S;

				for (i = 1; i < εCalcLength; i++)
				{
					if (j < ev.Length)
						if (i == states[j - 1].iEnd)
						{
							states[j].S = S;
							states[j].S1 = S1;
							states[j].S2 = S2;
							j++;
						}

					_sqrti = Math.Sqrt(i);
					d = EEDF[i];
					S -= d;
					d *= _sqrti;
					S1 += d;
					S2 += d * i;
				}

				states[ev.Length - 1].iEnd = εCalcLength;
				for (j = 0; j < ev.Length; j++)
					ThreadPool.QueueUserWorkItem(CalcABCHelper, states[j]);
				WaitHandle.WaitAll(ev);

				_kappa[1] = 1; _d[1] = 0;
				_sqrth = Math.Sqrt(Δε);

				for (i = 1; i < εCalcLength - 1; i++)
				{
					C[i] = C0[i] + (_sqrth * Math.Sqrt(i)) / (Nc * dtMax);
					_kappa[i + 1] = B[i] / (C[i] - _kappa[i] * A[i]);
				}
			}

			static void CalcABCHelper(object state)
			{
				CalcABCState stateAbc = state as CalcABCState;
				int i;
				double dσTr, dσTrM, _d, _sqrti, B0sigmaRe = 0, S = stateAbc.S, S1 = stateAbc.S1, S2 = stateAbc.S2;
				for (i = stateAbc.iBegin; i < stateAbc.iEnd; i++)
				{
					if (i == 1)
					{
						dσTr = σTr[2] - σTr[1];
						dσTrM = σTrM[2] - σTrM[1];
					}
					else
						if (i == εCalcLength - 1)
						{
							dσTr = σTr[εCalcLength - 1] - σTr[εCalcLength - 2];
							dσTrM = σTrM[εCalcLength - 1] - σTrM[εCalcLength - 2];
						}
						else
						{
							dσTr = 0.5 * (σTr[i + 1] - σTr[i - 1]);
							dσTrM = 0.5 * (σTrM[i + 1] - σTrM[i - 1]);
						}

					p[i] = (DC ? 1.9770e-011 * θ * θ * i / (Δε * σTr[i]) :
						6.9544e-012 * Eω * Eω * i * i * σTr[i]) +
						5.6073e-016 * T * σTrM[i] * i * i;

					q[i] = (DC ? 1.9770e-011 * θ * θ * (1 - i * dσTr / σTr[i]) / (Δε * σTr[i]) :
						6.9544e-012 * Eω * Eω * i * (2 * σTr[i] + i * dσTr)) +
						6.5069e-012 * Δε * i * i * σTrM[i] +
						5.6073e-016 * T * i * (2 * σTrM[i] + i * dσTrM) +
						2.9414e-012 * i * B0sigmaRe;

					r[i] = 6.5069e-012 * Δε * i * (2 * σTrM[i] + i * dσTrM) +
							2.9414e-012 * B0sigmaRe - dɣdt0[Ne] * Math.Sqrt(Δε * i) / Nc;//переделать, dsigmaRe !!!

					// интеграл столкновений Ландау
					_sqrti = Math.Sqrt(i);

					_d = ɣ[Ne] * lnΛ * Math.Sqrt(Δε);

					p[i] += 1.6490e-023 * _d * (S2 + i * _sqrti * S);
					q[i] += 1.0993e-023 * _d * (S1 + i * _sqrti * S);
					r[i] += 1.6490e-023 * _d * _sqrti * EEDF[i];

					_d = EEDF[i];
					S -= _d;
					_d *= _sqrti;
					S1 += _d;
					S2 += _d * i;

					/*	
						A[i]  = p[i] + β*dt*0.25*q[i]*(q[i]+q[i-1]) - β*0.5*q[i];
						B[i]  = p[i] + β*dt*0.25*q[i]*(q[i+1]+q[i]) + β*0.5*q[i];
						C0[i] = 2*p[i] + β*dt*0.25*q[i]*( q[i+1] + 2*q[i]+q[i-1] );
					*/
					if (q[i] > 0)
					{
						A[i] = p[i];
						B[i] = p[i] + q[i];
						C0[i] = 2 * p[i] + q[i];
					}
					else
					{
						A[i] = p[i] - q[i];
						B[i] = p[i];
						C0[i] = 2 * p[i] - q[i];
					}

					if (i == 0 || i > 3 * εCalcLength / 4)
						continue;

					if (q[i] > 0)
						if (dtMax > Math.Sqrt(Δε * i) / (Nc * q[i]))
							dtMax = Math.Sqrt(Δε * i) / (Nc * q[i]);
					if (q[i] < 0)
						if (dtMax > -Math.Sqrt(Δε * i) / (Nc * q[i]))
							dtMax = -Math.Sqrt(Δε * i) / (Nc * q[i]);
				}

				ev[stateAbc.j].Set();
			}

			static void CalcDVL()
			{
				for (int i = 1; i < εCalcLength - 1; i++)
					_F[i] = 0;

				states[ev.Length - 1].iEnd = εCalcLength - 1;
				for (int k = 0; k < ev.Length; k++)
					ThreadPool.QueueUserWorkItem(
						(object state) =>
						{
							var stateAbc = state as CalcABCState;

							for (int i = stateAbc.iBegin; i < stateAbc.iEnd; i++)
							{
								double Stf;
								_F[i] = EEDF[i] * r[i] + EEDF[i] * Math.Sqrt(ε[i]) / (Nc * dtMax);
								//+	alpha*q[i] * ( 0.5*(EEDF[i+1]-EEDF[i-1]) + dtMax*0.25*( (q[i+1]+q[i])*EEDF[i+1] - (q[i+1]+2*q[i]+q[i-1])*EEDF[i] + (q[i]+q[i-1])*EEDF[i-1] ) );

								Stf = 0;

								foreach (var R in Реакции7Ред)
								{
									double[] σ = R.Сечение.σ;
									double ɣ_прод_1, ɣ_комп_1 = R.Ур_комп_1 == -1 ? ɣ[R.Комп_1] : R.X_комп_1;
									int j, iv0;

									Stf -= ɣ_комп_1 * i * σ[i] * EEDF[i];	//	x0 !!!
									iv0 = Convert.ToInt32(R.Порог / Δε);
									switch (R.Тип_столкновения)
									{
										case 1: // соударение
											j = i + iv0;
											if (j < εCalcLength)
												Stf += j * σ[j] * ɣ_комп_1 * EEDF[j];
											break;
										case 2: // ионизация
											j = 2 * i + iv0;
											if (j < εCalcLength)
												Stf += 2 * ɣ_комп_1 * j * σ[j] * EEDF[j];	//	x0 !!!
											break;
										case 3: // прилипание
											break;
										case 4: // возбуждение, идёт обратная по принципу детального равновесия.
											ɣ_прод_1 = R.Ур_прод_1 == -1 ? ɣ[R.Продукт_1] : R.X_прод_1;
											if (i > iv0)
												Stf += ɣ_прод_1 * i * σ[i] * EEDF[i - iv0];
											j = i + iv0;
											if (j < εCalcLength)
												Stf += j * σ[j] * (ɣ_комп_1 * EEDF[j] - ɣ_прод_1 * EEDF[i]);
											break;
									}
								}

								_F[i] += 5.9310e-009 * Δε * Stf;
							}

							ev[stateAbc.j].Set();
						}, states[k]);

				WaitHandle.WaitAll(ev);
				_d[1] = 0;

				for (int i = 1; i < εCalcLength - 1; i++)
					_d[i + 1] = (A[i] * _d[i] + _F[i]) / (C[i] - A[i] * _kappa[i]);
			}

			static double[] ɣToCompare;
			static double θToCompare;

			static void Normalize()
			{
				double d;
				int i;

				for (d = 0, i = 0; i < εLength; i++)
					d += EEDF[i] * Math.Sqrt(i);
				for (i = 0; i < εLength; i++)
					EEDF[i] /= d;
			}

			/// <summary>
			/// Пересчитывать ФРЭЭ только в случае значительных изменений в составе смеси.
			/// </summary>
			static bool IfClacEEDF()
			{
				int i;
				double D;

				D = Math.Abs(θToCompare - θ);
				θToCompare = θ;

				if (D / θ >= εPrecision)
					return true;

				for (i = 0; i < ВеществаLength; i++)
				{
					if (ɣToCompare[i] == 0)
					{
						if (ɣ[i] > 1e-16)
							break;
					}
					else
					{
						D = Math.Abs(ɣToCompare[i] - ɣ[i]);
						if (D / ɣToCompare[i] >= εPrecision && D > 1e-16)
							break;
					}
				}

				return i != ВеществаLength;
			}

			public static bool ForceCalcEEDF { get; set; }

			public static void CalcEEDF()
			{
				bool b;
				int i, l = 0;
				double _Te, _sqrth = Math.Sqrt(Δε);

				if (εInvalidated)
					SetEEDFSetka();

				if (!ForceCalcEEDF)
					if (!IfClacEEDF())
						return;

				θ = θ0 * N0 / N;

				for (i = 0; i < ВеществаLength; i++)
					ɣToCompare[i] = ɣ[i];

				CalcTransportSection();

				SetBolzmann();

				if (DC && θ > 0 || !DC && Eω > 0)
				{
					CalcABC();

					do
					{
						l++;
						CalcDVL();

						EEDF[εCalcLength - 1] = 0;
						for (i = 0; i < εCalcLength; i++)
							EEDFold[i] = EEDF[i];
						for (i = εCalcLength - 1; i >= 1; i--)
							EEDF[i - 1] = _kappa[i] * EEDF[i] + _d[i];
						Normalize();

						b = false;
						for (i = εCalcLength - 1; i >= 1 && !b; i--)
							if (EEDFold[i - 1] != 0)
								b = (Math.Abs((EEDF[i - 1] - EEDFold[i - 1]) / EEDFold[i - 1]) > εPrecision);
					}
					while (b && l < 10000);

					_Te = 0;
					for (i = 1; i < εCalcLength; i++)
						_Te += EEDF[i] * i * Math.Sqrt(i);

					Te = 2d / 3d * Δε * _Te;
				}
				else
					Te = T / 11610;

				_Te = 0;
				for (i = 1; i < εCalcLength; i++)
					_Te += EEDF[i] * i * σTrM[i];

				Kel = _Te * _sqrth;
				Extrapolate();
				CalcConstants();
			}

			static void Extrapolate()
			{
				double a = (Math.Log(EEDF[εApprTo]) - Math.Log(EEDF[εApprFrom])) / (εApprTo - εApprFrom),
					b = Math.Log(EEDF[εApprFrom]) - a * εApprFrom;

				for (int i = εApprTo; i < εLength; i++)
					EEDF[i] = Math.Exp(a * i + b);
			}

			static void CalcConstants()
			{
				foreach (var R in Реакции7Ред)
				{
					double[] σ = R.Сечение.σ;
					double D, _sqrth = Math.Sqrt(Δε); ;
					int i, i0;

					for (D = 0, i = 1; i < εCalcLength; i++)
						D += σ[i] * i * EEDF[i];
					R.Константа = /*5.9310e-009*/3.5716e+015 * _sqrth * D;
					if (R.Тип_столкновения == 4)	// возбуждение, идёт обратная по принципу детального равновесия.
					{
						for (D = 0, i = 1; i < εCalcLength; i++)
						{
							i0 = i + Convert.ToInt32(R.Порог / Δε);
							if (i + i0 < εCalcLength)
								D += σ[i + i0] * (i + i0) * EEDF[i];
						}
						R.КонстантаОбр = 3.5716e+015 * _sqrth * D;
					}
				}
			}

			private static void SetBolzmann()
			{
				//int i;

				//if (Te == 0)
				//{
				//    for (i = 0; i < εCalcLength; i++)
				//        EEDF[i] = 0;
				//    EEDF[1] = 1;
				//}
				//else
				//{
				//    double s = 1 / (1 - Math.Exp(-Δε / Te));
				//    for (i = 0; i < εCalcLength; i++)
				//        EEDF[i] = Math.Exp(-ε[i] / Te) * s;
				//}
				double _Te = T / 11610, s = 1 / (1 - Math.Exp(-Δε / _Te));
				for (int i = 0; i < εCalcLength; i++)
					EEDF[i] = Math.Exp(-ε[i] / _Te) * s;

				EEDF[εCalcLength - 1] = 0;
			}

			public static void Initialize()
			{
				ɣToCompare = new double[ВеществаLength];

				if (εInvalidated)
					SetEEDFSetka();
			}

			public static void SetEEDFSetka()
			{
				εInvalidated = false;

				ε = new double[εLength];
				EEDF = new double[εLength];
				EEDFold = new double[εLength];
				p = new double[εLength];
				q = new double[εLength];
				r = new double[εLength];
				A = new double[εLength];
				B = new double[εLength];
				C = new double[εLength];
				C0 = new double[εLength];
				_F = new double[εLength];
				σTr = new double[εLength];
				σTrM = new double[εLength];
				_kappa = new double[εLength];
				_d = new double[εLength];
				RenormEEDF = 1 / (Δε * Math.Sqrt(Δε));

				for (int i = 0; i < εLength; i++)
					ε[i] = Δε * i;

				foreach (var v in from a in Вещества where a.CrossSection != null || a.Z != 0 select a)
					v.NewCrossSection.ε = ε;
				foreach (var v in from a in Реакции7 select a)
					v.Сечение.ε = ε;

				ТранспортныеСечения = (from a in Вещества
									   where a.CrossSection != null
									   select new НумерованноеСечение()
									   {
										   Номер = Array.IndexOf<Вещество>(Вещества, a),
										   σ = a.CrossSection.σ,
										   σM = a.CrossSection.σM
									   }).ToArray<НумерованноеСечение>();

				ВращательныеСечения = (from a in Вещества
									   where a.CrossSection != null
									   select new KeyValuePair<int, double>(Array.IndexOf<Вещество>(Вещества, a),
										   a.σRot)).ToArray<KeyValuePair<int, double>>();

				int count = (εLength - 2) / ev.Length;

				states[0].iBegin = 1;
				for (int i = 1; i < ev.Length; i++)
					states[i].iBegin = states[i - 1].iEnd = states[i - 1].iBegin + count;
				states[ev.Length - 1].iEnd = εLength;

				CalcData.InitializeEEDF();
				CalcTransportSection();
			}

			[Optimized]
			public static void CalcTransportSection()
			{
				for (int i = 0; i < εLength; i++)
				{
					σTr[i] = σTrM[i] = 0;
					foreach (var v in ТранспортныеСечения)
					{
						σTr[i] += ɣ[v.Номер] * v.σ[i];
						σTrM[i] += ɣ[v.Номер] * v.σM[i];
					}
				}
			}

			public static double QelN
			{
				get
				{
					return 1.1680e+013 * ɣ[Ne] * N * Math.Sqrt(Δε) * (11604 * Te - T) * Kel;
				}
			}

			static double RenormEEDF;

			public static double[] ФРЭЭ
			{
				get
				{
					var v = new double[εLength - 1];
					if (RenormEEDF > 0)
						for (int i = 0; i < εLength - 1; i++)
							v[i] = EEDF[i] * RenormEEDF;

					return v;
				}
			}
		}
	}
}