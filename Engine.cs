using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;

namespace Flame3
{
    public static partial class Engine
    {
        /// <summary>
        /// Число Авогадро.
        /// </summary>
        const double Na = 6.022e+023;
        const double ln10 = 2.3025850929940456840179914546844;
        const string SemaphoreName = "FlameMaxPrograms";

        static AutoResetEvent[] ev = new AutoResetEvent[Environment.ProcessorCount];
        static CalcABCState[] states = new CalcABCState[ev.Length];
        static CalcРеакции[] statesG = new CalcРеакции[ev.Length];
        static CalcВещества[] statesV = new CalcВещества[ev.Length];
        static MethodInvoker stoppedCallback;
        static НумерованноеСечение[] ТранспортныеСечения;
        static KeyValuePair<int, double>[] ВращательныеСечения;
        static int[] kations, anions;
        /// <summary>
        /// Номер вещества, соответствующий электронам.
        /// </summary>
        public static int Ne { get; private set; }
        static double dtMax;
        static volatile bool work = true;
        static double[] dɣdt_1, dɣdt_2, dɣdt_3, dɣdt_res;
        static Semaphore semMaxPrograms;

        static Engine()
        {
            int i;

            for (i = 0; i < ev.Length; i++)
                ev[i] = new AutoResetEvent(false);

            for (i = 0; i < ev.Length; i++)
            {
                states[i] = new CalcABCState() { j = i };
                statesG[i] = new CalcРеакции() { _ev = ev[i] };
                statesV[i] = new CalcВещества() { _ev = ev[i] };
            }

            MinDtIndex = NoMinDtIndexValue;

            if (MaxPrograms > 0)
            {
                semMaxPrograms = Semaphore.OpenExisting(SemaphoreName);

                if (semMaxPrograms == null)
                    semMaxPrograms = new Semaphore(0, MaxPrograms, SemaphoreName);
            }
        }

        public static void Initialize()
        {
            InitializeƔ();
            ВеществаСпец.Initialize();
            InitializeReactions();
            Ne = Array.FindIndex(Вещества, c => c.Формула == "e");
            InitializeStateG();
            InitializeStateV();
            CalcData.Initialize();
            РасчётФРЭЭ.Initialize();
            kations = (from a in Вещества where a.Z > 0 select Array.IndexOf(Вещества, a)).ToArray();
            anions = (from a in Вещества where a.Z < 0 select Array.IndexOf(Вещества, a)).ToArray();
        }

        private static void InitializeReactions()
        {
            ReductionReactionAmount = Реакции.Length;
            Реакции1 = (from a in Реакции where a.ТипРеакции == 1 select a).ToArray<Реакция>();
            Реакции2 = (from a in Реакции where a.ТипРеакции == 2 select a).ToArray<Реакция>();
            Реакции3 = (from a in Реакции where a.ТипРеакции == 3 select a).ToArray<Реакция>();
            Реакции4 = (from a in Реакции where a.ТипРеакции == 4 select a).ToArray<Реакция>();
            Реакции5Ред = Реакции5 = (from a in Реакции where a.ТипРеакции == 5 select a).ToArray<Реакция>();
            Реакции6Ред = Реакции6 = (from a in Реакции where a.ТипРеакции == 6 select a).ToArray<Реакция>();
            Реакции7Ред = Реакции7 = (from a in Реакции where a.ТипРеакции == 7 select a).ToArray<Реакция>();

            foreach (var v in from c in Реакции
                              join m in Молекулы on c.Комп_1 equals m.Вещество
                              where c.Ур_комп_1 != -1
                              select new { c = c, m = m })
                v.c.M_комп_1 = v.m;
            foreach (var v in from c in Реакции
                              join m in Молекулы on c.Комп_2 equals m.Вещество
                              where c.Ур_комп_2 != -1
                              select new { c = c, m = m })
                v.c.M_комп_2 = v.m;
            foreach (var v in from c in Реакции
                              join m in Молекулы on c.Продукт_1 equals m.Вещество
                              where c.Ур_прод_1 != -1
                              select new { c = c, m = m })
                v.c.M_прод_1 = v.m;
            foreach (var v in from c in Реакции
                              join m in Молекулы on c.Продукт_2 equals m.Вещество
                              where c.Ур_прод_2 != -1
                              select new { c = c, m = m })
                v.c.M_прод_2 = v.m;
        }

        private static void InitializeƔ()
        {
            ɣ = new double[ВеществаLength];
            dɣdt0 = new double[ВеществаLength];
            dɣdt = new double[ВеществаLength];
            dɣdt_3 = new double[ВеществаLength];
            dɣdt_2 = new double[ВеществаLength];
            dɣdt_1 = new double[ВеществаLength];
            dɣdt_res = new double[ВеществаLength];
        }

