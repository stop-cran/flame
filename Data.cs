using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{

	public static partial class Engine
	{
		/// <summary>
		/// Данные для копирования.
		/// </summary>
		public static class CalcData
		{
			/// <summary>
			/// Мольные доли компонент смеси.
			/// </summary>
			public static double[] ɣ { get; private set; }
			/// <summary>
			/// Производные мольных долей по времени.
			/// </summary>
			public static double[] dɣdt { get; private set; }
			public static double[] Скорости { get; private set; }
			public static double[] Tvibr { get; set; }
			public static double[][] X { get; set; }
			public static double[][] dXdt { get; set; }
			public static double dΓdt0 { get; private set; }
			public static double dΓdt { get; private set; }
			/// <summary>
			/// ФРЭЭ, эВ^(-3/2). P{ε ≤ e ≤ ε + Δε} = EEDF(ε)*√ε + o(ε), где e - энергия электрона.
			/// </summary>
			public static double[] ФРЭЭ { get; private set; }
			/// <summary>
			/// Полная энтальпия системы, Дж/см³
			/// </summary>
			public static double H { get; private set; }
			/// <summary>
			/// Полный вклад энергии в систему, Вт/см³
			/// </summary>
			public static double dHdt { get; private set; }
			/// <summary>
			/// Производная температуры по времени, К/с.
			/// </summary>
			public static double dTdt { get; private set; }

			public static void Initialize()
			{
				ɣ = new double[ВеществаLength];
				dɣdt = new double[ВеществаLength];
				Скорости = new double[Реакции.Length];
				X = new double[Молекулы.Length][];
				dXdt = new double[Молекулы.Length][];
				Tvibr = new double[Молекулы.Length];
				for (int j = 0; j < Молекулы.Length; j++)
				{
					X[j] = new double[Молекулы[j].Уровни];
					dXdt[j] = new double[Молекулы[j].Уровни];
				}
			}

			public static void InitializeEEDF()
			{
				ФРЭЭ = РасчётФРЭЭ.ФРЭЭ;
			}

			public static void CopyData()
			{
				for (int i = 0; i < ВеществаLength; i++)
				{
					ɣ[i] = Engine.ɣ[i];
					dɣdt[i] = Engine.dɣdt[i];
				}

				for (int i = 0; i < Реакции.Length; i++)
					Скорости[i] = Реакции[i].Скорость;
				for (int j = 0; j < Молекулы.Length; j++)
				{
					double s = 0, e0 = Молекулы[j].E(0);
					int k = Молекулы[j].Вещество;

					for (int i = 0; i < Молекулы[j].Уровни; i++)
						s += 11610d * (Молекулы[j].E(i) - e0) * Молекулы[j].X[i];

					X[j] = (double[])Молекулы[j].X.Clone();
					dXdt[j] = (double[])Молекулы[j].dXdt.Clone();

					Tvibr[j] = ɣ[k] == 0 ? 0 : s / ɣ[k];
				}

				ФРЭЭ = РасчётФРЭЭ.ФРЭЭ;
				dΓdt0 = Engine.dΓdt0;
				dΓdt = Engine.dΓdt;
				H = Engine.H;
				dHdt = Engine.dHdt;
				dTdt = Engine.dTdt;
			}

			public static object ExportData()
			{
				return new DataForSave()
				{
					Time = t,
					T = T,
					N = N,
					ɣ = ɣ,
					X = X,
					dXdt = dXdt
				};
			}

			public static void ImportData(object data)
			{
				var data1 = data as DataForSave;

				t = data1.Time;
				T = data1.T;
				N = data1.N;
				Engine.ɣ = data1.ɣ;
				for (int j = 0; j < Молекулы.Length; j++)
					for (int i = 0; i < Молекулы[j].Уровни; i++)
					{
						Молекулы[j].X[i] = data1.X[j][i];
						Молекулы[j].dXdt[i] = data1.dXdt[j][i];
					}
				CopyData();
				Engine.OnFormat();
			}


			[Serializable]
			class DataForSave
			{
				public double Time { get; set; }
				public double T { get; set; }
				public double N { get; set; }
				public double[] ɣ { get; set; }
				public double[][] X { get; set; }
				public double[][] dXdt { get; set; }
			}
		}
	}
}