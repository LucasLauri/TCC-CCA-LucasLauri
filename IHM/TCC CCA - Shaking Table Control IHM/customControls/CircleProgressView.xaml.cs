using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LucasLauriHelpers.customControls
{
    /// <summary>
    /// Interaction logic for CircleProgressView.xaml
    /// </summary>
    public partial class CircleProgressView : UserControl
    {

        public static DependencyProperty FillProperty =
             DependencyProperty.Register("Fill", typeof(Color),
             typeof(CircleProgressView),
             new FrameworkPropertyMetadata(Colors.Black,
                                    FrameworkPropertyMetadataOptions.AffectsRender,
                                    new PropertyChangedCallback(FillChanged))
                 );

        /// <summary>
        /// Atualização visual do gráfico de acordo com o valor informado na propriedade fill
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void FillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CircleProgressView chart = (CircleProgressView)d;
            chart.Fill = (Color)e.NewValue;

            chart.GraficoPath.Fill = new SolidColorBrush(chart.Fill);
            chart.FullGraph.Fill = new SolidColorBrush(chart.Fill);
        }

        public Color Fill
        {
            get { return (Color)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static DependencyProperty ProgressProperty =
             DependencyProperty.Register("Progress", typeof(double),
             typeof(CircleProgressView),
             new FrameworkPropertyMetadata(new double(),
                                    FrameworkPropertyMetadataOptions.AffectsRender,
                                    new PropertyChangedCallback(ProgressChanged))
                 );

        /// <summary>
        /// Atualização visual do gráfico de acordo com o valor informado na propriedade progress
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void ProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            const double marginValue = 0;

            CircleProgressView chart = (CircleProgressView)d;

            //bool valueChanged = chart.Progress != chart.OldProgress;

            double progress = (double)e.NewValue;

            Point centerPoint = new Point((chart.Width-2)/ 2, (chart.Height -2)/ 2);

            ArcSegment arcSegment = (ArcSegment)chart.StatusGrafico.Segments[0];

            LineSegment lineSegment = (LineSegment)chart.StatusGrafico.Segments[1];

            chart.StatusGrafico.StartPoint = new Point(centerPoint.X, marginValue);

            lineSegment.Point = centerPoint;

            arcSegment.Size = new Size((chart.Width -2)/ 2 - marginValue, (chart.Height -2)/ 2 - marginValue);

            if (progress <= 0.5)
                arcSegment.IsLargeArc = false;
            else
                arcSegment.IsLargeArc = true;

            double ragAngle;

            if (progress < 1)
            {
                //ragAngle = Math.PI * (360 * progress - 90) / 180.0;
                ragAngle = Math.PI * (360 * progress - 90) / 180.0;

                double xValue = (centerPoint.X - marginValue) * Math.Cos(ragAngle) + (chart.Width -2)/ 2;
                double yValue = (centerPoint.Y - marginValue) * Math.Sin(ragAngle) + (chart.Height -2)/ 2;

                arcSegment.Point = new Point(xValue, yValue);

                chart.FullGraph.Visibility = Visibility.Collapsed;
                chart.GraficoPath.Visibility = Visibility.Visible;
            }
            else
            {
                chart.FullGraph.Visibility = Visibility.Visible;
                chart.GraficoPath.Visibility = Visibility.Collapsed;
            }

            //if (valueChanged)
            //{
            //    Task.Factory.StartNew(async () =>
            //    {
            //        chart.Dispatcher.Invoke(() =>
            //        {
            //            chart.ellipseTransform.ScaleX = 3;
            //            chart.ellipseTransform.ScaleY = 3;
            //        });

            //        await Task.Delay(1000);

            //        chart.Dispatcher.Invoke(() =>
            //        {
            //            chart.ellipseTransform.ScaleX = 1;
            //            chart.ellipseTransform.ScaleY = 1;
            //        });
            //    });
            //}


            //chart.OldProgress = chart.Progress;
        }

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set 
            { 
                SetValue(ProgressProperty, value);
            }
        }

        private bool _flipYControl;
        /// <summary>
        /// Se o controle deve sofrer um mirror em X
        /// </summary>
        public bool FlipYControl
        {
            get { return _flipYControl; }
            set 
            {                
                _flipYControl = value;

                Dispatcher.Invoke(() => {
                    ellipseTransform.ScaleY = FlipYControl? -1 : 1;
                });
            }
        }

        //private double _oldProgress;

        //public double OldProgress
        //{
        //    get { return _oldProgress; }
        //    set { _oldProgress = value; }
        //}


        public CircleProgressView()
        {
            InitializeComponent();
        }
    }
}