        private static void InitializeStateG()
        {
            var r1 = Реакции1.Branch<Реакция>(ev.Length);
            var r2 = Реакции2.Branch<Реакция>(ev.Length);
            var r3 = Реакции3.Branch<Реакция>(ev.Length);
            var r4 = Реакции4.Branch<Реакция>(ev.Length);

            for (int i = 0; i < ev.Length; i++)
            {
                statesG[i].dɣdt0 = new double[ВеществаLength];
                statesG[i].Реакции1 = r1[i];
                statesG[i].Реакции2 = r2[i];
                statesG[i].Реакции3 = r3[i];
                statesG[i].Реакции4 = r4[i];
                statesG[i].UndoReduction();
            }
        }

        private static void InitializeStateV()
        {
            statesV[0].iBegin = 0;
            for (int i = 1, count = ВеществаLength / ev.Length; i < ev.Length; i++)
                statesV[i].iBegin = statesV[i - 1].iEnd = statesV[i - 1].iBegin + count;
            statesV[ev.Length - 1].iEnd = ВеществаLength;
        }

        public static void SetBolzmann()
        {
            foreach (var v in Молекулы)
                v.SetBolzmann(ɣ[v.Вещество], T);
        }

        public static void SetBolzmannSpec(double Tv)
        {
            foreach (var v in Молекулы)
                v.SetBolzmann(ɣ[v.Вещество], Tv);
        }

        static double SHD;

        [Optimized]
        static void CalcTermDyn()
        {
            //double _SH = 0, _SHd = 0, _C = 0, _Qel_N = РасчётФРЭЭ.QelN, _μ = 0;
            double _sh0 = 0, _shd = 0, _μ = 0, _sh = 0, _cp = 0;

            foreach (var v in statesV)
                ThreadPool.QueueUserWorkItem(v.CalcTermDyn);
            WaitHandle.WaitAll(ev);

            foreach (var v in statesV)
            {
                _sh0 += v.sh0;
                _sh += v.sh;
                _shd += v.shd0;
                _cp += v.cp;
                _μ += v.μ;
            }

            H0 = 4.1841 * _sh0;
            H = 4.1841 * _sh;
            Cp = 4.1841 * _cp;
            μ = _μ;
            ɣCpCv = _cp / (_cp - 1.9872); // ɣ = Cp / Cv = Cp / (Cp - R);
            if (AreaSize == 0)
                dNdtA = 0;
            else
                dNdtA = Math.Sqrt(8.314e7 * T / (ɣCpCv * μ)) * (T0 * N0 / (T * N) - 1) / AreaSize;
            dTdt = (1.9872 * T * (dΓdt0 + dNdtA) + РасчётФРЭЭ.QelN - dΓdt0 * _sh - _shd) / (_cp - 1.9872);
            //dTdt = (РасчётФРЭЭ.QelN - dΓdt0 * _SH - _SHd) / _C; // dH == 0
        }

        static void CalcSHD()
        {
            double _shd = 0;

            foreach (var v in statesV)
                ThreadPool.QueueUserWorkItem(v.CalcSHD);
            WaitHandle.WaitAll(ev);

            foreach (var v in statesV)
                _shd += v.shd;

            SHD = 4.1841 * _shd;
        }

        [Optimized]
        static void CalcdɣdtMol()
        {
            foreach (var v in statesG)
                ThreadPool.QueueUserWorkItem(v.Calcdɣdt);
            WaitHandle.WaitAll(ev);

            for (int i = 0; i < ВеществаLength; i++)
            {
                double d = 0;
                foreach (var v in statesG)
                    d += v.dɣdt0[i];
                dɣdt0[i] = d;
            }
            foreach (var v in Молекулы)
                v.Reset();
        }

