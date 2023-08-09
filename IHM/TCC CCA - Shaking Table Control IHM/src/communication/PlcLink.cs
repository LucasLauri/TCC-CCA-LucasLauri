using LucasLauriHelpers.src;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;

namespace TCC_CCA___Shaking_Table_Control_IHM.src.communication
{
    public class PlcLink : INotifyPropertyChanged
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
        /// Estados possíveis para o link com o PLC
        /// </summary>
        public enum CommStates
        {
            [Description("Iniciando comunicação")]
            Starting,
            [Description("Aguardando...")]
            Idle,
            [Description("Enviando dados")]
            Receiving,
            [Description("Recebendo dados")]
            Sending,
            [Description("Comunicação finalizada")]
            Killed
        };

        /// <summary>
        /// Ip do PLC
        /// </summary>
        public static string PlcIp { get; } = "192.168.1.5";

        /// <summary>
        /// Porta do PLC
        /// </summary>
        public static int PlcPort { get; } = 3200;


        /// <summary>
        /// O IP do computador
        /// </summary>
        public static string PCIp { get; } = "192.168.1.10";

        /// <summary>
        /// Porta do PC
        /// </summary>
        public static int PCPort { get; } = 3201;

        /// <summary>
        /// Máximo tempo que a IHM ficará sem enviar dados ao PLC. Utilizado para sinal de vida IHM -> PLC
        /// </summary>
        public TimeSpan MaxTimeWithoutSend { get; set; } = TimeSpan.FromMilliseconds(1000);

        /// <summary>
        /// Timer responsável pela cadencia da comunicação com o PLC
        /// </summary>
        public Timer CommTimer { get; set; } = new Timer(1000);

        /// <summary>
        /// Cliente para comunicação TCP/IP com o PLC
        /// </summary>
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// Stream da comunicação TCP/IP com o PLC
        /// </summary>
        public NetworkStream TcpStream { get; set; }

        /// <summary>
        /// Tabela de entrada do TCP
        /// </summary>
        public TCPInputDataTable TCPInputDataTable { get; set; } = new TCPInputDataTable();

        /// <summary>
        /// Tabela de saída do TCP
        /// </summary>
        public TCPOutputDataTable TCPOutputDataTable { get; set; } = new TCPOutputDataTable();

        /// <summary>
        /// Bytes recebidos do PLC
        /// </summary>
        public byte[] TcpInputArray { get; set; } = new byte[500];


