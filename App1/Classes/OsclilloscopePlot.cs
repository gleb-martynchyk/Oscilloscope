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

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Xamarin.Android;

namespace OscilloscopeAndroid
{
    class OsclilloscopePlot
    {
        private PlotView view;
        private float x_scale = 1;
        private float y_scale = 1;

        public OsclilloscopePlot()
        {
        }

        public PlotView View
        {
            get { return view; }
            set { view = value; }
        }

        public PlotModel CreatePlotModel(Settings settings)
        {
            //this.model = new PlotModel { Title = "Linear Axis", TitleColor = OxyColors.GhostWhite, TextColor = OxyColors.GhostWhite };
            view.Model = new PlotModel { TextColor = OxyColors.GhostWhite };

            view.Model.PlotAreaBorderColor = OxyColors.White;

            view.Model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Maximum = (settings.DataSize - 1) * x_scale,
                Minimum = 0,
                TickStyle = TickStyle.Crossing,
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = OxyColor.Parse("#4A4A4A"),
                MinorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColor.Parse("#4A4A4A"),
                TicklineColor = OxyColors.GhostWhite
            });

            view.Model.Axes.Add(new LinearAxis
            {
                //IsZoomEnabled = false,    //можно ли зумить оси, должно стоять у двух
                Position = AxisPosition.Left,
                Maximum = 6 * y_scale,
                Minimum = -6 * y_scale,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.Parse("#4A4A4A"),
                MinorGridlineStyle = LineStyle.Solid,
                MinorGridlineColor = OxyColor.Parse("#4A4A4A"),
                TicklineColor = OxyColors.GhostWhite
            });
            return view.Model;
        }

        public void UpdatePlot(float[][] data)
        {
            view.Model.Series.Clear();

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

            //Симуляция данных на графике
            GenerateDataChannel1(series1);
            GenerateDataChannel2(series2);

            //Реальные данные
            //AddData(series1, data, 0);
            //AddData(series2, data, 1);

            view.Model.Series.Add(series1);
            view.Model.Series.Add(series2);
        }

        public void AddData(LineSeries series, float[][] data, int channel)
        {
            DataPoint point;
            for (int i = 0; i < data[channel].Length; i++)
            {
                point = new DataPoint(i, data[channel][i]);

                series.Points.Add(point);
            }
        }

        public void GenerateDataChannel1(LineSeries series)
        {
            DataPoint point;
            Random rnd = new Random();
            for (int i = 0; i < 50; i++)
            {
                point = new DataPoint(i, rnd.Next(0, 10));

                series.Points.Add(point);
            }
        }

        public void GenerateDataChannel2(LineSeries series)
        {
            DataPoint point;
            Random rnd = new Random();
            for (int i = 0; i < 50; i++)
            {
                point = new DataPoint(i, rnd.Next(0, 10) + 0.5);
                series.Points.Add(point);
            }
        }

        public void AxisX_increment(object sender, EventArgs e)
        {
            if (x_scale <= 1f)
            {
                x_scale += 0.1f;
            }
        }

        public void AxisX_decrease(object sender, EventArgs e)
        {
            if (x_scale >= 0.2f)
            {
                x_scale -= 0.1f;
            }
        }

        public void AxisY_increment(object sender, EventArgs e)
        {
            if (x_scale <= 1f)
            {
                y_scale += 0.1f;
            }
        }

        public void AxisY_decrease(object sender, EventArgs e)
        {
            if (y_scale >= 0.2f)
            {
                y_scale -= 0.1f;
            }
        }
    }
}