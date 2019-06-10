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
        private bool[] channelARange;
        private bool[] channelBRange;
        private double samplingPeriod;
        private int dataSize = 700;
        private ushort[] uInt16Buffer;
        public enum ranges { _5V, _2V, _1V, _500mV, _200mV, _100mV, _50mV, _20mV, _10mV, _5mV };

        public Dictionary<ranges, bool[]> range = new Dictionary<ranges, bool[]>();

        public Settings()
        {
            this.uInt16Buffer = new ushort[0];
            range.Add(ranges._5V, new bool[] { true, true, false, false });
            range.Add(ranges._2V, new bool[] { true, true, false, true });
            range.Add(ranges._1V, new bool[] { true, true, true, false });
            range.Add(ranges._500mV, new bool[] { true, false, false, false });
            range.Add(ranges._200mV, new bool[] { true, false, false, true });
            range.Add(ranges._100mV, new bool[] { true, false, true, false });
            range.Add(ranges._50mV, new bool[] { false, false, false, false });
            range.Add(ranges._20mV, new bool[] { false, false, false, true });
            range.Add(ranges._10mV, new bool[] { false, false, true, false });
            range.Add(ranges._5mV, new bool[] { false, false, true, true });
            ResetSettings();
        }

        public string Ip
        {
            get { return ip; }
        }

        public bool[] ActiveChannels
        {
            get { return activeChannels; }
        }

        public bool[] ChannelARange
        {
            get { return channelARange; }
        }

        public bool[] ChannelBRange
        {
            get { return channelBRange; }
        }

        public double SamplingPeriod
        {
            get { return samplingPeriod; }
        }

        public int DataSize
        {
            get { return dataSize; }
            set { dataSize = value; }
        }

        public ushort[] UInt16Buffer
        {
            get { return uInt16Buffer; }
        }



        public void SetSettings(Intent intent, Context context)
        {
            if (!intent.GetBooleanExtra("Save", false))
                throw new Exception();
            ip = intent.GetStringExtra("IP");
            if (ip == null || ip.Equals(""))
            {
                Toast.MakeText(context, "Неверный IP", ToastLength.Long).Show();
                throw new Exception();
            }
            activeChannels = intent.GetBooleanArrayExtra("activeChannels");
            if (activeChannels == new bool[] { false, false })
            {
                Toast.MakeText(context, "Включите хотя-бы 1 канал", ToastLength.Long).Show();
                throw new Exception();
            }
            channelARange = intent.GetBooleanArrayExtra("channelBRange");
            channelARange = intent.GetBooleanArrayExtra("channelARange");
            samplingPeriod = intent.GetDoubleExtra("samplingPeriod", 1e-6);
        }

        public void ResetSettings()
        {
            ip = "10.128.11.141";
            activeChannels = new bool[] { true, true };
            channelARange = range[ranges._5V];
            channelBRange = range[ranges._5V];
            samplingPeriod = 1e-3;
        }

        public Intent getSettingsIntent(Context context)
        {
            var intent = new Intent(context, typeof(SettingsView));
            intent.PutExtra("IP", ip);
            intent.PutExtra("activeChannels", activeChannels);
            intent.PutExtra("channelARange", channelARange);
            intent.PutExtra("channelBRange", channelBRange);
            intent.PutExtra("samplingPeriod", samplingPeriod);
            return intent;
        }
    }
}