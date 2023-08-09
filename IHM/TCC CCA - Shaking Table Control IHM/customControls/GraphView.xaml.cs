using LucasLauriHelpers.src;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using TCC_CCA___Shaking_Table_Control_IHM;
using TCC_CCA___Shaking_Table_Control_IHM.src;
using static TCC_CCA___Shaking_Table_Control_IHM.src.Program;

namespace LucasLauriHelpers.customControls
{
    /// <summary>
    /// Interaction logic for GraphView.xaml
    /// </summary>
    public partial class GraphView : UserControl, INotifyPropertyChanged
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
        /// Estados possíveis para o mouse
        /// </summary>
        public enum MouseStates { Idle, InCanvas, MouseDown }

        public static DependencyProperty GraphDatasProperty =
             DependencyProperty.Register("GraphDatas", typeof(List<GraphObj>),
             typeof(GraphView),
             new FrameworkPropertyMetadata(new List<GraphObj>(),
                                    FrameworkPropertyMetadataOptions.AffectsRender,
                                    new PropertyChangedCallback(GraphDatasChanged))
                 );

        /// <summary>
        /// Handle da mudança da lista de gráficos
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void GraphDatasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GraphView obj = (GraphView)d;


            CollectionViewSource collectionView = new CollectionViewSource();
            collectionView.Source = obj.GraphDatas;

            obj.GraphDatasView = collectionView.View;
            //GraphDatasView.SortDescriptions.Add(new SortDescription("TimeStamp", ListSortDirection.Descending));

