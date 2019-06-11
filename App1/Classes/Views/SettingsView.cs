using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace OscilloscopeAndroid
{
    [Activity(Label = "Activity1")]
    public class SettingsView : Activity
    {
        Button save;
        TextView ipTextView;
        TextView samplingFrequencyTextView;
        TextView dataSizeTextView;
        public string IP = "10.128.11.141";
        public bool[] activeChannel;
        public bool[] channelARange;
        public bool[] channelBRange;
        public int dataSize;
        public double SamplingPeriod;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.settings);
            // Create your application here
            IP = Intent.GetStringExtra("IP");
            activeChannel = Intent.GetBooleanArrayExtra("activeChannels");
            channelARange = Intent.GetBooleanArrayExtra("channelARange");
            channelBRange = Intent.GetBooleanArrayExtra("channelBRange");
            channelBRange = Intent.GetBooleanArrayExtra("channelBRange");
            dataSize = Intent.GetIntExtra("dataSize", 200);
            SamplingPeriod = Intent.GetDoubleExtra("samplingPeriod", 1e-3);

            ipTextView = FindViewById<TextView>(Resource.Id.textInputEditText1);
            ipTextView.Text = IP;

            samplingFrequencyTextView = FindViewById<TextView>(Resource.Id.textSamplingFreqInput);
            samplingFrequencyTextView.Text = (1 / SamplingPeriod).ToString();

            dataSizeTextView = FindViewById<TextView>(Resource.Id.textDataSizeInput);
            dataSizeTextView.Text = dataSize.ToString();

            save = FindViewById<Button>(Resource.Id.ButtonSave);
            save.Click += Save_Click;

            CheckBox ChannelEn1 = FindViewById<CheckBox>(Resource.Id.checkBox1);
            CheckBox ChannelEn2 = FindViewById<CheckBox>(Resource.Id.checkBox2);
            ChannelEn1.Checked = activeChannel[0];
            ChannelEn2.Checked = activeChannel[1];
            ChannelEn1.Click += ChannelEn1_Click;
            ChannelEn2.Click += ChannelEn2_Click;



        }

        private void Save_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainView));
            try
            {
                IP = ipTextView.Text;
                Double samplingFreq = Double.Parse(samplingFrequencyTextView.Text);
                dataSize = int.Parse(dataSizeTextView.Text);
                IPAddress address;
                if (IP == "" || IP == null || !IPAddress.TryParse(IP, out address))
                {
                    Toast.MakeText(ApplicationContext, "Неверный IP", ToastLength.Long).Show();
                    throw new Exception();
                }
                if (!activeChannel[0] && !activeChannel[1])
                {
                    Toast.MakeText(ApplicationContext, "Включите хотя-бы 1 канал", ToastLength.Long).Show();
                    throw new Exception();
                }
                if (samplingFreq <= 0 || samplingFreq > 1e9)
                {
                    Toast.MakeText(ApplicationContext, "Частота дисктретизации должна быть больше 0 и меньше 1E9", ToastLength.Long).Show();
                    throw new Exception();
                }
                if (dataSize <= 0 || dataSize > 1400)
                {
                    Toast.MakeText(ApplicationContext, "Количетсво выборок должно быть больше 0 и меньше 1400", ToastLength.Long).Show();
                    throw new Exception();
                }

                intent.PutExtra("IP", IP);
                intent.PutExtra("activeChannels", activeChannel);
                intent.PutExtra("channelARange", channelARange);
                intent.PutExtra("channelBRange", channelBRange);
                intent.PutExtra("dataSize", dataSize);
                intent.PutExtra("samplingPeriod", 1/samplingFreq);

                intent.PutExtra("changed", true);
                StartActivity(intent);
            }
            catch
            {
                Toast.MakeText(ApplicationContext, "Неверные настройки", ToastLength.Long).Show();
            }
        }

        private void ChannelEn1_Click(object sender, EventArgs e)
        {
            activeChannel[0] = !activeChannel[0];
        }

        private void ChannelEn2_Click(object sender, EventArgs e)
        {
            activeChannel[1] = !activeChannel[1];
        }
    }
}