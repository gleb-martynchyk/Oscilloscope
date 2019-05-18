﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace App1
{
    [Activity(Label = "Activity1")]
    public class Settings : Activity
    {
        Button save;
        TextView IpText;
        public string IP = "10.128.11.141";
        public bool[] activeChannel;
        public bool[] ChannelRange;
        public double SamplingPeriod;
        CheckBox ReangeCh2A;
        CheckBox ReangeCh2mA;
        CheckBox ReangeCh3A;
        CheckBox ReangeCh3mA;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SettingsLayout);
            // Create your application here
            IP = Intent.GetStringExtra("IP");
            activeChannel = Intent.GetBooleanArrayExtra("ActiveChannel");
            ChannelRange = Intent.GetBooleanArrayExtra("ChannelRange");
            SamplingPeriod = Intent.GetDoubleExtra("SamplingPeriod",1e-3);

            IpText = FindViewById<TextView>(Resource.Id.textInputEditText1);
            save = FindViewById<Button>(App1.Resource.Id.ButtonSave);
            save.Click += Save_Click;
            IpText.Text = IP;

            CheckBox ChannelEn1 = FindViewById<CheckBox>(Resource.Id.checkBox1);
            CheckBox ChannelEn2 = FindViewById<CheckBox>(Resource.Id.checkBox2);

            ChannelEn1.Checked = activeChannel[0];
            ChannelEn2.Checked = activeChannel[1];

            if (!ChannelRange[0])
            {
                ReangeCh2A.Checked = true;
                ReangeCh2mA.Checked = false;
            }
            else
            {
                ReangeCh2mA.Checked = true;
                ReangeCh2A.Checked = false;
            }
            if (!ChannelRange[1])
            {
                ReangeCh3A.Checked = true;
                ReangeCh3mA.Checked = false;
            }
            else
            {
                ReangeCh3mA.Checked = true;
                ReangeCh3A.Checked = false;
            }
        }



        private void ChannelEn4_Click(object sender, EventArgs e)
        {
            activeChannel[3] = !activeChannel[3];
        }

        private void ChannelEn3_Click(object sender, EventArgs e)
        {
            activeChannel[2] = !activeChannel[2];
        }

        private void ChannelEn2_Click(object sender, EventArgs e)
        {
            activeChannel[1] = !activeChannel[1];
        }

        private void ChannelEn1_Click(object sender, EventArgs e)
        {
            activeChannel[0] = !activeChannel[0];
        }

        private void Save_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            try
            {
                IP = IpText.Text;
                intent.PutExtra("Save", true);
                if (IP == "" || IP == null)
                {
                    Toast.MakeText(ApplicationContext, "Неверный IP", ToastLength.Long).Show();
                    throw new Exception();
                }
                intent.PutExtra("IP", IP);
                intent.PutExtra("ActiveChannel", activeChannel);
                if (activeChannel[0] == false&& activeChannel[1] == false&& activeChannel[2] == false&& activeChannel[3] == false)
                {
                    Toast.MakeText(ApplicationContext, "Включите хотя-бы 1 канал", ToastLength.Long).Show();
                    throw new Exception();
                }
                intent.PutExtra("ChannelRange", ChannelRange);
                intent.PutExtra("SamplingPeriod", SamplingPeriod);
                StartActivity(intent);
            }
            catch { }
        }
    }
}