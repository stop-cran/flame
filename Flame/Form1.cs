using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Flame3;
using System.Threading;
using Flame3.Properties;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Microsoft.Office.Interop.Excel;

namespace KryptonForm
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        private string TextExtra { get; set; }

        static ToolTip tError = new ToolTip() { ToolTipIcon = ToolTipIcon.Error, IsBalloon = true };
        bool collectData;
        static List<double[]> reactionRates = new List<double[]>();
        List<double[]> vibrTemp = new List<double[]>();
        bool dontAskOnClose, exitAfterSave, increaseKCIndex;
        int KCNumber;

        static string DataFolder { get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "Модель разряда"; } }
        static string ExcelFolder { get { return DataFolder + Path.DirectorySeparatorChar + "Отчёты"; } }
        static string InputDatFolder { get { return DataFolder + Path.DirectorySeparatorChar + "Входные данные"; } }
        static string OutputDatFolder { get { return DataFolder + Path.DirectorySeparatorChar + "Выходные данные"; } }
        static string SchemeFolder { get { return DataFolder + Path.DirectorySeparatorChar + "Схемы"; } }

        static double Parse2(double defValue, string text)
        {
            try
            {
                return double.Parse(text);
            }
            catch
            {
                MessageBox.Show("Неверный формат", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return defValue;
            }
        }

        static double Parse(double defValue, object sender)
        {
            return Parse2(defValue, ((RibbonTextBox)sender).TextBoxText);
        }

        static int Parse2(int defValue, string text)
        {
            try
            {
                return int.Parse(text);
            }
            catch
            {
                MessageBox.Show("Неверный формат", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return defValue;
            }
        }

        static int Parse(int defValue, object sender)
        {
            return Parse2(defValue, ((RibbonTextBox)sender).TextBoxText);
        }

        Thread workThread = new Thread(Engine.CalcLoop) { IsBackground = true };

        public Form1()
        {
            InitializeComponent();

            Engine.T = Settings.Default.T;
            Engine.T0 = Engine.T;
            Engine.N = Settings.Default.N;
            Engine.N0 = Engine.N;
            Engine.θ = Settings.Default.tetta;
            Engine.θ0 = Settings.Default.tetta;
            Engine.θmax = Engine.θ0;
            Engine.РасчётФРЭЭ.Eω = Settings.Default.Ew;
            Engine.РасчётФРЭЭ.DC = Settings.Default.DC;
            Engine.РасчётФРЭЭ.Δε = Settings.Default.de;
            Engine.РасчётФРЭЭ.εLength = Settings.Default.eLength;
            Engine.РасчётФРЭЭ.εCalcLength = Settings.Default.eCalcLength;
            Engine.РасчётФРЭЭ.εApprFrom = Settings.Default.eApprFrom;
            Engine.РасчётФРЭЭ.εApprTo = Settings.Default.eApprTo;
            Engine.РасчётФРЭЭ.εPrecision = Settings.Default.ePrecision;
            Engine.Precision = Settings.Default.Precision;
            Engine.EEDFrequency = Settings.Default.EEDFrequency;
            Engine.PlotFrequency = Settings.Default.PlotFrequency;
            Engine.PlotFrequency2 = 100;
            Engine.FormatFrequency = Settings.Default.FormatFrequency;
            Engine.dtInit = Settings.Default.dtInit;
            Engine.ReductionFrequency = Settings.Default.ReductionFrequency;
            Engine.ReductionFrequency2 = Settings.Default.ReductionFrequency2;
            Engine.ReductionLimit = Settings.Default.ReductionLimit;
            Engine.Format += Engine_Format;
            Engine.Plot += Engine_Plot;
            Engine.Plot2 += new MethodInvoker(Engine_Plot2);
            Engine.ɣMin = Settings.Default.GammaMin;
            Engine.CriticalOscillationFactor = Settings.Default.СriticalOscillationFactor;
            Engine.AutoSaveDataSet = (AutoSave)Settings.Default.AutoSaveDataSet;
            Engine.AutoSaveDataSetInterval = Settings.Default.AutoSaveDataSetInterval;
            Engine.AutoSaveNeeded += new EventHandler(Engine_AutoSaveNeeded);
            Engine.C3H8ɣMin = Settings.Default.C3H8GammaMin;
            Engine.EɣMax = Settings.Default.EGammaMax;
            Engine.AreaSize = Settings.Default.AreaSize;

            rgbEnableAutomation.Checked = Settings.Default.EnableAutomation;
            kryptonRibbonGroupButton1.Checked = collectData = Settings.Default.WriteData;
            rbEnablereduction.Checked = Settings.Default.ApplyReduction;

            if (!DesignMode)
            {
                bsaClearLastDataSet.Click += (sender, e) => krbLastDataSet.TextBoxText = string.Empty;
                rtbTemp.Click += (sender, e) =>
                    {
                        Engine.T = Parse(Engine.T, sender);
                        Engine.T0 = Engine.T;
                        Engine.SetBolzmann();
                        Settings.Default.T = Engine.T;
                    };
                rtbConcentration.Click += (sender, e) =>
                    {
                        Engine.N = Parse(Engine.N, sender);
                        Engine.N0 = Engine.N;
                        Settings.Default.N = Engine.N;
                    };
                rtbEn.Click += (sender, e) =>
                    {
                        Engine.θ = Parse(Engine.θ, sender);
                        Engine.θ0 = Settings.Default.tetta;
                        Engine.θmax = Engine.θ0;
                        Settings.Default.tetta = Engine.θ;
                    };
                rtbEw.Click += (sender, e) => Engine.РасчётФРЭЭ.Eω = Parse(Engine.РасчётФРЭЭ.Eω, sender);
                bsSelectEN.Click +=
                    (sender, e) =>
                    {
                        bsSelectEw.Checked = !bsSelectEN.Checked;
                        Engine.РасчётФРЭЭ.DC = true;
                    };
                bsSelectEw.Click +=
                    (sender, e) =>
                    {
                        bsSelectEN.Checked = !bsSelectEN.Checked;
                        Engine.РасчётФРЭЭ.DC = false;
                    };
                rtbGammaMin.Click += (sender, e) => Engine.ɣMin = Parse(Engine.ɣMin, sender);
                krbGammaC3H8Min.Click += (sender, e) => Engine.C3H8ɣMin = Parse2(Engine.C3H8ɣMin, krbGammaC3H8Min.TextBoxText);
                krbCalcToE.Click += (sender, e) => Engine.EɣMax = Parse(Engine.EɣMax, sender);
                rtbOscillationFactor.Click += (sender, e) => Engine.CriticalOscillationFactor = Parse(Engine.CriticalOscillationFactor, sender);
                rtbAutomation.Click += (sender, e) => Settings.Default.AutoCalcStep = Parse2(Settings.Default.AutoCalcStep, rtbAutomation.TextBoxText);
                bsExit.Click += (sender, e) =>
                {
                    if (MessageBox.Show("Завершить работу?", Text, MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                        Close();
                };
                rtbStep.Click += (sender, e) => Engine.РасчётФРЭЭ.Δε = Parse(Engine.РасчётФРЭЭ.Δε, sender);
                rtbNodes.Click += (sender, e) => Engine.РасчётФРЭЭ.εLength = Parse2(Engine.РасчётФРЭЭ.εLength, rtbNodes.TextBoxText);
                rtbNodes2.Click += (sender, e) => Engine.РасчётФРЭЭ.εCalcLength = Parse(Engine.РасчётФРЭЭ.εCalcLength, sender);
                rtbNodes3.Click += (sender, e) => Engine.РасчётФРЭЭ.εApprFrom = Parse(Engine.РасчётФРЭЭ.εApprFrom, sender);
                rtbNodes4.Click += (sender, e) => Engine.РасчётФРЭЭ.εApprTo = Parse(Engine.РасчётФРЭЭ.εApprTo, sender);
                rtbEEDFPrec.Click += (sender, e) => Engine.РасчётФРЭЭ.εPrecision = Parse(Engine.РасчётФРЭЭ.εPrecision, sender);
                rtbEedfFreq.Click += (sender, e) => Engine.EEDFrequency = Parse(Engine.EEDFrequency, sender);
                rtbFormFreq.Click += (sender, e) => Engine.FormatFrequency = Parse(Engine.FormatFrequency, sender);
                kryptonRibbonGroupButton1.Click += (sender, e) => Settings.Default.WriteData = collectData = kryptonRibbonGroupButton1.Checked;
                krbRefreshForm.Click += (sender, e) => FormatData();
                rtbPlotFreq.Click += (sender, e) => Engine.PlotFrequency = Parse(Engine.PlotFrequency, sender);
                rtbReductionFreq.Click +=
                    (sender, e) => Engine.ReductionFrequency = Parse(Engine.ReductionFrequency, sender);
                rtbReductionFreq2.Click +=
                    (sender, e) => Engine.ReductionFrequency2 = Parse(Engine.ReductionFrequency2, sender);
                rtbReductionLimit.Click +=
                    (sender, e) =>
                    {
                        Engine.ReductionLimit = Parse(Engine.ReductionLimit, sender);
                        Engine.SetReductionLimit();
                        FormatData();
                    };
                rbEnablereduction.Click += new EventHandler(rbEnablereduction_Click);
                bsaPrograms.Click += (sender, e) => Engine.MaxPrograms = Parse(Engine.MaxPrograms, sender);
                rtbTimer.Click += (sender, e) =>
                {
                    if (rtbTimer.Checked)
                        try
                        {
                            Engine.Timer = Parse(0d, sender);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            rtbTimer.Checked = false;
                        }
                    else
                    {
                        rtbTimer.TextBoxText = string.Empty;
                        Engine.IsTimerSet = false;
                    }
                };

                qbStart.Click += (sender, e) =>
                    {
                        workingPending = true;
                        if (!Working)
                            Working = true;
                    };
                qbPause.Click += (sender, e) => workingPending = false;
                qbStop.Click += (sender, e) => Engine.Stop(() =>
                {
                    workThread = new Thread(Engine.CalcLoop);
                    qbPause.Enabled = qbStop.Enabled = qbRestart.Enabled = false;
                    krbCalcEEDF.Enabled = qbStart.Enabled = true;
                });
                qbRestart.Click += (sender, e) => Engine.Stop(() =>
                {
                    workThread = new Thread(Engine.CalcLoop);
                    Working = true;
                });
                rtbAutoSaveDataSet.Click += new EventHandler(bsaAutoSaveDataSet_Click);
                bsaAutoSaveAndExit.Click += new EventHandler(bsaAutoSaveDataSet_Click);

                krbAutoLoad.Click += new EventHandler(bsaAutoLoad_Click);
                bsaAutoLoadInvert.Checked = Settings.Default.AutoLoadInvert;
                krbKCReactions.Click += new EventHandler(bsaKCReactions_Click);
                krbKCIndex.Click += (sender, e) => Settings.Default.KCIndex = Parse(Settings.Default.KCIndex, sender);
                krbKCAlpha.Click += (sender, e) => Settings.Default.KCAlpha = Parse(Settings.Default.KCAlpha, sender);
                krbKCT1.Click += (sender, e) => Settings.Default.KCT1 = Parse(Settings.Default.KCT1, sender);
                krbKCT2.Click += (sender, e) => Settings.Default.KCT2 = Parse(Settings.Default.KCT2, sender);
                krtbReductionSchemeFactor.Click += (sender, e) => Settings.Default.ReductionFactor = Parse(Settings.Default.ReductionFactor, sender);
                krtbReductionSchemeFile.Click += (sender, e) =>
                    {
                        Settings.Default.ReductionFile = string.Empty;
                        krtbReductionSchemeFile.TextBoxText = string.Empty;
                    };
                rtbAreaSize.Click += (sender, e) =>
                    {
                        if (rtbAreaSize.TextBoxText == "∞" || rtbAreaSize.TextBoxText == "")
                            Engine.AreaSize = 0;
                        else
                            Engine.AreaSize = Parse(Engine.AreaSize, sender);
                        if (Engine.AreaSize == 0)
                            rtbAreaSize.TextBoxText = "∞";
                    };
            }
        }

        void bsaKCReactions_Click(object sender, EventArgs e)
        {
            string text = krbKCReactions.TextBoxText.Replace(" ", string.Empty);
            bool assign = true;
            int i;

            if (!string.IsNullOrEmpty(text) && Settings.Default.KCReactions != text)
                foreach (var reaction in text.Split(','))
                    if (!int.TryParse(reaction, out i))
                    {
                        assign = false;
                        break;
                    }

            if (assign)
            {
                Settings.Default.KCReactions = text;
                Settings.Default.KCIndex = 0;
                krbKCIndex.TextBoxText = Settings.Default.KCIndex.ToString();
            }

            krbKCReactions.TextBoxText = Settings.Default.KCReactions;
        }

        bool bsaAutoSaveDataSetInterLock;

        void bsaAutoSaveDataSet_Click(object sender, EventArgs e)
        {
            if (!bsaAutoSaveDataSetInterLock)
                try
                {
                    bsaAutoSaveDataSetInterLock = true;
                    if (rtbAutoSaveDataSet.Checked)
                        Engine.AutoSaveDataSet = bsaAutoSaveAndExit.Checked ?
                            AutoSave.SaveAndExit : AutoSave.Save;
                    else
                    {
                        Engine.AutoSaveDataSet = AutoSave.No;
                        bsaAutoSaveAndExit.Checked = false;
                    }
                    if (Engine.AutoSaveDataSet != AutoSave.No)
                        Engine.AutoSaveDataSetInterval = Parse(Engine.AutoSaveDataSetInterval, sender);
                    rtbAutoSaveDataSet.Checked = !rtbAutoSaveDataSet.Checked;
                }
                finally
                {
                    bsaAutoSaveDataSetInterLock = false;
                }
        }

        void bsaAutoLoad_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoLoad = krbAutoLoad.Checked;

            if (Settings.Default.AutoLoad)
            {
                krbLastDataSet.TextBoxText = string.Empty;
                Settings.Default.AutoLoadIndex = Parse(Settings.Default.AutoLoadIndex, sender);
            }
        }

        string GetNewDataFileName()
        {
            string fileName = OutputDatFolder + Path.DirectorySeparatorChar + TextExtra +
                (Engine.РасчётФРЭЭ.DC ? "E_N = " + Engine.θ0 + " Td" : "E_ω = " + Engine.РасчётФРЭЭ.Eω + " В_см*ГГц") +
                ", t=" + Engine.t.ToString("0.########E-0").Replace(".", ","), fileName2 = fileName;
            for (int i = 0; File.Exists(fileName2 + ".dat"); i++, fileName2 = fileName + " (" + i + ")") ;

            return fileName + ".dat";
        }

        void Engine_AutoSaveNeeded(object sender, EventArgs e)
        {
            if (Engine.AutoSaveDataSet != AutoSave.No)
            {
                SaveData(GetNewDataFileName());

                if (Engine.AutoSaveDataSet == AutoSave.SaveAndExit)
                {
                    exitAfterSave = true;
                    dontAskOnClose = true;
                    Close();
                }
            }
        }

        List<double> tIgn_t = new List<double>(1024);
        List<double> tIgn_dTdt = new List<double>(1024);
        List<double> tIgn_O2 = new List<double>(1024);

        void Engine_Plot2()
        {
            tIgn_t.Add(Engine.t);
            tIgn_dTdt.Add(Engine.dTdt);
            tIgn_O2.Add(Engine.ɣ[Flame3.Engine.ВеществаСпец.IndO2]);
        }

        void Engine_Plot()
        {
            if (collectData)
            {
                double[] a = new double[Engine.ВеществаLength + FixedCurveCount];
                bool changed = true;

                Array.Copy(Engine.CalcData.ɣ, 0, a, FixedCurveCount, Engine.ВеществаLength);
                a[0] = Engine.t;
                a[1] = Engine.T;
                a[2] = Engine.dTdt;
                a[3] = Engine.dQedt;
                a[4] = Engine.H0;
                a[5] = Engine.H;
                a[6] = Engine.Cp;
                a[7] = Engine.θ0;
                a[8] = Engine.Te;
                a[9] = Engine.N;

                if (plMain.CurveY != null)
                    if (plMain.CurveY.Count > 0)
                        if (plMain.CurveY[0].Count > 0)
                        {
                            changed = false;
                            for (int i = 1; i < a.Length; i++)
                            {
                                double d = plMain.CurveY[i - 1][plMain.CurveY[0].Count - 1];

                                if (d == 0)
                                    changed = true;
                                else
                                    changed = Math.Abs((a[i] - d) / d) > 0.1;

                                if (changed)
                                    break;
                            }
                        }

                if (changed)
                {
                    plMain.Add(a);

                    a = new double[Engine.Реакции.Length + 1];
                    a[0] = Engine.t;
                    Array.Copy(Engine.CalcData.Скорости, 0, a, 1, Engine.Реакции.Length);
                    reactionRates.Add(a);

                    a = new double[Engine.Молекулы.Length + 1];
                    a[0] = Engine.t;
                    Array.Copy(Engine.CalcData.Tvibr, 0, a, 0, Engine.Молекулы.Length);
                    vibrTemp.Add(a);
                }
            }
        }

        bool Working
        {
            get
            {
                return (workThread.ThreadState & ~(ThreadState.Background | ThreadState.WaitSleepJoin)) ==
                    ThreadState.Running;
            }
            set
            {
                if (InvokeRequired)
                    Invoke((MethodInvoker)(() => Working = value));
                else
                    if (Working ^ value)
                {
                    qbPause.Enabled = qbStop.Enabled = value;
                    krbCalcEEDF.Enabled = qbStart.Enabled = !value;
                    qbRestart.Enabled = true;
                    pbWorking.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;

                    switch (workThread.ThreadState & ~ThreadState.Background)
                    {
                        case ThreadState.Running:
                        case ThreadState.WaitSleepJoin:
                            if (value)
                                throw new InvalidOperationException();
                            else
                                workThread.Suspend();
                            break;
                        case ThreadState.Stopped:
                            if (!value)
                                throw new InvalidOperationException();
                            workThread.Start();
                            break;
                        case ThreadState.Suspended:
                            if (!value)
                                throw new InvalidOperationException();
                            workThread.Resume();
                            break;
                        case ThreadState.Unstarted:
                            if (!value)
                                throw new InvalidOperationException();
                            workThread.Start();
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        bool workingEEDF;

        bool WorkingEEDF
        {
            get
            {
                return workingEEDF;
            }
            set
            {
                if (WorkingEEDF ^ value)
                {
                    qbStart.Enabled = krbCalcEEDF.Enabled = !value;
                    workingEEDF = value;
                    pbWorking.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
                }
            }
        }

        IAsyncResult formatResult;
        bool workingPending;

        void Engine_Format(object sender, CancelEventArgs e)
        {
            if (formatResult != null)
                if (!formatResult.IsCompleted)
                    EndInvoke(formatResult);
            if (e.Cancel)
                workingPending = false;
            formatResult = BeginInvoke((MethodInvoker)FormatData);
        }

        void FormatData()
        {
            try
            {
                var newReduction = new List<int>(Engine.NewReduction);

                rtbTemp.TextBoxText = Engine.T.ToString("#####.##");
                rtbConcentration.TextBoxText = Engine.N.ToString("0.####E-0");
                rtbElectronTemperature.TextBoxText = Engine.Te.ToString("##0.####");
                rtbEn.TextBoxText = Engine.θ.ToString("#####.##");
                rtbEw.TextBoxText = Engine.РасчётФРЭЭ.Eω.ToString();
                rtbTime.TextBoxText = Engine.t.ToString("0.####E-0");
                rtbTimeStep.TextBoxText = Engine.dtAvg.ToString("0.####E-0");
                rtbAreaSize.TextBoxText = Engine.AreaSize.ToString("0.####E-0");

                double dt = double.MaxValue;
                int ind = -1;

                for (int i = 0; i < Engine.ВеществаLength; i++)
                    if (!newReduction.Contains(i))
                        if (Math.Abs(Engine.CalcData.dɣdt[i]) * Engine.dtInit > Engine.ɣMin && Engine.CalcData.ɣ[i] > Engine.ɣMin)
                        {
                            double d = Math.Abs(Engine.CalcData.ɣ[i] / Engine.CalcData.dɣdt[i]);

                            if (d < dt)
                            {
                                dt = d;
                                ind = i;
                            }
                        }

                if (ind != -1)
                    rtbTimeStep.TextBoxText += " (" + Engine.Вещества[ind].Формула + ")";

                rtbEntalpy.TextBoxText = Engine.CalcData.H.ToString("#0.####");
                rtbStep.TextBoxText = Engine.РасчётФРЭЭ.Δε.ToString();
                rtbNodes.TextBoxText = Engine.РасчётФРЭЭ.εLength.ToString();
                rtbNodes2.TextBoxText = Engine.РасчётФРЭЭ.εCalcLength.ToString();
                rtbNodes3.TextBoxText = Engine.РасчётФРЭЭ.εApprFrom.ToString();
                rtbNodes4.TextBoxText = Engine.РасчётФРЭЭ.εApprTo.ToString();
                rtbEEDFPrec.TextBoxText = Engine.РасчётФРЭЭ.εPrecision.ToString();
                rtbEedfFreq.TextBoxText = Engine.EEDFrequency.ToString();
                rtbFormFreq.TextBoxText = Engine.FormatFrequency.ToString();
                rtbPlotFreq.TextBoxText = Engine.PlotFrequency.ToString();
                rtbPlotLen.TextBoxText = plMain.CurveX.Count.ToString();
                rtbReductionFreq.TextBoxText = Engine.ReductionFrequency.ToString();
                rtbReductionFreq2.TextBoxText = Engine.ReductionFrequency2.ToString();
                rtbReductionLimit.TextBoxText = Engine.ReductionLimit.ToString();
                krbKCReactions.TextBoxText = Settings.Default.KCReactions;
                krbKCIndex.TextBoxText = Settings.Default.KCIndex.ToString();
                krbKCAlpha.TextBoxText = Settings.Default.KCAlpha.ToString();
                krbKCT1.TextBoxText = Settings.Default.KCT1.ToString();
                krbKCT2.TextBoxText = Settings.Default.KCT2.ToString();
                rtbReductionReactionAmount.TextBoxText = Engine.ReductionReactionAmount.ToString();
                rtbGammaMin.TextBoxText = Engine.ɣMin.ToString();
                krbGammaC3H8Min.TextBoxText = Engine.C3H8ɣMin.ToString();
                krbCalcToE.TextBoxText = Engine.EɣMax.ToString();
                rtbTimer.Checked = Engine.IsTimerSet;
                rtbTimer.TextBoxText = Engine.IsTimerSet ? Engine.Timer.ToString() : string.Empty;
                rtbOscillationFactor.TextBoxText = Engine.CriticalOscillationFactor.ToString();
                rtbAutomation.TextBoxText = Settings.Default.AutoCalcStep.ToString();
                rtbAutoSaveDataSet.Checked = Engine.AutoSaveDataSet == AutoSave.No;
                bsaAutoSaveAndExit.Checked = Engine.AutoSaveDataSet == AutoSave.SaveAndExit;
                rtbAutoSaveDataSet.TextBoxText = Engine.AutoSaveDataSetInterval.ToString("0.####E-0");
                krbAutoLoad.TextBoxText = Settings.Default.AutoLoadIndex.ToString();
                krbAutoLoad.Checked = Settings.Default.AutoLoad;
                krtbReductionSchemeFactor.TextBoxText = Settings.Default.ReductionFactor.ToString();
                krtbReductionSchemeFile.TextBoxText = Settings.Default.ReductionFile;
                rtbAreaSize.TextBoxText = Engine.AreaSize == 0 ? "∞" : Engine.AreaSize.ToString();

                foreach (var v in from r in kineticsDataSet.реакции
                                  join rr in Engine.Реакции on r.Номер equals rr.Номер
                                  select new { a = r, b = rr })
                {
                    v.a.Константа = v.b.Константа;
                    v.a.КонстантаОбр = v.b.КонстантаОбр;
                    v.a.Скорость = v.b.Скорость;
                    v.a.Значимая = v.b.Significant;// && v.b.SignificantRKS;
                }

                foreach (var v in from v in kineticsDataSet.Состав
                                  join vv in Engine.Вещества on v.Вещество equals vv.Номер
                                  select new { a = v, b = vv, Номер = v.Номер })
                {
                    v.a.Доля = Engine.CalcData.ɣ[v.Номер];
                    v.a.Производная_доли = Engine.CalcData.dɣdt[v.Номер];
                    v.a.Теплоёмкость = v.b.Cp;
                    v.a.Энтальпия = v.b.H;
                    v.a.Значимое = v.b.NewUserSignificant;
                }

                krtReduction2.Text = "Редукция " + newReduction.Aggregate<int, string>(null,
                    (x, y) => (x == null ? null : x + ", ") + Engine.Вещества[y].Формула);

                for (int j = 0; j < Engine.Молекулы.Length; j++)
                {
                    var v = Engine.Молекулы[j];

                    for (int i = 0; i < v.Уровни; i++)
                        foreach (var w in from c in kineticsDataSet.Колебательные_распределения
                                          where c.Молекула == Engine.Вещества[v.Вещество].Номер && c.Уровень == i
                                          select c)
                        {
                            w.Доля = Engine.CalcData.X[j][i];
                            w.Производная_доли = Engine.CalcData.dXdt[j][i];
                        }
                }

                plotEEDF.CurveX.Clear();
                plotEEDF.CurveX.AddRange(Engine.РасчётФРЭЭ.ε);
                plotEEDF.CurveY.Clear();
                plotEEDF.CurveY.Add(new Curve() { Color = Color.Black, Width = 1, AvgList = plotEEDF.CurveX });
                plotEEDF.CurveY[0].AddRange(Engine.CalcData.ФРЭЭ);
                plotEEDF.Invalidate();

                if (Engine.SignalState)
                {
                    Engine_Plot();
                    Engine.Release();
                    workingPending = false;
                }

                Working = workingPending;
                WorkingEEDF = false;

                if (Engine.SignalState)
                {
                    SaveData(GetNewDataFileName());
                    OnExcel();

                    if (Settings.Default.EnableAutomation)
                    {
                        dontAskOnClose = true;
                        Close();
                    }
                }
            }
            catch
            { }

            //if (Engine.Реакции != null)
            //    krgScheme.TextLine1 = "Схема (" + Engine.Реакции.Count(x => x.SignificantRKS) + " реакций)";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            OnLoad();
        }

        void CreateRecombinationReactions()
        {
            string[] ionsFrmA = new[] { "O2-", "O-", "O3-", "NO-", "NO2-", "NO3-*", "NO3-", "N2O-", "OH-" },
                ionsFrmB = new[] { "N2+", "O2+", "N+", "O+", "NO+", "NO2+", "N2O+", "OH+", "H2O+", "HO2+" },
                ionsFrmBC = new[]{"N2+", "N", "N", "O2+", "O", "O", "NO+", "N", "O", "NO2+", "N", "O2", "N2O+", "N2", "O",
                "N3+", "N2", "N", "N4+", "N2", "N2", "O4+", "O2", "O2", "NO+*O2", "NO", "O2", "NO+*NO", "NO", "NO", "O2+*N2", "O2", "N2",
                "H3O+", "H2O", "H", "O3H2+", "H2O", "O2", "OH+", "O", "H", "H2O+", "OH", "H", "HO2+", "OH", "O", "HN2+", "N2", "H"},
                ionsFrmC3 = new[] { "N2+", "O2+", "N+", "O+", "NO+", "NO2+", "N2O+", "OH+", "H2O+", "HO2+" },
                ionsFrmCD = new[]{"N3+", "N", "N2", "N4+", "N2", "N2", "O4+", "O2", "O2", "NO+*N2", "NO", "N2", "NO+*O2", "NO", "O2",
                    "NO+*NO", "NO", "NO", "O2+*N2", "O2", "N2", "H3O+", "H2O", "O", "O3H2+", "O2", "H2O"},
                    ionsFrmA5 = new[] { "O2-", "O-", "OH-" }, ionsFrmB5 = new[] { "N2+", "O2+", "NO+", "O+", "N+", "OH+" },
                    ionsFrm61 = new[] { "N+", "NO2", "O+", "O3", "NO+", "NO3" },
                    ionsFrm62 = new[] { "N2+", "N2O", "O2+", "O3", "NO+", "NO2", "O+", "O2", "N+", "NO" };

            int[] ionsA = Find(ionsFrmA), ionsB = Find(ionsFrmB), ionsBC = Find(ionsFrmBC), neutralsA = FindN(ionsFrmA),
                neutralsB = FindN(ionsFrmB), ionsC3 = Find(ionsFrmC3), neutralsC3 = FindN(ionsFrmC3), ionsCD = Find(ionsFrmCD),
                ionsA5 = Find(ionsFrmA5), neutralsA5 = FindN(ionsFrmA5), ionsB5 = Find(ionsFrmB5), neutralsB5 = FindN(ionsFrmB5),
                ions61 = Find(ionsFrm61), ions62 = Find(ionsFrm62);
            int o4m = Find("O4-"), o2 = Find("O2"), o2m = Find("O2-"), om = Find("O-");

            // I A- + B+ = A + B
            for (int i = 0; i < ionsA.Length; i++)
                for (int j = 0; j < ionsB.Length; j++)
                {
                    var row = kineticsDataSet.реакции.NewреакцииRow();

                    row.Комп_1 = ionsA[i];
                    row.Коэф_комп_1 = 1;
                    row.Комп_2 = ionsB[j];
                    row.Коэф_комп_2 = 1;
                    row.Продукт_1 = neutralsA[i];
                    row.Коэф_прод_1 = 1;
                    row.Продукт_2 = neutralsB[j];
                    row.Коэф_прод_2 = 1;
                    row.AP = 2.086e+18;
                    row.MP = -0.5;
                    row.EP = row.AM = row.EM = 0;
                    row.Флаг_Te = row.М = false;

                    kineticsDataSet.реакции.Rows.Add(row);
                }

            // II A- + (BC)+ = A + B + C
            for (int i = 0; i < ionsA.Length; i++)
                for (int j = 0; j < ionsBC.Length / 3; j++)
                {
                    var row = kineticsDataSet.реакции.NewреакцииRow();

                    row.Комп_1 = ionsA[i];
                    row.Коэф_комп_1 = 1;
                    row.Комп_2 = ionsBC[3 * j];
                    row.Коэф_комп_2 = 1;
                    row.Продукт_1 = neutralsA[i];
                    row.Коэф_прод_1 = 1;
                    row.Продукт_2 = ionsBC[3 * j + 1];
                    row.Коэф_прод_2 = 1;
                    row.Продукт_3 = ionsBC[3 * j + 2];
                    row.Коэф_прод_3 = 1;
                    row.AP = 6.022e+16;
                    row.EP = row.AM = row.EM = 0;
                    row.Флаг_Te = row.М = false;

                    kineticsDataSet.реакции.Rows.Add(row);
                }

            // III O4- + C+ = 2*O2 + C
            for (int j = 0; j < ionsC3.Length; j++)
            {
                var row = kineticsDataSet.реакции.NewреакцииRow();

                row.Комп_1 = o4m;
                row.Коэф_комп_1 = 1;
                row.Комп_2 = ionsC3[j];
                row.Коэф_комп_2 = 1;
                row.Продукт_1 = o2;
                row.Коэф_прод_1 = 2;
                row.Продукт_2 = neutralsC3[j];
                row.Коэф_прод_2 = 1;
                row.AP = 6.022e+16;
                row.EP = row.AM = row.EM = 0;
                row.Флаг_Te = row.М = false;

                kineticsDataSet.реакции.Rows.Add(row);
            }

            // IV O4- + (CD)+ = 2 O2 + C + D
            for (int j = 0; j < ionsCD.Length / 3; j++)
            {
                var row = kineticsDataSet.реакции.NewреакцииRow();

                row.Комп_1 = o4m;
                row.Коэф_комп_1 = 1;
                row.Комп_2 = ionsCD[3 * j];
                row.Коэф_комп_2 = 1;
                row.Продукт_1 = o2;
                row.Коэф_прод_1 = 2;
                row.Продукт_2 = ionsCD[3 * j + 1];
                row.Коэф_прод_2 = 1;
                row.Продукт_3 = ionsCD[3 * j + 2];
                row.Коэф_прод_3 = 1;
                row.AP = 6.022e+16;
                row.EP = row.AM = row.EM = 0;
                row.Флаг_Te = row.М = false;

                kineticsDataSet.реакции.Rows.Add(row);
            }

            // V A- + B+ + M = A + B + M
            for (int i = 0; i < ionsA5.Length; i++)
                for (int j = 0; j < ionsB5.Length; j++)
                {
                    var row = kineticsDataSet.реакции.NewреакцииRow();

                    row.Комп_1 = ionsA5[i];
                    row.Коэф_комп_1 = 1;
                    row.Комп_2 = ionsB5[j];
                    row.Коэф_комп_2 = 1;
                    row.Продукт_1 = neutralsA5[i];
                    row.Коэф_прод_1 = 1;
                    row.Продукт_2 = neutralsB5[j];
                    row.Коэф_прод_2 = 1;
                    row.AP = 1.131e+29;
                    row.MP = -2.5;
                    row.М = true;
                    row.EP = row.AM = row.EM = 0;
                    row.Флаг_Te = false;

                    kineticsDataSet.реакции.Rows.Add(row);
                }

            // VI A- + B+ + M = AB + M
            for (int i = 0; i < ions61.Length / 2; i++)
            {
                var row = kineticsDataSet.реакции.NewреакцииRow();

                row.Комп_1 = o2m;
                row.Коэф_комп_1 = 1;
                row.Комп_2 = ions61[2 * i];
                row.Коэф_комп_2 = 1;
                row.Продукт_1 = ions61[2 * i + 1];
                row.Коэф_прод_1 = 1;
                row.AP = 1.131e+29;
                row.MP = -2.5;
                row.М = true;
                row.EP = row.AM = row.EM = 0;
                row.Флаг_Te = false;

                kineticsDataSet.реакции.Rows.Add(row);
            }

            for (int i = 0; i < ions62.Length / 2; i++)
            {
                var row = kineticsDataSet.реакции.NewреакцииRow();

                row.Комп_1 = om;
                row.Коэф_комп_1 = 1;
                row.Комп_2 = ions62[2 * i];
                row.Коэф_комп_2 = 1;
                row.Продукт_1 = ions62[2 * i + 1];
                row.Коэф_прод_1 = 1;
                row.AP = 1.131e+29;
                row.MP = -2.5;
                row.М = true;
                row.EP = row.AM = row.EM = 0;
                row.Флаг_Te = false;

                kineticsDataSet.реакции.Rows.Add(row);
            }
        }

        static double GetDatFileTime(string fileName)
        {
            int ind;

            fileName = Path.GetFileNameWithoutExtension(fileName);
            ind = fileName.IndexOf("t=");

            return Convert.ToDouble(fileName.Substring(ind + 2));
        }

        void ReadNewReductionScheme()
        {
            if (!string.IsNullOrEmpty(Settings.Default.ReductionFile))
                if (File.Exists(Settings.Default.ReductionFile))
                {
                    var lReactions = new List<int>();
                    var doc = new XmlDocument();

                    doc.Load(Settings.Default.ReductionFile);

                    //foreach (XmlNode n in doc.SelectNodes("Схема//Вещества//Вещество"))
                    //    lSpec.Add(Convert.ToInt32(n.Attributes["Номер"].Value));
                    foreach (XmlNode n in doc.SelectNodes("Схема//Реакции//Реакция"))
                        lReactions.Add(Convert.ToInt32(n.Attributes["Номер"].Value));

                    foreach (var v in kineticsDataSet.реакции.Select())
                        if (!lReactions.Contains(Convert.ToInt32(v["Номер"])))
                            v.Delete();
                    kineticsDataSet.реакции.AcceptChanges();
                    //{
                    //    XmlNode n;
                    //    XmlAttribute a;

                    //    n = doc.SelectSingleNode("Схема//Реакции//Реакция[@Номер='" + v.Номер + "']//ЗначимоеВещество[@Номер='" +
                    //        Engine.Вещества[v.Комп_1].Номер + "']");

                    //    if (n != null)
                    //    {
                    //        v.Комп1_Огр = true;
                    //        a = n.Attributes["МинДоля"];
                    //        if (a != null)
                    //            v.Комп1_Мин = Convert.ToDouble(a.Value);
                    //        a = n.Attributes["МаксДоля"];
                    //        if (a != null)
                    //            v.Комп1_Макс = Convert.ToDouble(a.Value);
                    //    }

                    //    if (v.Тип_столкновения == 2 || v.Тип_столкновения == 3)
                    //    {
                    //        n = doc.SelectSingleNode("Схема//Реакции//Реакция[@Номер='" + v.Номер + "']//ЗначимоеВещество[@Номер='" +
                    //            Engine.Вещества[Engine.Ne].Номер + "']");

                    //        if (n != null)
                    //        {
                    //            v.Комп2_Огр = true;
                    //            a = n.Attributes["МинДоля"];
                    //            if (a != null)
                    //                v.Комп2_Мин = Convert.ToDouble(a.Value);
                    //            a = n.Attributes["МаксДоля"];
                    //            if (a != null)
                    //                v.Комп2_Макс = Convert.ToDouble(a.Value);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (v.Комп_2 != -1)
                    //        {
                    //            n = doc.SelectSingleNode("Схема//Реакции//Реакция[@Номер='" + v.Номер + "']//ЗначимоеВещество[@Номер='" +
                    //                Engine.Вещества[v.Комп_2].Номер + "']");

                    //            if (n != null)
                    //            {
                    //                v.Комп2_Огр = true;
                    //                a = n.Attributes["МинДоля"];
                    //                if (a != null)
                    //                    v.Комп2_Мин = Convert.ToDouble(a.Value);
                    //                a = n.Attributes["МаксДоля"];
                    //                if (a != null)
                    //                    v.Комп2_Макс = Convert.ToDouble(a.Value);
                    //            }
                    //        }

                    //        if (v.Комп_3 != -1)
                    //        {
                    //            n = doc.SelectSingleNode("Схема//Реакции//Реакция[@Номер='" + v.Номер + "']//ЗначимоеВещество[@Номер='" +
                    //                Engine.Вещества[v.Комп_3].Номер + "']");

                    //            if (n != null)
                    //            {
                    //                v.Комп3_Огр = true;
                    //                a = n.Attributes["МинДоля"];
                    //                if (a != null)
                    //                    v.Комп3_Мин = Convert.ToDouble(a.Value);
                    //                a = n.Attributes["МаксДоля"];
                    //                if (a != null)
                    //                    v.Комп3_Макс = Convert.ToDouble(a.Value);
                    //            }
                    //        }
                    //    }
                    //}
                    //else
                    //    v.SignificantRKSInternal = false;
                }
        }

        void OnLoad()
        {
            химические_веществаTableAdapter1.Fill(kineticsDataSet.химические_вещества);
            реакцииTableAdapter1.Fill(kineticsDataSet.реакции);
            теплоёмкостьTableAdapter1.Fill(kineticsDataSet.теплоёмкость);
            энтальпияTableAdapter1.Fill(kineticsDataSet.энтальпия);
            транспортное_сечениеTableAdapter1.Fill(kineticsDataSet.транспортное_сечение);
            молекулаTableAdapter1.Fill(kineticsDataSet.молекула);
            сечение_реакцииTableAdapter1.Fill(kineticsDataSet.сечение_реакции);
            энергия_ГиббсаTableAdapter1.Fill(kineticsDataSet.энергия_Гиббса);
            атомTableAdapter1.Fill(kineticsDataSet.Атом);
            атомный_составTableAdapter1.Fill(kineticsDataSet.Атомный_состав);
            //using (var f = File.Create(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
            //    "\\Химическая кинетика.dat"))
            //    new BinaryFormatter().Serialize(f, kineticsDataSet);

            //using (var f = File.Open(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
            //    Path.DirectorySeparatorChar + "Химическая кинетика.dat", FileMode.Open, FileAccess.Read))
            //using (var ds = new BinaryFormatter().Deserialize(f) as KineticsDataSet)
            //{
            //    foreach (var row in ds.химические_вещества.Select())
            //        kineticsDataSet.химические_вещества.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.реакции.Select())
            //        kineticsDataSet.реакции.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.теплоёмкость.Select())
            //        kineticsDataSet.теплоёмкость.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.энтальпия.Select())
            //        kineticsDataSet.энтальпия.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.транспортное_сечение.Select())
            //        kineticsDataSet.транспортное_сечение.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.молекула.Select())
            //        kineticsDataSet.молекула.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.сечение_реакции.Select())
            //        kineticsDataSet.сечение_реакции.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.энергия_Гиббса.Select())
            //        kineticsDataSet.энергия_Гиббса.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.Атом.Select())
            //        kineticsDataSet.Атом.Rows.Add(row.ItemArray);
            //    foreach (var row in ds.Атомный_состав.Select())
            //        kineticsDataSet.Атомный_состав.Rows.Add(row.ItemArray);
            //}
            химическиеВеществаBindingSource.Sort = реакцииBindingSource.Sort = "Номер";


            foreach (var row in from s in kineticsDataSet.химические_вещества select s)
            {
                var row1 = kineticsDataSet.Состав.NewСоставRow();
                row1.Номер = kineticsDataSet.Состав.Rows.Count;
                row1.Вещество = row.Номер;
                row1.Доля = row1.Производная_доли = 0;
                kineticsDataSet.Состав.Rows.Add(row1);
            }

            foreach (var row in from s in kineticsDataSet.молекула select s)
                for (int l = 0; l < row.Уровни; l++)
                {
                    var row1 = kineticsDataSet.Колебательные_распределения.NewКолебательные_распределенияRow();
                    row1.Номер = kineticsDataSet.Колебательные_распределения.Rows.Count;
                    row1.Молекула = row.Молекула;
                    row1.Уровень = l;
                    row1.Доля = row1.Производная_доли = 0;
                    kineticsDataSet.Колебательные_распределения.Rows.Add(row1);
                }
            //foreach (var s in kineticsDataSet.реакции.Select("Номер in (2323,1482,1483,1484,1485,1486,1487,1488,1489,1490,1491,1492,1493," +
            //    "1505,1506,1890,1891,2044,2045,2046,2047,2048,2049,2050,2051,2052,2312,2313,2314)"))
            //    s.Delete();

            kineticsDataSet.реакции.AcceptChanges();

            Engine.Вещества = CreateВещества();
            Engine.Молекулы = CreateМолекулы();
            CreateRecombinationReactions();
            ReadNewReductionScheme();
            WriteEquations();
            Engine.Реакции = CreateРеакции();
            Engine.Initialize();

            double a, b, c, d;

            a = 79d;
            b = 21d;
            c = b / 5d;
            d = a + b + c;

            Engine.ɣ[Find("e") - 1] = 1e-17;
            Engine.ɣ[Find("N2+") - 1] = 1e-17;
            Engine.ɣ[Find("N2") - 1] = a / d;
            Engine.ɣ[Find("O2") - 1] = b / d;
            Engine.ɣ[Find("C3H8") - 1] = c / d;
            //Engine.ɣ[Find("N2") - 1] = a / d;
            //Engine.ɣ[Find("O2") - 1] = b / d;

            Engine.CalcData.CopyData();
            Engine.SetBolzmann();

            plotEEDF.NewCurveX.AddRange(Engine.РасчётФРЭЭ.ε);
            plMain.NewCurveX.Clear();
            reactionRates.Clear();
            vibrTemp.Clear();

            var cc = new Curve[Engine.ВеществаLength + FixedCurveCount - 1];

            for (int i = 0; i < Engine.ВеществаLength + FixedCurveCount - 1; i++)
                cc[i] = new Curve() { Color = Color.Blue, Width = 2, AvgList = plMain.CurveX };
            plMain.CurveY.AddRange(cc);

            OpenReductionScheme(Settings.Default.LastReductionScheme);
            ApplyReductionScheme(Settings.Default.ApplyReduction);

            FormatData();

            if (Settings.Default.AutoLoad)
            {
                string[] files = Directory.GetFiles(InputDatFolder);

                if (Settings.Default.AutoLoadIndex >= files.Length)
                {
                    Settings.Default.EnableAutomation = false;
                    Settings.Default.LastDataSet = string.Empty;
                }
                else
                {
                    int i = Settings.Default.AutoLoadInvert ? -1 : 1;
                    Array.Sort<string>(files, (x, y) => i * Math.Sign(GetDatFileTime(x) - GetDatFileTime(y)));
                    Settings.Default.LastDataSet = files[Settings.Default.AutoLoadIndex];
                }
            }

            if (!string.IsNullOrEmpty(Settings.Default.LastDataSet))
                LoadData(Settings.Default.LastDataSet);

            if (Settings.Default.EnableAutomation)
                qbStart.PerformClick();

            //ThreadPool.QueueUserWorkItem(state =>
            //    {
            //        Thread.Sleep(500);
            //        if (Settings.Default.EnableAutomation)
            //            if (new FormAsk().ShowDialog() == DialogResult.OK)
            //                qbStart.PerformClick();
            //    });

            //ThreadPool.QueueUserWorkItem((object state) =>
            //    {
            //        DateTime dt = DateTime.Now;
            //        for (int k = 0; k < 40; k++)
            //        {
            //            Engine.CalcEEDF();
            //            Engine.ResetEEDF();
            //        }
            //        TimeSpan t = DateTime.Now - dt;
            //        Engine.OnFormat();
            //        MessageBox.Show(t + "\r\n" + t.Seconds + "\r\n" + t.Milliseconds);
            //    });
        }

        private Молекула[] CreateМолекулы()
        {
            return (from m in kineticsDataSet.молекула
                    join a in Engine.Вещества on m.Молекула equals a.Номер
                    join b in Engine.Вещества on m.Атом equals b.Номер
                    select new Молекула()
                    {
                        D = m.Расстояние,
                        Вещество = Array.IndexOf(Engine.Вещества, a),
                        Атом = Array.IndexOf(Engine.Вещества, b),
                        ħω = m.ħω,
                        αħω = m.αħω,
                        ħωвр = m.ħω_вр,
                        Mred = m.Пр_масса,
                        P = m.P,
                        Q = m.Q,
                        θ = m.θ,
                        Уровни = m.Уровни,
                        Расстояние = m.Расстояние
                    }).ToArray<Молекула>();
        }

        /// <summary>
        /// Написать уравнение для каждой реакции
        /// </summary>
        void WriteEquations()
        {
            foreach (var s in from r in kineticsDataSet.реакции
                              join c1 in kineticsDataSet.химические_вещества on r.Комп_1 equals c1.Номер
                              join c2 in kineticsDataSet.химические_вещества on r["Комп 2"].IsNull(1) equals c2.Номер
                              join c3 in kineticsDataSet.химические_вещества on r["Комп 3"].IsNull(1) equals c3.Номер
                              join c4 in kineticsDataSet.химические_вещества on r["Продукт 1"].IsNull(1) equals c4.Номер
                              join c5 in kineticsDataSet.химические_вещества on r["Продукт 2"].IsNull(1) equals c5.Номер
                              join c6 in kineticsDataSet.химические_вещества on r["Продукт 3"].IsNull(1) equals c6.Номер
                              select r.Уравнение = r.IfIsNotNull("СпецФормула", "(Спец) ") + r.Коэф_комп_1.StoichKoef() + c1.Формула +
                              r.IfIsNotNull("Ур комп 1", "(v=" + r["Ур комп 1"] + ")") +
                              r.IfIsNotNull("Комп 2", " + " + r["Коэф комп 2"].StoichKoef() + c2.Формула +
                              r.IfIsNotNull("Ур комп 2", "(v=" + r["Ур комп 2"] + ")")) +
                              r.IfIsNotNull("Комп 3", " + " + r["Коэф комп 3"].StoichKoef() + c3.Формула) +
                              r.IfIsNotNull("Тип столкновения", " + e") + (r.М ? " + M" : null) + " " +
                              (r["Тип столкновения"] + "" == "4" ? "↔" : r["Тип столкновения"] is DBNull ? r["AM"] + "" == "0" ? "→" : "↔" : "→") + " " +
                              r.Коэф_прод_1.StoichKoef() + c4.Формула +
                              r.IfIsNotNull("Ур прод 1", "(v=" + r["Ур прод 1"] + ")") +
                              r.IfIsNotNull("Продукт 2", " + " + r["Коэф прод 2"].StoichKoef() + c5.Формула +
                              r.IfIsNotNull("Ур прод 2", "(v=" + r["Ур прод 2"] + ")")) +
                              r.IfIsNotNull("Продукт 3", " + " + r["Коэф прод 3"].StoichKoef() + c6.Формула) +
                              (r["Тип столкновения"] is DBNull ? null : r.Тип_столкновения == 3 ? null :
                              r.Тип_столкновения == 2 ? " + 2e" : " + e") + (r.М ? " + M" : null)) ;

        }

        Реакция[] CreateРеакции()
        {
            var реакции = (from c in kineticsDataSet.реакции
                           join c1 in Engine.Вещества on c.Комп_1 equals c1.Номер
                           join c2 in Engine.Вещества on c["Комп 2"].IsNull(1) equals c2.Номер
                           join c3 in Engine.Вещества on c["Комп 3"].IsNull(1) equals c3.Номер
                           join p1 in Engine.Вещества on c.Продукт_1 equals p1.Номер
                           join p2 in Engine.Вещества on c["Продукт 2"].IsNull(1) equals p2.Номер
                           join p3 in Engine.Вещества on c["Продукт 3"].IsNull(1) equals p3.Номер
                           select new Реакция()
                           {
                               Номер = c.Номер,
                               Комп_1 = Array.IndexOf<Вещество>(Engine.Вещества, c1),
                               Комп_2 = c["Комп 2"] is DBNull ? -1 : Array.IndexOf<Вещество>(Engine.Вещества, c2),
                               Комп_3 = c["Комп 3"] is DBNull ? -1 : Array.IndexOf<Вещество>(Engine.Вещества, c3),
                               Продукт_1 = Array.IndexOf<Вещество>(Engine.Вещества, p1),
                               Продукт_2 = c.IsПродукт_2Null() ? -1 :
                               Array.IndexOf<Вещество>(Engine.Вещества, p2),
                               Продукт_3 = c.IsПродукт_3Null() ? -1 :
                               Array.IndexOf<Вещество>(Engine.Вещества, p3),
                               Коэф_комп_1 = c.Коэф_комп_1,
                               Коэф_комп_2 = c["Коэф комп 2"].IsNull(0),
                               Коэф_комп_3 = c["Коэф комп 3"].IsNull(0),
                               Коэф_прод_1 = c.Коэф_прод_1,
                               Коэф_прод_2 = c["Коэф прод 2"].IsNull(0),
                               Коэф_прод_3 = c["Коэф прод 3"].IsNull(0),
                               Ур_комп_1 = c["Ур комп 1"].IsNull(-1),
                               Ур_комп_2 = c["Ур комп 2"].IsNull(-1),
                               Ур_прод_1 = c["Ур прод 1"].IsNull(-1),
                               Ур_прод_2 = c["Ур прод 2"].IsNull(-1),
                               HasP = !(c["AP"] is DBNull),
                               AP = c["AP"].IsNull(0d),
                               MP = c["MP"].IsNull(0d),
                               EP = c["EP"].IsNull(0d),
                               HasM = !(c["AM"] is DBNull),
                               AM = c["AM"].IsNull(0d),
                               MM = c["MM"].IsNull(0d),
                               EM = c["EM"].IsNull(0d),
                               М = c.М,
                               Флаг_Te = c.Флаг_Te,
                               Тип_столкновения = c["Тип столкновения"].IsNull(-1),
                               Порог = c["Порог"].IsNull(0),
                               СпецФормула = c["СпецФормула"].IsNull(-1)
                           }).ToArray<Реакция>();

            foreach (var v in реакции)
                v.Init();
            foreach (var v in from a in реакции
                              join b in kineticsDataSet.сечение_реакции on a.Номер equals b.Реакция
                              select new { a = a, s = new ТочкаСечения() { ε = b.ε, σ = b.σ } })
                v.a.НовоеСечение.NewStoredSection.Add(v.s);

            //foreach (var v in from a in реакции
            //                  join b in kineticsDataSet.реакции on a.Номер equals b.Номер
            //                  where a.Сечение != null
            //                  select new { a = a, b = b })
            //    if (v.a.Сечение.StoredSection.Count > 0)
            //        v.b.Уравнение = "до " + v.a.Сечение.StoredSection[v.a.Сечение.StoredSection.Count - 1].ε + " " + v.b.Уравнение;

            // Проверка баланса атомов и электронов
            foreach (var v in from r in реакции
                              where (r.AP != 0 || r.AM != 0 || r.Тип_столкновения != -1)
                              join rr in kineticsDataSet.реакции on r.Номер equals rr.Номер
                              join a in kineticsDataSet.Атомный_состав on rr.Комп_1 equals a.Вещество
                              select r.atomP[a.Атом - 1] += a.Количество * r.Коэф_комп_1) ;
            foreach (var v in from r in реакции
                              where r.Комп_2 != -1 && (r.AP != 0 || r.AM != 0 || r.Тип_столкновения != -1)
                              join rr in kineticsDataSet.реакции on r.Номер equals rr.Номер
                              join a in kineticsDataSet.Атомный_состав on rr.Комп_2 equals a.Вещество
                              select r.atomP[a.Атом - 1] += a.Количество * r.Коэф_комп_2) ;
            foreach (var v in from r in реакции
                              where r.Комп_3 != -1 && (r.AP != 0 || r.AM != 0 || r.Тип_столкновения != -1)
                              join rr in kineticsDataSet.реакции on r.Номер equals rr.Номер
                              join a in kineticsDataSet.Атомный_состав on rr.Комп_3 equals a.Вещество
                              select r.atomP[a.Атом - 1] += a.Количество * r.Коэф_комп_3) ;
            foreach (var v in from r in реакции
                              where (r.AP != 0 || r.AM != 0 || r.Тип_столкновения != -1)
                              join rr in kineticsDataSet.реакции on r.Номер equals rr.Номер
                              join a in kineticsDataSet.Атомный_состав on rr.Продукт_1 equals a.Вещество
                              select r.atomM[a.Атом - 1] += a.Количество * r.Коэф_прод_1) ;
            foreach (var v in from r in реакции
                              where r.Продукт_2 != -1 && (r.AP != 0 || r.AM != 0 || r.Тип_столкновения != -1)
                              join rr in kineticsDataSet.реакции on r.Номер equals rr.Номер
                              join a in kineticsDataSet.Атомный_состав on rr.Продукт_2 equals a.Вещество
                              select r.atomM[a.Атом - 1] += a.Количество * r.Коэф_прод_2) ;
            foreach (var v in from r in реакции
                              where r.Продукт_3 != -1 && (r.AP != 0 || r.AM != 0 || r.Тип_столкновения != -1)
                              join rr in kineticsDataSet.реакции on r.Номер equals rr.Номер
                              join a in kineticsDataSet.Атомный_состав on rr.Продукт_3 equals a.Вещество
                              select r.atomM[a.Атом - 1] += a.Количество * r.Коэф_прод_3) ;

            foreach (var v in from r in реакции
                              where r.Тип_столкновения == 2
                              select r.atomM[0]++) ;
            foreach (var v in from r in реакции
                              where r.Тип_столкновения == 3
                              select r.atomP[0]++) ;

            if (Settings.Default.KCReactions.Length > 0)
            {
                string[] reactions = Settings.Default.KCReactions.Split(',');

                if (Settings.Default.KCIndex >= 0 && Settings.Default.KCIndex < reactions.Length)
                {
                    int number;

                    if (int.TryParse(reactions[Settings.Default.KCIndex], out number))
                        foreach (var row in from a in kineticsDataSet.реакции
                                            join b in реакции on a.Номер equals b.Номер
                                            where a.Номер == number
                                            select new { a = a, b = b })
                        {
                            if (row.b.Сечение == null)
                            {
                                row.b.AP = Settings.Default.KCAlpha * row.a.AP;
                                row.b.AM = Settings.Default.KCAlpha * row.a.AM;
                            }
                            else
                                for (int i = 0; i < row.b.Сечение.StoredSection.Count; i++)
                                {
                                    var v = row.b.Сечение.StoredSection[i];

                                    v.σ = Settings.Default.KCAlpha * v.σ;
                                }

                            increaseKCIndex = true;
                            KCNumber = number;
                        }
                }
            }

            var ss = (from r in реакции where r.atomP.Sum() != r.atomM.Sum() select r.Номер.ToString()).ToArray<string>();

            if (ss.Length > 0)
            {
                Clipboard.SetText(string.Join("\r\n", ss));
                throw new Exception("Нарушение баланса атомов.\r\n" +
                    "Номера реакций (скопированы в буфер обмена):\r\n" + string.Join("\r\n", ss));
            }

            return реакции;
        }

        static int Find(string name)
        {
            return Array.Find(Engine.Вещества, c => c.Формула == name).Номер;
        }

        public static int[] Find(params string[] names)
        {
            var result = new int[names.Length];

            for (int i = 0; i < names.Length; i++)
                result[i] = Find(names[i]);

            return result;
        }

        public static int[] FindN(params string[] names)
        {
            var result = new int[names.Length];

            for (int i = 0; i < names.Length; i++)
                result[i] = Find(names[i].Replace("+", string.Empty).Replace("-", string.Empty).Replace("*", string.Empty));

            return result;
        }

        Вещество[] CreateВещества()
        {
            var вещества = new Вещество[kineticsDataSet.химические_вещества.Rows.Count];

            int i = 0;
            foreach (var v in from c in kineticsDataSet.химические_вещества select c)
                вещества[i++] = new Вещество()
                {
                    Номер = v.Номер,
                    Формула = v.Формула,
                    Hform = v["H"].IsNull(0),
                    M = (from a in kineticsDataSet.Атомный_состав
                         join b in kineticsDataSet.Атом on a.Атом equals b.Номер
                         where !(b["Вес"] is DBNull) && a.Вещество == v.Номер
                         select b.Вес * a.Количество).Sum(),
                    Z = (from a in kineticsDataSet.Атомный_состав
                         join b in kineticsDataSet.Атом on a.Атом equals b.Номер
                         where !(b["Заряд"] is DBNull) && a.Вещество == v.Номер
                         select b.Заряд * a.Количество).Sum()
                };

            foreach (var v in from a in вещества
                              join b in kineticsDataSet.теплоёмкость on a.Номер equals b.Вещество
                              select new { x = a, y = new double[] { b.cpa1, b.cpa2, b.cpa3, b.cpa4, b.cpa5, b.cpa6 } })
                v.x.SetCp(v.y);
            foreach (var v in from a in вещества
                              join b in kineticsDataSet.энтальпия on a.Номер equals b.Вещество
                              select new
                              {
                                  x = a,
                                  y = new double[] { b.H0, b._H_1, b.H1, b.H2, b.H3, b.H4, b.H5, b.H6, b.H7, b.H8 }
                              })
                v.x.SetH(v.y);
            foreach (var v in from a in вещества
                              join b in kineticsDataSet.энергия_Гиббса on a.Номер equals b.Вещество
                              select new
                              {
                                  x = a,
                                  y = new[] { b.a0, b.aln, b._a_2, b._a_1, b.a1, b.a2, b.a3 },
                                  z = b.H
                              })
            {
                v.x.SetGibbs(v.y);
                v.x.Hform0K = v.z;
            }

            foreach (var v in from a in kineticsDataSet.транспортное_сечение
                              join b in вещества on a.Вещество equals b.Номер
                              select new { x = b, sp = new ТочкаСечения() { ε = a.ε, σ = a.σ } })
                v.x.NewCrossSection.NewStoredSection.Add(v.sp);

            return вещества;
        }

        private void bnApply_Click(object sender, EventArgs e)
        {
            double d = kineticsDataSet.Состав.Sum<KineticsDataSet.СоставRow>(row => row.Доля);

            foreach (var v in kineticsDataSet.Состав)
                Engine.ɣ[v.Номер] = (v.Доля /= d);

            Engine.SetBolzmann();

            FormatData();
        }

        void rbEnablereduction_Click(object sender, EventArgs e)
        {
            ApplyReductionScheme(rbEnablereduction.Checked);
            Settings.Default.ApplyReduction = rbEnablereduction.Checked;
            rtbReductionReactionAmount.TextBoxText = Engine.ReductionReactionAmount.ToString();
        }

        private void ApplyReductionScheme(bool enable)
        {
            if (Engine.EnableReduction = enable)
            {
                foreach (var v in from v in kineticsDataSet.Состав
                                  join vv in Engine.Вещества on v.Вещество equals vv.Номер
                                  select vv.NewUserSignificant = v.Значимое) ;

                Engine.UserReduction();
            }
            else
                Engine.UndoReduction();
        }

        private void plotEEDF_MouseMove(object sender, MouseEventArgs e)
        {
            var focus = plotEEDF.FocusPosition;

            tslX.Text = focus.X.ToString();
            tslY.Text = focus.Y.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.WindowsShutDown && !dontAskOnClose)
                if (MessageBox.Show("Завершить работу?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    e.Cancel = true;
            base.OnFormClosing(e);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Working)
            {
                workThread.Abort();
                Engine.Release();
            }
            if (Settings.Default.EnableAutomation && (Engine.SignalState || exitAfterSave))
            {
                if (Engine.РасчётФРЭЭ.DC)
                    Settings.Default.tetta += Settings.Default.AutoCalcStep;
                else
                    Engine.РасчётФРЭЭ.Eω += Settings.Default.AutoCalcStep;
                if (Settings.Default.AutoLoad)
                    Settings.Default.AutoLoadIndex++;
                if (increaseKCIndex)
                    Settings.Default.KCIndex++;
            }
            UpdateSettings();

            Settings.Default.Save();

            if (Settings.Default.EnableAutomation && (Engine.SignalState || exitAfterSave))
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void UpdateSettings()
        {
            //Settings.Default.tetta = Engine.θ;
            Settings.Default.Ew = Engine.РасчётФРЭЭ.Eω;
            Settings.Default.DC = Engine.РасчётФРЭЭ.DC;
            Settings.Default.de = Engine.РасчётФРЭЭ.Δε;
            Settings.Default.eLength = Engine.РасчётФРЭЭ.εLength;
            Settings.Default.eCalcLength = Engine.РасчётФРЭЭ.εCalcLength;
            Settings.Default.eApprFrom = Engine.РасчётФРЭЭ.εApprFrom;
            Settings.Default.eApprTo = Engine.РасчётФРЭЭ.εApprTo;
            Settings.Default.ePrecision = Engine.РасчётФРЭЭ.εPrecision;
            Settings.Default.Precision = Engine.Precision;
            Settings.Default.EEDFrequency = Engine.EEDFrequency;
            Settings.Default.PlotFrequency = Engine.PlotFrequency;
            Settings.Default.FormatFrequency = Engine.FormatFrequency;
            Settings.Default.dtInit = Engine.dtInit;
            Settings.Default.ReductionFrequency = Engine.ReductionFrequency;
            Settings.Default.ReductionFrequency2 = Engine.ReductionFrequency2;
            Settings.Default.ReductionLimit = Engine.ReductionLimit;
            Settings.Default.MaxPrograms = Engine.MaxPrograms;
            Settings.Default.GammaMin = Engine.ɣMin;
            Settings.Default.C3H8GammaMin = Engine.C3H8ɣMin;
            Settings.Default.EGammaMax = Engine.EɣMax;
            Settings.Default.СriticalOscillationFactor = Engine.CriticalOscillationFactor;
            Settings.Default.LastDataSet = krbLastDataSet.TextBoxText;
            Settings.Default.AutoSaveDataSet = (int)Engine.AutoSaveDataSet;
            Settings.Default.AutoSaveDataSetInterval = Engine.AutoSaveDataSetInterval;
            Settings.Default.AutoLoadInvert = bsaAutoLoadInvert.Checked;
            Settings.Default.KCReactions = krbKCReactions.TextBoxText;
            Settings.Default.AreaSize = Engine.AreaSize;
        }

        private void bnExcel_Click(object sender, EventArgs e)
        {
            OnExcel();
        }

        const int FixedCurveCount = 10;

        void OnExcel()
        {
            var oldCI = Thread.CurrentThread.CurrentCulture;
            int n1 = 0;
            double tIgn = 0, tIgn1 = 0, tIgn2 = 0, gC3H8, dgdtC3H8 = 0;

            try
            {
                gC3H8 = 0.5 * tIgn_O2[0];

                for (int i = 1; i < tIgn_t.Count; i++)
                {
                    var dgdt = -(tIgn_O2[i] - tIgn_O2[i - 1]) / (tIgn_t[i] - tIgn_t[i - 1]);

                    if (dgdt > dgdtC3H8)
                    {
                        dgdtC3H8 = dgdt;
                        n1 = i;
                    }
                }

                tIgn = tIgn_t[n1];

                for (int i = 0; i < tIgn_t.Count; i++)
                    if (tIgn_O2[i] < gC3H8)
                    {
                        tIgn1 = tIgn_t[i];
                        break;
                    }

                tIgn2 = tIgn_t[tIgn_dTdt.LastIndexOf(tIgn_dTdt.Max())];
            }
            catch
            { }

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var excel = new Microsoft.Office.Interop.Excel.Application();
            Worksheet ws;

            excel.Workbooks.Add(Type.Missing);
            excel.Workbooks[1].Title = TextExtra;
            ws = (Worksheet)excel.Workbooks[1].Worksheets[1];

            ws.get_Range("A1", "A1").Value = "Время, с";
            ws.get_Range("B1", "B1").Value = "Температура, К";
            ws.get_Range("C1", "C1").Value = "Скорость изменения температуры, К/с";
            ws.get_Range("D1", "D1").Value = "Энерговклад, Вт/моль";
            ws.get_Range("E1", "E1").Value = "Полная энтальпия системы при T = 298K, Дж/моль";
            ws.get_Range("F1", "F1").Value = "Полная энтальпия системы, Дж/моль";
            ws.get_Range("G1", "G1").Value = "Теплоёмкость при постоянном давлении, Дж/моль*K";
            ws.get_Range("H1", "H1").Value = "Приведённое поле в холодном газе, Td";
            ws.get_Range("I1", "I1").Value = "Температура электронов, эВ";
            ws.get_Range("J1", "J1").Value = "Концентрация, моль/см3";

            double[,] a = new double[plMain.CurveX.Count, Engine.ВеществаLength + FixedCurveCount];

            for (int j = 0; j < plMain.CurveX.Count; j++)
                a[j, 0] = plMain.CurveX[j];
            for (int k = 0; k < FixedCurveCount - 1; k++)
                for (int j = 0; j < plMain.CurveX.Count; j++)
                    a[j, k + 1] = plMain.CurveY[k][j];

            for (int i = 0; i < Engine.ВеществаLength; i++)
            {
                ((Range)ws.Cells[1, i + FixedCurveCount + 1]).Value = Engine.Вещества[i].Формула;

                for (int j = 0; j < plMain.CurveX.Count; j++)
                    a[j, i + FixedCurveCount] = plMain.CurveY[i + FixedCurveCount - 1][j];
            }

            ws.get_Range("A2", ((Range)ws.Cells[1, Engine.ВеществаLength + FixedCurveCount]).get_Address(Type.Missing, Type.Missing,
                XlReferenceStyle.xlA1, Type.Missing, Type.Missing).Split('$')[1] + (plMain.CurveX.Count + 1)).Value = a;

            ws = (Worksheet)excel.Workbooks[1].Worksheets[2];

            string[,] ss = new string[Engine.Реакции.Length + 1, 6];
            int[,] ii = new int[Engine.Реакции.Length, 1];
            double[,] dd = new double[Engine.Реакции.Length, 2];

            ss[0, 0] = "Номер";
            ss[0, 1] = "Сечение до";
            ss[0, 2] = "Расход";
            ss[0, 3] = "Реакция \\ Время, с";
            for (int i = 0; i < Engine.Реакции.Length; i++)
            {
                var r1 = Engine.Реакции[i];
                var r2 = (from x in kineticsDataSet.реакции where x.Номер == r1.Номер select x).ToArray()[0];

                ii[i, 0] = r2.Номер;
                if (r1.Сечение != null)
                    dd[i, 0] = r1.Сечение.StoredSection[r1.Сечение.StoredSection.Count - 1].ε;
                ss[i + 1, 3] = ' ' + r2.Уравнение.Substring(0, r2.Уравнение.IndexOf(" = ") + 1);
                ss[i + 1, 4] = r1.Тип_столкновения == 4 ? "↔" : r1.Тип_столкновения == -1 ? r1.HasM && r1.AM == 0 ? "→" : "↔" : "→";
                ss[i + 1, 5] = r2.Уравнение.Substring(r2.Уравнение.IndexOf(" = ") + 2) + ' ';
            }
            ws.get_Range("A1", "F" + (Engine.Реакции.Length + 1)).Value = ss;
            ws.get_Range("A2", "A" + (Engine.Реакции.Length + 1)).Value = ii;

            a = new double[Engine.Реакции.Length + 1, reactionRates.Count];
            for (int i = 0; i <= Engine.Реакции.Length; i++)
                for (int j = 0; j < reactionRates.Count; j++)
                    a[i, j] = reactionRates[j][i];

            for (int i = 0; i < Engine.Реакции.Length; i++)
                for (int j = 1; j < reactionRates.Count; j++)
                    dd[i, 1] += (a[i + 1, j] + a[i + 1, j - 1]) * (a[0, j] - a[0, j - 1]) * 0.5;

            ws.get_Range("B2", "C" + (Engine.Реакции.Length + 1)).Value = dd;
            ws.get_Range("G1", ((Range)ws.Cells[1, reactionRates.Count + 6]).get_Address(Type.Missing, Type.Missing,
                XlReferenceStyle.xlA1, Type.Missing, Type.Missing).Split('$')[1] + (Engine.Реакции.Length + 1)).Value = a;

            ws = (Worksheet)excel.Workbooks[1].Worksheets[3];

            ss = new string[Engine.Молекулы.Length + 1, 1];

            ss[0, 0] = "Время, с";
            for (int i = 0; i < Engine.Молекулы.Length; i++)
                ss[i + 1, 0] = (from x in kineticsDataSet.химические_вещества
                                where x.Номер == Engine.Вещества[Engine.Молекулы[i].Вещество].Номер
                                select x.Формула).ToArray<string>()[0];
            ws.get_Range("A1", "A" + (Engine.Молекулы.Length + 1)).Value = ss;

            a = new double[Engine.Молекулы.Length + 1, vibrTemp.Count];
            for (int i = 0; i <= Engine.Молекулы.Length; i++)
                for (int j = 0; j < vibrTemp.Count; j++)
                    a[i, j] = vibrTemp[j][i];
            ws.get_Range("B1", ((Range)ws.Cells[1, vibrTemp.Count + 1]).get_Address(Type.Missing, Type.Missing,
                XlReferenceStyle.xlA1, Type.Missing, Type.Missing).Split('$')[1] + (Engine.Молекулы.Length + 1)).Value = a;

            ws = (Worksheet)excel.Workbooks[1].Worksheets.Add(Type.Missing, excel.Workbooks[1].Worksheets[3], 1, Type.Missing);

            UpdateSettings();

            var aa = new string[Settings.Default.PropertyValues.Count + 1, 2];
            int i1 = 0;

            foreach (System.Configuration.SettingsPropertyValue p in Settings.Default.PropertyValues)
            {
                aa[i1, 0] = p.Name;
                aa[i1, 1] = p.PropertyValue.ToString();
                i1++;
            }
            aa[Settings.Default.PropertyValues.Count, 0] = "Сборка";
            aa[Settings.Default.PropertyValues.Count, 1] = System.Reflection.Assembly.GetExecutingAssembly().FullName;
            ws.get_Range("A1", "B" + (Settings.Default.PropertyValues.Count + 1)).Value = aa;

            string fileName = ExcelFolder + Path.DirectorySeparatorChar + TextExtra +
                (Engine.РасчётФРЭЭ.DC ? "E_N = " + Engine.θ0 + " Td" : "E_ω = " + Engine.РасчётФРЭЭ.Eω + " В_см*ГГц") +
                (string.IsNullOrEmpty(Settings.Default.KCReactions) ?
                ", tIgnC3H8=" + tIgn.ToString("0.########E-0").Replace(".", ",") + " (" + tIgn1.ToString("0.########E-0").Replace(".", ",") + ") " :
                ", Номер=" + KCNumber +
                ", αC3H8=" + (Math.Log(tIgn / Settings.Default.KCT1) / Math.Log(Settings.Default.KCAlpha)).ToString("0.########E-0").Replace(".", ",") + " (" +
                (Math.Log(tIgn1 / Settings.Default.KCT2) / Math.Log(Settings.Default.KCAlpha)).ToString("0.########E-0").Replace(".", ",") + ") ") +
                (tIgn2 > 0 && tIgn2 < tIgn1 ? ", tIgndTdt=" + tIgn2.ToString("0.########E-0").Replace(".", ",") : null), fileName2 = fileName;
            for (int i = 0; File.Exists(fileName2 + ".xls") || File.Exists(fileName2 + ".xlsx"); i++, fileName2 = fileName + " (" + i + ")") ;

            excel.Workbooks[1].SaveAs(fileName2, excel.Workbooks[1].FileFormat, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            //excel.ScreenUpdating = excel.Visible = excel.UserControl = true;
            excel.Workbooks[1].Close(false, Type.Missing, Type.Missing);
            excel.Quit();

            Thread.CurrentThread.CurrentCulture = oldCI;
        }

        void FromExcel(string fileName)
        {
            var excel = new Microsoft.Office.Interop.Excel.Application();
            Worksheet ws;

            excel.Workbooks.Open(fileName, Type.Missing, true, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing);

            ws = (Worksheet)excel.Workbooks[1].Worksheets[1];

            var bb = ws.UsedRange.Value as object[,];

            for (int i = 2; i < bb.GetLength(0); i++)
            {
                double[] aa = new double[Engine.ВеществаLength + 6];

                for (int j = 1; j < bb.GetLength(1); j++)
                    aa[j - 1] = Convert.ToDouble(bb[i, j]);
                plMain.Add(aa);
            }

            ws = (Worksheet)excel.Workbooks[1].Worksheets[2];

            var a = ws.UsedRange.Value as object[,];
            var ind = new int[a.GetLength(0)];

            ind[1] = -1;
            for (int i = 2; i < a.GetLength(0); i++)
                ind[i] = Array.FindIndex<Реакция>(Engine.Реакции, x => x.Номер == Convert.ToInt32(a[i, 1]));

            for (int j = 7; j < a.GetLength(1); j++)
            {
                var b = new double[Engine.Реакции.Length + 1];

                b[0] = Convert.ToDouble(a[2, j]);
                for (int i = 1; i < a.GetLength(0); i++)
                    //						if (ind[i] != -1)
                    b[ind[i] + 1] = Convert.ToDouble(a[i, j]);

                reactionRates.Add(b);
            }

            ws = (Worksheet)excel.Workbooks[1].Worksheets[3];



            excel.Workbooks[1].Close(false, Type.Missing, Type.Missing);
            excel.Quit();
        }

        private void rbOpenReduction_Click(object sender, EventArgs e)
        {
            var d = new OpenFileDialog()
            {
                InitialDirectory = Environment.CurrentDirectory + "\\Данные",
                Filter = "Схемы|*.xml"
            };

            if (d.ShowDialog() == DialogResult.OK)
            {
                OpenReductionScheme(d.FileName);

                Settings.Default.LastReductionScheme = d.FileName;
            }
        }

        private void OpenReductionScheme(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                foreach (var v in from a in Engine.Вещества
                                  join b in kineticsDataSet.Состав on a.Номер equals b.Вещество
                                  select a.NewUserSignificant = b.Значимое = true) ;
            else
            {
                var doc = new XmlDocument();

                doc.Load(fileName);

                var e1 = doc.SelectSingleNode("Вещества");

                foreach (var v in from a in Engine.Вещества
                                  join b in kineticsDataSet.Состав on a.Номер equals b.Вещество
                                  join c in e1.SelectNodes("Вещество").OfType<XmlNode>()
                                  on a.Формула equals c.Attributes["Формула"].Value
                                  select a.NewUserSignificant = b.Значимое = c.Attributes["Значимое"].Value == "Да") ;
            }
        }

        private void rbSaveReduction_Click(object sender, EventArgs e)
        {
            var d = new SaveFileDialog()
            {
                InitialDirectory = Environment.CurrentDirectory + "\\Данные",
                Filter = "Схемы|*.xml"
            };

            if (d.ShowDialog() == DialogResult.OK)
            {
                var doc = new XmlDocument();
                var e1 = doc.CreateElement("Вещества");

                doc.AppendChild(e1);

                foreach (var v in Engine.Вещества)
                {
                    var a = doc.CreateAttribute("Формула");
                    var b = doc.CreateAttribute("Значимое");
                    var ee = doc.CreateElement("Вещество");

                    a.Value = v.Формула;
                    b.Value = v.NewUserSignificant ? "Да" : "Нет";

                    ee.Attributes.Append(a);
                    ee.Attributes.Append(b);
                    e1.AppendChild(ee);
                }

                doc.Save(d.FileName);
            }
        }

        private void krbSaveData_Click(object sender, EventArgs e)
        {
            using (var d = new SaveFileDialog() { InitialDirectory = OutputDatFolder, Filter = "Файлы данных|*.dat" })
                if (d.ShowDialog(this) == DialogResult.OK)
                    SaveData(d.FileName);
        }

        private void LoadData(string fileName)
        {
            if (File.Exists(fileName))
                if (Path.GetExtension(fileName).ToLower().StartsWith("xls"))
                    FromExcel(fileName);
                else
                    using (var f = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        var obj = new BinaryFormatter().Deserialize(f);
                        var data = obj as SavedData;

                        if (data == null)
                            Engine.CalcData.ImportData(obj);
                        else
                        {
                            plMain.CurveX = data.curveX;
                            plMain.CurveY = data.curveY;
                            reactionRates = data.reactionRates;
                            vibrTemp = data.vibrTemp;
                            Engine.CalcData.ImportData(data.engineData);
                        }
                    }
        }

        private void SaveData(string fileName)
        {
            using (var f = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                try
                {
                    new BinaryFormatter().Serialize(f, new SavedData()
                    {
                        curveX = plMain.CurveX,
                        curveY = plMain.CurveY,
                        reactionRates = reactionRates,
                        vibrTemp = vibrTemp,
                        engineData = Engine.CalcData.ExportData()
                    });
                }
                catch
                { }
        }

        private void krbLoadData_Click(object sender, EventArgs e)
        {
            using (var d = new OpenFileDialog() { InitialDirectory = InputDatFolder, Filter = "Файлы данных|*.dat" })
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData(d.FileName);
                    krbLastDataSet.TextBoxText = d.FileName;
                }
        }

        //private void krbCalcEEDF_Click(object sender, EventArgs e)
        //{
        //    if (!Working && !WorkingEEDF)
        //    {
        //        WorkingEEDF = true;
        //        ThreadPool.QueueUserWorkItem(state => Engine.CalcEedfOnly());
        //    }
        //}
        private void krbCalcEEDF_Click(object sender, EventArgs e)
        {
            if (!Working && !WorkingEEDF)
            {
                WorkingEEDF = true;
                //ThreadPool.QueueUserWorkItem(state => Engine.CalcEedfOnly());
                var l = new List<int>();
                //var d1 = new double[] { 15, 20, 30, 40, 50, 60, 70, 80, 90, 100, 120, 140, 160, 180, 200 };
                var d1 = new double[] { 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 220, 240, 260, 280, 300, 320, 340,
                    360, 380, 400, 420, 440, 460, 480, 500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900,
                    2000, 2200, 2400, 2600, 2800, 3000, 3200, 3400, 3600, 3800, 4000, 4200, 4400, 4600, 4800, 5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000,
                    9500, 10000};
                var d2 = new double[] { 300, 3000, 5000, 7000, 9000 };
                l.AddRange(new int[] { 2644, 2645, 2646, 2647 });

                ThreadPool.QueueUserWorkItem(state =>
                {
                    var d = Engine.CalcConstArray(d1, d2, (from a in Engine.Реакции where l.Contains(a.Номер) select Array.IndexOf(Engine.Реакции, a)).ToArray());

                    var oldCI = Thread.CurrentThread.CurrentCulture;

                    Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                    var excel = new Microsoft.Office.Interop.Excel.Application();
                    Worksheet ws;

                    excel.Workbooks.Add(Type.Missing);
                    excel.Workbooks[1].Title = TextExtra;
                    ws = (Worksheet)excel.Workbooks[1].Worksheets[1];
                    ws.get_Range("A1", Type.Missing).Value = "E/N, Td";
                    ws.get_Range("B1", Type.Missing).Value = "Tv, K";
                    ws.get_Range("C1", Type.Missing).Value = "Te, эВ";
                    ws.get_Range("D1", Type.Missing).Value = "0,2";
                    ws.get_Range("E1", Type.Missing).Value = "2,5";
                    ws.get_Range("F1", Type.Missing).Value = "0,0";
                    ws.get_Range("G1", Type.Missing).Value = "0,0'";
                    ws.get_Range("A2", ((Range)ws.Cells[1, d.GetLength(1)]).get_Address(Type.Missing, Type.Missing,
                        XlReferenceStyle.xlA1, Type.Missing, Type.Missing).Split('$')[1] + (d.GetLength(0) + 1)).Value = d;

                    excel.Workbooks[1].SaveAs(ExcelFolder + Path.DirectorySeparatorChar + "test", excel.Workbooks[1].FileFormat, Type.Missing, Type.Missing,
                        Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing,
                        Type.Missing);
                    //excel.ScreenUpdating = excel.Visible = excel.UserControl = true;
                    excel.Workbooks[1].Close(false, Type.Missing, Type.Missing);
                    excel.Quit();

                    Thread.CurrentThread.CurrentCulture = oldCI;
                });
            }//250,300,350,400,500,600,700,800,900,1000,1500,2000,2500,
        }

        private void krbExportEEDF_Click(object sender, EventArgs e)
        {
            var oldCI = Thread.CurrentThread.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var excel = new Microsoft.Office.Interop.Excel.Application();
            Worksheet ws;

            excel.Workbooks.Add(Type.Missing);
            excel.Workbooks[1].Title = TextExtra;
            ws = (Worksheet)excel.Workbooks[1].Worksheets[1];

            ws.get_Range("A1", "A1").Value = "Энергия, эВ";
            ws.get_Range("B1", "B1").Value = "ФРЭЭ, эВ^-3/2";

            double[,] a = new double[Engine.РасчётФРЭЭ.εLength - 1, 2];

            for (int j = 0; j < Engine.РасчётФРЭЭ.εLength - 1; j++)
            {
                a[j, 0] = Engine.РасчётФРЭЭ.ε[j];
                a[j, 1] = Engine.CalcData.ФРЭЭ[j];
            }

            ws.get_Range("A2", "B" + (Engine.РасчётФРЭЭ.εLength)).Value = a;

            string fileName = ExcelFolder + Path.DirectorySeparatorChar + TextExtra +
                "EEDF, " + (Engine.РасчётФРЭЭ.DC ? "E_N = " + Engine.θ + " Td" : "E_ω = " + Engine.РасчётФРЭЭ.Eω + " В_см*ГГц") +
                ", t=" + Engine.t.ToString("0.########E-0").Replace(".", ","), fileName2 = fileName;
            for (int i = 0; File.Exists(fileName2 + ".xls") || File.Exists(fileName2 + ".xlsx"); i++, fileName2 = fileName + " (" + i + ")") ;

            excel.Workbooks[1].SaveAs(fileName2, excel.Workbooks[1].FileFormat, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            //excel.ScreenUpdating = excel.Visible = excel.UserControl = true;
            excel.Workbooks[1].Close(false, Type.Missing, Type.Missing);
            excel.Quit();

            Thread.CurrentThread.CurrentCulture = oldCI;
        }

        private void rgbEnableAutomation_Click(object sender, EventArgs e)
        {
            Settings.Default.EnableAutomation = rgbEnableAutomation.Checked;
        }


        private void bnPlotImport_Click(object sender, EventArgs e)
        {
            var d = new OpenFileDialog()
            {
                InitialDirectory = ExcelFolder,
                Filter = "Книги Excel|*.xlsx|Книги Excel 97-2003|*.xls"
            };

            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                FromExcel(d.FileName);
        }


        class ReactionRate
        {
            public Реакция Реакция { get; private set; }
            public double[] Скорость { get; private set; }
            public double СредняяСкорость { get; private set; }
            public Dictionary<int, List<double>> ЗначимыеВещества { get; private set; }

            public void AddSpec(int spec, double gamma)
            {
                if (!ЗначимыеВещества.ContainsKey(spec))
                    ЗначимыеВещества.Add(spec, new List<double>());

                ЗначимыеВещества[spec].Add(gamma);
            }

            public ReactionRate(int index)
            {
                ЗначимыеВещества = new Dictionary<int, List<double>>();
                Реакция = Engine.Реакции[index];

                Скорость = new double[reactionRates.Count];

                for (int j = 0; j < reactionRates.Count; j++)
                    Скорость[j] = reactionRates[j][index + 1];
                for (int j = 1; j < reactionRates.Count; j++)
                    СредняяСкорость += (reactionRates[j][index + 1] + reactionRates[j - 1][index + 1]) * (reactionRates[j][0] - reactionRates[j - 1][0]) * 0.5;
            }
        }

        private void krbReductionSchemeSave_Click(object sender, EventArgs e)
        {
            var specAll = new List<int>();
            var spec = new List<int>();
            var dd = new ReactionRate[Engine.Реакции.Length];
            int j0 = 0, j1 = reactionRates.Count - 1;

            for (int i = 0; i < Engine.Реакции.Length; i++)
                dd[i] = new ReactionRate(i);

            while (reactionRates[j0][0] < 1e-8)
                j0++;
            //while (plMain.CurveY[Engine.Ne + 4][j0] < 1e-11)
            //    j0++;
            //while (plMain.CurveY[Engine.Ne + 4][j1] > 1e-11)
            //    j1--;
            //while (reactionRates[j1][0] > 5.5e-4)
            //    j1--;

            //for (int i = 0; i < Engine.Вещества.Length; i++)
            //    if (Engine.ɣ[i] > 0)
            //        spec.Add(i);
            spec.Add(Engine.ВеществаСпец.IndC3H8);
            spec.Add(Engine.Ne);

            while (spec.Count > 0)
            {
                var s = spec.ToArray();

                specAll.AddRange(spec);
                spec.Clear();

                foreach (int num in s)
                    if (num == Engine.Ne)
                        for (int j = j0; j < j1; j++)
                        {
                            var r = (from a in dd
                                     where a.Реакция.Вещества.ContainsKey(num) && a.Скорость[j] != 0
                                     orderby Math.Abs(a.Скорость[j] * a.Реакция.Вещества[num]) descending
                                     select a).ToArray<ReactionRate>();

                            double sum = 0, sum1 = r.Sum(x => x.Скорость[j] * x.Реакция.Вещества[num]),
                            sum2 = sum1 * Settings.Default.ReductionFactor, sum3 = sum1 * (2 - Settings.Default.ReductionFactor);
                            double absSum = 0, absSum1 = r.Sum(x => Math.Abs(x.Скорость[j] * x.Реакция.Вещества[num])) * Settings.Default.ReductionFactor;


                            for (int i = 0; i < r.Length && absSum < absSum1 && (sum < sum2 || sum > sum3); i++)
                            {
                                var reaction = r[i].Реакция;

                                sum += r[i].Скорость[j] * r[i].Реакция.Вещества[num];
                                absSum += Math.Abs(r[i].Скорость[j] * r[i].Реакция.Вещества[num]);

                                r[i].AddSpec(reaction.Комп_1, plMain.CurveY[reaction.Комп_1 + 4][j]);
                                if (reaction.Комп_2 != -1)
                                    r[i].AddSpec(reaction.Комп_2, plMain.CurveY[reaction.Комп_2 + 4][j]);
                                if (reaction.Комп_3 != -1)
                                    r[i].AddSpec(reaction.Комп_3, plMain.CurveY[reaction.Комп_3 + 4][j]);

                                foreach (var n in r[i].Реакция.Вещества)
                                    if (!specAll.Contains(n.Key))
                                        spec.AddUnique(n.Key);
                            }
                        }
                    else
                    {
                        var r = (from a in dd
                                 where a.Реакция.Вещества.ContainsKey(num) && a.СредняяСкорость != 0
                                 orderby Math.Abs(a.СредняяСкорость * a.Реакция.Вещества[num]) descending
                                 select a).ToArray<ReactionRate>();

                        double sum = 0, sum1 = r.Sum(x => x.СредняяСкорость * x.Реакция.Вещества[num]),
                            sum2 = sum1 * Settings.Default.ReductionFactor, sum3 = sum1 * (2 - Settings.Default.ReductionFactor);
                        double absSum = 0, absSum1 = r.Sum(x => Math.Abs(x.СредняяСкорость * x.Реакция.Вещества[num])) * Settings.Default.ReductionFactor;

                        for (int i = 0; i < r.Length && absSum < absSum1 && (sum < sum2 || sum > sum3); i++)
                        {
                            sum += r[i].СредняяСкорость * r[i].Реакция.Вещества[num];
                            absSum += Math.Abs(r[i].СредняяСкорость * r[i].Реакция.Вещества[num]);
                            r[i].AddSpec(num, plMain.CurveY[num + 4][0]);

                            foreach (var n in r[i].Реакция.Вещества)
                                if (!specAll.Contains(n.Key))
                                    spec.AddUnique(n.Key);
                        }
                    }
            }

            specAll.Sort();

            var d = new XmlDocument();
            var e1 = d.CreateElement("Схема");
            var e2 = d.CreateElement("Вещества");
            var e3 = d.CreateElement("Реакции");

            d.AppendChild(e1);
            e1.AppendChild(e2);
            e1.AppendChild(e3);

            foreach (int i in specAll)
            {
                var v = Engine.Вещества[i];
                var eS = d.CreateElement("Вещество");
                var num = d.CreateAttribute("Номер");
                var name = d.CreateAttribute("Формула");

                num.Value = v.Номер.ToString();
                name.Value = v.Формула;

                eS.Attributes.Append(num);
                eS.Attributes.Append(name);
                e2.AppendChild(eS);
            }

            var reac = from i in dd where i.ЗначимыеВещества.Count > 0 orderby i.Реакция.Номер select i;
            var gammeMin = new double[Engine.ВеществаLength];
            var gammeMax = new double[Engine.ВеществаLength];

            for (int i = 0; i < Engine.ВеществаLength; i++)
            {
                gammeMin[i] = 1;
                gammeMax[i] = 0;

                for (int j = j0; j < j1; j++)
                {
                    double ddd = plMain.CurveY[i + 4][j];

                    if (gammeMin[i] > ddd)
                        gammeMin[i] = ddd;
                    if (gammeMax[i] < ddd)
                        gammeMax[i] = ddd;
                }
            }

            foreach (var v in reac)
            {
                var eR = d.CreateElement("Реакция");
                var num = d.CreateAttribute("Номер");
                var name = d.CreateAttribute("Уравнение");

                num.Value = v.Реакция.Номер.ToString();
                name.Value = kineticsDataSet.реакции.Select("Номер=" + v.Реакция.Номер)[0]["Уравнение"].ToString();

                eR.Attributes.Append(num);
                eR.Attributes.Append(name);
                e3.AppendChild(eR);

                //foreach (var item in from a in v.ЗначимыеВещества orderby a.Key select a)
                //{
                //    var eV = d.CreateElement("ЗначимоеВещество");
                //    var num1 = d.CreateAttribute("Номер");
                //    var name1 = d.CreateAttribute("формула");
                //    var sp = Engine.Вещества[item.Key];
                //    double dMin = item.Value.Min(), dMax = item.Value.Max();

                //    num1.Value = sp.Номер.ToString();
                //    name1.Value = sp.Формула;

                //    eV.Attributes.Append(num1);
                //    eV.Attributes.Append(name1);

                //    if (dMin > gammeMin[item.Key])
                //    {
                //        var min = d.CreateAttribute("МинДоля");

                //        min.Value = dMin.ToString("0.########E-0");
                //        eV.Attributes.Append(min);
                //    }
                //    if (dMax < gammeMax[item.Key])
                //    {
                //        var max = d.CreateAttribute("МаксДоля");

                //        max.Value = dMax.ToString("0.########E-0");
                //        eV.Attributes.Append(max);
                //    }

                //    eR.AppendChild(eV);
                //}
            }

            var dlg = new SaveFileDialog()
            {
                InitialDirectory = SchemeFolder,
                Filter = "Схемы|*.xml",
                FileName = "РКС, фактор - " + Settings.Default.ReductionFactor + ", " + specAll.Count + " веществ, " + reac.Count() + " реакций.xml"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
                d.Save(dlg.FileName);
        }

        private void krbReductionSchemeLoad_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                InitialDirectory = SchemeFolder,
                Filter = "Схемы|*.xml"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.ReductionFile = dlg.FileName;
                krtbReductionSchemeFile.TextBoxText = Settings.Default.ReductionFile;
            }
        }


        [Serializable]
        class SavedData
        {
            public AvgList curveX;
            public List<Curve> curveY;
            public List<double[]> reactionRates;
            public List<double[]> vibrTemp;
            public object engineData;
        }
    }
}