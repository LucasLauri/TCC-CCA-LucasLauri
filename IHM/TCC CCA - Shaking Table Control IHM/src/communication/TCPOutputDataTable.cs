using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static TCC_CCA___Shaking_Table_Control_IHM.src.Program;

namespace TCC_CCA___Shaking_Table_Control_IHM.src.communication
{
    public class TCPOutputDataTable : INotifyPropertyChanged
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
        /// Obj estático desta classe
        /// </summary>
        public static TCPOutputDataTable s_TCPOutputDataTable { get; set; }

        /// <summary>
        /// Se é o primeiro envio ao PLC
        /// </summary>
        public bool FirstSend { get; set; } = true;

        private bool _needsWrite;
        /// <summary>
        /// Se a escrita de variáveis é necessária
        /// </summary>
        public bool NeedsWrite
        {
            get => _needsWrite;
            set => SetField(ref _needsWrite, value);
        }

        private bool _heartBeat;
        /// <summary>
        /// Sinal de vida da IHM
        /// </summary>
        public bool HeartBeat
        {
            get => _heartBeat;
            set => SetField(ref _heartBeat, value);
        }

        private bool _cmdStart;
        /// <summary>
        /// Comando para dar start no ciclo de ensaio
        /// </summary>
        public bool CmdStart
        {
            get => _cmdStart;
            set
            {
                SetField(ref _cmdStart, value);
                NeedsWrite = true;
            }
        }

        private bool _cmdStop;
        /// <summary>
        /// Comando para dar stop no ciclo de ensaio
        /// </summary>
        public bool CmdStop
        {
            get => _cmdStop;
            set
            {
                SetField(ref _cmdStop, value);
                NeedsWrite = true;
            }
        }

        private bool _cmdRef;
        /// <summary>
        /// Comando para referenciar os eixos
        /// </summary>
        public bool CmdRef
        {
            get => _cmdRef;
            set
            {
                SetField(ref _cmdRef, value);
                NeedsWrite = true;
            }
        }

        private bool _cmdReset;
        /// <summary>
        /// Comando para resetar alarmes
        /// </summary>
        public bool CmdReset
        {
            get => _cmdReset;
            set
            {
                SetField(ref _cmdReset, value);
                NeedsWrite = true;
            }
        }

        /// <summary>
        /// Escreve os dados da tabela em um array de bytes para serem enviados ao PLC
        /// </summary>
        /// <returns>Bytes a serem enviados ao PLC</returns>
        public byte[] WriteTableData()
        {
            byte[] outputData = new byte[100];

            if (FirstSend) //Se é o primeiro envio de dados...
            {
                FirstSend = false;
            }

            HandleBoolData(ref outputData, HeartBeat, 0, 0);
            HandleBoolData(ref outputData, CmdStart, 0, 1);
            HandleBoolData(ref outputData, CmdStop, 0, 2);
            HandleBoolData(ref outputData, CmdRef, 0, 3);
            HandleBoolData(ref outputData, CmdReset, 0, 4);

            HandleFloatData(ref outputData, s_Program.DataContainer.PositionWindow, 2 * 2);
            HandleIntData(ref outputData, s_Program.DataContainer.PWMCycleTime, 4 * 2);
            HandleIntData(ref outputData, s_Program.DataContainer.PWMOnTime, 6 * 2);
            HandleShortData(ref outputData, s_Program.DataContainer.EncoderResolution, 8 * 2);
            HandleIntData(ref outputData, s_Program.DataContainer.PWMRefOnTime, 9 * 2);

            HandleFloatData(ref outputData, s_Program.DataContainer.ThreadStep, 11 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.RefXOffset, 13 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.PGain, 15 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.IGain, 17 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.DGain, 19 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.MinPWMDuty, 21 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.MaxPWMDuty, 23 * 2);
            HandleShortData(ref outputData, (short)s_Program.OperationPage.TargetWave, 25 * 2);
            HandleFloatData(ref outputData, s_Program.OperationPage.TargetFrequency, 26 * 2);
            HandleFloatData(ref outputData, s_Program.OperationPage.TargetAmplitude, 28 * 2);
            HandleFloatData(ref outputData, s_Program.DataContainer.HighPValue, 30 * 2);

            HeartBeat = !HeartBeat;

            CmdStart = false;
            CmdStop = false;
            CmdRef = false;
            CmdReset = false;

            NeedsWrite = false; 

            return outputData;
        }


        /// <summary>
        /// Trata a escrita de um dado do tipo boleano
        /// </summary>
        private void HandleBoolData(ref byte[] outputData, bool data, int byteOffset, int bitOffset)
        {
            outputData[byteOffset] = data ? (byte)(outputData[byteOffset] | (1 << bitOffset)) : outputData[byteOffset];
        }

        /// <summary>
        /// Trata a escrita de um dado do tipo short (word)
        /// </summary>
        private void HandleShortData(ref byte[] outputData, short data, int byteOffset)
        {
            byte[] dataBytes = BitConverter.GetBytes(data);

            for (int i = 0; i < dataBytes.Length; i++)
            {
                outputData[i + byteOffset] = dataBytes[i];
            }
        }

        /// <summary>
        /// Trata a escrita de um dado do tipo int (dword)
        /// </summary>
        private void HandleIntData(ref byte[] outputData, int data, int byteOffset)
        {
            byte[] dataBytes = BitConverter.GetBytes(data);

            for (int i = 0; i < dataBytes.Length; i++)
            {
                outputData[i + byteOffset] = dataBytes[i];
            }
        }


        /// <summary>
        /// Trata a escrita de um dado do tipo float (real)
        /// </summary>
        private void HandleFloatData(ref byte[] outputData, float data, int byteOffset)
        {
            byte[] dataBytes = BitConverter.GetBytes(data).ToArray();

            for (int i = 0; i < dataBytes.Length; i++)
            {
                outputData[i + byteOffset] = dataBytes[i];
            }
        }

        /// <summary>
        /// Trata a escrita de um dado do tipo byte
        /// </summary>
        private void HandleByteData(ref byte[] outputData, byte data, int byteOffset)
        {
            outputData[byteOffset] = data;
        }

    }
}
