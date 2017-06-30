using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	public class Реакция
	{
		public int Номер { get; set; }
		public bool М { get; set; }
		public int Комп_1 { get; set; }
		public int Коэф_комп_1 { get; set; }
		public int Ур_комп_1 { get; set; }
		public int Комп_2 { get; set; }
		public int Коэф_комп_2 { get; set; }
		public int Ур_комп_2 { get; set; }
		public int Комп_3 { get; set; }
		public int Коэф_комп_3 { get; set; }
		public int Продукт_1 { get; set; }
		public int Коэф_прод_1 { get; set; }
		public int Ур_прод_1 { get; set; }
		public int Продукт_2 { get; set; }
		public int Коэф_прод_2 { get; set; }
		public int Ур_прод_2 { get; set; }
		public int Продукт_3 { get; set; }
		public int Коэф_прод_3 { get; set; }
		public int Тип_столкновения { get; set; }
		public double Порог { get; set; }
		public bool HasP { get; set; }
		public double AP { get; set; }
		public double MP { get; set; }
		public double EP { get; set; }
		public bool HasM { get; set; }
		public double AM { get; set; }
		public double MM { get; set; }
		public double EM { get; set; }
		public bool Флаг_Te { get; set; }
		public int СпецФормула { get; set; }

		public double Комп1_Мин { get; set; }
		public double Комп1_Макс { get; set; }
		public double Комп2_Мин { get; set; }
		public double Комп2_Макс { get; set; }
		public double Комп3_Мин { get; set; }
		public double Комп3_Макс { get; set; }
		public bool Комп1_Огр { get; set; }
		public bool Комп2_Огр { get; set; }
		public bool Комп3_Огр { get; set; }

		//public bool IsCurrentlySignificant()
		//{
		//    double d = Engine.ɣ[Комп_1];

		//    if (d >= Комп1_Мин & d <= Комп1_Макс && Комп1_Огр)
		//        return true;

		//    if (Тип_столкновения == 2 || Тип_столкновения == 3 && Комп2_Огр)
		//    {
		//        d = Engine.ɣ[Engine.Ne];
		//        return d >= Комп2_Мин && d <= Комп2_Макс;
		//    }

		//    if (Комп_2 != -1 && Комп2_Огр)
		//    {
		//        d = Engine.ɣ[Комп_2];
		//        if (d >= Комп2_Мин && d <= Комп2_Макс)
		//            return true;
		//    }

		//    if (Комп_3 != -1 && Комп3_Огр)
		//    {
		//        d = Engine.ɣ[Комп_3];
		//        if (d >= Комп3_Мин && d <= Комп3_Макс)
		//            return true;
		//    }

		//    return !(Комп1_Огр || Комп2_Огр || Комп3_Огр);
		//}

		public bool Significant { get; set; }
		//public bool SignificantRKSInternal { get; set; }
		//public bool SignificantRKS
		//{
		//    get
		//    {
		//        return SignificantRKSInternal;
		//        //if (!SignificantRKSInternal)
		//        //    return false;
		//        //return IsCurrentlySignificant();
		//    }
		//}

		// e, H, C, N, O
		public readonly int[] atomP = new int[5];
		public readonly int[] atomM = new int[5];

		public double Константа { get; set; }
		public double КонстантаОбр { get; set; }
		public double Скорость { get; set; }
		public Сечение Сечение { get; private set; }
		/// <summary>
		/// Молекула компонента 1 (если Ур_комп_1 не пустой)
		/// </summary>
		public Молекула M_комп_1 { get; set; }
		/// <summary>
		/// Молекула компонента 2 (если Ур_комп_2 не пустой)
		/// </summary>
		public Молекула M_комп_2 { get; set; }
		/// <summary>
		/// Молекула продукта 1 (если Ур_прод_1 не пустой)
		/// </summary>
		public Молекула M_прод_1 { get; set; }
		/// <summary>
		/// Молекула продукта 2 (если Ур_прод_2 не пустой)
		/// </summary>
		public Молекула M_прод_2 { get; set; }
		/// <summary>
		/// Заселённость уровня Ур_комп_1 компонента 1 (если Ур_комп_1 не пустой)
		/// </summary>
		public double X_комп_1 { get { return M_комп_1.X[Ур_комп_1]; } set { M_комп_1.X[Ур_комп_1] = value; } }
		public double dXdt0_комп_1 { get { return M_комп_1.dXdt0[Ур_комп_1]; } set { M_комп_1.dXdt0[Ур_комп_1] = value; } }
		public double dXdt_комп_1 { get { return M_комп_1.dXdt[Ур_комп_1]; } set { M_комп_1.dXdt[Ур_комп_1] = value; } }
		/// <summary>
		/// Заселённость уровня Ур_комп_2 компонента 2 (если Ур_комп_2 не пустой)
		/// </summary>
		public double X_комп_2 { get { return M_комп_2.X[Ур_комп_2]; } set { M_комп_2.X[Ур_комп_2] = value; } }
		public double dXdt0_комп_2 { get { return M_комп_2.dXdt0[Ур_комп_2]; } set { M_комп_2.dXdt0[Ур_комп_2] = value; } }
		public double dXdt_комп_2 { get { return M_комп_2.dXdt[Ур_комп_2]; } set { M_комп_2.dXdt[Ур_комп_2] = value; } }
		/// <summary>
		/// Заселённость уровня Ур_комп_1 компонента 1 (если Ур_комп_1 не пустой)
		/// </summary>
		public double X_прод_1 { get { return M_прод_1.X[Ур_прод_1]; } set { M_прод_1.X[Ур_прод_1] = value; } }
		public double dXdt0_прод_1 { get { return M_прод_1.dXdt0[Ур_прод_1]; } set { M_прод_1.dXdt0[Ур_прод_1] = value; } }
		public double dXdt_прод_1 { get { return M_прод_1.dXdt[Ур_прод_1]; } set { M_прод_1.dXdt[Ур_прод_1] = value; } }
		/// <summary>
		/// Заселённость уровня Ур_комп_2 компонента 2 (если Ур_комп_2 не пустой)
		/// </summary>
		public double X_прод_2 { get { return M_прод_2.X[Ур_прод_2]; } set { M_прод_2.X[Ур_прод_2] = value; } }
		public double dXdt0_прод_2 { get { return M_прод_2.dXdt0[Ур_прод_2]; } set { M_прод_2.dXdt0[Ур_прод_2] = value; } }
		public double dXdt_прод_2 { get { return M_прод_2.dXdt[Ур_прод_2]; } set { M_прод_2.dXdt[Ур_прод_2] = value; } }

		public Реакция()
		{
			Significant = true;
			//SignificantRKSInternal = true;
			Комп1_Макс = 1;
			Комп2_Макс = 1;
			Комп3_Макс = 1;
		}

		public Сечение НовоеСечение
		{
			get
			{
				if (Сечение == null)
					Сечение = new Сечение();
				return Сечение;
			}
		}

		public int ТипРеакции
		{
			get
			{
				if (СпецФормула != -1)
					return Флаг_Te ? 9 : 8;
				if (Тип_столкновения != -1)
					return 7;
				if (Флаг_Te)
					return 6;
				if (Комп_3 != -1 || Продукт_3 != -1 || Ур_комп_1 != -1 || Ур_комп_2 != -1 ||
					Ур_прод_1 != -1 || Ур_прод_2 != -1)
					return 5;

				bool m0 = (MP == 0 && MM == 0), sc1 = (Коэф_комп_1 == 1 && Коэф_комп_2 == 1 &&
					Коэф_прод_1 == 1 && Коэф_прод_2 == 1);

				return m0 ? sc1 ? 4 : 2 : sc1 ? 3 : 1;
			}
		}

		public Dictionary<int, int> Вещества { get; private set; }


		public void Init()
		{
			var d = new Dictionary<int, int>();

			d.AddUnique(Комп_1, Коэф_комп_1);
			d.AddUnique(Комп_2, Коэф_комп_2);
			d.AddUnique(Комп_3, Коэф_комп_3);
			d.AddUnique(Продукт_1, -Коэф_прод_1);
			d.AddUnique(Продукт_2, -Коэф_прод_2);
			d.AddUnique(Продукт_3, -Коэф_прод_3);

			if (Тип_столкновения == 2)
				d.AddUnique(Engine.Ne, 1);
			if (Тип_столкновения == 3)
				d.AddUnique(Engine.Ne, -1);

			Вещества = new Dictionary<int, int>();

			foreach (var v in d)
				if (v.Value != 0 && v.Key != -1)
					Вещества.AddUnique(v.Key, v.Value);
		}
	}
}
