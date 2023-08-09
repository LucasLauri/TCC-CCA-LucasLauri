using LucasLauriHelpers.src;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TCC_CCA___Shaking_Table_Control_IHM;
using TCC_CCA___Shaking_Table_Control_IHM.src;
using static LucasLauriHelpers.src.Alarm;

namespace LucasLauriHelpers.pages
{

    /// <summary>
    /// Estado da visualização dos alarmes
    /// </summary>
    public enum AlarmsViewerStates { None, AllRight, Message, Alarm }

    /// <summary>
    /// Interaction logic for AlarmsViewer.xaml
    /// </summary>
    public partial class AlarmsViewerPage : Page, INotifyPropertyChanged
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

        //TODO: Corrigir simbolo de alarme e de mensagem

        public static AlarmsViewerPage s_AlarmsViewerPage { get; set; }

        /// <summary>
        /// Margin aplicada quando a visualização dos alarmes está escondida
        /// </summary>
        static Thickness NoExpandedMargin { get; set; } = new Thickness(0, -300, 0, 0);

        public Program Program { get; set; } = ((MainWindow)Application.Current.MainWindow).Program;

        /// <summary>
        /// Tempo que o ultimo alarme recebico é exibido na tela
        /// </summary>
        public TimeSpan LastAlarmShowingTime { get; set; } = TimeSpan.FromMilliseconds(5000);

        /// <summary>
        /// Data em que o último alarme foi recebido
        /// </summary>
        public DateTime LastAlarmShowData { get; set; }

        /// <summary>
        /// Lista de alarmes ativos no CNC
        /// </summary>
        public List<Alarm> CurrentAlarms { get; set; } = new List<Alarm>();

        /// <summary>
        /// Timer responsável por pegar os alarmes da máquina
        /// </summary>
        public Timer TimerGetAlarms { get; set; } = new Timer(250);

        /// <summary>
        /// Timer responsável por "piscar" o estado atual do alarme
        /// </summary>
        public Timer TimerBlinkAlarmsState { get; set; } = new Timer(500);

        /// <summary>
        /// Timer responsável por exibir, por N segundos, o último alarme adicionado
        /// </summary>
        public Timer TimerShowLastAddedAlarm { get; set; } = new Timer(100);


        private double _percentShowingAlarmTime;
        /// <summary>
        /// Porcentagem do tempo da indicação de tempo sendo exibido
        /// </summary>
        public double PercentShowingAlarmTime
        {
            get => _percentShowingAlarmTime;
            set => SetField(ref _percentShowingAlarmTime, value);
        }


        private AlarmsViewerStates _alarmsViewerState = AlarmsViewerStates.None;
        /// <summary>
        /// Nivel atual dos alarmes
        /// </summary>
        public AlarmsViewerStates AlarmsViewerState
        {
            get => _alarmsViewerState;
            set => SetField(ref _alarmsViewerState, value);
        }


        private AlarmsViewerStates _alarmsViewerBlinkState = AlarmsViewerStates.None;
        /// <summary>
        /// View do nível atual do alarme, utilizado para "piscar" nivel atual
        /// </summary>
        public AlarmsViewerStates AlarmsViewerBlinkState
        {
            get => _alarmsViewerBlinkState;
            set => SetField(ref _alarmsViewerBlinkState, value);
        }


        private bool _expanded;
        /// <summary>
        /// Se os alarmes estão visiveis (expandido = true) ou não (false)
        /// </summary>
        public bool Expanded
        {
            get => _expanded;
            set => SetField(ref _expanded, value);
        }


        private Thickness _alarmsViewerMargin = NoExpandedMargin;
        /// <summary>
        /// Margem utilizada para exibir e esconder o AlarmeViewer
        /// </summary>
        public Thickness AlarmsViewerMargin
        {
            get => _alarmsViewerMargin;
            set => SetField(ref _alarmsViewerMargin, value);
        }


        private Visibility _overlayVisibility = Visibility.Collapsed;
        /// <summary>
        /// Visibilidade do screenOverlay aplicado ao <see cref="MainWindow"/> quando o AlarmwViewer está expandido
        /// </summary>
        public Visibility OverlayVisibility
        {
            get => _overlayVisibility;
            set => SetField(ref _overlayVisibility, value);
        }


        private ICollectionView _currentAlarmsView;
        /// <summary>
        /// View da lista <see cref="CurrentAlarms"/>
        /// </summary>
        public ICollectionView CurrentAlarmsView
        {
            get => _currentAlarmsView;
            set => SetField(ref _currentAlarmsView, value);
        }


