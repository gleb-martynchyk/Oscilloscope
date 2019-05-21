using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Content;
using MeterFramework.AlmaMeter;
using BECSLibrary.Transport;

using OxyPlot.Xamarin.Android;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;


namespace OscilloscopeAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainView : AppCompatActivity
    {
        PlotView view;
        B382Meter Device;

        private readonly int DataSize = 30;

        TCPIPTransport _Transport = new TCPIPTransport();
        public const int ChannelCount = B382Meter.ChannelCount;

        #region <Настройки>
        public string IP;
        public bool[] activeChannel;
        public bool[] ChannelRange;
        public double SamplingPeriod;
        public ushort[] UInt16Buffer = new ushort[0];
        #endregion <Настройки>

        #region<Axis scale parametrs>
        float x_scale = 1;
        float y_scale = 1;
        #endregion<Axis scale parametrs>

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            try
            {
                //if settings changes
                TakeSettings();
            }
            catch
            {
                ResetSettings();
            }


            SetContentView(Resource.Layout.activity_main);

            Switch enable = FindViewById<Switch>(Resource.Id.switch1);

            view = FindViewById<PlotView>(Resource.Id.plot_view);


            // INIT
            _Transport.IPAddress = IPAddress.Parse(IP);
            _Transport.Port = 0x6871;
            _Transport.Timeout = 500;

            enable.CheckedChange += EnabelApplication;

            Button buttonAxisX_inc = FindViewById<Button>(Resource.Id.button_x_dec);
            Button buttonAxisX_dec = FindViewById<Button>(Resource.Id.button_x_inc);
            Button buttonSettings = FindViewById<Button>(Resource.Id.button2);
            buttonAxisX_inc.Click += AxisX_increment;
            buttonAxisX_dec.Click += AxisX_decrease;
            buttonSettings.Click += ButtinSettings_Click;
        }



        private void ButtinSettings_Click(object sender, System.EventArgs e)
        {
            var intent = new Intent(this, typeof(Settings));
            intent.PutExtra("IP", IP);
            intent.PutExtra("ActiveChannel", activeChannel);
            intent.PutExtra("ChannelRange", ChannelRange);
            intent.PutExtra("SamplingPeriod", SamplingPeriod);
            StartActivity(intent);
        }

        bool enabled = false;
        private void EnabelApplication(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (enabled == false)
            {
                enabled = true;
                Main();
            }
            else
            {
                _Transport.Disconnect();
                enabled = false;
            }
        }

        public async Task Main()
        {

            try
            {
                //_Transport.Connect();
            }
            catch (Exception exc)
            {
                Toast.MakeText(ApplicationContext, "Нет соединения" + exc.ToString(), ToastLength.Long).Show();
                throw new Exception();
            }

            try
            {
                float[][] DataBuffer = new float[][] { new float[DataSize], new float[DataSize], new float[DataSize], new float[DataSize] };
                Device = new B382Meter(_Transport);
                Device.ClearProtocol();
                Device.Reset(true);
                Device.SetMeasMode(false, true);
                //Device.ReadCalibrations();
                //var res = Device.GetIDs();

                //Вывод
                //Number1.Text = (res.Serial).ToString() + "|" + (res.FPGAVersion).ToString() + "|" + (res.TypeID).ToString();
                //String outText = (res.Serial).ToString() + "|" + (res.FPGAVersion).ToString() + "|" + (res.TypeID).ToString();
                //Toast.MakeText(ApplicationContext, outText, ToastLength.Long).Show();
                ApplySettings(Device);


                while (true)
                {
                    //----------- Основные методы
                    //Start(Device);
                    //GetDataStatus(Device);
                    //DataBuffer = ReadData(Device);
                    //ShowData(DataBuffer);
                    view.Model = CreatePlotModel();
                    UpdatePlot(view.Model, DataBuffer);

                    await Task.Delay(33);

                    if (enabled == false)
                        break;
                }
            }
            catch
            {
                Toast.MakeText(ApplicationContext, "Критическая ошибка", ToastLength.Long).Show();
            }
        }

        public bool IsConnected
        {
            get { return _Transport.Connected; }
        }

        public void TakeSettings()
        {
            if (!Intent.GetBooleanExtra("Save", false))
                throw new Exception();
            IP = Intent.GetStringExtra("IP");
            if (IP == null)
            {
                Toast.MakeText(ApplicationContext, "Неверный IP", ToastLength.Long).Show();
                throw new Exception();
            }
            activeChannel = Intent.GetBooleanArrayExtra("ActiveChannel");
            if (activeChannel == new bool[] { false, false, false, false })
            {
                Toast.MakeText(ApplicationContext, "Включите хотя-бы 1 канал", ToastLength.Long).Show();
                throw new Exception();
            }
            ChannelRange = Intent.GetBooleanArrayExtra("ChannelRange");
            SamplingPeriod = Intent.GetDoubleExtra("SamplingPeriod", 1e-3);
        }

        public void ResetSettings()
        {
            IP = "10.128.11.141";
            activeChannel = new bool[] { true, true, true, true };
            ChannelRange = new bool[] { false, false };
            SamplingPeriod = 1e-3;
        }

        void ApplySettings(B382Meter _Device)
        {

            // должно куда-то сохранять данные 
            //Device.SetChStates(true, _EnabledChannels[1].Protected, _EnabledChannels[2].Protected, _EnabledChannels[3].Protected, false);
            _Device.SetChStates(activeChannel[0], activeChannel[1], activeChannel[2], activeChannel[3], false); //Все 4 канала включены true
            //_Device.SetChStates(true, true, true, true, false); //Все 4 канала включены true
            _Device.SetSegment(0, DataSize, true);
            //_FrameDataDesc.DataSize = _DataSizeOscBuffered.Protected;

            _Device.SetSamplingPeriod(SamplingPeriod, true);
            //_FrameDataDesc.SamplingTime = _SamplingTimeOscBuffered.Protected;

            _Device.SetChRange(
            ChannelRange[0] ? EnumB382ChRange.I_2mA : EnumB382ChRange.I_2A,
            ChannelRange[1] ? EnumB382ChRange.I_2mA : EnumB382ChRange.I_2A,
            true);
            //_Device.FlushProtocol();
        }

        public enum EnumAction { Reset = 0x1, Run = 0x2, StopLogger = 0x10 };

        void Start(B382Meter _Device)
        {
            if (!IsConnected)
                throw new Exception();
            _Device.ClearProtocol();
            _Device.Start(true);
        }

        async Task GetDataStatus(B382Meter _Device)
        {
            if (!IsConnected)
                Toast.MakeText(ApplicationContext, "Устройство не готово", ToastLength.Long).Show();

            _Device.ClearProtocol();

            R4RegisterBase r4 = _Device.GetStatus();

            while (!r4.MemIsEnd)
            {
                await Task.Delay(200);
                r4 = _Device.GetStatus();
            }
            //Message: data is ready
            //Toast.MakeText(ApplicationContext, "data is ready", ToastLength.Long).Show();
        }

        public float[][] ReadData(B382Meter _Device)
        {
            //if (!IsConnected)
            //{
            //    Toast.MakeText(ApplicationContext, "Устройство не готово к  данных", ToastLength.Long).Show();
            //    throw new Exception();
            //}
            float[][] _DataBuffer = new float[][] { new float[DataSize], new float[DataSize], new float[DataSize], new float[DataSize] };
            int ActiveChannelCount = 0;
            for (int i = 0; i < activeChannel.Length; i++)
            {
                if (activeChannel[i] == true)
                    ActiveChannelCount++;
            }

            _Device.ClearProtocol();
            R4RegisterBase r4 = _Device.GetStatus();
            uint Start = (uint)(r4.PreReg - (r4.PreReg % ActiveChannelCount));
            // Указываем адрес, откуда будем читать
            _Device.PrepareForReading(Start);
            int count = DataSize * ActiveChannelCount;

            Array.Resize(ref UInt16Buffer, count);
            _Device.GetData(UInt16Buffer);

            //Calibration[] calibrs = GetCurrentCallibrations(_Device);

            for (int k = 0, j = 0; j < DataSize; j++)
            {
                for (int i = 0; i < ChannelCount; i++)
                    if (_DataBuffer[i].Length > 0)
                    {
                        _DataBuffer[i][j] = ParseRawData(UInt16Buffer[k]);
                        k++;
                    }
            }

            return _DataBuffer;
        }

        protected virtual float ParseRawData(ushort value)
        {
            return (value - 32768) * 30f / (32768f);
            //return calibr.ToValue(value);
            //return value / 10000.0f;
        }

        //protected Calibration[] GetCurrentCallibrations(B382Meter _Device)
        //{
        //    Calibration[] callibrs = new Calibration[ChannelCount];
        //    for (int i = 0; i < ChannelCount; i++)
        //        callibrs[i] = _Device.GetCalibration(i, AppliedRange(i));
        //    return callibrs;
        //}

        //private int AppliedRange(int ch)
        //{
        //    if (ch == 2 || ch == 3)
        //        return ChannelRange[ch] ? 0 : 1;
        //    //System.Diagnostics.Debug.Fail(string.Format("Channel {0} has not range settings", ch));
        //    return 0;
        //}

        public void ShowData(float[][] data)
        {
            double[] avr = new double[] { 0, 0, 0, 0 };
            int n = data[1].Length;
            int ActiveChannelCount = 0;
            for (int j = 0; j < activeChannel.Length; j++)
            {
                if (activeChannel[j] == true)
                    ActiveChannelCount++;
            }

            for (int j = 0; j < n; j++)
            {

                avr[0] += data[0][j];
                if (ActiveChannelCount >= 2)
                    avr[1] += data[1][j];
                if (ActiveChannelCount >= 3)
                    avr[2] += data[2][j];
                if (ActiveChannelCount >= 4)
                    avr[3] += data[3][j];
            }
            int i = 0;


        }

        private PlotModel CreatePlotModel()
        {
            //var plotModel = new PlotModel { Title = "Linear Axis", TitleColor = OxyColors.GhostWhite, TextColor = OxyColors.GhostWhite };
            var plotModel = new PlotModel { TextColor = OxyColors.GhostWhite };

            plotModel.PlotAreaBorderColor = OxyColors.White;

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Maximum = (DataSize - 1) * x_scale,
                Minimum = 0,
                TickStyle = TickStyle.Crossing,
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = OxyColor.Parse("#4A4A4A"),
                MinorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColor.Parse("#4A4A4A"),
                TicklineColor = OxyColors.GhostWhite
            });

            plotModel.Axes.Add(new LinearAxis
            {
                //IsZoomEnabled = false,    //можно ли зумить оси, должно стоять у двух
                Position = AxisPosition.Left,
                Maximum = 6,
                Minimum = -6,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.Parse("#4A4A4A"),
                MinorGridlineStyle = LineStyle.Solid,
                MinorGridlineColor = OxyColor.Parse("#4A4A4A"),
                TicklineColor = OxyColors.GhostWhite
            });
            return plotModel;
        }

        private void UpdatePlot(PlotModel plot, float[][] data)
        {
            plot.Series.Clear();

            var series1 = new LineSeries
            {
                Title = "A",
                MarkerType = MarkerType.None,
                StrokeThickness = 1,
                //MarkerSize = 2,
                //MarkerStroke = OxyColors.White,
                Color = OxyColors.Yellow
            };

            var series2 = new LineSeries
            {
                Title = "B",
                MarkerType = MarkerType.None,
                StrokeThickness = 1,
                //MarkerSize = 2,
                //MarkerStroke = OxyColors.Black
                Color = OxyColor.Parse("#0895d8")
            };

            //Эмяляция данных на графике
            GenerateData(series1);
            GenerateData2(series2);

            //AddData(series1, data, 0);
            //AddData(series2, data, 1);


            plot.Series.Add(series1);
            plot.Series.Add(series2);
        }

        private void AddData(LineSeries series, float[][] data, int channel)
        {
            DataPoint point;
            for (int i = 0; i < data[channel].Length; i++)
            {
                point = new DataPoint(i, data[channel][i]);

                series.Points.Add(point);
            }
        }

        private void GenerateData(LineSeries series)
        {

            DataPoint point;
            Random rnd = new Random();
            for (int i = 0; i < 50; i++)
            {
                point = new DataPoint(i, rnd.Next(0, 10));

                series.Points.Add(point);
            }
        }

        private void GenerateData2(LineSeries series)
        {

            DataPoint point;
            Random rnd = new Random();
            for (int i = 0; i < 50; i++)
            {
                point = new DataPoint(i, rnd.Next(0, 10) + 0.5);
                series.Points.Add(point);
            }
        }

        private void AxisX_increment(object sender, EventArgs e)
        {
            x_scale += (float)0.1;
        }

        private void AxisX_decrease(object sender, EventArgs e)
        {
            x_scale -= (float)0.1;
        }
    }
}

