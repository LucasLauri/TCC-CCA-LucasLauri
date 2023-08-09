using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using PdfSharp.Drawing;

namespace LucasLauriHelpers.src
{
    /// <summary>
    /// Classe base de um objeto que pode ser desenhado no <see cref="LucasLauriHelpers.customControls.GraphView"/>
    /// </summary>
    [DebuggerDisplay("{Id} e:{Enabled}")]
    [Serializable]
    [XmlInclude(typeof(GraphLine))]
    [XmlInclude(typeof(GraphFilledRegion))]
    public abstract class GraphObj : INotifyPropertyChanged, ICloneable
    {
        #region Eventos PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        /// <summary>
        /// Atualiza campo gerando evento para GUI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field">Campo a ser atualizado</param>
        /// <param name="value">Novo valor do campo</param>
        /// <param name="propertyName"></param>
        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            field = value;
            OnPropertyChanged(propertyName);
        }

        #endregion

        /// <summary>
        /// Multiplicador do <see cref="StrokeThickness"/> para exibir os pontos do obj se <see cref="ShowPoints"/> for TRUE
        /// </summary>
        public static double PointWidhtMultiplier { get; set; } = 10;

        /// <summary>
        /// Escala aplicada ao <see cref="OriginalPoints"/> para gerar o <see cref="ViewPoints"/>
        /// </summary>
        public Point ViewScale { get; set; } = new Point(1, 1);

        private string _id;
        /// <summary>
        /// Identificação destes dados
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        private string _additionalData;
        /// <summary>
        /// Dados adicionais armazenados neste obj
        /// </summary>
        public string AdditionalData
        {
            get => _additionalData;
            set => SetField(ref _additionalData, value);
        }

        private string _xMeasureUnity = "";
        /// <summary>
        /// Unidade de medida do eixo X
        /// </summary>
        public string XMeasureUnity
        {
            get => _xMeasureUnity;
            set => SetField(ref _xMeasureUnity, value);
        }

        private string _yMeasureUnity = "";
        /// <summary>
        /// Unidade de medida do eixo Y
        /// </summary>
        public string YMeasureUnity
        {
            get => _yMeasureUnity;
            set => SetField(ref _yMeasureUnity, value);
        }

        private bool needsViewUpdate;
        /// <summary>
        /// Se a visualização deste conjunto de dados precisa ser atualizada
        /// </summary>
        public bool NeedsViewUpdate
        {
            get => needsViewUpdate;
            set
            {
                if (value)
                    RefreshGeometryData();

                needsViewUpdate = value;
            }
        }

        private bool _forceRecenter;
        /// <summary>
        /// Se a recentralização da visualização deve ser forçada
        /// </summary>
        public bool ForceRecenter
        {
            get => _forceRecenter;
            set => SetField(ref _forceRecenter, value);
        }

        private List<Point> _originalPoints = new List<Point>();
        /// <summary>
        /// Lista de dados originais a serem apresentados no gráfico
        /// </summary>
        public List<Point> OriginalPoints
        {
            get => _originalPoints;
            set
            {
                _originalPoints = value;

                NeedsViewUpdate = true;
            }
        }

        private List<Point> _viewPoints = new List<Point>();
        /// <summary>
        /// Lista de dados a serem apresentados no gráfico
        /// </summary>
        public List<Point> ViewPoints
        {
            get => _viewPoints;
            set => _viewPoints = value;
        }

