using CsvHelper.Configuration;
using CsvHelper;
using LucasLauriHelpers.src;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Diagnostics;

namespace LucasLauriHelpers.windows
{
    /// <summary>
    /// Interaction logic for LogsViewer.xaml
    /// </summary>
    public partial class LogsViewer : Window, INotifyPropertyChanged
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

        public static LogsViewer s_LogsViewer { get; set; }

        /// <summary>
        /// Lista da seleção de tipos de mensagens visíveis
        /// </summary>
        public ObservableCollection<MessageTypesHolder> MessageTypesView { get; set; } = new ObservableCollection<MessageTypesHolder>();

        /// <summary>
        /// Timer responsável por ler o arquivo de log atual
        /// </summary>
        public Timer UpdateViewTimer { get; set; }

        /// <summary>
        /// Lista de logs obtidos do arquivo <see cref="Logger.CurrentLogFilePath"/>
        /// </summary>
        public List<LogItem> LogItems { get; set; } = new List<LogItem>();

        /// <summary>
        /// Data do último log
        /// </summary>
        public DateTime LastLogTime { get; set; }

        /// <summary>
        /// Se a janela foi fechada
        /// </summary>
        public bool WindowClosed { get; set; }

        private ICollectionView _logItemsView;
        /// <summary>
        /// View da lista <see cref="LogItems"/>
        /// </summary>
        public ICollectionView LogItemsView
        {
            get => _logItemsView;
            set => SetField(ref _logItemsView, value);
        }

        private int _maxNumberOfLogs = 500;
        /// <summary>
        /// Número de logs a serem exibidos
        /// </summary>
        public int MaxNumberOfLogs
        {
            get => _maxNumberOfLogs;
            set => SetField(ref _maxNumberOfLogs, value);
        }


        private bool _setTopMost = true;
        /// <summary>
        /// Se a janela de logs deve entrar em modo topmost
        /// </summary>
        public bool SetTopMost
        {
            get => _setTopMost;
            set
            {
                SetField(ref _setTopMost, value);

                Topmost = SetTopMost;
            }
        }

        private string _currentFile;
        /// <summary>
        /// Arquivo atualmente sendo usado de log
        /// </summary>
        public string CurrentFile
        {
            get => _currentFile;
            set => SetField(ref _currentFile, value);
        }

        private TimeSpan _updateViewElapsedTime;
        /// <summary>
        /// Tempo necessário para atualizar o viewer no ultimo tick
        /// </summary>
        public TimeSpan UpdateViewElapsedTime
        {
            get => _updateViewElapsedTime;
            set => SetField(ref _updateViewElapsedTime, value);
        }

        private DateTime _lastUpdateViewDate;
        /// <summary>
        /// Data do ultimo ciclo de atualização do viewer
        /// </summary>
        public DateTime LastUpdateViewDate
        {
            get => _lastUpdateViewDate;
            set => SetField(ref _lastUpdateViewDate, value);
        }


        public LogsViewer()
        {
            InitializeComponent();

            Topmost = true;

            string[] messageLogTypesNames = Enum.GetNames(typeof(Logger.MessageLogTypes));
            for (int i = 0; i < messageLogTypesNames.Length; i++)
            {
                MessageTypesView.Add(new MessageTypesHolder(messageLogTypesNames[i], true));
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            WindowClosed = true;
        }

        /// <summary>
        /// Inicia a página de visualização de logs criados pelo <see cref="Logger"/>
        /// </summary>
        public static void Start()
        {
            if (s_LogsViewer != null)
            {
                s_LogsViewer.Close();
            }

            Logger.LogMessage("LogsViewer aberto.", Logger.MessageLogTypes.Debug);

            s_LogsViewer = new LogsViewer();

            s_LogsViewer.DataContext = new { LogsViewer = s_LogsViewer };

            s_LogsViewer.CurrentFile = Logger.CurrentLogFilePath;

            s_LogsViewer.UpdateViewTimer = new Timer(500);
            s_LogsViewer.UpdateViewTimer.Elapsed += UpdateViewTimer_Elapsed;
            s_LogsViewer.UpdateViewTimer.Start();

            s_LogsViewer.Show();
        }

        private static void UpdateViewTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            s_LogsViewer.UpdateViewTimer.Stop();


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool needsRefresh = false;

            if (File.Exists(Logger.CurrentLogFilePath))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                    Delimiter = Logger.Delimiter,
                };

                try
                {
                    using (var reader = new StreamReader(s_LogsViewer.CurrentFile))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(new CsvHelper.TypeConversion.TypeConverterOptions { Formats = new[] { "dd/MM/yyyy HH:mm:ss.ffff" } });
                        List<LogItem> logs = csv.GetRecords<LogItem>().ToList();
                        if (s_LogsViewer.LastLogTime != logs.Last().TimeStamp)
                        {
                            s_LogsViewer.LastLogTime = logs.Last().TimeStamp;
                            s_LogsViewer.LogItems = logs;

                            needsRefresh = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //catch genérico, a view será taulizada no proximo tick do timer se necessário
                }
            }

            if (needsRefresh)
            {
                s_LogsViewer.UpdateViews();
            }

            stopwatch.Stop();
            s_LogsViewer.UpdateViewElapsedTime = stopwatch.Elapsed;

            s_LogsViewer.LastUpdateViewDate = DateTime.Now;

            if (s_LogsViewer.WindowClosed)
            {
                s_LogsViewer = null;
                return;
            }

            s_LogsViewer.UpdateViewTimer.Start();
        }

        /// <summary>
        /// Atualiza as views dessa página
        /// </summary>
        public void UpdateViews()
        {
            s_LogsViewer.Dispatcher.Invoke(() =>
            {
                //TODO: Verificar uso de memória desta view
                CollectionViewSource collectionView = new CollectionViewSource();
                collectionView.Source = s_LogsViewer.LogItems;

                s_LogsViewer.LogItemsView = collectionView.View;
                s_LogsViewer.LogItemsView.SortDescriptions.Add(new SortDescription("TimeStamp", ListSortDirection.Descending));

                s_LogsViewer.LogItemsView.Filter = obj =>
                {
                    LogItem p = obj as LogItem;
                    return s_LogsViewer.LogItems.IndexOf(p) > s_LogsViewer.LogItems.Count - s_LogsViewer.MaxNumberOfLogs - 1
                        && s_LogsViewer.MessageTypesView[(int)p.MessageType].Checked;
                };


            });
        }

        private void BtnChangeFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Choose log file to watch";
            op.Filter = "csv file|*.csv";
            if (op.ShowDialog() == true)
            {
                LogItems.Clear();
                s_LogsViewer.CurrentFile = op.FileName;
            }
        }
    }

    public class MessageTypesHolder : INotifyPropertyChanged
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

        private string _type;
        /// <summary>
        /// Nome do tipo de mensagem
        /// </summary>
        public string Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        private bool _checked;
        /// <summary>
        /// Se este tipo esta sendo visivel ou não
        /// </summary>
        public bool Checked
        {
            get => _checked;
            set
            {
                SetField(ref _checked, value);

                if (LogsViewer.s_LogsViewer != null)
                    LogsViewer.s_LogsViewer.UpdateViews();
            }
        }

        public MessageTypesHolder(string type, bool _checked)
        {
            Type = type;
            Checked = _checked;
        }

    }
}