        [Optimized]
        static void Calcdɣdt()
        {
            int i;
            double logT = Math.Log(T), k;

            foreach (var v in Реакции5Ред)
            {
                v.Константа = v.AP * Math.Exp(logT * v.MP + v.EP / T);
                v.КонстантаОбр = v.HasM ? v.AM * Math.Exp(logT * v.MM + v.EM / T) :
                    v.Константа * Pow(8.314 * T, v.Коэф_прод_1 + v.Коэф_прод_2 + v.Коэф_прод_3 -
                    v.Коэф_комп_1 - v.Коэф_комп_2 - v.Коэф_комп_3) *
                    Math.Exp(-(Вещества[v.Продукт_1].Gibbs +
                    (v.Продукт_2 == -1 ? 0 : Вещества[v.Продукт_2].Gibbs) +
                    (v.Продукт_3 == -1 ? 0 : Вещества[v.Продукт_3].Gibbs) -
                    Вещества[v.Комп_1].Gibbs -
                    (v.Комп_2 == -1 ? 0 : Вещества[v.Комп_2].Gibbs) -
                    (v.Комп_3 == -1 ? 0 : Вещества[v.Комп_3].Gibbs) -
                    (Вещества[v.Продукт_1].Hform0K +
                    (v.Продукт_2 == -1 ? 0 : Вещества[v.Продукт_2].Hform0K) +
                    (v.Продукт_3 == -1 ? 0 : Вещества[v.Продукт_3].Hform0K) -
                    Вещества[v.Комп_1].Hform0K -
                    (v.Комп_2 == -1 ? 0 : Вещества[v.Комп_2].Hform0K) -
                    (v.Комп_3 == -1 ? 0 : Вещества[v.Комп_3].Hform0K)) / T) / 8.314);

                k = v.Скорость = (v.Константа *
                    Pow(N * (v.Ур_комп_1 == -1 ? ɣ[v.Комп_1] : v.X_комп_1), v.Коэф_комп_1) *
                    (v.Комп_2 == -1 ? 1 : Pow(N * (v.Ур_комп_2 == -1 ? ɣ[v.Комп_2] : v.X_комп_2), v.Коэф_комп_2)) *
                    (v.Комп_3 == -1 ? 1 : Pow(N * ɣ[v.Комп_3], v.Коэф_комп_3)) -
                    v.КонстантаОбр *
                    Pow(N * (v.Ур_прод_1 == -1 ? ɣ[v.Продукт_1] : v.X_прод_1), v.Коэф_прод_1) *
                    (v.Продукт_2 == -1 ? 1 : Pow(N * (v.Ур_прод_2 == -1 ? ɣ[v.Продукт_2] : v.X_прод_2), v.Коэф_прод_2)) *
                    (v.Продукт_3 == -1 ? 1 : Pow(N * ɣ[v.Продукт_3], v.Коэф_прод_3))) / N;

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

            ВеществаСпец.Calcdɣdt();

            foreach (var v in Молекулы)
                v.CalculateVKin();

            foreach (var v in Молекулы)
                v.CopyX();
            for (i = 0; i < ВеществаLength; i++)
                dɣdt[i] = dɣdt0[i];

            dΓdt0 = 0;
            for (i = 0; i < ВеществаLength; i++)
                dΓdt0 += dɣdt0[i];

            for (i = 0; i < ВеществаLength; i++)
                dɣdt0[i] -= dΓdt0 * ɣ[i];
            foreach (var v in Молекулы)
                dɣdt0[v.Вещество] += v.dXdt0.Sum();
        }

        static void AddElectrondGdt()
        {
            double logTe = Math.Log(Te), k;
            int i;

            ВеществаСпец.AddElectrondGdt();

            foreach (var v in Реакции6Ред)
            {
                v.Константа = v.AP * Math.Exp(logTe * v.MP + v.EP / Te);
                v.КонстантаОбр = v.AM * Math.Exp(logTe * v.MM + v.EM / Te);

                k = v.Скорость = (v.Константа *
                    Pow(N * (v.Ур_комп_1 == -1 ? ɣ[v.Комп_1] : v.X_комп_1), v.Коэф_комп_1) *
                    (v.Комп_2 == -1 ? 1 : Pow(N * (v.Ур_комп_2 == -1 ? ɣ[v.Комп_2] : v.X_комп_2), v.Коэф_комп_2)) *
                    (v.Комп_3 == -1 ? 1 : Pow(N * ɣ[v.Комп_3], v.Коэф_комп_3))) / N;

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

            foreach (var v in Реакции7Ред)
            {
                k = v.Константа * N * ɣ[Ne] * ɣ[v.Комп_1];
                if (v.Тип_столкновения == 4)	// возбуждение, идёт обратная по принципу детального равновесия.
                    k -= v.КонстантаОбр * N * ɣ[Ne] * (v.Ур_прод_1 == -1 ? ɣ[v.Продукт_1] : v.X_прод_1);

                v.Скорость = k;

                if (v.Ур_комп_1 == -1)
                    dɣdt[v.Комп_1] -= k;
                else
                    v.dXdt_комп_1 -= k;

                if (v.Ур_прод_1 == -1)
                    dɣdt[v.Продукт_1] += v.Коэф_прод_1 * k;
                else
                    v.dXdt_прод_1 += v.Коэф_прод_1 * k;

                if (v.Продукт_2 != -1)
                    if (v.Ур_прод_2 == -1)
                        dɣdt[v.Продукт_2] += v.Коэф_прод_2 * k;
                    else
                        v.dXdt_прод_2 += v.Коэф_прод_2 * k;

                if (v.Продукт_3 != -1)
                    dɣdt[v.Продукт_3] += v.Коэф_прод_3 * k;

                switch (v.Тип_столкновения)
                {
                    case 2: // ионизация
                        dɣdt[Ne] += k;
                        break;

                    case 3: // прилипание
                        dɣdt[Ne] -= k;
                        break;
                }
            }

            dΓdt = 0;
            for (i = 0; i < ВеществаLength; i++)
                dΓdt += dɣdt[i];

            for (i = 0; i < ВеществаLength; i++)
                dɣdt[i] -= dΓdt * ɣ[i];
            foreach (var v in Молекулы)
                dɣdt[v.Вещество] += v.dXdt.Sum();
        }

        /// <summary>
        /// Схема разошлась, считать дальше смысла мало.
        /// </summary>
        public static bool SignalState
        {
            get
            {
                return SignalStateT || SignalStateE;
            }
        }

        public static bool SignalStateAlter
        {
            get
            {
                return !SignalStateT && SignalStateE;
            }
        }

        public static bool SignalStateE
        {
            get
            {
                return false;// ɣ[Ne] > 2 * EɣMax;
            }
        }

        public static bool SignalStateT
        {
            get
            {
                return T > 4000 || double.IsNaN(T) || ɣ[ВеществаСпец.IndO2] < C3H8ɣMin;
            }
        }

        static void Calcdt()
        {
            int i;
            bool b = true;
            double dt1, g, dgdt;

            dt = dtInit;
            MinDtIndex = NoMinDtIndexValue;

            for (i = 0; i < ВеществаLength; i++)
                if (Вещества[i].Significant)
                {
                    if (Math.Abs(dgdt = dɣdt_res[i]) * dtInit < ɣMin)
                        continue;

                    if ((g = ɣ[i]) <= ɣMin)
                    {
                        MinDtIndex = i;
                        dt = dtInit;
                        return;
                    }

                    dt1 = Precision * g / dgdt;
                    if (dgdt < 0)
                        dt1 = -dt1;
                    if (b)
                    {
                        b = false;
                        dt = dt1;
                        MinDtIndex = i;
                        continue;
                    }
                    if (dt1 < dt)
                    {
                        dt = dt1;
                        MinDtIndex = i;
                    }
                }

            //foreach (var v in Молекулы)
            //    for (i = 0; i < v.Уровни; i++)
            //    {
            //        if ((dgdt = v.dXdt[i]) == 0)
            //            continue;

            //        if ((g = v.X[i]) < 1e-20)
            //        //{
            //            continue;
            //            //dt = dtInit;
            //            //return;
            //        //}

            //        dt1 = Precision * g / dgdt;
            //        if (dgdt < 0)
            //            dt1 = -dt1;
            //        if (b)
            //        {
            //            b = false;
            //            dt = dt1;
            //            continue;
            //        }
            //        if (dt1 < dt)
            //            dt = dt1;
            //    }
        }

        static void Stop()
        {
            work = false;
        }

        public static void Stop(MethodInvoker stoppedCallback)
        {
            Engine.stoppedCallback = stoppedCallback;
            Stop();
        }

        public static void CalcEedfOnly()
        {
            Te = 0;
            РасчётФРЭЭ.ResetEEDF();
            РасчётФРЭЭ.CalcEEDF();
            РасчётФРЭЭ.ResetEEDF();
            РасчётФРЭЭ.CalcEEDF();
            РасчётФРЭЭ.ResetEEDF();
            РасчётФРЭЭ.CalcEEDF();
            OnPlot();
            OnFormat();
        }

        static void OnFormat()
        {
            OnFormat(false);
        }

        static void OnFormat(bool stop)
        {
            try
            {
                CalcData.CopyData();
                if (Format != null)
                    Format(null, new CancelEventArgs(stop));
            }
            catch
            { }
        }

        static void OnPlot()
        {
            CalcData.CopyData();
            if (Plot != null)
                Plot();
        }

        static void OnPlot2()
        {
            if (Plot2 != null)
                Plot2();
        }


        public static void OnReduction(bool reductionBreak)
        {
            if (EnableReduction && !reductionBreak)
            {
                for (int i = 0; i < ВеществаLength; i++)
                    Вещества[i].Significant = ɣ[i] >= ReductionLimit;

                ApplyReductionOnReactions();
            }
            else
                UndoReduction();
        }

        public static void SetReductionLimit()
        {
            for (int i = 0; i < ВеществаLength; i++)
                Вещества[i].UserSignificant = ɣ[i] > ReductionLimit;
        }

        public static void UndoReduction()
        {
            foreach (var v in Реакции)
                v.Significant = true;
            foreach (var v in Вещества)
                v.Significant = true;
            foreach (var v in statesG)
                v.UndoReduction();
            ВеществаСпец.UndoReduction();

            //Реакции5Ред = (from a in Реакции5 where a.SignificantRKS select a).ToArray<Реакция>();
            //Реакции6Ред = (from a in Реакции6 where a.SignificantRKS select a).ToArray<Реакция>();
            //Реакции7Ред = (from a in Реакции7 where a.SignificantRKS select a).ToArray<Реакция>();
            Реакции5Ред = Реакции5;
            Реакции6Ред = Реакции6;
            Реакции7Ред = Реакции7;

            //ReductionReactionAmount = Реакции.Count(x => x.SignificantRKS);
        }

        public static void UserReduction()
        {
            for (int i = 0; i < ВеществаLength; i++)
                Вещества[i].Significant = Вещества[i].UserSignificant;

            ApplyReductionOnReactions();
        }

        static void ApplyReductionOnReactions()
        {
            foreach (var v in Реакции)
                v.Significant = true;
            //foreach (var v in Реакции)
            //{
            //    v.Significant = Вещества[v.Комп_1].Significant && Вещества[v.Продукт_1].Significant;
            //    if (v.Комп_2 != -1)
            //        v.Significant &= Вещества[v.Комп_2].Significant;
            //    if (v.Комп_3 != -1)
            //        v.Significant &= Вещества[v.Комп_3].Significant;
            //    if (v.Продукт_2 != -1)
            //        v.Significant &= Вещества[v.Продукт_2].Significant;
            //    if (v.Продукт_3 != -1)
            //        v.Significant &= Вещества[v.Продукт_3].Significant;
            //}

            //foreach (var s in from a in Вещества where !a.Significant select a)
            //{
            //    var r = new List<Реакция>();

            //    foreach (var v in Реакции)
            //    {
            //        bool b = false;

            //        b = Вещества[v.Комп_1] == s || Вещества[v.Продукт_1] == s;
            //        if (v.Комп_2 != -1)
            //            b |= Вещества[v.Комп_2] == s;
            //        if (v.Комп_3 != -1)
            //            b |= Вещества[v.Комп_3] == s;
            //        if (v.Продукт_2 != -1)
            //            b |= Вещества[v.Продукт_2] == s;
            //        if (v.Продукт_3 != -1)
            //            b |= Вещества[v.Продукт_3] == s;

            //        if (b)
            //            r.Add(v);
            //    }

            //    r.Sort((x, y) => Math.Sign(Math.Abs(x.Скорость) - Math.Abs(y.Скорость)));

            //    double sum = r.Sum<Реакция>(a => Math.Abs(a.Скорость));
            //    int i = 0;

            //    for (double sum1 = 0; sum1 < Precision * sum; i++, sum1 += Math.Abs(r[i].Скорость)) ;

            //    for (; i < r.Count; i++)
            //        r[i].Significant = false;
            //}

            foreach (var v in from a in Реакции where !a.Significant/* || !a.SignificantRKS*/ select a)
                v.Скорость = v.Константа = v.КонстантаОбр = 0;

            ReductionReactionAmount = Реакции.Count(x => x.Significant/* && x.SignificantRKS*/);

            foreach (var v in statesG)
                v.DoReduction();
            ВеществаСпец.DoReduction();

            Реакции5Ред = (from a in Реакции5 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
            Реакции6Ред = (from a in Реакции6 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
            Реакции7Ред = (from a in Реакции7 where a.Significant/* && a.SignificantRKS*/ select a).ToArray<Реакция>();
        }

        public static void Release()
        {
            if (MaxPrograms > 0)
                semMaxPrograms.Release();
        }

        static void Normalizeɣ()
        {
            double s = 1 / ɣ.Sum();

            for (int i = 0; i < ВеществаLength; i++)
                ɣ[i] *= s;
        }

        class RegularizationInfo
        {
            int cnt;

            public RegularizationInfo(Реакция реакция)
            {
                Реакция = реакция;
            }

            public Реакция Реакция { get; private set; }
            public double СредПроизводная { get; private set; }

            public void Add(double dgdt)
            {
                СредПроизводная = СредПроизводная * cnt + dgdt;
                cnt++;
                СредПроизводная /= cnt;
            }
        }

        static Dictionary<int, RegularizationInfo> newReduction = new Dictionary<int, RegularizationInfo>();
        static List<int> newReductionForFormat = new List<int>();

        public static IList<int> NewReduction { get { return newReductionForFormat; } }

        static double dtAbs(int spec)
        {
            if (dɣdt[spec] == 0)
                return double.PositiveInfinity;
            return Math.Abs(ɣ[spec] / dɣdt[spec]);
        }

        static void NomralizeIons()
        {
            double kationsSum = kations.Sum(i => ɣ[i]), anionsSum = anions.Sum(i => ɣ[i]),
                avgSum = (kationsSum + anionsSum) * 0.5, kationNormKoef = avgSum / kationsSum, anionNormKoef = avgSum / anionsSum;

            foreach (int i in kations)
                ɣ[i] *= kationNormKoef;

            foreach (int i in anions)
                ɣ[i] *= anionNormKoef;
        }

        static bool stopOnTimer = false;
        const double θmin = 30;

        public static double[,] CalcConstArray(double[] E_N, double[] Tv, int[] reactions)
        {
            double[,] result = new double[E_N.Length * Tv.Length, reactions.Length + 3];

            for (int i = 0; i < E_N.Length; i++)
                for (int j = 0; j < Tv.Length; j++)
                {
                    SetBolzmannSpec(Tv[j]);
                    θ0 = E_N[i];
                    Te = 0;
                    РасчётФРЭЭ.ResetEEDF();
                    РасчётФРЭЭ.CalcEEDF();
                    РасчётФРЭЭ.ResetEEDF();
                    РасчётФРЭЭ.CalcEEDF();
                    РасчётФРЭЭ.ResetEEDF();
                    РасчётФРЭЭ.CalcEEDF();

                    result[i + j * E_N.Length, 0] = E_N[i];
                    result[i + j * E_N.Length, 1] = Tv[j];
                    result[i + j * E_N.Length, 2] = Te;
                    for (int k = 0; k < reactions.Length; k++)
                        result[i + j * E_N.Length, 3 + k] = Реакции[reactions[k]].Константа;
                }

            return result;
        }

        static double oldH;

        static double geAddition()
        {
            return (ɣ[Ne] - EɣMax * N0 / N) * (1 + 1e6 * (1 - EɣMax * N0 / (N * ɣ[Ne])) * (1 - EɣMax * N0 / (N * ɣ[Ne]))) * 1e7;
        }

        public static void CalcLoop()
        {
            int eedfCnt = EEDFrequency, formatCnt = FormatFrequency, plotCnt = PlotFrequency, plotCnt2 = PlotFrequency2,
                reductionCnt = ReductionFrequency, eedfCnt1 = 0, tether = 0;
            double tAutoSave = AutoSaveDataSetInterval, lastθ0 = 0, apprdθ0dt = 0, apprθ0 = 0, apprθ0t = 0, θ1 = 0, lastdGdtE = 0, eedfPrec1 = 0, ge = 0,
                tetherθ0 = 0;
            int[] signifInd = new int[0];
            bool θControlOn = false;
            //int reductionFrequency = 0, reductionSchemeCnt = 10;
            //bool reductionBreak = false, undoReduction = false;
            //Вещество[] signifComp = new Вещество[0];

            if (MaxPrograms > 0)
                semMaxPrograms.WaitOne();

            while (work)
            {
                dtAvg = (dtAvg * formatCnt + dt) / (formatCnt + 1);

                eedfCnt++;
                formatCnt++;
                plotCnt++;
                plotCnt2++;

                if (tAutoSave > 0)
                    tAutoSave -= dt;
                else
                {
                    tAutoSave = AutoSaveDataSetInterval;
                    if (AutoSaveDataSet != AutoSave.No && AutoSaveNeeded != null)
                        AutoSaveNeeded(Type.Missing, EventArgs.Empty);
                }

                stopOnTimer |= SignalStateE;

                if (plotCnt >= PlotFrequency || stopOnTimer)
                {
                    plotCnt = 0;
                    OnPlot();
                }

                if (plotCnt2 >= PlotFrequency2 || stopOnTimer)
                {
                    plotCnt2 = 0;
                    OnPlot2();
                }

                if (EɣMax > 0)
                    if (ɣ[Ne] > EɣMax * N0 / N)
                        θControlOn = true;

                Normalizeɣ();
                NomralizeIons();

                if (θControlOn)
                {
                    ge = (dɣdt[Ne] + geAddition()) / ɣ[Ne];

                    if (!РасчётФРЭЭ.ForceCalcEEDF && /*ɣ[Ne] > EɣMax * N0 / N &&*/ Math.Abs(ge) > eedfPrec1)
                        if (ɣ[Ne] > 1.01 * EɣMax * N0 / N)
                            θ0 = 0;
                        else
                            if (ɣ[Ne] < 0.99 * EɣMax * N0 / N)
                                θ0 = θmax;
                            else
                            {
                                РасчётФРЭЭ.ForceCalcEEDF = true;
                                lastθ0 = θ0;
                                θ1 = θ0;
                                eedfCnt1 = 0;
                                eedfPrec1 = 10000;
                                lastdGdtE = ge;
                            }
                }

                if (eedfCnt >= EEDFrequency || РасчётФРЭЭ.ForceCalcEEDF)
                {
                    eedfCnt = 0;

                    if (РасчётФРЭЭ.ForceCalcEEDF)
                    {
                        double θ0_1 = θ0;

                        if (lastdGdtE == ge)
                        {
                            double newθ0 = apprθ0 + apprdθ0dt * (t - apprθ0t);

                            if (Math.Abs(newθ0 - θ0) > 1e-10)
                                θ0 = newθ0;
                            else
                                θ0 = θ0 - 5 - 5 * new Random().NextDouble();
                        }
                        else
                            θ0 = (θ0 * lastdGdtE - ge * lastθ0) / (lastdGdtE - ge);

                        if (θ0 >= θmax || θ0 <= θmin && tether == 0)
                        {
                            tether = 2;
                            if (lastθ0 >= θmax || lastθ0 <= θmin)
                                tetherθ0 = (θmax + θmin) / 2;
                            else
                                tetherθ0 = lastθ0;
                        }

                        switch (tether)
                        {
                            case 2:
                                θ0 = tetherθ0 + 5 + 5 * new Random().NextDouble();
                                tether = 1;
                                break;
                            case 1:
                                θ0 = tetherθ0 - 5 - 5 * new Random().NextDouble();
                                tether = 0;
                                break;
                        }

                        lastθ0 = θ0_1;
                        lastdGdtE = ge;
                        eedfCnt1++;
                        if (eedfCnt1 % 5 == 0)
                            eedfPrec1 *= 2;
                    }

                    РасчётФРЭЭ.CalcEEDF();
                }

                if (formatCnt >= FormatFrequency || stopOnTimer)
                {
                    formatCnt = 0;
                    for (int i = 0; i < ВеществаLength; i++)
                        if (ɣ[i] < ɣMin)
                            ɣ[i] = 0;
                    OnFormat(stopOnTimer);
                    stopOnTimer = false;
                }

                //if (EnableReduction)
                //    if (reductionCnt >= reductionFrequency)
                //    {
                //        reductionCnt = 0;

                //        reducted = true;

                //        if (reductionBreak)
                //        {
                //            if (undoReduction)
                //            {
                //                undoReduction = false;
                //                UndoReduction();
                //            }
                //            else
                //            {
                //                foreach (var v in from a in signifComp where a.Significant select a)
                //                    v.CalcOscillationFactor();
                //                reductionBreak = false;

                //                foreach (var v in from a in signifComp
                //                                  where a.Significant && Math.Abs(a.OscillationFactor) < CriticalOscillationFactor
                //                                  orderby a.AbsDerivative descending
                //                                  select a)
                //                {
                //                    v.Significant = false;
                //                    reductionBreak = true;
                //                    break;
                //                }
                //                foreach (var v in from a in signifComp where Math.Abs(a.OscillationFactor) >= CriticalOscillationFactor select a)
                //                    v.Significant = true;

                //                //var vv = (from a in signifComp
                //                //          where a.Significant && Math.Abs(a.OscillationFactor) < criticalOscillationFactor
                //                //          orderby a.AvgDerivative descending
                //                //          select a).ToArray<Вещество>();

                //                //if (vv.Length == 0)
                //                //    reductionBreak = 0;
                //                //else
                //                //    foreach (var v in vv)
                //                //    {
                //                //        v.Significant = false;
                //                //        reductionBreak--;
                //                //        break;
                //                //    }
                //            }
                //            reductionFrequency = ReductionFrequency2;
                //        }
                //        else
                //        {
                //            UserReduction();
                //            signifComp = (from a in Вещества where !a.UserSignificant select a).ToArray<Вещество>();
                //            foreach (var v in from a in signifComp where Math.Abs(a.OscillationFactor) >= CriticalOscillationFactor select a)
                //                v.Significant = true;
                //            reductionBreak = true;
                //            signifInd = (from a in signifComp select Array.IndexOf(Вещества, a)).ToArray<int>();
                //            undoReduction = true;
                //            reductionFrequency = ReductionFrequency;
                //        }
                //    }
                //    else
                //        reductionCnt++;
                //else
                //    if (reducted)
                //    {
                //        reducted = false;
                //        UndoReduction();
                //    }

                CalcdɣdtMol();
                Calcdɣdt();
                CalcTermDyn();
                AddElectrondGdt();
                CalcSHD();

                if (EnableReduction)
                {
                    if (reductionCnt >= ReductionFrequency)
                    {
                        reductionCnt = 0;

                        var dict = new Dictionary<int, RegularizationInfo>();

                        foreach (var r in Реакции)
                            foreach (var v in r.Вещества)
                                if (!dict.ContainsKey(v.Key) && !newReduction.ContainsKey(v.Key) && !Вещества[v.Key].NewUserSignificant && ɣ[v.Key] > ɣMin)
                                {
                                    double d = Math.Abs(dɣdt[v.Key] / (r.Скорость * v.Value));

                                    if (d < Precision)
                                        dict.Add(v.Key, new RegularizationInfo(r));
                                }

                        var vv = new Dictionary<int, double>();

                        for (int i = 0; i < ВеществаLength; i++)
                            if (ɣ[i] > ɣMin && !newReduction.ContainsKey(i))
                                if (dɣdt[i] != 0)
                                    vv.Add(i, dtAbs(i));
                                else
                                    vv.Add(i, double.PositiveInfinity);

                        if (dict.Count > 0)
                        {
                            //int n = vv.OrderBy<KeyValuePair<int, double>, double>(x => x.Value).ToArray()[0].Key;
                            double dMin = vv.Min<KeyValuePair<int, double>>(x => x.Value);
                            int n = dict.OrderBy<KeyValuePair<int, RegularizationInfo>, double>(x => dtAbs(x.Key)).ToArray()[0].Key;

                            //if (dict.ContainsKey(n))
                            if (dtAbs(n) < 100 * dMin)
                                newReduction.Add(n, dict[n]);
                            else
                            {
                                newReductionForFormat.Clear();
                                newReductionForFormat.AddRange(newReduction.Keys);
                                newReduction.Clear();
                            }
                        }
                        else
                        {
                            newReductionForFormat.Clear();
                            newReductionForFormat.AddRange(newReduction.Keys);
                            newReduction.Clear();
                        }

                        //var l = new List<double>();
                        //double dtMinRed = 0, dev = 0;
                        //int cnt = 0;

                        //for (int ii = 0; ii < ВеществаLength; ii++)
                        //    if (!newReduction.ContainsKey(ii))
                        //        if (Math.Abs(dɣdt[ii]) * dtInit > ɣMin && ɣ[ii] > ɣMin)
                        //        {
                        //            var d = Math.Abs(ɣ[ii] / dɣdt[ii]);

                        //            if (d < 1e-3)
                        //                l.Add(Math.Log(d));
                        //        }

                        //foreach (double d in l)
                        //{
                        //    dtMinRed += d;
                        //    cnt++;
                        //}

                        //dtMinRed /= cnt;

                        //foreach (double d in l)
                        //{
                        //    double dd = d - dtMinRed;
                        //    dev += dd * dd;
                        //}

                        //dtMinRed = Math.Exp(dtMinRed);// - 3 * Math.Sqrt(dev / (cnt - 1)));

                        //foreach (var key in newReduction.Keys.ToArray())
                        //    if (Math.Abs(ɣ[key] / dɣdt[key]) > dtMinRed)
                        //        newReduction.Remove(key);
                        //if (dict.Count > 0)
                        //{
                        //    var v = (from a in dict orderby a.Value.ФакторОсц select a).ToArray()[0];
                        //    newReduction.Add(v.Key, v.Value);
                        //}
                        //else
                    }
                    else
                        reductionCnt++;

                    bool b = reductionCnt >= ReductionFrequency / 5;

                    foreach (var v in newReduction)
                    {
                        v.Value.Add(dɣdt[v.Key]);
                        dɣdt[v.Key] = b ? v.Value.СредПроизводная * 1.25 : 0;
                    }
                }

                if (РасчётФРЭЭ.ForceCalcEEDF)
                    if (Math.Abs(dɣdt[Ne] + geAddition()) / ɣ[Ne] > eedfPrec1)
                        dɣdt_res[Ne] = dɣdt[Ne];
                    else
                    {
                        if (t > apprθ0t)
                        {
                            if (apprθ0t > 0)
                                apprdθ0dt = (θ1 - apprθ0) / (t - apprθ0t);
                            apprθ0t = t;
                        }

                        apprθ0 = θ1;
                        РасчётФРЭЭ.ForceCalcEEDF = false;
                    }

                if (!РасчётФРЭЭ.ForceCalcEEDF)
                    TimeStep();
            }

            Reset();
            if (stoppedCallback != null)
            {
                stoppedCallback();
                stoppedCallback = null;
            }
        }

        static void TimeStep()
        {
            for (int i = 0; i < ВеществаLength; i++)
                dɣdt_res[i] = (9 * dɣdt[i] + 19 * dɣdt_1[i] - 5 * dɣdt_2[i] - dɣdt_3[i]) / 22;
            //dɣdt_res[i] = (55 * dɣdt[i] - 59 * dɣdt_1[i] + 37 * dɣdt_2[i] - 9 * dɣdt_3[i]) / 24; // метод Адамса 4 порядка
            //dɣdt_res[i] = (9 * dɣdt[i] + 19 * dɣdt_1[i] - 5 * dɣdt_2[i] - dɣdt_3[i]) / 48; // метод Адамса-Моултона 4 порядка - НЕПРАВИЛЬНЫЙ!!!
            //dɣdt_res[i] = (dɣdt[i] + dɣdt_1[i]) / 2; // 2 порядок точности
            //dɣdt_res[i] = (dɣdt[i] + 2 * dɣdt_1[i] + dɣdt_2[i]) / 4; // 3 порядок точности
            //foreach(int i in ВеществаСпец.CalcSpec)
            //    dɣdt_res[i] = (dɣdt[i] + 3 * dɣdt_1[i] + 3 * dɣdt_2[i] + dɣdt_3[i]) / 8;//???
            dɣdt_3 = dɣdt_2;
            dɣdt_2 = dɣdt_1;
            dɣdt_1 = dɣdt.Clone() as double[];

            Calcdt();
            //if (EnableReduction && reductionCnt < reductionFrequency)
            //    for (int i = 0; i < signifComp.Length; i++)
            //    {
            //        var v = signifComp[i];
            //        v.Derivative += dɣdt_res[signifInd[i]] * dt;
            //        v.AbsDerivative += Math.Abs(dɣdt_res[signifInd[i]]) * dt;
            //    }

            if (IsTimerSet)
                if (t + dt > Timer)
                {
                    dt = Timer - t;
                    stopOnTimer = true;
                    IsTimerSet = false;
                }

            for (int i = 0; i < ВеществаLength; i++)
                if (Вещества[i].Significant)
                    ɣ[i] += dɣdt_res[i] * dt;

            foreach (var v in Молекулы)
                v.TimeStep();

            dQedt = /*(H - oldH) / dt*/ Cp * dTdt + SHD - 8.314 * T * (dΓdt + dTdt / T + dNdtA);
            oldH = H;
            T += dTdt * dt;
            N += N * (dΓdt + dNdtA) * dt;
            //if (AreaSize != 0)
            //    AreaSize += Math.Sqrt(8.314e7 * T / (9 * ɣCpCv * μ)) * (1 - N0 * T0 / (N * T)) * dt;
            t += dt;
        }

        static void Reset()
        {
            work = true;

            for (int i = 0; i < ВеществаLength; i++)
                dɣdt[i] = dɣdt0[i] = 0;

            РасчётФРЭЭ.ResetEEDF();
            foreach (var v in Молекулы)
                v.Reset();

            SetBolzmann();

            t = dTdt = dHdt = 0;

            CalcdɣdtMol();
            Calcdɣdt();
            CalcTermDyn();
            AddElectrondGdt();
            Calcdt();

            OnFormat();
        }

        static double Pow(double x, int d)
        {
            switch (d)
            {
                case -1: return 1 / x;
                case 0: return 1;
                case 1: return x;
                case 2: return x * x;
                case 3: return x * x * x;
                case 4: return x * x * x * x;
                default: throw new ArgumentOutOfRangeException("Поддерживаются только целые степени от -1 до 4");
            }
        }
    }

    public class OptimizedAttribute : Attribute { }
}