        private bool _isConnected;
        /// <summary>
        /// Se o PLC esta conectado a IHM
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set => SetField(ref _isConnected, value);
        }

        private CommStates _commState;
        /// <summary>
        /// Estado atual do link com o PLC
        /// </summary>
        public CommStates CommState
        {
            get => _commState;
            set => SetField(ref _commState, value);
        }

        private DateTime _lastCommCycleDate;
        /// <summary>
        /// Ultima data de execução do <see cref="CommTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        public DateTime LastCommCycleDate
        {
            get => _lastCommCycleDate;
            set => SetField(ref _lastCommCycleDate, value);
        }

        private DateTime _lastReceivedDate;
        /// <summary>
        /// Data em que o ultimo <see cref="TCPInputDataTable.GetTableValues(byte[])"/> foi executado pelo <see cref="HandleReceivingState"/>
        /// </summary>
        public DateTime LastReceivedDate
        {
            get => _lastReceivedDate;
            set => SetField(ref _lastReceivedDate, value);
        }

        private DateTime _lastSendDate;
        /// <summary>
        /// Data em que o ultimo <see cref="HandleSendingState"/> foi executado
        /// </summary>
        public DateTime LastSendDate
        {
            get => _lastSendDate;
            set => SetField(ref _lastSendDate, value);
        }

        private int _receivedSize;
        /// <summary>
        /// Número de bytes recebidos do PLC
        /// </summary>
        public int ReceivedSize
        {
            get => _receivedSize;
            set => SetField(ref _receivedSize, value);
        }

        public PlcLink()
        {
            IPAddress ipAddress = IPAddress.Parse(PCIp);
            IPEndPoint ipLocalEndPoint = new IPEndPoint(ipAddress, PCPort);
            TcpClient = new TcpClient(ipLocalEndPoint);

            CommTimer.Elapsed += CommTimer_Elapsed;
            CommTimer.Start();
        }

        private void CommTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            CommTimer.Stop();

            IsConnected = CommState != CommStates.Starting && CommState != CommStates.Killed;

            switch (CommState)
            {
                case CommStates.Starting:
                    HandleStartingState();
                    break;
                case CommStates.Idle:
                    HandleIdleState();
                    break;
                case CommStates.Receiving:
                    HandleReceivingState();
                    break;
                case CommStates.Sending:
                    HandleSendingState();
                    break;
                case CommStates.Killed:
                    HandleKilledState();
                    break;
                default:
#if DEBUG
                    Debugger.Break();
#endif
                    Logger.LogMessage($"{nameof(CommState)} '{CommState}' não reconhecido!", Logger.MessageLogTypes.Error);

                    break;
            }

            LastCommCycleDate = DateTime.Now;

            CommTimer.Start();
        }

        /// <summary>
        /// Trata o estado <see cref="CommStates.Starting"/> no método <see cref="CommTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        private void HandleStartingState()
        {
            Logger.LogMessage($"Iniciando a comunicação com o PLC...", Logger.MessageLogTypes.Debug);

            try
            {
                TcpClient.Connect(IPAddress.Parse(PlcIp), PlcPort);

                if (!TcpClient.Connected)
                {
                    Logger.LogMessage($"Não foi possível se conectar com o PLC, tentando novamente em {CommTimer.Interval} ms...", Logger.MessageLogTypes.Debug);
                }
                else
                {

                    TcpStream = TcpClient.GetStream();

                    CommTimer.Interval = 2;

                    CommState = CommStates.Idle;

                    Logger.LogMessage($"Comunicação com o PLC iniciada.", Logger.MessageLogTypes.Info);
                }
            }
            catch (Exception ex)
            {
                if (TcpStream != null)
                    TcpStream.Close();
            }

        }

        /// <summary>
        /// Trata o estado <see cref="CommStates.Idle"/> no método <see cref="CommTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        private void HandleIdleState()
        {
            int timeComp = LastSendDate.Add(MaxTimeWithoutSend).CompareTo(DateTime.Now);

            if (TCPOutputDataTable.NeedsWrite || TCPOutputDataTable.FirstSend || timeComp < 0)
                CommState = CommStates.Sending;
            else
                CommState = CommStates.Receiving;
        }

        /// <summary>
        /// Trata o estado <see cref="CommStates.Receiving"/> no método <see cref="CommTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        private void HandleReceivingState()
        {

            try
            {
                ReceivedSize = TcpStream.Read(TcpInputArray, 0, TcpInputArray.Length);
            }
            catch
            {
                if (TcpStream != null)
                    TcpStream.Close();

                CommState = CommStates.Starting;
                return;
            }

            if (ReceivedSize == TcpInputArray.Length && TcpInputArray[0] == 0xAA && TcpInputArray.Last() == 0xBB)
            {
                TCPInputDataTable.GetTableValues(TcpInputArray);
                LastReceivedDate = DateTime.Now;
            }
            else
            {
#if DEBUG
                //Debugger.Break();
#endif
            }

            CommState = CommStates.Idle;
        }

        /// <summary>
        /// Trata o estado <see cref="CommStates.Sending"/> no método <see cref="CommTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        private void HandleSendingState()
        {
            byte[] tcpOutputArray = TCPOutputDataTable.WriteTableData();

            TcpStream.Write(tcpOutputArray, 0, tcpOutputArray.Length);

            LastSendDate = DateTime.Now;

            CommState = CommStates.Idle;
        }

        /// <summary>
        /// Trata o estado <see cref="CommStates.Killed"/> no método <see cref="CommTimer_Elapsed(object?, ElapsedEventArgs)"/>
        /// </summary>
        private void HandleKilledState()
        {
            Logger.LogMessage("A comunicação com o CNC foi finalizada.", Logger.MessageLogTypes.Warning);
            CommTimer.Stop();
        }
    }
}
