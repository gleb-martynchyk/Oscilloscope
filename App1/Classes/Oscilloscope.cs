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
        private TCPIPTransport transport = new TCPIPTransport();
        private int dataSize = 30;
        private Settings settings;
        private ushort[] UInt16Buffer;

        public Oscilloscope(TCPIPTransport transport)
        {
            this.settings = new Settings();
            this.device = new B320Oscilloscope(transport);
            this.transport = transport;
            this.UInt16Buffer = new ushort[0];
        }

        public int DataSize
        {
            get { return this.dataSize; }
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
            //if (!IsConnected)
            //    Toast.MakeText(ApplicationContext, "Устройство не готово", ToastLength.Long).Show();

            device.ClearProtocol();

            R4RegisterBase r4 = device.GetStatus();

            while (!r4.MemIsEnd)
            {
                await Task.Delay(200);
                r4 = device.GetStatus();
            }
            //Message: data is ready
            //Toast.MakeText(ApplicationContext, "data is ready", ToastLength.Long).Show();
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
            float[][] _DataBuffer = new float[][] { new float[dataSize], new float[dataSize] };
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
            int count = dataSize * ActiveChannelCount;

            Array.Resize(ref UInt16Buffer, count);
            device.GetData(UInt16Buffer);

            //Calibration[] calibrs = GetCurrentCallibrations(_Device);

            for (int k = 0, j = 0; j < dataSize; j++)
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
            device.SetSegment(0, this.DataSize, true);

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

        //private int AppliedRange(int ch)
        //{
        //    if (ch == 2 || ch == 3)
        //        return ChannelRange[ch] ? 0 : 1;
        //    //System.Diagnostics.Debug.Fail(string.Format("Channel {0} has not range settings", ch));
        //    return 0;
        //}


    }


}