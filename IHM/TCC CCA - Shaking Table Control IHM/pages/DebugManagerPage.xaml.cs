using LucasLauriHelpers.src;
using LucasLauriHelpers.windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using TCC_CCA___Shaking_Table_Control_IHM.src;
using TCC_CCA___Shaking_Table_Control_IHM;

namespace LucasLauriHelpers.pages
{
    /// <summary>
    /// Interaction logic for DebugManager.xaml
    /// </summary>
    public partial class DebugManagerPage : Page, INotifyPropertyChanged
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

        public Program Program { get; set; } = ((MainWindow)Application.Current.MainWindow).Program;

        private bool _expanded;
        /// <summary>
        /// Se o DebugManager está visível (expandido = true) ou não (false)
        /// </summary>
        public bool Expanded
        {
            get => _expanded;
            set => SetField(ref _expanded, value);
        }

        private Visibility _overlayVisibility = Visibility.Collapsed;
        /// <summary>
        /// Visibilidade do screenOverlay aplicado ao <see cref="MainWindow"/> quando o DebugManger está expandido
        /// </summary>
        public Visibility OverlayVisibility
        {
            get => _overlayVisibility;
            set => SetField(ref _overlayVisibility, value);
        }


        private double _debuggerHeight = 40;
        /// <summary>
        /// My property summary
        /// </summary>
        public double DebuggerHeight
        {
            get => _debuggerHeight;
            set => SetField(ref _debuggerHeight, value);
        }


        private double _debuggerOpacity = 0.5;
        /// <summary>
        /// Opacidade do debugger
        /// </summary>
        public double DebuggerOpacity
        {
            get => _debuggerOpacity;
            set => SetField(ref _debuggerOpacity, value);
        }

        public DebugManagerPage()
        {

            InitializeComponent();

            Program.DebugManagerPage = this;
        }

        /// <summary>
        /// Exibe ou esconde o DebugManager
        /// </summary>
        public void ToggleDebugManagerVisibility()
        {
            if (Expanded)
            {
                Logger.LogMessage("Debug manager fechado!", Logger.MessageLogTypes.Warning);

                DebuggerHeight = 40;
                DebuggerOpacity = 0.5;

                OverlayVisibility = Visibility.Collapsed;

                Expanded = false;
            }
            else
            {
                Logger.LogMessage("Debug manager aberto!", Logger.MessageLogTypes.Warning);

                DebuggerHeight = double.NaN;
                DebuggerOpacity = 1;

                OverlayVisibility = Visibility.Visible;

                Expanded = true;
            }
        }

        private void BtnDebug_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleDebugManagerVisibility();
        }

        private void BtnShowLogs_Click(object sender, RoutedEventArgs e)
        {
            LogsViewer.Start();
        }

        private void BtnDebug_Click(object sender, RoutedEventArgs e)
        {
            //List<string> points = new List<string>();

            //foreach (MeasurePoint measurePoint in Program.MeasurePoints)
            //    points.Add($"{measurePoint.Center.X}; {measurePoint.Center.Y}");

            //File.WriteAllLines($"{PathDevFolder}findedCenters - {DateTime.Now:FFFFFFF}.csv", points);
        }

    }
}
