using LucasLauriHelpers.src;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TCC_CCA___Shaking_Table_Control_IHM.src;

namespace TCC_CCA___Shaking_Table_Control_IHM.pages
{
    /// <summary>
    /// Tipos de ondas implementadas
    /// </summary>
    public enum WaveTypes
    {
        [Description("Senoidal")]
        Sinusoidal,
        [Description("Quadrada")]
        Square
    }


    /// <summary>
    /// Interaction logic for OperationPage.xaml
    /// </summary>
    public partial class OperationPage : Page, INotifyPropertyChanged
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
        /// Ordem das curvas em <see cref="PositionGraphData"/>
        /// </summary>
        public enum PositionCurvesOrder
        {
            [Description("Obj para fixar o zoom do gráfico")]
            Dummy,
            [Description("Posição desejada para a mesa")]
            TargetPosition,
            [Description("Posição atual da mesa")]
            Position
        };

        /// <summary>
        /// Ordem das curvas em <see cref="ControlGraphData"/>
        /// </summary>
        public enum ControlCurverOrder
        {
            [Description("Obj para fixar o zoom do gráfico")]
            Dummy,
            [Description("Parcela proporcional do controle")]
            PValue,
            [Description("Parcela integral do controle")]
            IValue,
            [Description("Parcela derivativa do controle")]
            DValue,
            [Description("Sinal de controle")]
            UValue
        }

        public Program Program { get; set; } = ((MainWindow)Application.Current.MainWindow).Program;

        /// <summary>
        /// Timer para atualizar o gráfico 
        /// </summary>
        public Timer UpdateGraphTimer { get; set; } = new Timer(1000 / 30.0);

        private List<GraphObj> _positionGraphData = new List<GraphObj>();
        /// <summary>
        /// Dados de posição atualmente sendo apresentados no gráfico
        /// </summary>
        public List<GraphObj> PositionGraphData
        {
            get => _positionGraphData;
            set => SetField(ref _positionGraphData, value);
        }

        private List<GraphObj> _controlGraphData = new List<GraphObj>();
        /// <summary>
        /// Dados de controle atualmente sendo apresentados no gráfico
        /// </summary>
        public List<GraphObj> ControlGraphData
        {
            get => _controlGraphData;
            set => SetField(ref _controlGraphData, value);
        }

        private TimeSpan _updateGraphTime;
        /// <summary>
        /// Tempo necessário para executar o métdodo <see cref="UpdateGraphTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        public TimeSpan UpdateGraphTime
        {
            get => _updateGraphTime;
            set => SetField(ref _updateGraphTime, value);
        }

        private WaveTypes _targetWave;
        /// <summary>
        /// Tipo de onda de referência
        /// </summary>
        public WaveTypes TargetWave
        {
            get => _targetWave;
            set => SetField(ref _targetWave, value);
        }
        private float _targetAmplitude = 20f;
        /// <summary>
        /// Amplitude desejada para a onda de referência
        /// </summary>
        public float TargetAmplitude
        {
            get => _targetAmplitude;
            set => SetField(ref _targetAmplitude, value);
        }

        private float _targetFrequency = 1f;
        /// <summary>
        /// Frequência desejada para a onda de referência
        /// </summary>
        public float TargetFrequency
        {
            get => _targetFrequency;
            set => SetField(ref _targetFrequency, value);
        }

        public OperationPage()
        {
            InitializeComponent();

            Program.OperationPage = this;

            GraphObj dummyObj = new GraphLine("Dummy", Colors.Transparent, enabled: true);
            dummyObj.OriginalPoints.Add(new Point(0, -55));
            dummyObj.OriginalPoints.Add(new Point(65536 / 1000.0, 55));
            dummyObj.NeedsViewUpdate = true;

            GraphObj targePositionCurve = new GraphLine("Posição desejada", Colors.Yellow, enabled: true);
            GraphObj positionCurve = new GraphLine("Posição", Colors.Blue, enabled: true);
            PositionGraphData.Add(dummyObj);
            PositionGraphData.Add(targePositionCurve);
            PositionGraphData.Add(positionCurve);

            dummyObj = new GraphLine("Dummy", Colors.Transparent, enabled: true);
            dummyObj.OriginalPoints.Add(new Point(0, -24));
            dummyObj.OriginalPoints.Add(new Point(65536 / 1000.0, 24));
            dummyObj.NeedsViewUpdate = true;

            GraphObj pCurve = new GraphLine("Parcela proporcional", Colors.Red, enabled: true);
            GraphObj iCurve = new GraphLine("Parcela integral", Colors.Green, enabled: true);
            GraphObj dCurve = new GraphLine("Parcela derivativa", Colors.Blue, enabled: true);
            GraphObj uCurve = new GraphLine("Sinal de controle", Colors.Purple, enabled: true);

            ControlGraphData.Add(dummyObj);
            ControlGraphData.Add(pCurve);
            ControlGraphData.Add(iCurve);
            ControlGraphData.Add(dCurve);
            ControlGraphData.Add(uCurve);

            UpdateGraphTimer.Elapsed += UpdateGraphTimer_Elapsed;
            UpdateGraphTimer.Start();
        }

