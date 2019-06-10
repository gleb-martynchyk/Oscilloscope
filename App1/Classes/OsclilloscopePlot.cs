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
        private int dataSize;

        public OsclilloscopePlot(int datasize)
        {
            this.dataSize = datasize;
        }

        public PlotView View
        {
            get { return view; }
            set { view = value; }
        }

        public void CreatePlotModel()
        {
            //this.model = new PlotModel { Title = "Linear Axis", TitleColor = OxyColors.GhostWhite, TextColor = OxyColors.GhostWhite };
            view.Model = new PlotModel { TextColor = OxyColors.GhostWhite };

            view.Model.PlotAreaBorderColor = OxyColors.White;

            //axis x - time
            view.Model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                //Maximum = (dataSize - 1) * x_scale,
                //Minimum = 0,
                TickStyle = TickStyle.Crossing,
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = OxyColor.Parse("#4A4A4A"),
                MinorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColor.Parse("#4A4A4A"),
                TicklineColor = OxyColors.GhostWhite
            });

            //axis x - time
            view.Model.Axes.Add(new LinearAxis
            {
                //IsZoomEnabled = false,    //можно ли зумить оси, должно стоять у двух
                Position = AxisPosition.Left,
                //Maximum = 12 * y_scale,
                //Minimum = -2 * y_scale,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.Parse("#4A4A4A"),
                MinorGridlineStyle = LineStyle.Solid,
                MinorGridlineColor = OxyColor.Parse("#4A4A4A"),
                TicklineColor = OxyColors.GhostWhite
            });
        }

        public void UpdatePlotModel()
        {
            view.Model.InvalidatePlot(true);
        }

        public void AddDataToPlot(float[][] data)
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
            Random random = new Random();
            AddData(series1, GenerateData(random));
            //if (data.Length == 2)
            AddData(series2, GenerateData(random));

            //Реальные данные
            //AddData(series1, data[0]);
            //AddData(series2, data[1]);

            view.Model.Series.Add(series1);
            view.Model.Series.Add(series2);
        }

        public void AddFakeDataToPlot(Settings settings)
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
            Random random = new Random();
            AddData(series1, GenerateData(random));
            if (settings.ActiveChannels.Length == 2)
                AddData(series2, GenerateData(random));

            //Реальные данные
            //AddData(series1, data[0]);
            //AddData(series2, data[1]);

            view.Model.Series.Add(series1);
            view.Model.Series.Add(series2);
        }

        public void AddData(LineSeries series, float[] data)
        {
            DataPoint point;
            for (int i = 0; i < data.Length; i++)
            {
                point = new DataPoint(i, data[i]);
                series.Points.Add(point);
            }
        }

        public float[] GenerateData(Random random)
        {
            float[] data = new float[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                data[i] = random.Next(0, 12);
            }
            return data;
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