using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TCC_CCA___Shaking_Table_Control_IHM.src.communication
{
    public class TCPInputDataTable : INotifyPropertyChanged
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
        /// GraphDatas recebidos do PLC
        /// </summary>
        public GraphData[] GraphDatas { get; set; } = new GraphData[150];

        #region Propriedades - IOs do PLC

        private bool _xChannelA;
        /// <summary>
        /// Canal A do encoder do eixo X
        /// </summary>
        public bool XChannelA
        {
            get => _xChannelA;
            set => SetField(ref _xChannelA, value);
        }

        private bool _xChannelB;
        /// <summary>
        /// Canal B do encoder do eixo X
        /// </summary>
        public bool XChannelB
        {
            get => _xChannelB;
            set => SetField(ref _xChannelB, value);
        }

        private bool _xHardwarePlusLimit;
        /// <summary>
        /// Limite de hardware positivo do eixo X
        /// </summary>
        public bool XHardwarePlusLimit
        {
            get => _xHardwarePlusLimit;
            set => SetField(ref _xHardwarePlusLimit, value);
        }

        private bool _xHardwareMinusLimit;
        /// <summary>
        /// Limite de hardware negativo do eixo X
        /// </summary>
        public bool XHardwareMinusLimit
        {
            get => _xHardwareMinusLimit;
            set => SetField(ref _xHardwareMinusLimit, value);
        }

        private bool _buttonEmg;
        /// <summary>
        /// Botão de emergência
        /// </summary>
        public bool ButtonEmg
        {
            get => _buttonEmg;
            set => SetField(ref _buttonEmg, value);
        }

        private bool _xMotorEnabled;
        /// <summary>
        /// Motor do eixo X habilitado
        /// </summary>
        public bool XMotorEnabled
        {
            get => _xMotorEnabled;
            set => SetField(ref _xMotorEnabled, value);
        }

        private bool _xMotorPlusDirection;
        /// <summary>
        /// Comando para movimentação do motor X em sentido positivo
        /// </summary>
        public bool XMotorPlusDirection
        {
            get => _xMotorPlusDirection;
            set => SetField(ref _xMotorPlusDirection, value);
        }

        private bool _xMotorMinusDirection;
        /// <summary>
        /// Comando para movimentação do motor X em sentido negativo
        /// </summary>
        public bool XMotorMinusDirection
        {
            get => _xMotorMinusDirection;
            set => SetField(ref _xMotorMinusDirection, value);
        }

        #endregion


        private bool _alrmEmg;
        /// <summary>
        /// Alarme de emergencia acionada
        /// </summary>
        public bool AlrmEmg
        {
            get => _alrmEmg;
            set => SetField(ref _alrmEmg, value);
        }

        private bool _alrmRefTimeout;
        /// <summary>
        /// Alarme de timeout na referencia
        /// </summary>
        public bool AlrmRefTimeout
        {
            get => _alrmRefTimeout;
            set => SetField(ref _alrmRefTimeout, value);
        }

        private bool _alrmPositiveHardwareLimit;
        /// <summary>
        /// Alarme de limite de hardware positivo
        /// </summary>
        public bool AlrmPositiveHardwareLimit
        {
            get => _alrmPositiveHardwareLimit;
            set => SetField(ref _alrmPositiveHardwareLimit, value);
        }

        private bool _alrmNegativeHardwareLimit;
        /// <summary>
        /// Alarme de limite de hardware negativo
        /// </summary>
        public bool AlrmNegativeHardwareLimit
        {
            get => _alrmNegativeHardwareLimit;
            set => SetField(ref _alrmNegativeHardwareLimit, value);
        }

        private bool _msgPositiveHardwareLimit;
        /// <summary>
        /// Mensagem de limite de hardware positivo
        /// </summary>
        public bool MsgPositiveHardwareLimit
        {
            get => _msgPositiveHardwareLimit;
            set => SetField(ref _msgPositiveHardwareLimit, value);
        }

        private bool _msgNegativeHardwareLimit;
        /// <summary>
        /// Mensagem de limite de hardware negativo
        /// </summary>
        public bool MsgNegativeHardwareLimit
        {
            get => _msgNegativeHardwareLimit;
            set => SetField(ref _msgNegativeHardwareLimit, value);
        }

        private float _currentXPositon;
        /// <summary>
        /// Posição atual do eixo X
        /// </summary>
        public float CurrentXPositon
        {
            get => _currentXPositon;
            set => SetField(ref _currentXPositon, value);
        }

        private short _currentGraphDataIndex;
        /// <summary>
        /// Index atual do dado de gráfico sendo recebido
        /// </summary>
        public short CurrentGraphDataIndex
        {
            get => _currentGraphDataIndex;
            set => SetField(ref _currentGraphDataIndex, value);
        }

        private GraphData _lastGraphData;
        /// <summary>
        /// My property summary
        /// </summary>
        public GraphData LastGraphData
        {
            get => _lastGraphData;
            set => SetField(ref _lastGraphData, value);
        }

        /// <summary>
        /// Atualiza os dados da tabela de acordo com os bytes passados como argumento
        /// </summary>
        /// <param name="inputData">Bytes recebidos do PLC</param>
        public void GetTableValues(byte[] inputData)
        {
            XChannelA = (inputData[1 * 2] & (1 << 0)) == (1 << 0);
            XChannelB = (inputData[1 * 2] & (1 << 1)) == (1 << 1);
            XHardwareMinusLimit = (inputData[1 * 2] & (1 << 2)) == (1 << 2);
            XHardwarePlusLimit = (inputData[1 * 2] & (1 << 3)) == (1 << 3);
            ButtonEmg = (inputData[1 * 2] & (1 << 7)) == (1 << 7);

            XMotorEnabled = (inputData[2 * 2] & (1 << 0)) == (1 << 0);
            XMotorPlusDirection = (inputData[2 * 2] & (1 << 2)) == (1 << 2);
            XMotorMinusDirection = (inputData[2 * 2] & (1 << 3)) == (1 << 3);

            AlrmEmg = (inputData[5 * 2] & (1 << 0)) == (1 << 0);
            AlrmRefTimeout = (inputData[5 * 2] & (1 << 1)) == (1 << 1);
            AlrmPositiveHardwareLimit = (inputData[5 * 2] & (1 << 2)) == (1 << 2);
            AlrmNegativeHardwareLimit = (inputData[5 * 2] & (1 << 3)) == (1 << 3);

            MsgPositiveHardwareLimit = (inputData[6 * 2] & (1 << 0)) == (1 << 0);
            MsgNegativeHardwareLimit = (inputData[6 * 2] & (1 << 1)) == (1 << 1);

            CurrentXPositon = BitConverter.ToSingle(inputData, 3 * 2);

            short newGraphDataIndex = BitConverter.ToInt16(inputData, 28 * 2);

#if DEBUG
            if (CurrentGraphDataIndex + 1 != newGraphDataIndex && newGraphDataIndex != 0)
            {
                //Debugger.Break();
            }
#endif

            CurrentGraphDataIndex = newGraphDataIndex;
            for (int i = 0; i < 15; i++)
            {
                GraphData graphData = new GraphData();
                graphData.GetData(inputData, i * 28 + 29 * 2);
                GraphDatas[i + 15 * CurrentGraphDataIndex] = graphData;
            }

            LastGraphData = GraphDatas.Last();

        }

    }
}