            //GraphDatasView.Filter = obj =>
            //{
            //    LogItem p = obj as LogItem;
            //    return s_LogsViewer.LogItems.IndexOf(p) > s_LogsViewer.LogItems.Count - s_LogsViewer.MaxNumberOfLogs - 1
            //        && s_LogsViewer.MessageTypesView[(int)p.MessageType].Checked;
            //};
        }

        /// <summary>
        /// Dados a serem apresentados no gráfico
        /// </summary>
        public List<GraphObj> GraphDatas
        {
            get { return (List<GraphObj>)GetValue(GraphDatasProperty); }
            set { SetValue(GraphDatasProperty, value);}
        }

        public static DependencyProperty ViewScaleProperty =
             DependencyProperty.Register("ViewScale", typeof(Point),
             typeof(GraphView),
             new FrameworkPropertyMetadata(new Point(1, 1),
                                    FrameworkPropertyMetadataOptions.AffectsRender)
                 );

        /// <summary>
        /// Escala customizada aplicada a visualização dos dados
        /// </summary>
        public Point ViewScale
        {
            get { return (Point)GetValue(ViewScaleProperty); }
            set { SetValue(ViewScaleProperty, value); }
        }


        public static DependencyProperty IsExpandedProperty =
             DependencyProperty.Register("IsExpanded", typeof(bool),
             typeof(GraphView),
             new FrameworkPropertyMetadata(false,
                                    FrameworkPropertyMetadataOptions.AffectsRender)
         );

        /// <summary>
        /// Escala customizada aplicada a visualização dos dados
        /// </summary>
        public bool IsExpanded
        {
            get { return (bool)GetValue(ViewScaleProperty); }
            set { SetValue(ViewScaleProperty, value); }
        }


        private ICollectionView _graphDatasView;
        /// <summary>
        /// View da lista <see cref="GraphDatas"/>
        /// </summary>
        public ICollectionView GraphDatasView
        {
            get => _graphDatasView;
            set => SetField(ref _graphDatasView, value);
        }

        public Program Program { get; set; } = ((MainWindow)Application.Current.MainWindow).Program;

        /// <summary>
        /// Timer responsável por atualizar os dados do gráfico
        /// </summary>
        public Timer UpdaterTimer { get; set; } = new Timer(50);

        /// <summary>
        /// Se o método <see cref="RefreshViews"/> esta sendo executado. Bool utilizado como um "semaforo"
        /// </summary>
        public bool RefreshingView { get; set; }

        /// <summary>
        /// Se é a primeira vez que o método <see cref="RefreshViews"/> é utilizado.
        /// </summary>
        public bool FirstViewRefresh { get; set; } = true;

        /// <summary>
        /// Passo com que o zoom é alterado
        /// </summary>
        public double ZoomStep { get; set; } = 10;

        /// <summary>
        /// Passo com que o offset é alterado
        /// </summary>
        public double OffsetStep { get; set; } = 0.1;

        /// <summary>
        /// Tempo em que o ultimo click foi feito, utilizado para reconhecer duplo click 
        /// </summary>
        public DateTime LastMouseClickDate { get; set; }

        private MouseStates _mouseState = MouseStates.Idle;
        /// <summary>
        /// Estado atual do mouse
        /// </summary>
        public MouseStates MouseState
        {
            get => _mouseState;
            set
            {
                if (value == MouseStates.MouseDown && MouseState == MouseStates.Idle)
                    InitialMousePosition = Mouse.GetPosition(GraphCanvas);

                SetField(ref _mouseState, value);
            }
        }

        private Point _mouseDataPoint;
        /// <summary>
        /// Ponto do mouse no espaço de dados
        /// </summary>
        public Point MouseDataPoint
        {
            get => _mouseDataPoint;
            set => SetField(ref _mouseDataPoint, value);
        }

        private Point _initialMousePosition;
        /// <summary>
        /// Posição inicial do mouse no inico do drag
        /// </summary>
        public Point InitialMousePosition
        {
            get => _initialMousePosition;
            set => SetField(ref _initialMousePosition, value);
        }

        private bool _forceViewRefresh;
        /// <summary>
        /// Se a atualização da view deve ser forçada
        /// </summary>
        public bool ForceViewRefresh
        {
            get => _forceViewRefresh;
            set => SetField(ref _forceViewRefresh, value);
        }

        private TimeSpan _refreshViewsDeltaTime;
        /// <summary>
        /// Tempo necessário para executar o método <see cref="RefreshViews"/>
        /// </summary>
        public TimeSpan RefreshViewsDeltaTime
        {
            get => _refreshViewsDeltaTime;
            set => SetField(ref _refreshViewsDeltaTime, value);
        }

        private Point _tickStep;
        /// <summary>
        /// Tamanho de cada tick do grafico
        /// </summary>
        public Point TickStep
        {
            get => _tickStep;
            set => SetField(ref _tickStep, value);
        }

        private Point _scale = new Point(1,1);
        /// <summary>
        /// Escala aplicada ao gráfico
        /// </summary>
        public Point Scale
        {
            get => _scale;
            set => SetField(ref _scale, value);
        }

        private Point _offset;
        /// <summary>
        /// Offset, no espaço de dados, aplicado ao gráfico
        /// </summary>
        public Point Offset
        {
            get => _offset;
            set => SetField(ref _offset, value);
        }

        private double[] _xDataRange;
        /// <summary>
        /// Range do <see cref="GraphDatas"/> no eixo X
        /// </summary>
        public double[] XDataRange
        {
            get => _xDataRange;
            set => SetField(ref _xDataRange, value);
        }

        private double[] _yDataRange;
        /// <summary>
        /// Range do <see cref="GraphDatas"/> no eixo Y
        /// </summary>
        public double[] YDataRange
        {
            get => _yDataRange;
            set => SetField(ref _yDataRange, value);
        }

        private double _dataWidht;
        /// <summary>
        /// Comprimento dos dados (<see cref="XDataRange[1]"/> - <see cref="XDataRange[1]"/>)
        /// </summary>
        public double DataWidht
        {
            get => _dataWidht;
            set => SetField(ref _dataWidht, value);
        }

        private double _dataHeight;
        /// <summary>
        /// Altura dos dados (<see cref="YDataRange[1]"/> - <see cref="YDataRange[1]"/>)
        /// </summary>
        public double DataHeight
        {
            get => _dataHeight;
            set => SetField(ref _dataHeight, value);
        }

        private Rect _graphROI;
        /// <summary>
        /// Região de interese do gráfico
        /// </summary>
        public Rect GraphROI
        {
            get => _graphROI;
            set => SetField(ref _graphROI, value);
        }

        private Visibility _grahpOptionsVisibility = Visibility.Collapsed;
        /// <summary>
        /// Visibilidade as opções do gráfico (click esquerdo do mouse)
        /// </summary>
        public Visibility GrahpOptionsVisibility
        {
            get => _grahpOptionsVisibility;
            set => SetField(ref _grahpOptionsVisibility, value);
        }

        private Thickness _graphOptionsMargin = new Thickness();
        /// <summary>
        /// Margin do menu de opções
        /// </summary>
        public Thickness GraphOptionsMargin
        {
            get => _graphOptionsMargin;
            set => SetField(ref _graphOptionsMargin, value);
        }

        private bool _isDataMenuExpanded;
        /// <summary>
        /// Se o menu de seleção de dados está expandido
        /// </summary>
        public bool IsDataMenuExpanded
        {
            get => _isDataMenuExpanded;
            set => SetField(ref _isDataMenuExpanded, value);
        }

        private int _graphDatasCount;
        /// <summary>
        /// Count da lista <see cref="GraphDatas"/>
        /// </summary>
        public int GraphDatasCount
        {
            get => _graphDatasCount;
            set => SetField(ref _graphDatasCount, value);
        }

        public GraphView()
        {
            //TODO: Colocar ajuda no gráfico?
            //TODO: Ao dar zoom com o scroll do mouse centralizar zoom no mouse

            InitializeComponent();

            UpdaterTimer.Elapsed += UpdaterTimer_Elapsed;
            UpdaterTimer.Start();
        }



        private void UpdaterTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Program == null)
            {
                this.Dispatcher.Invoke(() => {
                    Program = ((MainWindow)Application.Current.MainWindow).Program;
                });
                return;
            }

            if (Program.MainWindow == null)
                return;

            if (IsDataMenuExpanded)
                return;

            UpdaterTimer.Stop();

            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    RefreshViews();
                });
            }
            catch (TaskCanceledException ex)
            {
                //Este rrro ocorre ao finalizar programa enquanto este método é executado, desconsiderar
            }
            catch (InvalidOperationException ex)
            {
                //Erro ocorre ao editar os pontos do gráfico, desconsiderar
            }


            UpdaterTimer.Start();
        }

        /// <summary>
        /// Atualiza a visualização do gráfico
        /// </summary>
        public void RefreshViews()
        {
            if (GraphDatas == null)
            {
                GraphCanvas.Children.Clear();
                GraphDatasCount = 0;
                return;
            }

            int visibleGraphs = 0;

            foreach(GraphObj graphObj in GraphDatas)
            {
                if (!graphObj.Enabled) continue;
                visibleGraphs++;
            }

            GraphDatasCount = visibleGraphs;

            if (GraphDatasCount == 0 || GraphCanvas.ActualHeight == 0 || GraphCanvas.ActualWidth == 0 )
            {
                GraphCanvas.Children.Clear();
                return;
            }

            if (RefreshingView)
                return;

            RefreshingView = true;


            foreach (GraphObj data in GraphDatas)
            {
                if (!data.Enabled)
                    continue;

                if (data.ViewScale.X != ViewScale.X || data.ViewScale.X != ViewScale.X)
                {
                    data.ViewScale = ViewScale;
                    data.RefreshGeometryData();
                }
            }

            if (FirstViewRefresh)
            {
                FirstViewRefresh = false;
                CalculateGraphROI(fitGraph: true);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<UIElement> canvasChildrens = new List<UIElement>();

            bool axesDrawned = false;

            foreach (GraphObj graphObj in GraphDatas)
            {
                if (graphObj.ForceRecenter)
                {
                    CalculateGraphROI(fitGraph: true, distorceGraph: true);
                    ForceViewRefresh = true;
                    break;
                }
            }

            for (int i = 0; i < GraphDatas.Count; i++)
            {
                GraphObj data = GraphDatas[i];
                if (!data.Enabled)
                    continue;

                if (!data.NeedsViewUpdate && !ForceViewRefresh || data.Widht == 0 || data.Height == 0)
                    continue;

                if (!axesDrawned)
                {
                    DrawAxes(canvasChildrens); //Desenha os eixos
                    axesDrawned = true;
                }
                
                //Desenha os dados do gráfico
                data.Draw(canvasChildrens, GetViewPointFromData);

                data.NeedsViewUpdate = false;
            }

            if (canvasChildrens.Count > 0)
            {
                GraphCanvas.Children.Clear();

                for (int i = 0; i < canvasChildrens.Count; i++)
                {
                    UIElement children = canvasChildrens[i];
                    GraphCanvas.Children.Add(children);
                }

                RefreshViewsDeltaTime = stopwatch.Elapsed;
            }


            ForceViewRefresh = false;
            RefreshingView = false;

            stopwatch.Stop();
        }

        /// <summary>
        /// Desenha os eixos do gráfico
        /// </summary>
        /// <param name="canvasChildrens"></param>
        private void DrawAxes(List<UIElement> canvasChildrens)
        {
            double dataWidht = XDataRange[1] - XDataRange[0];
            double dataHeight = YDataRange[1] - YDataRange[0];

            if (dataWidht == 0 || dataHeight == 0)
                return;

            //foreach (GraphObj data in GraphDatas)
            //{
            //    if (!data.Enabled)
            //        continue;

            //    if (!data.NeedsViewUpdate && !ForceViewRefresh)
            //        continue;

            //    //Dados do gráfico
            //    data.Draw(canvasChildrens, GetViewPointFromData);

            //    data.NeedsViewUpdate = false;
            //}

            int numOfTicks = 5;
            Point newTickStep = new Point(GraphCanvas.ActualWidth / Scale.X / numOfTicks, GraphCanvas.ActualHeight / Scale.Y / numOfTicks);

            if (newTickStep.X <= 0 || newTickStep.Y <= 0)
            {
//#if DEBUG
//                Debugger.Break();
//#endif
//                Logger.LogMessage($"O {nameof(TickStep)} encontrado ({TickStep}) é inválido!", Logger.MessageLogTypes.Error);

//                return;
            }
            else
                TickStep = newTickStep;

            double tickWidhtPercent = 0.025;  ///Porcentagem do canvas que o tick irá preencher
            double tickWidht = Math.Min(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight) * tickWidhtPercent;

            Point rangeMultiplication = new Point(1 + 2 * TickStep.X / dataWidht, 1 + 2 * TickStep.Y / dataHeight); //Multiplicador de range para criar os eixos

            #region Linhas do plano XY

            bool[] axesOutOfBounds = IsPointOutOfBounds(new Point(0, 0));

            Point axesLeftPoint = GetViewPointFromData(new Point(XDataRange[0] < 0 ? XDataRange[0] * rangeMultiplication.X : 0, 0));
            Point axesRightPoint = GetViewPointFromData(new Point(XDataRange[1] * rangeMultiplication.X, 0));
            Point axesBottomPoint = GetViewPointFromData(new Point(0, YDataRange[0] < 0 ? YDataRange[0] * rangeMultiplication.Y : 0));
            Point axesTopPoint = GetViewPointFromData(new Point(0, YDataRange[1] * rangeMultiplication.Y));

            //Linhas do plano cartesiano
            canvasChildrens.Add(GetGraphBodyLine(axesLeftPoint, axesRightPoint, Colors.Black)); //Abscissas
            canvasChildrens.Add(GetGraphBodyLine(axesBottomPoint, axesTopPoint, Colors.Black)); //Ordenadas

            #endregion

            #region Ticks dos eixos XY

            for (double x = TickStep.X; x <= XDataRange[1] * rangeMultiplication.X; x += TickStep.X)
            {
                canvasChildrens.Add(GetGraphBodyLine(
                    GetViewPointFromData(new Point(x, YDataRange[0] * rangeMultiplication.Y)),
                    GetViewPointFromData(new Point(x, YDataRange[1] * rangeMultiplication.Y)), Colors.LightGray)); //Grid X +
                canvasChildrens.Add(GetXTickLine(x, tickWidht, axesOutOfBounds)); //Linha dos ticks (graduação) das abscissas +
                canvasChildrens.Add(GetXTickText(x, tickWidht, axesOutOfBounds)); //Texto dos ticks (graduação) das abscissas +
            }

            for (double x = -TickStep.X; x >= XDataRange[0] * rangeMultiplication.X; x -= TickStep.X)
            {
                canvasChildrens.Add(GetGraphBodyLine(
                    GetViewPointFromData(new Point(x, YDataRange[0] * rangeMultiplication.Y)),
                    GetViewPointFromData(new Point(x, YDataRange[1] * rangeMultiplication.Y)), Colors.LightGray)); //Grid X -
                canvasChildrens.Add(GetXTickLine(x, tickWidht, axesOutOfBounds)); //Linha dos ticks (graduação) das abscissas -
                canvasChildrens.Add(GetXTickText(x, tickWidht, axesOutOfBounds)); //Texto dos ticks (graduação) das abscissas -
            }

            for (double y = TickStep.Y; y <= YDataRange[1] * rangeMultiplication.Y; y += TickStep.Y)
            {
                canvasChildrens.Add(GetGraphBodyLine(
                    GetViewPointFromData(new Point((XDataRange[0] > 0 ? 0 : XDataRange[0]) * rangeMultiplication.X, y)),
                    GetViewPointFromData(new Point(XDataRange[1] * rangeMultiplication.X, y)), Colors.LightGray)); //Grid Y +
                canvasChildrens.Add(GetYTickLine(y, tickWidht, axesOutOfBounds)); //Linha dos ticks (graduação) das ordenadas +
                canvasChildrens.Add(GetYTickText(y, tickWidht, axesOutOfBounds)); //Texto dos ticks (graduação) das ordenadas +
            }

            for (double y = -TickStep.Y; y >= YDataRange[0] * rangeMultiplication.Y; y -= TickStep.Y)
            {
                canvasChildrens.Add(GetGraphBodyLine(
                    GetViewPointFromData(new Point((XDataRange[0] > 0 ? 0 : XDataRange[0]) * rangeMultiplication.X, y)),
                    GetViewPointFromData(new Point(XDataRange[1] * rangeMultiplication.X, y)), Colors.LightGray)); //Grid Y -
                canvasChildrens.Add(GetYTickLine(y, tickWidht, axesOutOfBounds)); //Linha dos ticks (graduação) das ordenadas -
                canvasChildrens.Add(GetYTickText(y, tickWidht, axesOutOfBounds)); //Texto dos ticks (graduação) das ordenadas -
            }


            canvasChildrens.Add(GetXAxisText(axesRightPoint, axesOutOfBounds)); //Texto da Abscissas
            canvasChildrens.Add(GetYAxisText(axesTopPoint, axesOutOfBounds)); //Texto da Ordenada

            #endregion
        }

        /// <summary>
        /// Função auxiliar para criar o texto (unidades de medida) do eixo X 
        /// </summary>
        private TextBlock GetXAxisText(Point axesRightPoint, bool[] axesOutOfBounds)
        {
            TextBlock xText = new TextBlock();

            List<string> xUnitys = new List<string>();

            foreach(GraphObj graphObj in GraphDatas)
            {
                if (!graphObj.Enabled)
                    continue;

                if (graphObj.GetType() != typeof(GraphLine))
                    continue;

                GraphLine curve = (GraphLine) graphObj;
                
                if(!xUnitys.Contains(curve.XMeasureUnity) && !curve.XMeasureUnity.Equals(""))
                    xUnitys.Add(curve.XMeasureUnity);
            }

            string text = "";

            if(xUnitys.Count > 0)
            {
                text = "[";

                foreach (string xUnity in xUnitys)
                    text += $"{xUnity};";

                text = text.Remove(text.Length - 1); //Remove último ';'

                text += "]";
            }

            xText.Text = text;

            xText.RenderTransformOrigin = new Point(0.5, 0.5);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, -1)); //Corrige inversão do eixo Y do gráfico
            transformGroup.Children.Add(new RotateTransform(90));
            xText.RenderTransform = transformGroup;
            xText.Background = new SolidColorBrush(Colors.White);
            xText.Padding = new Thickness(2);

            Size textSize = Helpers.GetTextBlockSize(xText);
            double desirebleMargin = axesRightPoint.X - textSize.Width - 15;
            double xMargin = desirebleMargin < ActualWidth ? desirebleMargin : ActualWidth - textSize.Width - 15;

            if (!axesOutOfBounds[0] && !axesOutOfBounds[1]) //Eixo X dentro da parte visivel
            {
                xText.Margin = new Thickness(xMargin, axesRightPoint.Y, 0, 0);
            }
            else if (axesOutOfBounds[0]) //Eixo X abaixo da parte visivel
            {
                xText.Margin = new Thickness(xMargin, 2, 0, 0);
            }
            else //Eixo X acima da parte visivel
            {
                xText.Margin = new Thickness(xMargin, GraphCanvas.ActualHeight - textSize.Height - 10, 0, 0);
            }

            return xText;
        }


        /// <summary>
        /// Função auxiliar para criar o texto de um tick do eixo Y
        /// </summary>
        private TextBlock GetYAxisText(Point axesTopPoint, bool[] axesOutOfBounds)
        {
            TextBlock yText = new TextBlock();

            List<string> yUnitys = new List<string>();

            foreach (GraphObj graphObj in GraphDatas)
            {
                if (!graphObj.Enabled)
                    continue;

                if (graphObj.GetType() != typeof(GraphLine))
                    continue;

                GraphLine curve = (GraphLine)graphObj;

                if (!yUnitys.Contains(curve.YMeasureUnity) && !curve.YMeasureUnity.Equals(""))
                    yUnitys.Add(curve.YMeasureUnity);
            }

            string text = "";

            if (yUnitys.Count > 0)
            {
                text = "[";

                foreach (string xUnity in yUnitys)
                    text += $"{xUnity};";

                text = text.Remove(text.Length - 1); //Remove último ';'

                text += "]";
            }

            yText.Text = text;

            yText.RenderTransformOrigin = new Point(0.5, 0.5);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, -1)); //Corrige inversão do eixo Y do gráfico
            //transformGroup.Children.Add(new RotateTransform(90));
            yText.RenderTransform = transformGroup;
            yText.Background = new SolidColorBrush(Colors.White);
            yText.Padding = new Thickness(4);

            Size textSize = Helpers.GetTextBlockSize(yText);
            double desirebleMargin = axesTopPoint.Y + textSize.Height - 5;
            double yMargin = desirebleMargin < ActualHeight ? desirebleMargin : ActualHeight - textSize.Height - 15;

            if (!axesOutOfBounds[2] && !axesOutOfBounds[3]) //Eixo Y dentro da parte visivel
            {
                yText.Margin = new Thickness(axesTopPoint.X + 5, yMargin, 0, 0);
            }
            else if (axesOutOfBounds[2]) //Eixo Y a esquerda da parte visivel
            {
                yText.Margin = new Thickness(axesTopPoint.X > 0 ? +axesTopPoint.X + 5 : 0, yMargin, 0, 0);
            }
            else //Eixo Y a direita da parte visivel
            {
                yText.Margin = new Thickness(GraphCanvas.ActualWidth - textSize.Width - 5, yMargin, 0, 0);
            }


            return yText;
        }


        /// <summary>
        /// Função auxiliar para criar o texto de um tick do eixo X
        /// </summary>
        /// <param name="x">Cota com onde o texto será colocado</param>
        private TextBlock GetXTickText(double x, double tickWidht, bool[] axesOutOfBounds)
        {
            Point tickPoint = GetViewPointFromData(new Point(x, 0));

            TextBlock xText = new TextBlock();
            xText.Text = Math.Round((x / ViewScale.X), 3).ToString("F3");

            xText.RenderTransformOrigin = new Point(0.5, 0.5);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, -1)); //Corrige inversão do eixo Y do gráfico
            transformGroup.Children.Add(new RotateTransform(90));
            xText.RenderTransform = transformGroup;
            xText.Background = new SolidColorBrush(Colors.White);
            xText.Padding = new Thickness(2);

            double xMargin = tickPoint.X - Helpers.GetTextBlockSize(xText).Width / 2.0 - 2;

            if (!axesOutOfBounds[0] && !axesOutOfBounds[1]) //Eixo X dentro da parte visivel
            {
                xText.Margin = new Thickness(xMargin, tickPoint.Y + tickWidht / 2.0, 0, 0);
            }
            else if (axesOutOfBounds[0]) //Eixo X abaixo da parte visivel
            {
                xText.Margin = new Thickness(xMargin, tickWidht / 2.0, 0, 0);
            }
            else //Eixo X acima da parte visivel
            {
                xText.Margin = new Thickness(xMargin, GraphCanvas.ActualHeight - Helpers.GetTextBlockSize(xText).Height - tickWidht - 2, 0, 0);
            }

            return xText;
        }       

        /// <summary>
        /// Função auxiliar para criar o texto de um tick do eixo Y
        /// </summary>
        /// <param name="y">Cota com onde o texto será colocado</param>
        private TextBlock GetYTickText(double y, double tickWidht, bool[] axesOutOfBounds)
        {
            Point tickPoint = GetViewPointFromData(new Point(0, y));

            TextBlock yText = new TextBlock();
            yText.Text = Math.Round((y / ViewScale.X), 3).ToString("F3");

            yText.RenderTransformOrigin = new Point(0.5, 0.5);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, -1)); //Corrige inversão do eixo Y do gráfico
            //transformGroup.Children.Add(new RotateTransform(90));
            yText.RenderTransform = transformGroup;
            yText.Background = new SolidColorBrush(Colors.White);
            yText.Padding = new Thickness(4);

            double yMargin = tickPoint.Y - Helpers.GetTextBlockSize(yText).Height / 2.0 - 2;

            if (!axesOutOfBounds[2] && !axesOutOfBounds[3]) //Eixo Y dentro da parte visivel
            {
                yText.Margin = new Thickness(tickPoint.X + tickWidht / 2.0, yMargin, 0, 0);
            }
            else if (axesOutOfBounds[2]) //Eixo Y a esquerda da parte visivel
            {
                yText.Margin = new Thickness(tickWidht / 2.0, yMargin, 0, 0);
            }
            else //Eixo Y a direita da parte visivel
            {
                yText.Margin = new Thickness(GraphCanvas.ActualWidth - tickWidht - Helpers.GetTextBlockSize(yText).Width, yMargin, 0, 0);
            }


            return yText;
        }

        /// <summary>
        /// Função auxiliar para criar um tick do eixo X
        /// </summary>
        /// <param name="x">Cota x onde o tick será criado</param>
        private Line GetXTickLine(double x, double tickWidht, bool[] axesOutOfBounds)
        {
            Point tickTopPoint = GetViewPointFromData(new Point(x, 0));
            Point tickBottomPoint = GetViewPointFromData(new Point(x, 0));

            if (!axesOutOfBounds[0] && !axesOutOfBounds[1]) //Eixo X dentro da parte visivel
            {
                tickTopPoint.Offset(0, tickWidht / 2.0);
                tickBottomPoint.Offset(0, -tickWidht / 2.0);
            }
            else if (axesOutOfBounds[0]) //Eixo X abaixo da parte visivel
            {
                tickTopPoint = new Point(tickTopPoint.X, tickWidht / 2.0);
                tickBottomPoint = new Point(tickBottomPoint.X, 0);
            }
            else //Eixo X acima da parte visivel
            {
                tickTopPoint = new Point(tickTopPoint.X, GraphCanvas.ActualHeight - tickWidht);
                tickBottomPoint = new Point(tickBottomPoint.X, GraphCanvas.ActualHeight);
            }

            Line tickX = GetGraphBodyLine(tickTopPoint, tickBottomPoint, Colors.Black);

            return tickX;
        }

        /// <summary>
        /// Função auxiliar para criar um tick do eixo Y
        /// </summary>
        /// <param name="y">Cota y onde o tick será criado</param>
        private Line GetYTickLine(double y, double tickWidht, bool[] axesOutOfBounds)
        {
            Point tickLeftPoint = GetViewPointFromData(new Point(0, y));
            Point tickRightPoint = GetViewPointFromData(new Point(0, y));

            if (!axesOutOfBounds[2] && !axesOutOfBounds[3]) //Eixo Y dentro da parte visivel
            {
                tickLeftPoint.Offset(-tickWidht / 2.0, 0);
                tickRightPoint.Offset(tickWidht / 2.0, 0);
            }
            else if (axesOutOfBounds[2]) //Eixo Y a esquerda da parte visivel
            {
                tickLeftPoint = new Point(0, tickLeftPoint.Y);
                tickRightPoint = new Point(tickWidht / 2.0, tickRightPoint.Y);
            }
            else //Eixo Y a direita da parte visivel
            {
                tickLeftPoint = new Point(GraphCanvas.ActualWidth - tickWidht / 2.0, tickLeftPoint.Y);
                tickRightPoint = new Point(GraphCanvas.ActualWidth, tickLeftPoint.Y);
            }

            Line tickY = GetGraphBodyLine(tickLeftPoint, tickRightPoint, Colors.Black);

            return tickY;
        }

        /// <summary>
        /// Função auxiliar para criar a linha para o corpo do gráfico como os eixos e os ticks
        /// </summary>
        private Line GetGraphBodyLine(Point a, Point b, Color color)
        {
            Line tick = new Line();
            tick.X1 = a.X;
            tick.Y1 = a.Y;
            tick.X2 = b.X;
            tick.Y2 = b.Y;
            tick.Stroke = new SolidColorBrush(color);
            tick.StrokeThickness = 1;

            return tick;
        }

        /// <summary>
        /// Calcula o <see cref="GraphROI"/>
        /// </summary>
        private void CalculateGraphROI(bool fitGraph = false, bool distorceGraph = false)
        {
  
            double dataZoomOut = 0.05; //Porcentagem do zoom que será removida para que os dados não grudem nas bordas do gráfico

            //Maior escala para que o ponto de dado mais distante seja exibido na borda do gráfico (maior zoom sem ocultar qualquer ponto)
            Point maxScale = new Point(-1,-1);
            double undistorcedScale = -1;

            if (fitGraph)
                Offset = new Point(double.MaxValue, double.MaxValue);

            CalculateDataRanges();

            if (fitGraph)
            {
                double xScale = GraphCanvas.ActualWidth / DataWidht * (1 - dataZoomOut);
                double yScale = GraphCanvas.ActualHeight / DataHeight * (1 - dataZoomOut);

                undistorcedScale = Math.Max(undistorcedScale, Math.Min(xScale, yScale));

                maxScale = new Point
                (
                    Math.Max(maxScale.X, xScale),
                    Math.Max(maxScale.Y, yScale)
                );

                if (Double.IsInfinity(undistorcedScale) || undistorcedScale <= 0)
                    undistorcedScale = 1;

                if (Double.IsInfinity(maxScale.X) || maxScale.X <= 0 || Double.IsInfinity(maxScale.Y) || maxScale.Y <= 0)
                    maxScale = new Point(1,1);

                

                if (distorceGraph)
                {
                    Scale = new Point(maxScale.X, maxScale.Y);
                }
                else
                {
                    Scale = new Point(undistorcedScale, undistorcedScale);
                }

                Offset = new Point
                (
                    ((DataWidht - GraphCanvas.ActualWidth / Scale.X) / 2.0) + DataWidht * dataZoomOut,
                    ((DataHeight - GraphCanvas.ActualHeight / Scale.Y) / 2.0) + DataHeight * dataZoomOut
                );

            }

            GraphROI = new Rect(
                new Point(XDataRange[0] - Offset.X, YDataRange[0] - Offset.Y),
                new Point(XDataRange[1], YDataRange[1]));
        }

        /// <summary>
        /// Calcula os ranges dos dados em X (<see cref="XDataRange"/>) e Y (<see cref="YDataRange"/>)
        /// </summary>
        private void CalculateDataRanges()
        {
            XDataRange = new double[] { double.MaxValue, double.MinValue };
            YDataRange = new double[] { double.MaxValue, double.MinValue };

            for (int i = 0; i < GraphDatas.Count; i++)
            {
                GraphObj data = GraphDatas[i];
                if (!data.Enabled)
                    continue;

                data.RefreshGeometryData();

                XDataRange[0] = Math.Min(XDataRange[0], data.MinX);
                XDataRange[1] = Math.Max(XDataRange[1], data.MaxX);

                YDataRange[0] = Math.Min(YDataRange[0], data.MinY);
                YDataRange[1] = Math.Max(YDataRange[1], data.MaxY);
            }

            DataWidht = XDataRange[1] - XDataRange[0];
            DataHeight = YDataRange[1] - YDataRange[0];
        }

        /// <summary>
        /// Transforma um ponto no espaço de dados para um ponto no espaço de visualização de acordo com o <see cref="GraphROI"/>
        /// </summary>
        /// <param name="dataPoint">Ponto de dado a ser transformado</param>
        /// <returns>Ponto de dado tranformado para o espaço de visualização</returns>
        private Point GetViewPointFromData(Point dataPoint)
        {
            Rect viewRec = new Rect(0, 0, GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);

            double viewX = dataPoint.X;
            double viewY = dataPoint.Y;

            viewX = viewRec.Left + Scale.X * (viewX - GraphROI.Left);
            viewY = viewRec.Top + Scale.Y * (viewY - GraphROI.Top);

            Point viewPoint = new Point(
                viewX,
                viewY
                );

            return viewPoint;
        }

        /// <summary>
        /// Transforma um ponto no espaço de visualização para um ponto no espaço de dados de acordo com o <see cref="GraphROI"/>
        /// </summary>
        /// <param name="viewPoint">Ponto de dado a ser transformado</param>
        /// <returns>Ponto de visualização tranformado para o espaço de dados</returns>
        private Point GetDataPointFromView(Point viewPoint)
        {
            Rect viewRec = new Rect(0, 0, GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);

            double dataX = viewPoint.X;
            double dataY = viewPoint.Y;

            //viewX = viewRec.Left + Scale * (viewX - GraphROI.Left);
            //viewY = viewRec.Top + Scale * (viewY - GraphROI.Top);

            dataX = (viewPoint.X - viewRec.Left) / Scale.X + GraphROI.Left;
            dataY = (viewPoint.Y - viewRec.Top) / Scale.Y + GraphROI.Top;

            Point dataPoint = new Point(
                dataX,
                dataY
                );

            return dataPoint;
        }

        /// <summary>
        /// Verifica se um ponto de dado esta fora da região de visualização em X ou em Y.
        /// </summary>
        /// <param name="dataPoint"></param>
        /// <returns>bool[]{X a baixo, X a acima, Y a esquerd, Y a direita}</returns>
        private bool[] IsPointOutOfBounds(Point dataPoint)
        {
            Rect viewRec = new Rect(0, 0, GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);

            Point viewPoint = GetViewPointFromData(dataPoint);
            bool[] outBounds = new bool[] 
            { 
                viewPoint.Y < viewRec.Top, 
                viewPoint.Y > viewRec.Bottom, 
                viewPoint.X < viewRec.Left, 
                viewPoint.X > viewRec.Right 
            };
            return outBounds;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateGraphROI(fitGraph: true);
            ForceViewRefresh = true;
        }

        #region Métodos de controle do gráfico pelos botões

        private void BtnZoomMinus_Click(object sender, RoutedEventArgs e)
        {
            Point newScale = new Point(Scale.X - ZoomStep, Scale.Y - ZoomStep);

            Scale = Scale.X > 0 && Scale.Y > 0 ? newScale : Scale;

            ForceViewRefresh = true;
        }

        private void BtnZoomPlus_Click(object sender, RoutedEventArgs e)
        {
            Point newScale = new Point(Scale.X + ZoomStep, Scale.Y + ZoomStep);
            Scale = newScale;

            ForceViewRefresh = true;
        }

        private void BtnFit_Click(object sender, RoutedEventArgs e)
        {
            CalculateGraphROI(fitGraph: true);
            ForceViewRefresh = true;
        }

        private void BtnOffsetXMinus_Click(object sender, RoutedEventArgs e)
        {
            Offset = new Point(Offset.X - OffsetStep, Offset.Y);
            CalculateGraphROI();
            ForceViewRefresh = true;
        }

        private void BtnOffsetXPlus_Click(object sender, RoutedEventArgs e)
        {
            Offset = new Point(Offset.X + OffsetStep, Offset.Y);
            CalculateGraphROI();
            ForceViewRefresh = true;
        }

        private void BtnOffsetYMinus_Click(object sender, RoutedEventArgs e)
        {
            Offset = new Point(Offset.X, Offset.Y - OffsetStep);
            CalculateGraphROI();
            ForceViewRefresh = true;
        }

        private void BtnOffsetYPlus_Click(object sender, RoutedEventArgs e)
        {
            Offset = new Point(Offset.X, Offset.Y + OffsetStep);
            CalculateGraphROI();
            ForceViewRefresh = true;
        }

        #endregion

        private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double deltaZoom = e.Delta / 10;

            Point newScale = new Point(Scale.X - deltaZoom, Scale.Y - deltaZoom);

            Scale = newScale.X > 0 && newScale.Y > 0 ? newScale : Scale;

            ForceViewRefresh = true;

            GrahpOptionsVisibility = Visibility.Collapsed;
        }

        private void GraphCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsDataMenuExpanded = false;

            if (GraphDatas == null) return;
            if (GraphDatas.Count == 0) return;

            #region Botão direito do mouse

            if (e.RightButton == MouseButtonState.Pressed && GraphDatasCount > 0)
            {
                Point mousePoint = Mouse.GetPosition(GraphCanvas);

                GraphOptionsMargin = new Thickness(mousePoint.X + 15, GraphCanvas.ActualHeight - mousePoint.Y, 0, 0);

                GrahpOptionsVisibility = Visibility.Visible;
                return;
            }

            #endregion

            #region Botão esquerdo do mouse

            GrahpOptionsVisibility = Visibility.Collapsed;
            InitialMousePosition = Mouse.GetPosition(GraphCanvas);

            MouseState = MouseStates.MouseDown;

            #endregion
        }

        private void MouseUp_GraphCanvas(object sender, MouseButtonEventArgs e)
        {
            MouseState = MouseStates.InCanvas;
        }

        private void MouseEnter_GraphCanvas(object sender, MouseEventArgs e)
        {
            MouseState = MouseStates.InCanvas;
        }

        private void MouseLeave_GraphCanvas(object sender, MouseEventArgs e)
        {
            LastMouseClickDate = DateTime.MinValue;
            MouseState = MouseStates.Idle;

            //GrahpOptionsVisibility = Visibility.Collapsed;
        }

        private void MouseMove_GraphCanvas(object sender, MouseEventArgs e)
        {
            if (GraphDatas == null)
                return;

            if (GraphDatas.Count == 0)
                return;

            Point mousePos = Mouse.GetPosition(GraphCanvas);

            Point dummyMouseDataPoint = GetDataPointFromView(mousePos);
            MouseDataPoint = new Point(dummyMouseDataPoint.X / ViewScale.X, dummyMouseDataPoint.Y / ViewScale.Y);

            if (MouseState != MouseStates.MouseDown)
                return;

            Point mousePosInData = GetDataPointFromView(mousePos);
            Point InitialMousePositionInData = GetDataPointFromView(InitialMousePosition);

            Point newOffset = new Point(
                Offset.X - (InitialMousePositionInData.X - mousePosInData.X),
                Offset.Y - (InitialMousePositionInData.Y - mousePosInData.Y));

            if (Math.Abs(newOffset.X - Offset.X) > 0.01 || Math.Abs(newOffset.Y - Offset.Y) > 0.01)
            {
                InitialMousePosition = mousePos;
                Offset = newOffset;
                Program.MainWindow.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        CalculateGraphROI();
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                });
                ForceViewRefresh = true;
            }
        }

        private void BtnFitData_Click(object sender, RoutedEventArgs e)
        {
            if (GraphDatas == null)
                return;

            try
            {
                CalculateGraphROI(fitGraph: true, distorceGraph: true);
                ForceViewRefresh = true;
            }
            catch (InvalidOperationException ex)
            {

            }

            GrahpOptionsVisibility = Visibility.Collapsed;
        }


        private void BtnFitSqueraData_Click(object sender, RoutedEventArgs e)
        {
            if (GraphDatas == null)
                return;

            CalculateGraphROI(fitGraph: true, distorceGraph: false);
            ForceViewRefresh = true;

            GrahpOptionsVisibility = Visibility.Collapsed;
        }

        private void ViewCheckBox_Click(object sender, RoutedEventArgs e)
        {
            GraphObj graphObj = ((FrameworkElement)(sender)).DataContext as GraphObj;

            //graphObj.Enabled = !graphObj.Enabled;

            IsDataMenuExpanded = false;
            CalculateGraphROI(fitGraph: true, distorceGraph: true);
            ForceViewRefresh = true;
        }

        private void BtnExpand_MouseDown(object sender, MouseButtonEventArgs e)
        {
            s_Program.ToggleFocusGraph(GraphDatas);
        }

        private void BtnDownloadData_Click(object sender, RoutedEventArgs e)
        {
            GraphObj[] graphDatasBuffer = new GraphObj[GraphDatas.Count];

            GraphDatas.CopyTo(graphDatasBuffer);

            List<string> fileLines = new List<string>();

            string s = "t,";

            for (int i = 1; i < graphDatasBuffer.Length; i++)
            {
                s += $"{graphDatasBuffer[i].Id},";
            }

            fileLines.Add(s);

            for (int i = 1; i < graphDatasBuffer[1].OriginalPoints.Count; i++)
            {
                s = $"{GetCSVDouble(graphDatasBuffer[1].OriginalPoints[i].X)}, {GetCSVDouble(graphDatasBuffer[1].OriginalPoints[i].Y)},";

                for(int j = 2; j < graphDatasBuffer.Length; j++)
                {
                    s += $"{GetCSVDouble(graphDatasBuffer[j].OriginalPoints[i].Y)}, ";
                }

                fileLines.Add(s);
            }

            

            File.WriteAllLines($@"D:\.UFRGS\.TCC\graphDown{DateTime.Now.Microsecond}.csv", fileLines);
        }

        /// <summary>
        /// Obtem um double formatado para o CSV de dados
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetCSVDouble(double value)
        {
            return value.ToString("F5").Replace(",", ".");
        }
    }

}