        private Visibility _lastAlarmVisibility = Visibility.Collapsed;
        /// <summary>
        /// Visibilidade da exibição de alarme adicionado
        /// </summary>
        public Visibility LastAlarmVisibility
        {
            get => _lastAlarmVisibility;
            set => SetField(ref _lastAlarmVisibility, value);
        }


        private Alarm _lastAlarm = new Alarm(AlarmsIds.None, "", AlarmsLevels.Message, "");
        /// <summary>
        /// Último alarme recebido
        /// </summary>
        public Alarm LastAlarm
        {
            get => _lastAlarm;
            set => SetField(ref _lastAlarm, value);
        }

        public AlarmsViewerPage()
        {
            s_AlarmsViewerPage = this;
            Program.AlarmsViewerPage = this;

            TimerBlinkAlarmsState.Elapsed += TimerBlinkAlarmsState_Elapsed;
            TimerBlinkAlarmsState.Start();


            CollectionViewSource collectionView = new CollectionViewSource();
            collectionView.Source = CurrentAlarms;

            CurrentAlarmsView = collectionView.View;

            CurrentAlarmsView.SortDescriptions.Add(new SortDescription("TimeStamp", ListSortDirection.Descending));

            TimerShowLastAddedAlarm.Elapsed += TimerShowLastAddedAlarm_Elapsed;

            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TimerGetAlarms.Elapsed += TimerGetAlarms_Elapsed;
            TimerGetAlarms.Start();
        }

        private void TimerShowLastAddedAlarm_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerShowLastAddedAlarm.Stop();

            if (DateTime.Now.CompareTo(LastAlarmShowData.Add(LastAlarmShowingTime)) > 0)
            {
                LastAlarmVisibility = Visibility.Collapsed;
                return;
            }
            else
            {
                PercentShowingAlarmTime = LastAlarmShowData.Add(LastAlarmShowingTime).Subtract(DateTime.Now).TotalMilliseconds / LastAlarmShowingTime.TotalMilliseconds;
            }

