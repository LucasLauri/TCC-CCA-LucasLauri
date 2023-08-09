using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TCC_CCA___Shaking_Table_Control_IHM.src
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("t: {Timestamp} x: {Position}")]
    public class GraphData : INotifyPropertyChanged
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
        /// Tamnho, em bytes, de um conjunto de dados
        /// </summary>
        public static int ByteSize { get; set; } 

        private int _timestamp;
        /// <summary>
        /// Timestamp, em ms, deste dado
        /// </summary>
        public int Timestamp
        {
            get => _timestamp;
            set => SetField(ref _timestamp, value);
        }

        private float _targetPosition;
        /// <summary>
        /// Posição desejada em mm
        /// </summary>
        public float TargetPosition
        {
            get => _targetPosition;
            set => SetField(ref _targetPosition, value);
        }

        private float _position;
        /// <summary>
        /// Posição atual em mm
        /// </summary>
        public float Position
        {
            get => _position;
            set => SetField(ref _position, value);
        }

        private float _pValue;
        /// <summary>
        /// Valor da parcela proporcional do controle em % PWM
        /// </summary>
        public float PValue
        {
            get => _pValue;
            set => SetField(ref _pValue, value);
        }

        private float _iValue;
        /// <summary>
        /// Valor da parcela integral do controle em % PWM
        /// </summary>
        public float IValue
        {
            get => _iValue;
            set => SetField(ref _iValue, value);
        }

        private float _dValue;
        /// <summary>
        /// Valor da parcela derivativa do controle em % PWM
        /// </summary>
        public float DValue
        {
            get => _dValue;
            set => SetField(ref _dValue, value);
        }

        private float _uValue;
        /// <summary>
        /// Sinal de controle aplicado ao motor em % PWM
        /// </summary>
        public float UValue
        {
            get => _uValue;
            set => SetField(ref _uValue, value);
        }

        public GraphData()
        {
        }

        /// <summary>
        /// Obtem os dados para obj de um vetor de bytes
        /// </summary>
        public void GetData(byte[] inputData, int offset)
        {
            Timestamp = BitConverter.ToInt32(inputData, offset + 0);
            TargetPosition = BitConverter.ToSingle(inputData, offset + 2 * 2);
            Position = BitConverter.ToSingle(inputData, offset + 4 * 2);
            PValue = BitConverter.ToSingle(inputData, offset + 6 * 2);
            IValue = BitConverter.ToSingle(inputData, offset + 8 * 2);
            DValue = BitConverter.ToSingle(inputData, offset + 10 * 2);
            UValue = BitConverter.ToSingle(inputData, offset + 12 * 2);
        }
    }
}
