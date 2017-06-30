using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	public static partial class Engine
	{
		public class CalcРеакции : CalcBase
		{
			public double[] dɣdt0;
			public Реакция[] Реакции1, Реакции2, Реакции3, Реакции4;
			Реакция[] Реакции1Ред, Реакции2Ред, Реакции3Ред, Реакции4Ред;
			bool reducted = true;

			public void UndoReduction()
			{
				if (reducted)
				{
					//Реакции1Ред = (from a in Реакции1 where a.SignificantRKS select a).ToArray<Реакция>();
					//Реакции2Ред = (from a in Реакции2 where a.SignificantRKS select a).ToArray<Реакция>();
					//Реакции3Ред = (from a in Реакции3 where a.SignificantRKS select a).ToArray<Реакция>();
					//Реакции4Ред = (from a in Реакции4 where a.SignificantRKS select a).ToArray<Реакция>();
					Реакции1Ред = Реакции1;
					Реакции2Ред = Реакции2;
					Реакции3Ред = Реакции3;
					Реакции4Ред = Реакции4;

					reducted = false;
				}
			}

			public void DoReduction()
			{
				Реакции1Ред = (from a in Реакции1 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
				Реакции2Ред = (from a in Реакции2 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
				Реакции3Ред = (from a in Реакции3 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
				Реакции4Ред = (from a in Реакции4 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
				reducted = true;
			}

			public void Calcdɣdt(object state)
			{
				double logT = Math.Log(T), k;

				for (int i = 0; i < ВеществаLength; i++)
					dɣdt0[i] = 0;

				foreach (var v in Реакции1Ред)
				{
					v.Константа = v.AP * Math.Exp(logT * v.MP + v.EP / T);
					v.КонстантаОбр = v.HasM ? v.AM * Math.Exp(logT * v.MM + v.EM / T) :
						v.Константа * Pow(8.314 * T, v.Коэф_прод_1 + v.Коэф_прод_2 -
						v.Коэф_комп_1 - v.Коэф_комп_2) *
						Math.Exp(-(Вещества[v.Продукт_1].Gibbs +
						(v.Продукт_2 == -1 ? 0 : Вещества[v.Продукт_2].Gibbs) -
						Вещества[v.Комп_1].Gibbs -
						(v.Комп_2 == -1 ? 0 : Вещества[v.Комп_2].Gibbs) -
						(Вещества[v.Продукт_1].Hform0K +
						(v.Продукт_2 == -1 ? 0 : Вещества[v.Продукт_2].Hform0K) -
						Вещества[v.Комп_1].Hform0K -
						(v.Комп_2 == -1 ? 0 : Вещества[v.Комп_2].Hform0K)) / T) / 8.314);

					k = v.Константа *
						Pow(N * ɣ[v.Комп_1], v.Коэф_комп_1) *
						(v.Комп_2 == -1 ? 1 : Pow(N * ɣ[v.Комп_2], v.Коэф_комп_2)) -
						v.КонстантаОбр *
						Pow(N * ɣ[v.Продукт_1], v.Коэф_прод_1) *
						(v.Продукт_2 == -1 ? 1 : Pow(N * ɣ[v.Продукт_2], v.Коэф_прод_2));

					if (!v.М)
						k /= N;

					v.Скорость = k;
					dɣdt0[v.Комп_1] -= v.Коэф_комп_1 * k;
					dɣdt0[v.Продукт_1] += v.Коэф_прод_1 * k;
					if (v.Комп_2 != -1)
						dɣdt0[v.Комп_2] -= v.Коэф_комп_2 * k;
					if (v.Продукт_2 != -1)
						dɣdt0[v.Продукт_2] += v.Коэф_прод_2 * k;
				}

				foreach (var v in Реакции2Ред)
				{
					v.Константа = v.AP * Math.Exp(v.EP / T);
					v.КонстантаОбр = v.HasM ? v.AM * Math.Exp(v.EM / T) :
						v.Константа * Pow(8.314 * T, v.Коэф_прод_1 + v.Коэф_прод_2 -
						v.Коэф_комп_1 - v.Коэф_комп_2) *
						Math.Exp(-(Вещества[v.Продукт_1].Gibbs +
						(v.Продукт_2 == -1 ? 0 : Вещества[v.Продукт_2].Gibbs) -
						Вещества[v.Комп_1].Gibbs -
						(v.Комп_2 == -1 ? 0 : Вещества[v.Комп_2].Gibbs) -
						(Вещества[v.Продукт_1].Hform0K +
						(v.Продукт_2 == -1 ? 0 : Вещества[v.Продукт_2].Hform0K) -
						Вещества[v.Комп_1].Hform0K -
						(v.Комп_2 == -1 ? 0 : Вещества[v.Комп_2].Hform0K)) / T) / 8.314);

					k = v.Константа *
						Pow(N * ɣ[v.Комп_1], v.Коэф_комп_1) *
						(v.Комп_2 == -1 ? 1 : Pow(N * ɣ[v.Комп_2], v.Коэф_комп_2)) -
						v.КонстантаОбр *
						Pow(N * ɣ[v.Продукт_1], v.Коэф_прод_1) *
						(v.Продукт_2 == -1 ? 1 : Pow(N * ɣ[v.Продукт_2], v.Коэф_прод_2));

					if (!v.М)
						k /= N;

					v.Скорость = k;
					dɣdt0[v.Комп_1] -= v.Коэф_комп_1 * k;
					dɣdt0[v.Продукт_1] += v.Коэф_прод_1 * k;
					if (v.Комп_2 != -1)
						dɣdt0[v.Комп_2] -= v.Коэф_комп_2 * k;
					if (v.Продукт_2 != -1)
						dɣdt0[v.Продукт_2] += v.Коэф_прод_2 * k;
				}

				foreach (var v in Реакции3Ред)
				{
					v.Константа = v.AP * Math.Exp(logT * v.MP + v.EP / T);
					v.КонстантаОбр = v.HasM ? v.AM * Math.Exp(logT * v.MM + v.EM / T) :
						v.Константа *
						Math.Exp(-(Вещества[v.Продукт_1].Gibbs + Вещества[v.Продукт_2].Gibbs -
						Вещества[v.Комп_1].Gibbs - Вещества[v.Комп_2].Gibbs -
							(Вещества[v.Продукт_1].Hform0K + Вещества[v.Продукт_2].Hform0K -
							Вещества[v.Комп_1].Hform0K - Вещества[v.Комп_2].Hform0K) / T) / 8.314);

					k = v.Скорость = N * (v.Константа * ɣ[v.Комп_1] * (v.Комп_2 == -1 ? 1 : ɣ[v.Комп_2]) -
						v.КонстантаОбр * ɣ[v.Продукт_1] * (v.Продукт_2 == -1 ? 1 : ɣ[v.Продукт_2]));

					dɣdt0[v.Комп_1] -= k;
					dɣdt0[v.Комп_2] -= k;
					dɣdt0[v.Продукт_1] += k;
					dɣdt0[v.Продукт_2] += k;
				}

				foreach (var v in Реакции4Ред)
				{
					v.Константа = v.AP * Math.Exp(v.EP / T);
					v.КонстантаОбр = v.HasM ? v.AM * Math.Exp(v.EM / T) :
						v.Константа *
						Math.Exp(-(Вещества[v.Продукт_1].Gibbs + Вещества[v.Продукт_2].Gibbs -
						Вещества[v.Комп_1].Gibbs - Вещества[v.Комп_2].Gibbs -
							(Вещества[v.Продукт_1].Hform0K + Вещества[v.Продукт_2].Hform0K -
							Вещества[v.Комп_1].Hform0K - Вещества[v.Комп_2].Hform0K) / T) / 8.314);

					k = v.Скорость = N * (v.Константа * ɣ[v.Комп_1] * (v.Комп_2 == -1 ? 1 : ɣ[v.Комп_2]) -
						v.КонстантаОбр * ɣ[v.Продукт_1] * (v.Продукт_2 == -1 ? 1 : ɣ[v.Продукт_2]));

					dɣdt0[v.Комп_1] -= k;
					dɣdt0[v.Комп_2] -= k;
					dɣdt0[v.Продукт_1] += k;
					dɣdt0[v.Продукт_2] += k;
				}

				_ev.Set();
			}
		}
	}
}
