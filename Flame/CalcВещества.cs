using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame3
{
	public static partial class Engine
	{
		public class CalcВещества : CalcBase
		{
			public int iBegin, iEnd;
            public double sh0, sh, shd0, shd, cp, μ;

			public void CalcTermDyn(object state)
			{
				sh0 = sh = shd0 = cp = μ = shd = 0;

				for (int i = iBegin; i < iEnd; i++)
				{
					var v = Engine.Вещества[i];

					if (v.Significant && v.M != 0)
					{
						v.CalculateTermDynData();
						cp += v.Cp * ɣ[i];
						sh += v.H * ɣ[i];
                        shd0 += v.H * dɣdt0[i];
						sh0 += v.Hform * ɣ[i];
						μ += v.M * ɣ[i];
					}
				}

				_ev.Set();
			}

			public void CalcSHD(object state)
			{
				shd = 0;

				for (int i = iBegin; i < iEnd; i++)
				{
					var v = Engine.Вещества[i];

					if (v.Significant && v.M != 0)
						shd += v.H * dɣdt[i];
				}

				_ev.Set();
			}
		}
	}
}
