using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	public class Сечение
	{
		public IList<ТочкаСечения> NewStoredSection
		{
			get
			{
				if (StoredSection == null)
					StoredSection = new List<ТочкаСечения>();
				return StoredSection;
			}
		}

		public IList<ТочкаСечения> StoredSection { get; private set; }

		public double[] setkaEnergy;

		public double[] ε
		{
			get
			{
				return setkaEnergy;
			}
			set
			{
				if ((setkaEnergy = value) == null)
					σ = null;
				else
				{
					σ = new double[ε.Length];
					σM = new double[ε.Length];

					if (StoredSection != null)
						OnSetSetka();
					else
						OnSetSetkaLandau();

					if (M != 0)
						for (int i = 0; i < ε.Length; i++)
							σM[i] = σ[i] / M;
				}
			}
		}

		public double M { get; set; }
		public double[] σ { get; private set; }
		public double[] σM { get; private set; }

		void OnSetSetka()
		{
			double[] setka1 = new double[ε.Length + 1];

			setka1[0] = ε[0];
			setka1[ε.Length] = ε[ε.Length - 1];

			for (int i = 1; i < ε.Length; i++)
				setka1[i] = 0.5 * (ε[i] + ε[i - 1]);


			for (int i = 0; i < StoredSection.Count - 1; i++)
			{
				ТочкаСечения sp = StoredSection[i], spNext = StoredSection[i + 1];
				int j = Array.IndexOf<double>(setka1, Array.FindLast<double>(setka1, x => x <= sp.ε)),
					jNext = Array.IndexOf<double>(setka1, Array.FindLast<double>(setka1, x => x < spNext.ε));
				double a = 0.5 * (spNext.σ - sp.σ) / (spNext.ε - sp.ε),
					b = (sp.σ * spNext.ε - spNext.σ * sp.ε) / (spNext.ε - sp.ε);

				for (int k = j; k <= jNext && k < ε.Length; k++)
				{
					double x1 = Math.Max(sp.ε, setka1[k]), x2 = Math.Min(spNext.ε, setka1[k + 1]);

					σ[k] += (a * (x1 + x2) + b) * (x2 - x1);
				}
			}

			for (int i = 0; i < ε.Length; i++)
				σ[i] /= setka1[i + 1] - setka1[i];
		}

		const double lnL = 10;

		void OnSetSetkaLandau()
		{
			for (int i = 0; i < ε.Length; i++)
				σ[i] = 651.39 * lnL / (ε[i] * ε[i]);
			for (int i = 0; i < ε.Length; i++)
				if (double.IsInfinity(σ[i]))
					σ[i] = σ[i == 0 ? 1 : i - 1];
		}
	}
}