        private void UpdateGraphTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!Program.PlcLink.IsConnected) return;


            UpdateGraphTimer.Stop();

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                foreach (GraphData graphData in Program.PlcLink.TCPInputDataTable.GraphDatas)
                {
                    if (graphData == null) continue;

                    Point positionPoint = new Point(graphData.Timestamp / 1000.0, graphData.Position);
                    Point targetPosition = new Point(positionPoint.X, graphData.TargetPosition);

                    Point pValue = new Point(positionPoint.X, graphData.PValue);
                    Point iValue = new Point(positionPoint.X, graphData.IValue);
                    Point dValue = new Point(positionPoint.X, graphData.DValue);
                    Point uValue = new Point(positionPoint.X, graphData.UValue);

                    if (PositionGraphData[(int)PositionCurvesOrder.Position].OriginalPoints.Count == 0)
                    {
                        PositionGraphData[(int)PositionCurvesOrder.Position].OriginalPoints.Add(positionPoint);
                        PositionGraphData[(int)PositionCurvesOrder.TargetPosition].OriginalPoints.Add(targetPosition);

                        ControlGraphData[(int)ControlCurverOrder.PValue].OriginalPoints.Add(pValue);
                        ControlGraphData[(int)ControlCurverOrder.IValue].OriginalPoints.Add(iValue);
                        ControlGraphData[(int)ControlCurverOrder.DValue].OriginalPoints.Add(dValue);
                        ControlGraphData[(int)ControlCurverOrder.UValue].OriginalPoints.Add(uValue);
                        continue;
                    }

                    Point lastPoint = PositionGraphData[(int)PositionCurvesOrder.Position].OriginalPoints.Last();
                    double deltaT = positionPoint.X - lastPoint.X;

                    if (graphData.Timestamp <= 1000)
                    {
                        for (int i = 1; i < PositionGraphData.Count; i++)
                            PositionGraphData[i].OriginalPoints.Clear();

                        for (int i = 1; i < ControlGraphData.Count; i++)
                            ControlGraphData[i].OriginalPoints.Clear();
                    }

                    if (deltaT > 0)
                    {
                        PositionGraphData[(int)PositionCurvesOrder.Position].OriginalPoints.Add(positionPoint);
                        PositionGraphData[(int)PositionCurvesOrder.TargetPosition].OriginalPoints.Add(targetPosition);

                        ControlGraphData[(int)ControlCurverOrder.PValue].OriginalPoints.Add(pValue);
                        ControlGraphData[(int)ControlCurverOrder.IValue].OriginalPoints.Add(iValue);
                        ControlGraphData[(int)ControlCurverOrder.DValue].OriginalPoints.Add(dValue);
                        ControlGraphData[(int)ControlCurverOrder.UValue].OriginalPoints.Add(uValue);
                    }
                }

                for (int i = 1; i < PositionGraphData.Count; i++)
                    PositionGraphData[i].NeedsViewUpdate = true;

                for (int i = 1; i < ControlGraphData.Count; i++)
                    ControlGraphData[i].NeedsViewUpdate = true;
            }
            catch (TaskCanceledException ex)
            {
                //Este rrro ocorre ao finalizar programa enquanto este método é executado, desconsiderar
            }
            catch (InvalidOperationException ex)
            {
                //Erro ocorre ao editar os pontos do gráfico, desconsiderar
            }

            UpdateGraphTimer.Start();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Program.PlcLink.TCPOutputDataTable.CmdStart = true;
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Program.PlcLink.TCPOutputDataTable.CmdStop = true;
        }

        private void BtnRefAxes_Click(object sender, RoutedEventArgs e)
        {
            Program.PlcLink.TCPOutputDataTable.CmdRef = true;
        }

        private void RadioSinosoidal_Click(object sender, RoutedEventArgs e)
        {
            TargetWave = WaveTypes.Sinusoidal;
        }

        private void RadioSquare_Click(object sender, RoutedEventArgs e)
        {
            TargetWave = WaveTypes.Square;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            Program.PlcLink.TCPOutputDataTable.CmdReset = true;
        }
    }

    /// <summary>
    /// Converter para binding de radio buttons (https://stackoverflow.com/questions/9212873/binding-radiobuttons-group-to-a-property-in-wpf)
    /// </summary>
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? parameter : Binding.DoNothing;

        }
    }
}