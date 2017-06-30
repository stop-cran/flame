using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	public static partial class Engine
	{
		public static class ВеществаСпец
		{
			static int NN, NN_p, NN2_p, NN4_p, NN_2D, NN_2P, NN2_b1pu, NN2_B3pg, NN2_a_1sum, NM2N2, NM2N2_A, NM2N2_C, NC3H8, NO2;
			public static int IndC3H8 { get { return NC3H8; } }
			public static int IndO2 { get { return NO2; } }
			//public static int[] CalcSpec { get; private set; }
			static Молекула n2, n2A, n2C;

			static Реакция[] Реакции8 { get; set; }
			static Реакция[] Реакции8Ред { get; set; }
			static Реакция[] Реакции9 { get; set; }
			static Реакция[] Реакции9Ред { get; set; }
			static bool reducted = true;

			public static void UndoReduction()
			{
				if (reducted)
				{
					//Реакции8Ред = (from a in Реакции8 where a.SignificantRKS select a).ToArray<Реакция>();
					//Реакции9Ред = (from a in Реакции9 where a.SignificantRKS select a).ToArray<Реакция>();
					Реакции8Ред = Реакции8;
					Реакции9Ред = Реакции9;

					reducted = false;
				}
			}

			public static void DoReduction()
			{
				Реакции8Ред = (from a in Реакции8 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
				Реакции9Ред = (from a in Реакции9 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
				reducted = true;
			}

			static int Find(string name)
			{
				return Array.FindIndex(Вещества, c => c.Формула == name);
			}

			static int FindM(int id)
			{
				return Array.FindIndex<Молекула>(Молекулы, c => c.Вещество == id);
			}

			public static void Initialize()
			{
				Реакции8 = (from a in Реакции where a.ТипРеакции == 8 select a).ToArray<Реакция>();
				Реакции9 = (from a in Реакции where a.ТипРеакции == 9 select a).ToArray<Реакция>();
				UndoReduction();

				NN = Find("N");
				NN_p = Find("N+");
				NN2_p = Find("N2+");
				NN4_p = Find("N4+");
				NN_2D = Find("N 2D");
				NN_2P = Find("N 2P");
				NN2_b1pu = Find("N2 b1πu");
				NN2_B3pg = Find("N2 B3πg");
				NN2_a_1sum = Find("N2 a'1σu-");
				NM2N2 = Find("N2");
				NM2N2_A = Find("N2 A3Σu+");
				NM2N2_C = Find("N2 C3πu");
				NC3H8 = Find("C3H8");
				NO2 = Find("O2");

				//CalcSpec = new int[] { NO3, Find("H2O2"), Find("HO2") };

				int in2 = FindM(NM2N2), in2A = FindM(NM2N2_A), in2C = FindM(NM2N2_C);

				n2 = Молекулы[in2] = new МолекулаN2(Молекулы[in2]);
				n2A = Молекулы[in2A];
				n2C = Молекулы[in2C];
			}

			static double CalcRate(Реакция v)
			{
				return (v.Константа *
				Pow(N * (v.Ур_комп_1 == -1 ? ɣ[v.Комп_1] : v.X_комп_1), v.Коэф_комп_1) *
				(v.Комп_2 == -1 ? 1 : Pow(N * (v.Ур_комп_2 == -1 ? ɣ[v.Комп_2] : v.X_комп_2), v.Коэф_комп_2)) *
				(v.Комп_3 == -1 ? 1 : Pow(N * ɣ[v.Комп_3], v.Коэф_комп_3)) -
				v.КонстантаОбр *
				Pow(N * (v.Ур_прод_1 == -1 ? ɣ[v.Продукт_1] : v.X_прод_1), v.Коэф_прод_1) *
				(v.Продукт_2 == -1 ? 1 : Pow(N * (v.Ур_прод_2 == -1 ? ɣ[v.Продукт_2] : v.X_прод_2), v.Коэф_прод_2)) *
				(v.Продукт_3 == -1 ? 1 : Pow(N * ɣ[v.Продукт_3], v.Коэф_прод_3))) / N;
			}

			static void PartCalcdɣdt0(Реакция v)
			{
				double k;

				k = v.Скорость = CalcRate(v);

				if (v.Ур_комп_1 == -1)
					dɣdt0[v.Комп_1] -= v.Коэф_комп_1 * k;
				else
					v.dXdt0_комп_1 -= v.Коэф_комп_1 * k;

				if (v.Ур_прод_1 == -1)
					dɣdt0[v.Продукт_1] += v.Коэф_прод_1 * k;
				else
					v.dXdt0_прод_1 += v.Коэф_прод_1 * k;

				if (v.Комп_2 != -1)
					if (v.Ур_комп_2 == -1)
						dɣdt0[v.Комп_2] -= v.Коэф_комп_2 * k;
					else
						v.dXdt0_комп_2 -= v.Коэф_комп_2 * k;

				if (v.Продукт_2 != -1)
					if (v.Ур_прод_2 == -1)
						dɣdt0[v.Продукт_2] += v.Коэф_прод_2 * k;
					else
						v.dXdt0_прод_2 += v.Коэф_прод_2 * k;

				if (v.Комп_3 != -1)
					dɣdt0[v.Комп_3] -= v.Коэф_комп_3 * k;
				if (v.Продукт_3 != -1)
					dɣdt0[v.Продукт_3] += v.Коэф_прод_3 * k;
			}

			static void PartCalcdɣdt(Реакция v)
			{
				double k;

				k = v.Скорость = CalcRate(v);

				if (v.Ур_комп_1 == -1)
					dɣdt[v.Комп_1] -= v.Коэф_комп_1 * k;
				else
					v.dXdt_комп_1 -= v.Коэф_комп_1 * k;

				if (v.Ур_прод_1 == -1)
					dɣdt[v.Продукт_1] += v.Коэф_прод_1 * k;
				else
					v.dXdt_прод_1 += v.Коэф_прод_1 * k;

				if (v.Комп_2 != -1)
					if (v.Ур_комп_2 == -1)
						dɣdt[v.Комп_2] -= v.Коэф_комп_2 * k;
					else
						v.dXdt_комп_2 -= v.Коэф_комп_2 * k;

				if (v.Продукт_2 != -1)
					if (v.Ур_прод_2 == -1)
						dɣdt[v.Продукт_2] += v.Коэф_прод_2 * k;
					else
						v.dXdt_прод_2 += v.Коэф_прод_2 * k;

				if (v.Комп_3 != -1)
					dɣdt[v.Комп_3] -= v.Коэф_комп_3 * k;
				if (v.Продукт_3 != -1)
					dɣdt[v.Продукт_3] += v.Коэф_прод_3 * k;
			}

			public static void Calcdɣdt()
			{
				int i;
				double logT = Math.Log(T), k;

				foreach (var v in Реакции8Ред)
					switch (v.СпецФормула)
					{
						case 1:
							v.Константа = 1.16e+009 * Math.Exp(0.98 * logT) / (1 - Math.Exp(-3129 / T));
							PartCalcdɣdt0(v);
							break;

						case 2:
							v.Константа = 1.9e+009 * Math.Exp(0.98 * logT) / (1 - Math.Exp(-3129 / T));
							PartCalcdɣdt0(v);
							break;

						case 3:
							v.Константа = 6e+011;
							v.Скорость = 0;

							for (i = 8; i < n2.Уровни; i++)
							{
								k = v.Константа * N * n2.X[i] * ɣ[NN2_b1pu];
								v.Скорость += k;
								n2.dXdt0[i] -= k;
								dɣdt0[NN2_b1pu] -= k;	//N2 b1πu
								dɣdt0[NN4_p] += k;	//N4+
								dɣdt0[Ne] += k;
							}
							break;

						case 4:
							v.Константа = 6e+011;
							v.Скорость = 0;

							for (i = 0; i < n2C.Уровни; i++)
							{
								for (int w = 14 - i; w < n2.Уровни; w++)
								{
									k = v.Константа * N * n2C.X[i] * n2.X[w];
									v.Скорость += k;
									n2C.dXdt0[i] -= k;
									n2.dXdt0[w] -= k;
									dɣdt0[NN4_p] += k;
									dɣdt0[Ne] += k;
								}
							}
							break;

						case 5:
							v.Константа = 6e+011;
							v.Скорость = 0;

							for (i = 26; i < n2.Уровни; i++)
							{
								k = v.Константа * N * n2.X[i] * ɣ[NN2_a_1sum];
								v.Скорость += k;
								n2.dXdt0[i] -= k;
								dɣdt0[NN2_a_1sum] -= k;	//N2 a'1σu-
								dɣdt0[NN4_p] += k;	//N4+
								dɣdt0[Ne] += k;
							}
							break;

						case 6:
							v.Константа = 0.25 * (T > 600 ? 6.901e+014 : 3e+014 * Math.Exp(500 / T));
							v.Скорость = k = v.Константа * N * N * ɣ[NN] * ɣ[NN] * ɣ[NM2N2];
							dɣdt0[NN] -= 2 * k;
							n2.dXdt0[n2.Уровни - 1] += k;
							break;

						case 7:
							v.Константа = 0.75 * (T > 600 ? 6.901e+014 : 3e+014 * Math.Exp(500 / T));
							v.Скорость = k = v.Константа * N * N * ɣ[NN] * ɣ[NN] * ɣ[NM2N2];
							dɣdt0[NN] -= 2 * k;
							dɣdt0[NN2_B3pg] += 0.6 * k;
							n2.dXdt0[6] += 0.2 * k;
							n2.dXdt0[7] += 0.2 * k;
							break;

						case 8:
							v.Константа = 0.25 * ((T > 600) ? (6.901e+014) : (3e+014 * Math.Exp(500 / T))) * 0.533 * Math.Sqrt(T);
							v.Скорость = k = v.Константа * N * n2.X[n2.Уровни - 1] * ɣ[NM2N2];
							n2.dXdt0[n2.Уровни - 1] -= k;
							dɣdt0[NN] += 2 * k;
							break;

						case 9:
							v.Константа = 5.18e+013 * Math.Exp(-1398 / T) * (1 - Math.Exp(-2062.1 / T)) / (1 - Math.Exp(-3353 / T));
							PartCalcdɣdt0(v);
							break;

						case 10:
							v.Константа = 6.87e+013 * Math.Exp(-477.4 / T) * (1 - Math.Exp(-2062.1 / T)) / (1 - Math.Exp(-3353 / T));
							PartCalcdɣdt0(v);
							break;

						case 11:
							v.Константа = Math.Exp(ln10 * (T <= 900 ? (-14.6 + 3.583e-003 * (T - 300)) + 1 :
								T <= 1500 ? (-13.4 + 1.694e-003 * (T - 300)) + 1 :
								(-11.8 + 1.083e-003 * (T - 300)) + 1));
							PartCalcdɣdt0(v);
							break;

						case 12:
							v.Константа = (1.16e+010 * T * Math.Exp(1268.2 / T) / (1 + 3 * Math.Exp(-70 / T) + 5 * Math.Exp(-188 / T)));
							v.Скорость = 0;

							for (i = 4; i < n2.Уровни; i++)
							{
								k = v.Константа * N * n2.X[i] * ɣ[NN_p];
								v.Скорость += k;
								n2.dXdt0[i] -= k;
								dɣdt0[NN_p] -= k;	//N+
								dɣdt0[NN2_p] += k;	//N2+
								dɣdt0[NN] += k;
							}
							break;

						case 13:
							// O2 + O+ = O + O2+
							v.Константа = 1.987e+013 * Math.Exp(-0.00169 * T);
							PartCalcdɣdt0(v);
							break;

						case 25:
							// O+ + N2 = NO+ + N
							v.Константа = 1.8066e+12 * Math.Exp(-0.00311 * T);
							PartCalcdɣdt0(v);
							break;
					}
			}

			public static void AddElectrondGdt()
			{
				double Ke0, logT = Math.Log(T), logTe = Math.Log(Te), logEn, k;

				foreach (var v in Реакции9Ред)
					switch (v.СпецФормула)
					{
						case 14:
							// O2 + e = O + O-//3e+14
							v.Константа = 3e+12 * Math.Exp(-28.32 / θ);
							PartCalcdɣdt(v);
							break;
						case 15:
							// 2*e + H+ = H + e
							Ke0 = 2.298e+021;// *Pow(N * ɣ[Ne], 2) * ɣ[NH_p];
							logEn = Math.Log(13.59 / Te); k = Ke0 / (Math.Exp(2.33 * logEn) + 4.38 * Math.Exp(1.72 * logEn) + 1.32 * 13.59 / Te);
							logEn -= 1.386;/*Math.Log(4)*/ k += Ke0 / (Math.Exp(2.33 * logEn) + 4.38 * Math.Exp(1.72 * logEn) + 1.32 * 13.59 / Te);
							logEn -= 0.81;/*Math.Log(9)-Math.Log(4)*/ k += Ke0 / (Math.Exp(2.33 * logEn) + 4.38 * Math.Exp(1.72 * logEn) + 1.32 * 13.59 / Te);
							logEn -= 0.575;/*Math.Log(16)-Math.Log(9)*/ k += Ke0 / (Math.Exp(2.33 * logEn) + 4.38 * Math.Exp(1.72 * logEn) + 1.32 * 13.59 / Te);
							logEn -= 0.446;/*Math.Log(25)-Math.Log(16)*/ k += Ke0 / (Math.Exp(2.33 * logEn) + 4.38 * Math.Exp(1.72 * logEn) + 1.32 * 13.59 / Te);
							v.Константа = k;
							PartCalcdɣdt(v);
							break;

						case 16:
							// e + H2+ = H + H
							v.Константа = 1.6073e+019 * Math.Exp(-logTe * 0.5 - logT);
							PartCalcdɣdt(v);
							break;

						case 17:
							// e + H3+ = 3*H
							v.Константа = 1.806e+019 * Math.Exp(-logTe * 0.5 - logT);
							PartCalcdɣdt(v);
							break;

						case 18:
							// O2 + e = O2(a) + e
							if (θ > 0)
							{
								v.Константа = ((θ > 4) ? (3.798e+013 * Math.Exp(-0.8059 / θ)) : (6.02e+014 * Math.Exp(-11.973 / θ)));
								PartCalcdɣdt(v);
							}
							break;

						case 19:
							// O2 + e = O2(b) + e
							if (θ > 0)
							{
								v.Константа = ((θ > 3) ? (3.798e+012 * Math.Exp(-1.658 / θ)) : (1.9e+014 * Math.Exp(-13.82 / θ)));
								PartCalcdɣdt(v);
							}
							break;

						case 20:
							// 2 * O2 + e = O2 + O2 -
							if (17.03 - 0.906 * θ > 0)
							{
								v.Константа = (17.03 - 0.906 * θ) * 1e+16;
								//PartCalcdɣdt(v);
								v.Скорость = k = CalcRate(v); // v.Константа * Pow(N * ɣ[NO2], 2) * ɣ[Ne];
								//dɣdt[NO2] -= k;
								//dɣdt[Ne] -= k;
								//dɣdt[NO2_m] += k;
							}
							break;

						case 21:
							// O3 + e = O + O2 + e
							v.Константа = 3.1e+17 * Math.Exp(-32.236 / θ);
							PartCalcdɣdt(v);
							break;

						case 22:
							// O2 + O2 + e = O2- + O2
							v.Константа = 1.312e+17 * (1 / Te) * Math.Exp(-600 / T) * Math.Exp(700 * (Te - 8.613e-5 * T) / (Te * T));
							PartCalcdɣdt(v);
							break;

						case 23:
							// O2 + N2 + e = O2- + N2
							v.Константа = 2.591e+13 * (1 / (Te * Te)) * Math.Exp(-70 / T) * Math.Exp(1500 * (Te - 8.613e-5 * T) / (Te * T));
							PartCalcdɣdt(v);
							break;

						case 24:
							// e + N2O + N2 = N2O- + N2
							v.Константа = 3.626e+16 * (4.72 * (Te + 0.412) * (Te + 0.412) - 1.268);
							PartCalcdɣdt(v);
							break;
						case 26:
							// O2 + e = O + O-
							v.Константа = θ < 80 ? 3.018e+14 * Math.Exp(-283.2/θ) : 3.8e+13 * Math.Exp(-131.2/θ);
							PartCalcdɣdt(v);
							break;
					}
			}
		}
	}
}
