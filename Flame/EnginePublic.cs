using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace Flame3
{
	/// <summary>
	/// Открытые свойства типа.
	/// </summary>
	public static partial class Engine
	{
		public static event CancelEventHandler Format;
		public static event MethodInvoker Plot;
		public static event MethodInvoker Plot2;
		static Вещество[] вещества;
		public static Вещество[] Вещества
		{
			get
			{
				return вещества;
			}
			set
			{
				вещества = value;
				ВеществаLength = value.Length;
			}
		}
		public static int ВеществаLength { get; private set; }
		public static Молекула[] Молекулы { get; set; }
		/// <summary>
		/// Все реакции.
		/// </summary>
		public static Реакция[] Реакции { get; set; }
		/// <summary>
		/// Реакции с константой, рассчитываемой по формуле Аррениуса.
		/// У каждой реакции два компонента и два продукта.
		/// </summary>
		public static Реакция[] Реакции1 { get; set; }
		/// <summary>
		/// Реакции с константой, рассчитываемой по формуле Аррениуса.
		/// У каждой реакции два компонента и два продукта.
		/// M± == 0.
		/// </summary>
		public static Реакция[] Реакции2 { get; set; }
		/// <summary>
		/// Реакции с константой, рассчитываемой по формуле Аррениуса.
		/// У каждой реакции два компонента и два продукта.
		/// Все стехиометрические коэффициенты равны 1.
		/// </summary>
		public static Реакция[] Реакции3 { get; set; }
		/// <summary>
		/// Реакции с константой, рассчитываемой по формуле Аррениуса.
		/// У каждой реакции два компонента и два продукта.
		/// Все стехиометрические коэффициенты равны 1.
		/// M± == 0.
		/// </summary>
		public static Реакция[] Реакции4 { get; set; }
		/// <summary>
		/// Реакции с константой, рассчитываемой по формуле Аррениуса.
		/// Без ограничений.
		/// </summary>
		public static Реакция[] Реакции5 { get; set; }
		public static Реакция[] Реакции5Ред { get; set; }
		/// <summary>
		/// Реакции с константой, зависящей от температуры электронов и рассчитываемой по формуле Аррениуса.
		/// </summary>
		public static Реакция[] Реакции6 { get; set; }
		public static Реакция[] Реакции6Ред { get; set; }
		/// <summary>
		/// Реакции с участием электронов с заданным сечением.
		/// </summary>
		public static Реакция[] Реакции7 { get; set; }
		public static Реакция[] Реакции7Ред { get; set; }

		/// <summary>
		/// Мольные доли компонент смеси.
		/// </summary>
		public static double[] ɣ { get; private set; }
		/// <summary>
		/// Производные мольных долей по времени, без учёта влияния электрического поля.
		/// </summary>
		public static double[] dɣdt0 { get; private set; }
		/// <summary>
		/// Производные мольных долей по времени.
		/// </summary>
		public static double[] dɣdt { get; private set; }

		public static double dΓdt0 { get; private set; }
		public static double dΓdt { get; private set; }
		/// <summary>
		/// Энерговклад разряда
		/// </summary>
		public static double dQedt { get; private set; }
		public static double dNdtA { get; private set; }
		/// <summary>
		/// Частота расчёта ФРЭЭ. EEDFrequency = 100 - 1 раз в 100 циклов.
		/// </summary>
		public static int EEDFrequency { get; set; }
		/// <summary>
		/// Частота обновления данных. FormatFrequency = 100 - 1 раз в 100 циклов.
		/// </summary>
		public static int FormatFrequency { get; set; }
		/// <summary>
		/// Частота записи данных на график. PlotFrequency = 100 - 1 раз в 100 циклов.
		/// </summary>
		public static int PlotFrequency { get; set; }
		public static int PlotFrequency2 { get; set; }
		/// <summary>
		/// Включить редукцию.
		/// </summary>
		public static bool EnableReduction { get; set; }
		/// <summary>
		/// Количество реакций в редуцированной схеме.
		/// </summary>
		public static int ReductionReactionAmount { get; private set; }
		/// <summary>
		/// Наименьшая мольная доля вещества, учитываемого в схеме.
		/// </summary>
		public static double ReductionLimit { get; set; }
		/// <summary>
		/// Частота расчёта редукции.
		/// </summary>
		public static int ReductionFrequency { get; set; }
		/// <summary>
		/// Количество циклов расчёта без редукции.
		/// </summary>
		public static int ReductionFrequency2 { get; set; }
		/// <summary>
		/// Количество одновременно запускаемых программ, 0 - без ограничений.
		/// </summary>
		public static int MaxPrograms { get; set; }
		/// <summary>
		/// Время, с
		/// </summary>
		public static double t { get; set; }
		static double _T;
		public static double T0 { get; set; }
		/// <summary>
		/// Температура смеси, K
		/// </summary>
		public static double T
		{
			get
			{
				return _T;
			}
			set
			{
				if (_T != value)
				{
					_T = value;
					Вещество.OnTChanged();
				}
			}
		}


		static double _θ;
		/// <summary>
		/// Приведённое значение электрическое поле, Td.
		/// </summary>
		public static double θ
		{
			get
			{
				return _θ;
			}
			set
			{
				_θ = value;
				Te = T / 11610;
			}
		}

		public static double θ0 { get; set; }
		public static double θmax { get; set; }

		/// <summary>
		/// Температура электронов, эВ.
		/// </summary>
		public static double Te { get; private set; }

		static double _N;

		public static double N0 { get; set; }
		/// <summary>
		/// Молярная концентрация, моль/см3.
		/// </summary>
		public static double N
		{
			get { return _N; }
			set
			{
				_N = value;
				Nc = value * Na;
			}
		}

		/// <summary>
		/// Концентрация, см-3.
		/// </summary>
		static double Nc { get; set; }

		/// <summary>
		/// Полная энтальпия системы, Дж/моль
		/// </summary>
		public static double H { get; private set; }
		/// <summary>
		/// Полная энтальпия системы при T = 298K, Дж/моль
		/// </summary>
		public static double H0 { get; private set; }
		/// <summary>
		/// Теплоёмкость при постоянном давлении, Дж/моль*K
		/// </summary>
		public static double Cp { get; private set; }
		/// <summary>
		/// Cp/Cv
		/// </summary>
		public static double ɣCpCv { get; private set; }
		/// <summary>
		/// Средняя молярная масса смеси.
		/// </summary>
		public static double μ { get; private set; }
		/// <summary>
		/// Характерный размер области разряда.
		/// </summary>
		public static double AreaSize { get; set; }
		/// <summary>
		/// Полный вклад энергии в систему, Вт/см³
		/// </summary>
		public static double dHdt { get; private set; }
		/// <summary>
		/// Производная температуры по времени, К/с.
		/// </summary>
		public static double dTdt { get; private set; }

		public static double dtInit { get; set; }
		internal static double dt;
		public static double dtAvg { get; private set; }
		const int NoMinDtIndexValue = -1;
		public static int MinDtIndex { get; private set; }
		public static bool NoMinDtIndex { get { return MinDtIndex == NoMinDtIndexValue; } }
		public static double Precision { get; set; }
		/// <summary>
		/// Минимальная величина мольной доли.
		/// </summary>
		public static double ɣMin { get; set; }
		public static double C3H8ɣMin { get; set; }
		public static double EɣMax { get; set; }
		public static double CriticalOscillationFactor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Never)]
		static double timer;
		[EditorBrowsable(EditorBrowsableState.Never)]
		static bool isTimerSet;
		public static double Timer
		{
			get
			{
				return timer;
			}
			set
			{
				if (value <= t)
					throw new ArgumentOutOfRangeException("Значение таймера должно быть больше текущего времени.");
				timer = value;
				IsTimerSet = true;
			}
		}

		public static bool IsTimerSet
		{
			get
			{
				return isTimerSet;
			}
			set
			{
				if (!value)
					timer = 0;
				isTimerSet = value;
			}
		}

		public static AutoSave AutoSaveDataSet { get; set; }
		public static double AutoSaveDataSetInterval { get; set; }
		public static event EventHandler AutoSaveNeeded;
	}

	public enum AutoSave { No = 0, Save = 1, SaveAndExit = 2 };
}
