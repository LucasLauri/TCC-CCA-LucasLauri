using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace LucasLauriHelpers.src
{
    [Serializable]
    public class DataContainer : INotifyPropertyChanged
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

        public static DataContainer s_DataContainer { get; set; }

        /// <summary>
        /// Se o datacontainer está sendo iniciado (TRUE) ou não (FALSE)
        /// </summary>
        [XmlIgnore]
        public static bool Initializing { get; set; } = true;

        private int _pWMCycleTime = 1000;
        /// <summary>
        /// Tempo, ms, de ciclo do PWM
        /// </summary>
        public int PWMCycleTime
        {
            get => _pWMCycleTime;
            set => SetField(ref _pWMCycleTime, value);
        }

        //TODO: Checar se o tempo on é menor que o periodo do PWM
        private int _pWMOnTime = 250;
        /// <summary>
        /// Tempo, ms, que o sinal sinal do PWM ficará ativo (dever ser menor ou igual ao <see cref="PWMCycleTime"/>)
        /// </summary>
        public int PWMOnTime
        {
            get => _pWMOnTime;
            set => SetField(ref _pWMOnTime, value);
        }

        private int _pWMRefOnTime = 150;
        /// <summary>
        /// Tempo, ms, que o sinal sinal do PWM ficará ativo para fazer a referência dos eixos 
        /// </summary>
        public int PWMRefOnTime
        {
            get => _pWMRefOnTime;
            set => SetField(ref _pWMRefOnTime, value);
        }


        private float _threadStep = 6;
        /// <summary>
        /// Passo, em mm, do fuso
        /// </summary>
        public float ThreadStep
        {
            get => _threadStep;
            set => SetField(ref _threadStep, value);
        }

        private short _encoderResolution = 1000;
        /// <summary>
        /// Resolução do encoder
        /// </summary>
        public short EncoderResolution
        {
            get => _encoderResolution;
            set => SetField(ref _encoderResolution, value);
        }

        private float _refXOffset = 175/2.0f;
        /// <summary>
        /// Offset de referência utilizado para zerar o eixo X
        /// </summary>
        public float RefXOffset
        {
            get => _refXOffset;
            set => SetField(ref _refXOffset, value);
        }

        //TODO: Colocar unidades dos ganhos
        private float _pGain = 0.1f;
        /// <summary>
        /// Ganho proporcional sendo utilizado
        /// </summary>
        public float PGain
        {
            get => _pGain;
            set => SetField(ref _pGain, value);
        }

        private float _iGain = 0;
        /// <summary>
        /// Ganho integral sendo utilizado
        /// </summary>
        public float IGain
        {
            get => _iGain;
            set => SetField(ref _iGain, value);
        }

        private float _dGain = 0;
        /// <summary>
        /// Ganho derivativo sendo utilizado
        /// </summary>
        public float DGain
        {
            get => _dGain;
            set => SetField(ref _dGain, value);
        }


        private float _highPValue = 500;
        /// <summary>
        /// Valor do polo de alta frequência
        /// </summary>
        public float HighPValue
        {
            get => _highPValue;
            set => SetField(ref _highPValue, value);
        }

        private float _maxPWMDuty = 100f;
        /// <summary>
        /// Maior duty cycle do PWM
        /// </summary>
        public float MaxPWMDuty
        {
            get => _maxPWMDuty;
            set => SetField(ref _maxPWMDuty, value);
        }

        private float _minPWMDuty = 25f;
        /// <summary>
        /// Menor tempo de ciclo do PWM
        /// </summary>
        public float MinPWMDuty
        {
            get => _minPWMDuty;
            set => SetField(ref _minPWMDuty, value);
        }

        private float _positionWindow = 0.1f;
        /// <summary>
        /// Janela de posição, em mm, da mesa
        /// </summary>
        public float PositionWindow
        {
            get => _positionWindow;
            set => SetField(ref _positionWindow, value);
        }

        public DataContainer()
        {
        }

        #region Leitura e escrita do DataContainer

        /// <summary>
        /// Carrega os dados do <seealso cref="DataContainer"/> do arquivo informado
        /// </summary>
        public static DataContainer LoadData(string dataContainerFilePath)
        {
            DataContainer dataContainer = null;

            string backupFile = $@"{Path.GetDirectoryName(dataContainerFilePath)}\dataContainer_backup.xml";

            try
            {

                if (!File.Exists(dataContainerFilePath))
                {
                    dataContainer = new DataContainer();
                    s_DataContainer = dataContainer;
                    s_DataContainer.LoadDefaultValues();
                }
                else
                {
                    XmlSerializer xs = new XmlSerializer(typeof(DataContainer));
                    using (StreamReader fs = new StreamReader(dataContainerFilePath))
                    {
                        dataContainer = xs.Deserialize(fs) as DataContainer;

                        s_DataContainer = dataContainer;
                    }
                }
                DataContainer.Initializing = false;

                if (File.Exists(backupFile))
                    File.Delete(backupFile);

                dataContainerFilePath = dataContainerFilePath.Replace("_backup", "");

                if (!File.Exists(dataContainerFilePath))
                    s_DataContainer.SaveData(dataContainerFilePath);

                File.Copy(dataContainerFilePath, backupFile);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debugger.Break();
#endif
                if (File.Exists(backupFile))
                {
                    return LoadData(backupFile);
                }

            }
            return dataContainer;
        }

        /// <summary>
        /// Salva os dados do <seealso cref="DataContainer"/>
        /// </summary>
        public void SaveData(string dataContainerFilePath)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DataContainer));

            using (StreamWriter fs = new StreamWriter(dataContainerFilePath))
            {
                xmlSerializer.Serialize(fs, this);
            }
        }

        /// <summary>
        /// Carrega os valores padrões para o datacontainer
        /// </summary>
        internal void LoadDefaultValues()
        {
            //TODO: Se necessário carregar os valores padrões do datacontainer
        }

        #endregion
    }
}
