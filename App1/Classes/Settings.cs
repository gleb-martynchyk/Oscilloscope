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
        private ushort[] uInt16Buffer;

        public Settings()
        {
            ResetSettings();
            this.uInt16Buffer = new ushort[0];
        }

        public string Ip
        {
            get { return this.ip; }
        }

        public bool[] ActiveChannels
        {
            get { return this.activeChannels; }
        }

        public bool[] ChannelsRange
        {
            get { return this.channelsRange; }
        }

        public double SamplingPeriod
        {
            get { return this.samplingPeriod; }
        }

        public ushort[] UInt16Buffer
        {
            get { return this.uInt16Buffer; }
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
            ip = "192.168.0.141";
            activeChannels = new bool[] { true, true, true, true };
            channelsRange = new bool[] { false, false };
            samplingPeriod = 1e-3;
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