        private double _strokeThickness;
        /// <summary>
        /// Espessura das linhas do gráfico
        /// </summary>
        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetField(ref _strokeThickness, value);
        }

        private bool _showPoints = false;
        /// <summary>
        /// Se os pontos do obj devem ser apresentados
        /// </summary>
        public bool ShowPoints
        {
            get { return _showPoints; }
            set { _showPoints = value; }
        }

        private bool _enabled;
        /// <summary>
        /// Se este obj está habilidado (deve ser exbido no gráfico)
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }


        private bool _legendEnabled;
        /// <summary>
        /// Se este obj deve ser apresentado na legenda
        /// </summary>
        public bool LegendEnabled
        {
            get => _legendEnabled;
            set => SetField(ref _legendEnabled, value);
        }

        #region Propriedades - Estatística

        /// <summary>
        /// Maior valor de X dos dados
        /// </summary>
        public double MaxX { get; set; }

        /// <summary>
        /// Menor valor de X dos dados
        /// </summary>
        public double MinX { get; set; }

        /// <summary>
        /// Maior valor de Y dos dados
        /// </summary>
        public double MaxY { get; set; }

        /// <summary>
        /// Menor valor de Y dos dados
        /// </summary>
        public double MinY { get; set; }

        /// <summary>
        /// Comprimento dos dados
        /// </summary>
        public double Widht { get; set; }

        /// <summary>
        /// Altura dos dados
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Valor médio em X dos dados
        /// </summary>
        public double MeanX { get; set; }

        /// <summary>
        /// Valor médio em Y dos dados
        /// </summary>
        public double MeanY { get; set; }

        /// <summary>
        /// Desvio padrão dos dados
        /// </summary>
        public double StandardDeviation { get; set; }

        #endregion

        public GraphObj()
        {

        }

        public GraphObj(string id, double strokeThickness, bool showPoints, bool enabled, bool legendEnabled = true, string xMeasureUnity = "", string yMeasureUnity = "", string additionalData = "")
        {
            Id = id;
            StrokeThickness = strokeThickness;
            ShowPoints = showPoints;
            Enabled = enabled;
            LegendEnabled = legendEnabled;
            XMeasureUnity = xMeasureUnity;
            YMeasureUnity = yMeasureUnity;
            AdditionalData = additionalData;
        }

        /// <summary>
        /// Cria os desenhos dos dados e aciona a lista fornecedida
        /// </summary>
        public abstract void Draw(List<UIElement> canvasChildrens, Func<Point, Point> GetViewPointFromData);

        /// <summary>
        /// Criar os desenhos dos dados em um PDF
        /// </summary>
        public abstract void DrawInPDF(XGraphics gfx, Func<Point, Point> GetViewPointFromData);

        /// <summary>
        /// Atualiza as propriedades geométricas deste conjunto de dados
        /// </summary>
        public void RefreshGeometryData()
        {
            ViewPoints.Clear();

            for (int i = 0; i < OriginalPoints.Count; i++)
            {
                Point point = OriginalPoints[i];
                ViewPoints.Add(new Point(point.X * ViewScale.X, point.Y * ViewScale.Y));
            }

            double sumY = 0;
            double sumX = 0;

            MaxX = double.MinValue;
            MinX = double.MaxValue;
            MaxY = double.MinValue;
            MinY = double.MaxValue;

            try
            {
                foreach (Point point in ViewPoints)
                {
                    sumY += point.Y;
                    sumX += point.X;

                    MaxX = Math.Max(MaxX, point.X);
                    MinX = Math.Min(MinX, point.X);
                    MaxY = Math.Max(MaxY, point.Y);
                    MinY = Math.Min(MinY, point.Y);
                }

                MeanY = sumY / ViewPoints.Count;
                MeanX = sumX / ViewPoints.Count;

                double sdDenominator = 0;
                foreach (Point point in ViewPoints)
                {
                    sdDenominator += Math.Pow(point.X - MeanY, 2);
                }

                StandardDeviation = Math.Sqrt(sdDenominator / ViewPoints.Count);

                Widht = Math.Abs(MaxX - MinX);
                Height = Math.Abs(MaxY - MinY);
            }
            catch (Exception)
            {

            }

           
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Uma linha a ser desenhada no <see cref="LucasLauriHelpers.customControls.GraphView"/>
    /// </summary>
    [Serializable]
    public class GraphLine : GraphObj
    {
        /// <summary>
        /// Cor do traço destes pontos
        /// </summary>
        public Color Color { get; set; } = Colors.Red;

        public GraphLine() : base() { }

        public GraphLine (string  id, Color color, bool enabled, bool legendEnabled = true, double strokeThickness = 1, bool showPoints = false, string xMeasureUnity = "", string yMeasureUnity = "", string additionalData = "") : base(id, strokeThickness, showPoints, enabled, legendEnabled, xMeasureUnity, yMeasureUnity, additionalData)
        {
            Color = color;
        }

        public override void Draw(List<UIElement> canvasChildrens, Func<Point, Point> GetViewPointFromData)
        {
            Polyline pLine = new Polyline();
            PointCollection collection = new PointCollection();
            List<Ellipse> points = new List<Ellipse>();


            for (int i = 0; i < OriginalPoints.Count; i++)
            {
                Point point = OriginalPoints[i];
                Point viewPoint = GetViewPointFromData(new Point(point.X, point.Y));
                collection.Add(viewPoint);

                if (!ShowPoints)
                    continue;

                Ellipse pointEllipse = new Ellipse();
                pointEllipse.Width = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Height = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Margin = new Thickness(viewPoint.X - pointEllipse.Width / 2.0, viewPoint.Y - pointEllipse.Height / 2.0, 0, 0);
                pointEllipse.Fill = new SolidColorBrush(Color);

                points.Add(pointEllipse);
            }

            pLine.Points = collection;
            pLine.StrokeThickness = StrokeThickness;
            pLine.Stroke = new SolidColorBrush(Color);

            canvasChildrens.Add(pLine);//Dados do gráfico
            canvasChildrens.AddRange(points);
        }

        public override void DrawInPDF(XGraphics gfx, Func<Point, Point> GetViewPointFromData)
        {
            XColor xColor = XColor.FromArgb(Color.A, Color.R, Color.G, Color.B);

            List<XPoint> points = new List<XPoint>();
            List<Ellipse> ellipses = new List<Ellipse>();

            for (int i = 0; i < OriginalPoints.Count; i++)
            {
                Point point = OriginalPoints[i];
                Point viewPoint = GetViewPointFromData(new Point(point.X, point.Y));
                points.Add(new XPoint(viewPoint.X, viewPoint.Y));

                if (!ShowPoints)
                    continue;

                Ellipse pointEllipse = new Ellipse();
                pointEllipse.Width = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Height = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Margin = new Thickness(viewPoint.X - pointEllipse.Width / 2.0, viewPoint.Y - pointEllipse.Height / 2.0, 0, 0);

                ellipses.Add(pointEllipse);
            }


            gfx.DrawLines(new XPen(xColor, StrokeThickness), points.ToArray());

            if (ShowPoints)
                foreach(Ellipse ellipse in ellipses)
                {
                    gfx.DrawEllipse(new XSolidBrush(xColor), 
                        new XRect
                        (
                            ellipse.Margin.Left, ellipse.Margin.Top, ellipse.Width, ellipse.Height
                        ));
                }
        }
    }


    /// <summary>
    /// Uma região a ser desenhada no <see cref="LucasLauriHelpers.customControls.GraphView"/>
    /// </summary>
    [Serializable]
    public abstract class IGraphRegion : GraphObj
    {
        /// <summary>
        /// Cor de preenchimento da região
        /// </summary>
        public Color RegionColor { get; set; } = Colors.Red;

        /// <summary>
        /// Cor da borda da região
        /// </summary>
        public Color StokeColor { get; set; } = Colors.Purple;

        public IGraphRegion() : base() { }

        public IGraphRegion(string id, Color regionColor, Color stokeColor, bool enabled, bool legendEnabled = true, double strokeThickness = 1, bool showPoints = false, string additionalData = "") : base(id, strokeThickness, showPoints, enabled, legendEnabled, additionalData)
        {
            RegionColor = regionColor;
            StokeColor = stokeColor;
        }
    }


    /// <summary>
    /// Uma região a ser desenhada no <see cref="LucasLauriHelpers.customControls.GraphView"/>
    /// </summary>
    [Serializable]
    public class GraphFilledRegion : IGraphRegion
    {
        public GraphFilledRegion() : base() { }

        public GraphFilledRegion(string id, Color regionColor, Color stokeColor, bool enabled, bool legendEnabled = true, double strokeThickness = 1, bool showPoints = false, string additionalData = "")
            : base(id, regionColor, stokeColor, enabled, legendEnabled, strokeThickness, showPoints, additionalData)
        {
            RegionColor = regionColor;
            StokeColor = stokeColor;
        }

        public override void Draw(List<UIElement> canvasChildrens, Func<Point, Point> GetViewPointFromData)
        {
            Polyline pLine = new Polyline();
            PointCollection collection = new PointCollection();
            List<Ellipse> points = new List<Ellipse>();

            foreach (Point point in OriginalPoints)
            {
                Point viewPoint = GetViewPointFromData(new Point(point.X, point.Y));
                collection.Add(viewPoint);

                if (!ShowPoints)
                    continue;

                Ellipse pointEllipse = new Ellipse();
                pointEllipse.Width = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Height = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Margin = new Thickness(viewPoint.X - pointEllipse.Width / 2.0, viewPoint.Y - pointEllipse.Height / 2.0, 0, 0);
                pointEllipse.Fill = new SolidColorBrush(StokeColor);

                points.Add(pointEllipse);
            }

            pLine.Points = collection;
            pLine.StrokeThickness = StrokeThickness;
            pLine.Stroke = new SolidColorBrush(StokeColor);
            pLine.Fill = new SolidColorBrush(RegionColor);

            canvasChildrens.Add(pLine);//Dados do gráfico
            canvasChildrens.AddRange(points);//Dados do gráfico
        }

        public override void DrawInPDF(XGraphics gfx, Func<Point, Point> GetViewPointFromData)
        {
            XColor xStrokeColor = XColor.FromArgb(StokeColor.A, StokeColor.R, StokeColor.G, StokeColor.B);
            XColor xFillColor = XColor.FromArgb(RegionColor.A, RegionColor.R, RegionColor.G, RegionColor.B);

            List<XPoint> points = new List<XPoint>();
            List<Ellipse> ellipses = new List<Ellipse>();

            for (int i = 0; i < OriginalPoints.Count; i++)
            {
                Point point = OriginalPoints[i];
                Point viewPoint = GetViewPointFromData(new Point(point.X, point.Y));
                points.Add(new XPoint(viewPoint.X, viewPoint.Y));

                if (!ShowPoints)
                    continue;

                Ellipse pointEllipse = new Ellipse();
                pointEllipse.Width = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Height = StrokeThickness * PointWidhtMultiplier;
                pointEllipse.Margin = new Thickness(viewPoint.X - pointEllipse.Width / 2.0, viewPoint.Y - pointEllipse.Height / 2.0, 0, 0);
            }


            gfx.DrawLines(new XPen(xStrokeColor, StrokeThickness), points.ToArray());
            gfx.DrawClosedCurve(new XSolidBrush(xFillColor), points.ToArray(), XFillMode.Winding, 0);

            if (ShowPoints)
                foreach (Ellipse ellipse in ellipses)
                {
                    gfx.DrawEllipse(new XSolidBrush(xStrokeColor),
                        new XRect
                        (
                            ellipse.Margin.Left, ellipse.Margin.Top, ellipse.Width, ellipse.Height
                        ));
                }
        }
    }

    /// <summary>
    /// Uma região circular a ser desenhada no <see cref="LucasLauriHelpers.customControls.GraphView"/>
    /// </summary>
    [Serializable]
    public class GraphPoinstRegion : IGraphRegion
    {
        /// <summary>
        /// Tolerancia utiliza para criar a região
        /// </summary>
        public Point Tolerance { get; set; }

        public GraphPoinstRegion() : base() { }

        public GraphPoinstRegion(string id, Point tolerance, Color regionColor, Color stokeColor, bool enabled, bool legendEnabled = true, double strokeThickness = 1, bool showPoints = false, string additionalData = "") 
            : base(id, regionColor, stokeColor, enabled, legendEnabled, strokeThickness, showPoints, additionalData)
        {
            Tolerance = tolerance;
            RegionColor = regionColor;
            StokeColor = stokeColor;
        }

        public override void Draw(List<UIElement> canvasChildrens, Func<Point, Point> GetViewPointFromData)
        {
            List<Ellipse> regions = new List<Ellipse>();

            for (int i = 0; i < OriginalPoints.Count; i++)
            {
                Point point = OriginalPoints[i];
                Point viewPoint = GetViewPointFromData(new Point(point.X, point.Y));

                Point tolerancePoint = GetViewPointFromData(new Point(point.X * (1 + Tolerance.X), point.Y * (1 + Tolerance.Y)));
                Point regionDiameter = new Point((viewPoint.X - tolerancePoint.X) * 2.0, (viewPoint.Y - tolerancePoint.Y) * 2.0);

                Ellipse region = new Ellipse();
                region.Width = Math.Abs(regionDiameter.X);
                region.Height = Math.Abs(regionDiameter.Y);
                region.Margin = new Thickness(viewPoint.X - region.Width / 2.0, viewPoint.Y - region.Height / 2.0, 0, 0);
                region.Fill = new SolidColorBrush(RegionColor);

                regions.Add(region);
            }

            canvasChildrens.AddRange(regions);//Regiões
        }

        public override void DrawInPDF(XGraphics gfx, Func<Point, Point> GetViewPointFromData)
        {
            XColor xFillColor = XColor.FromArgb(RegionColor.A, RegionColor.R, RegionColor.G, RegionColor.B);

            List<Ellipse> regions = new List<Ellipse>();
            
            for (int i = 0; i < OriginalPoints.Count; i++)
            {
                Point point = OriginalPoints[i];
                Point viewPoint = GetViewPointFromData(new Point(point.X, point.Y));

                Point tolerancePoint = GetViewPointFromData(new Point(point.X * (1 + Tolerance.X), point.Y * (1 + Tolerance.Y)));
                Point regionDiameter = new Point((viewPoint.X - tolerancePoint.X) * 2.0, (viewPoint.Y - tolerancePoint.Y) * 2.0);

                Ellipse regionEllipse = new Ellipse();
                regionEllipse.Width = Math.Abs(regionDiameter.X);
                regionEllipse.Height = Math.Abs(regionDiameter.Y);
                regionEllipse.Margin = new Thickness(viewPoint.X - regionEllipse.Width / 2.0, viewPoint.Y - regionEllipse.Height / 2.0, 0, 0);

                regions.Add(regionEllipse);
            }

            foreach (Ellipse region in regions)
            {
                gfx.DrawEllipse(new XSolidBrush(xFillColor),
                    new XRect
                    (
                        region.Margin.Left, region.Margin.Top, region.Width, region.Height
                    ));
            }
        }
    }
}
