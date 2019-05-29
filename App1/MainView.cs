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

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Xamarin.Android;

namespace OscilloscopeAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainView : AppCompatActivity
    {
        private TCPIPTransport transport = new TCPIPTransport();
        private Oscilloscope oscilloscope;
        private Settings settings;
        private B320Oscilloscope device;
        private OsclilloscopePlot osclilloscopePlot;
        bool enabled = false;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            oscilloscope = new Oscilloscope(transport);
            settings = new Settings();
            oscilloscope.Settings = settings;
            device = oscilloscope.Device;

            try
            {
                settings.SetSettings(Intent, ApplicationContext);
            }
            catch
            {
                settings.ResetSettings();
            }

            SetContentView(Resource.Layout.activity_main);

            Switch enableSwitch = FindViewById<Switch>(Resource.Id.switch1);

            //TODO exception null reference
            osclilloscopePlot.View = FindViewById<PlotView>(Resource.Id.plot_view);

            // INIT
            transport.IPAddress = IPAddress.Parse(settings.Ip);
            transport.Port = 0x6871;
            transport.Timeout = 500;

            enableSwitch.CheckedChange += EnabelApplicationAsync;

            Button buttonAxisX_inc = FindViewById<Button>(Resource.Id.button_x_dec);
            Button buttonAxisX_dec = FindViewById<Button>(Resource.Id.button_x_inc);
            buttonAxisX_inc.Click += osclilloscopePlot.AxisX_increment;
            buttonAxisX_dec.Click += osclilloscopePlot.AxisX_decrease;

            Button buttonAxisY_inc = FindViewById<Button>(Resource.Id.button_y_dec);
            Button buttonAxisY_dec = FindViewById<Button>(Resource.Id.button_y_inc);
            buttonAxisY_inc.Click += osclilloscopePlot.AxisY_increment;
            buttonAxisY_dec.Click += osclilloscopePlot.AxisY_decrease;

            Button buttonSettings = FindViewById<Button>(Resource.Id.button2);

            buttonSettings.Click += ButtinSettings_Click;
            Context context = ApplicationContext;
        }


        private async void EnabelApplicationAsync(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (enabled == false)
            {
                enabled = true;
                //await oscilloscope.Main(enabled, osclilloscopePlot);
                await oscilloscope.Simulation(enabled, osclilloscopePlot);
            }
            else
            {
                transport.Disconnect();
                enabled = false;
            }
        }


        private void ButtinSettings_Click(object sender, System.EventArgs e)
        {
            StartActivity(settings.getSettingsIntent(this));
        }

    }
}