            TimerShowLastAddedAlarm.Start();
        }


        private void TimerGetAlarms_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerGetAlarms.Stop();

            List<Alarm> alarms = GetActualAlarms(); //Alarmes atuais

            bool alarmsChanged = false;

            //Adiciona os alarmes novos
            for (int i = 0; i < alarms.Count; i++)
            {
                alarmsChanged = AddAlarm(alarms[i]);
            }

            //Remove os alarmes que não estão mais ativos 
            for (int i = 0; i < CurrentAlarms.Count; i++)
            {
                if (!alarms.Contains(CurrentAlarms[i]))
                    alarmsChanged = RemoveAlarm(CurrentAlarms[i]);
            }

            if (alarmsChanged)
                UpdateViews();

            TimerGetAlarms.Start();
        }

        /// <summary>
        /// Obtem os alarmes atuais do PLC
        /// </summary>
        /// <returns></returns>
        private List<Alarm> GetActualAlarms()
        {
            List<Alarm> actualAlarms = new List<Alarm>();

            if (!Program.PlcLink.IsConnected)
            {
                actualAlarms.Add(new Alarm(
                    AlarmsIds.NoComm,
                    "Sem comunicação com o PLC! Tentando reconectar...",
                    AlarmsLevels.Alarm,
                    helpText: $"Possíveis soluções:{Environment.NewLine}" +
                                $"  - Verifique as condições do cabo ethernet que conecta o computador com o PLC;{Environment.NewLine}" +
                                $"  - Certifique-se que o IP do computador é '192.168.1.10'."
                ));

                return actualAlarms;
            }

            if (Program.PlcLink.TCPInputDataTable.AlrmEmg)
                actualAlarms.Add(new Alarm(
                  AlarmsIds.Emg,
                  "Botão de emergência acionado!",
                  AlarmsLevels.Alarm,
                  helpText: "O botão de emergência foi acionado. Verifique e solucione o motivo pelo acionamento e então desative o botão de emergência e pressione o botão de reset."
              ));

            if (Program.PlcLink.TCPInputDataTable.AlrmRefTimeout)
                actualAlarms.Add(new Alarm(
                  AlarmsIds.RefTimeout,
                  "Não foi possível referenciar a mesa dentro do tempo máximo estipulado!",
                  AlarmsLevels.Alarm,
                  helpText: "Verifique o correto acionamento do motor e dos sensores de fim de curso e repita o processo."
              ));

            if (Program.PlcLink.TCPInputDataTable.AlrmPositiveHardwareLimit)
                actualAlarms.Add(new Alarm(
                  AlarmsIds.PositiveHardwareLimite,
                  "O sensor de limite de curso positivo foi acionado durante um ensaio!",
                  AlarmsLevels.Alarm,
                  helpText: "Verifique os parâmetros do ensaio, de contorle e o correto acionamento do motor e dos sensores de fim de curso e repita o ensaio."
              ));

            if (Program.PlcLink.TCPInputDataTable.AlrmNegativeHardwareLimit)
                actualAlarms.Add(new Alarm(
                  AlarmsIds.NegativeHardwareLimite,
                  "O sensor de limite de curso negativo foi acionado durante um ensaio!",
                  AlarmsLevels.Alarm,
                  helpText: "Verifique os parâmetros do ensaio, de contorle e o correto acionamento do motor e dos sensores de fim de curso e repita o ensaio."
              ));

            if (Program.PlcLink.TCPInputDataTable.MsgPositiveHardwareLimit)
                actualAlarms.Add(new Alarm(
                  AlarmsIds.MsgPositiveLimite,
                  "O sensor de limite de curso positivo esta acionado.",
                  AlarmsLevels.Message,
                  helpText: "Verifique o correto acionamento do motor e dos sensores de fim de curso e, se necessário, repita o processo em andamento."
              ));

            if (Program.PlcLink.TCPInputDataTable.MsgNegativeHardwareLimit)
                actualAlarms.Add(new Alarm(
                  AlarmsIds.MsgNegativeLimite,
                  "O sensor de limite de curso negativo esta acionado.",
                  AlarmsLevels.Message,
                  helpText: "Verifique o correto acionamento do motor e dos sensores de fim de curso e, se necessário, repita o processo em andamento."
              ));

            return actualAlarms;
        }

        private void TimerBlinkAlarmsState_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerBlinkAlarmsState.Stop();


            AlarmsViewerStates currentState = AlarmsViewerStates.AllRight;

            foreach (Alarm alarm in CurrentAlarms)
            {
                if (alarm.AlarmLevel == AlarmsLevels.Alarm)
                {
                    currentState = AlarmsViewerStates.Alarm;
                    break;
                }
                else if (alarm.AlarmLevel == AlarmsLevels.Message)
                {
                    currentState = AlarmsViewerStates.Message;
                }
            }

            AlarmsViewerState = currentState;

            if (AlarmsViewerBlinkState == AlarmsViewerStates.None)
            {
                switch (AlarmsViewerState)
                {
                    case AlarmsViewerStates.None:
                        AlarmsViewerBlinkState = AlarmsViewerStates.None;
                        break;
                    case AlarmsViewerStates.AllRight:
                        AlarmsViewerBlinkState = AlarmsViewerStates.AllRight;
                        break;
                    case AlarmsViewerStates.Message:
                        AlarmsViewerBlinkState = AlarmsViewerStates.Message;
                        break;
                    case AlarmsViewerStates.Alarm:
                        AlarmsViewerBlinkState = AlarmsViewerStates.Alarm;
                        break;
                }
            }
            else
            {
                switch (AlarmsViewerState)
                {
                    case AlarmsViewerStates.None:
                        AlarmsViewerBlinkState = AlarmsViewerStates.None;
                        break;
                    case AlarmsViewerStates.AllRight:
                        AlarmsViewerBlinkState = AlarmsViewerStates.AllRight;
                        break;
                    case AlarmsViewerStates.Message:
                        AlarmsViewerBlinkState = AlarmsViewerStates.None;
                        break;
                    case AlarmsViewerStates.Alarm:
                        AlarmsViewerBlinkState = AlarmsViewerStates.None;
                        break;
                }
            }

            TimerBlinkAlarmsState.Start();
        }

        /// <summary>
        /// Adiciona, se necessário, um novo alarme a lista <see cref="CurrentAlarms"/>
        /// </summary>
        /// <param name="newAlarm">Alarme a ser adicionado</param>
        private bool AddAlarm(Alarm newAlarm, bool updateViews = true)
        {
            if (!CurrentAlarms.Contains(newAlarm))
            {
                CurrentAlarms.Add(newAlarm);


                switch (newAlarm.AlarmLevel)
                {
                    case AlarmsLevels.Alarm:
                        Logger.LogMessage($"Alarme '{(int)newAlarm.Identifier} ({newAlarm.Identifier}) - {newAlarm.Text}' adicionado.", Logger.MessageLogTypes.Error);
                        break;
                    case AlarmsLevels.Message:
                        Logger.LogMessage($"Mensagem '{(int)newAlarm.Identifier} ({newAlarm.Identifier}) - {newAlarm.Text}' adicionada.", Logger.MessageLogTypes.Warning);
                        break;
                    default:
#if DEBUG
                        Debugger.Break(); //Indica erro de programação, "AlarmLevel" não implmentado
#endif
                        break;
                }

                if (!Expanded)
                {
                    LastAlarm = newAlarm;

                    PercentShowingAlarmTime = 1;
                    LastAlarmVisibility = Visibility.Visible;

                    LastAlarmShowData = DateTime.Now;
                    TimerShowLastAddedAlarm.Start();
                }

                if (updateViews)
                {
                    try
                    {
                        Program.MainWindow.Dispatcher.Invoke(() =>
                        {
                            UpdateViews();
                        });
                    }
                    catch (TaskCanceledException ex)
                    {
                        //TaskCanceledException ocorre ao fechar o programa, não tem problema pois o programa esta sendo fechado
                    }
                }
                return true;
            }

            return false;
        }


        /// <summary>
        /// Remove, se existir, o alarme da lista <see cref="CurrentAlarms"/>
        /// </summary>
        /// <param name="removeAlarm">Alarme a ser removido</param>
        private bool RemoveAlarm(Alarm removeAlarm, bool updateViews = true)
        {
            if (CurrentAlarms.Contains(removeAlarm))
            {
                Alarm alrmBuffer = new Alarm();
                foreach (Alarm alarm in CurrentAlarms)
                    if (alarm.Identifier == removeAlarm.Identifier)
                    {
                        alrmBuffer = alarm;
                        break;
                    }

                CurrentAlarms.Remove(removeAlarm);

                Logger.LogMessage($"Alarme/Mensagem '{alrmBuffer.Identifier} - {alrmBuffer.Text}' removido.", Logger.MessageLogTypes.Info);

                if (updateViews)
                {
                    try
                    {
                        Program.MainWindow.Dispatcher.Invoke(() =>
                        {
                            UpdateViews();
                        });
                    }
                    catch (TaskCanceledException ex)
                    {
                        //TaskCanceledException ocorre ao fechar o programa, não tem problema pois o programa esta sendo fechado
                    }

                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Retorna o pior nivel de alarme atual da máquina
        /// </summary>
        public AlarmsLevels GetWorstCurrentAlarmLevel()
        {
            AlarmsLevels worstLevel = AlarmsLevels.None;

            foreach (Alarm alarm in CurrentAlarms)
                if (worstLevel < alarm.AlarmLevel)
                    worstLevel = alarm.AlarmLevel;

            return worstLevel;
        }

        /// <summary>
        /// Ativa ou desativa, de acordo com o estado atual, a visibilidade do AlarmsViewer
        /// </summary>
        public void ToggleAlarmsViewerVisibility()
        {
            if (Expanded)
            {
                AlarmsViewerMargin = NoExpandedMargin;

                OverlayVisibility = Visibility.Collapsed;

                Expanded = false;
            }
            else
            {
                AlarmsViewerMargin = new Thickness(0, 0, 0, 0);

                OverlayVisibility = Visibility.Visible;
                Expanded = true;
            }

            foreach (Alarm alarm in CurrentAlarms)
                alarm.HelpVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Atualiza as views da tela
        /// </summary>
        public void UpdateViews()
        {
            try
            {
                Program.MainWindow.Dispatcher.Invoke(() =>
                {
                    CurrentAlarmsView.Refresh();
                });
            }
            catch (TaskCanceledException)
            {
                //Erro ao fechar o programa enquanto a task esta sendo executada. Não é problema pois, justamente, o programa será fechado 
            }
        }

        private void BtnAlamrs_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LastAlarmVisibility = Visibility.Collapsed;

            ToggleAlarmsViewerVisibility();
        }

        private void GridAlarm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Alarm alarm = (Alarm)((FrameworkElement)sender).DataContext;
            alarm.HelpVisibility = alarm.HelpVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            Alarm alarm = (Alarm)((FrameworkElement)sender).DataContext;
            alarm.HelpVisibility = alarm.HelpVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public class AlarmsIdsToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)((AlarmsIds)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
