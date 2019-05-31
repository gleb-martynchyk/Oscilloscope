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
        private bool enabled = false;

        public Oscilloscope(TCPIPTransport transport, Context applicationContext)
        {
            this.device = new B320Oscilloscope(transport);
            this.transport = transport;
            this.UInt16Buffer = new ushort[0];
            this.applicationContext = applicationContext;
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

        public async Task Main(OsclilloscopePlot osclilloscopePlot)
        {
            enabled = true;
            plot = new OsclilloscopePlot();

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
                float[][] DataBuffer = new float[][] { new float[settings.DataSize], new float[settings.DataSize] };
                device = new B320Oscilloscope(transport);
                device.ClearProtocol();
                device.Reset(true);
                device.InitDDR(true);
                //no logger
                //device.SetMeasMode(false, true);
                device.SetMeasMode(true, true);
                //device.ReadCalibrations();
                var res = device.GetIDs();

                //Вывод
                //String outText = (res.Serial).ToString() + "  " + (res.FPGAVersion).ToString() + "  " + (res.TypeID).ToString();
                //Toast.MakeText(applicationContext, outText, ToastLength.Long).Show();

                ApplySettings(settings);

                while (enabled)
                {
                    Start();
                    await GetDataStatus();
                    DataBuffer = GetData();
                    osclilloscopePlot.CreatePlotModel(settings);
                    osclilloscopePlot.AddDataToPlot(DataBuffer);

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(applicationContext, "Критическая ошибка:" + ex.Message, ToastLength.Long).Show();
                enabled = false;
            }
        }

        public async Task Simulation(OsclilloscopePlot osclilloscopePlot)
        {
            enabled = true;
            plot = new OsclilloscopePlot();
            try
            {
                while (enabled)
                {
                    osclilloscopePlot.CreatePlotModel(settings);
                    osclilloscopePlot.AddDataToPlot(null);

                    await Task.Delay(33);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(applicationContext, "Критическая ошибка:" + ex.Message, ToastLength.Long).Show();
                enabled = false;
            }
        }

        public void StopMain()
        {
            enabled = false;
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

        public float[][] GetData()
        {
            if (!IsConnected)
            {
                Toast.MakeText(applicationContext, "Устройство не готово к  данных", ToastLength.Long).Show();
                throw new Exception();
            }

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
            uint segment = (Start) >> 8;
            uint offset = (Start) & 0xFF;

            // Указываем адрес, откуда будем читать
            device.PrepareForReading((uint)segment);
            int count = settings.DataSize * ActiveChannelCount;

            Array.Resize(ref UInt16Buffer, (int)(count + offset));
            device.GetData(UInt16Buffer);

            //Calibration[] calibrs = GetCurrentCallibrations(_Device);

            for (int k = 0, j = 0; j < settings.DataSize; j++)
            {
                for (int i = 0; i < ActiveChannelCount; i++)
                    if (_DataBuffer[i].Length > 0)
                    {
                        _DataBuffer[i][j] = ParseRawData(UInt16Buffer[k + offset]);
                        //_DataBuffer[i][j] = (float)UInt16Buffer[offset + ActiveChannelCount * j];
                        k++;
                    }
            }

            return _DataBuffer;
        }

        public void ApplySettings(Settings settings)
        {
            device.ClearProtocol();
            device.Reset(false);
            // TODO: должно куда-то сохранять данные, файл xml
            //device.SetChStates(activeChannels[0], activeChannels[1], activeChannels[2], activeChannels[3], false); //Все 4 канала включены true
            device.SetChStates(true, true, false);
            device.SetSegment(0, settings.DataSize, false);

            device.SetSamplingPeriod(settings.SamplingPeriod, true);

            //Device.SetSynchSettings

            device.SetGains(true, false, false, false, false,
                             true, false, false, false, false, true);

            device.FlushProtocol();
        }

        protected virtual float ParseRawData(ushort value)
        {
            //умножать на диапазон
            //return (value - 32768) * 1f / (32768f);
            return ((short)value) / (4098f);
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