using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	internal static class ExtensionMethods
	{
		public static int IsNull(this object value, int defaultValue)
		{
			return value is DBNull ? defaultValue : Convert.ToInt32(value);
		}

		public static double IsNull(this object value, double defaultValue)
		{
			return value is DBNull ? defaultValue : Convert.ToDouble(value);
		}

		public static object IfIsNotNull(this System.Data.DataRow row, string column, object value)
		{
			return row.IsNull(column) ? null : value;
		}

		public static string StoichKoef(this object value)
		{
			return value.ToString() == "1" ? null : value + " ";
		}

		public static T[][] Branch<T>(this T[] value, int parts)
		{
			if (parts < 1)
				throw new ArgumentOutOfRangeException();

			T[][] result = new T[parts][];

			int i, inc = value.Length / parts;
			for (i = 0; i < parts - 1; i++)
			{
				result[i] = new T[inc];
				Array.Copy(value, i * inc, result[i], 0, inc);
			}

			result[i] = new T[value.Length - i * inc];
			Array.Copy(value, i * inc, result[i], 0, value.Length - i * inc);

			return result;
		}

		public static void AddUnique(this List<int> list, int item)
		{
			if (!list.Contains(item) && item != -1)
				list.Add(item);
		}

		public static void AddUnique(this Dictionary<int, int> list, int key, int value)
		{
			if (list.ContainsKey(key))
				list[key] += value;
			else
				list.Add(key, value);
		}
	}
}
