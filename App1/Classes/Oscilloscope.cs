using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BECSLibrary.Transport;
using MeterFramework.AlmaMeter;

namespace OscilloscopeAndroid
{
    class Oscilloscope
    {
        public const int ChannelCount = B320Oscilloscope.ChannelCount;
        private B320Oscilloscope device;
        private TCPIPTransport transport;
        private Settings settings;
        private ushort[] UInt16Buffer;
        private Context applicationContext;
        private OsclilloscopePlot plot;

        public Oscilloscope(TCPIPTransport transport)
        {
            this.device = new B320Oscilloscope(transport);
            this.transport = transport;
            this.UInt16Buffer = new ushort[0];
        }

        public B320Oscilloscope Device
        {
            get { return this.device; }
        }

        public bool IsConnected
        {
            get { return transport.Connected; }
        }

        public Settings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        public Context ApplicationContext
        {
            get { return applicationContext; }
            set { applicationContext = value; }
        }

        public async Task Main(bool enabled, OsclilloscopePlot osclilloscopePlot)
        {
            try
            {
                transport.Connect();
            }
            catch (Exception exc)
            {
                Toast.MakeText(applicationContext, "Нет соединения" + exc.ToString(), ToastLength.Long).Show();
                throw new Exception();
            }

            try
            {
                plot = new OsclilloscopePlot();
                float[][] DataBuffer = new float[][] { new float[settings.DataSize], new float[settings.DataSize],
                    new float[settings.DataSize], new float[settings.DataSize] };
                device = new B320Oscilloscope(transport);
                device.ClearProtocol();
                device.InitDDR(true);
                device.Reset(true);
                device.SetMeasMode(false, true);
                //device.ReadCalibrations();
                var res = device.GetIDs();

                //Вывод
                String outText = (res.Serial).ToString() + "  " + (res.FPGAVersion).ToString() + "  " + (res.TypeID).ToString();
                Toast.MakeText(applicationContext, outText, ToastLength.Long).Show();
                ApplySettings(settings);


                while (true)
                {
                    //----------- Основные методы
                    Start();
                    await GetDataStatus();
                    DataBuffer = ReadData();
                    //ShowData(DataBuffer);
                    osclilloscopePlot.CreatePlotModel(settings);
                    osclilloscopePlot.UpdatePlot(DataBuffer);

                    await Task.Delay(10000);
                    if (!enabled)
                        break;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(applicationContext, "Критическая ошибка:" + ex.Message, ToastLength.Long).Show();
                enabled = false;
            }
        }

        public async Task Simulation(bool enabled, OsclilloscopePlot osclilloscopePlot)
        {
            try
            {
                plot = new OsclilloscopePlot();

                while (true)
                {
                    osclilloscopePlot.CreatePlotModel(settings);
                    osclilloscopePlot.UpdatePlot(null);

                    await Task.Delay(33);

                    if (enabled == false)
                        break;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(applicationContext, "Критическая ошибка:" + ex.Message, ToastLength.Long).Show();
                enabled = false;
            }
        }

        public void Start()
        {
            if (!IsConnected)
                throw new Exception();
            device.ClearProtocol();
            device.Start(true);
        }

        public async Task GetDataStatus()
        {
            if (!IsConnected)
                Toast.MakeText(applicationContext, "Устройство не готово", ToastLength.Long).Show();

            device.ClearProtocol();

            R4RegisterBase r4 = device.GetStatus();

            while (!r4.MemIsEnd)
            {
                await Task.Delay(200);
                r4 = device.GetStatus();
            }
            //Message: data is ready
            Toast.MakeText(applicationContext, "data is ready", ToastLength.Long).Show();
        }

        public void ShowData(float[][] data)
        {
            double[] avr = new double[] { 0, 0, 0, 0 };
            int n = data[1].Length;
            int ActiveChannelCount = 0;
            for (int j = 0; j < settings.ActiveChannels.Length; j++)
            {
                if (settings.ActiveChannels[j] == true)
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

        public float[][] ReadData()
        {
            //if (!IsConnected)
            //{
            //    Toast.MakeText(ApplicationContext, "Устройство не готово к  данных", ToastLength.Long).Show();
            //    throw new Exception();
            //}
            float[][] _DataBuffer = new float[][] { new float[settings.DataSize], new float[settings.DataSize] };
            int ActiveChannelCount = 0;
            for (int i = 0; i < settings.ActiveChannels.Length; i++)
            {
                if (settings.ActiveChannels[i])
                    ActiveChannelCount++;
            }

            ActiveChannelCount = 2;

            device.ClearProtocol();
            R4RegisterBase r4 = device.GetStatus();
            uint Start = (uint)(r4.PreReg - (r4.PreReg % ActiveChannelCount));
            // Указываем адрес, откуда будем читать
            device.PrepareForReading(Start);
            int count = settings.DataSize * ActiveChannelCount;

            Array.Resize(ref UInt16Buffer, count);
            device.GetData(UInt16Buffer);

            //Calibration[] calibrs = GetCurrentCallibrations(_Device);

            for (int k = 0, j = 0; j < settings.DataSize; j++)
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

        public void ApplySettings(Settings settings)
        {
            // TODO: должно куда-то сохранять данные, файл xml
            //device.SetChStates(activeChannels[0], activeChannels[1], activeChannels[2], activeChannels[3], false); //Все 4 канала включены true
            device.SetChStates(true, true, false); //Все 4 канала включены true
            device.SetSegment(0, settings.DataSize, true);

            device.SetSamplingPeriod(settings.SamplingPeriod, true);

            device.SetGains(true, true, true, false, false,
                             true, true, true, false, false, true);
            device.FlushProtocol();
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


    }


}