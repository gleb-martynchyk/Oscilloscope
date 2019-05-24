using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MeterFramework.AlmaMeter;

namespace OscilloscopeAndroid
{
    class Settings
    {
        private string ip;
        private bool[] activeChannels;
        private bool[] channelsRange;
        private double samplingPeriod;
        private ushort[] UInt16Buffer = new ushort[0];

        public string Ip
        {
            get
            {
                return this.ip;
            }
        }

        public void SetSettings(Intent intent, Context context)
        {
            if (!intent.GetBooleanExtra("Save", false))
                throw new Exception();
            ip = intent.GetStringExtra("IP");
            if (ip == null)
            {
                Toast.MakeText(context, "Неверный IP", ToastLength.Long).Show();
                throw new Exception();
            }
            activeChannels = intent.GetBooleanArrayExtra("ActiveChannel");
            if (activeChannels == new bool[] { false, false, false, false })
            {
                Toast.MakeText(context, "Включите хотя-бы 1 канал", ToastLength.Long).Show();
                throw new Exception();
            }
            channelsRange = intent.GetBooleanArrayExtra("ChannelRange");
            samplingPeriod = intent.GetDoubleExtra("SamplingPeriod", 1e-3);
        }

        public void ResetSettings()
        {
            ip = "10.128.11.141";
            activeChannels = new bool[] { true, true, true, true };
            channelsRange = new bool[] { false, false };
            samplingPeriod = 1e-3;
        }

        public void ApplySettings(B382Meter _Device, Oscilloscope oscilloscope)
        {

            // TODO: должно куда-то сохранять данные, файл xml
            _Device.SetChStates(activeChannels[0], activeChannels[1], activeChannels[2], activeChannels[3], false); //Все 4 канала включены true
            //_Device.SetChStates(true, true, true, true, false); //Все 4 канала включены true
            _Device.SetSegment(0, oscilloscope.DataSize, true);
            //_FrameDataDesc.DataSize = _DataSizeOscBuffered.Protected;

            _Device.SetSamplingPeriod(samplingPeriod, true);
            //_FrameDataDesc.SamplingTime = _SamplingTimeOscBuffered.Protected;

            _Device.SetChRange(
            channelsRange[0] ? EnumB382ChRange.I_2mA : EnumB382ChRange.I_2A,
            channelsRange[1] ? EnumB382ChRange.I_2mA : EnumB382ChRange.I_2A,
            true);
            //_Device.FlushProtocol();
        }

        public Intent getSettingsIntent(Context context)
        {
            var intent = new Intent(context, typeof(SettingsView));
            intent.PutExtra("IP", ip);
            intent.PutExtra("ActiveChannel", activeChannels);
            intent.PutExtra("ChannelRange", channelsRange);
            intent.PutExtra("SamplingPeriod", samplingPeriod);
            return intent;
        }
    }
}