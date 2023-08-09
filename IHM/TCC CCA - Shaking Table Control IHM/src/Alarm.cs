using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace LucasLauriHelpers.src
{
    /// <summary>
    /// Niveis possíveis para alarme
    /// </summary>
    public enum AlarmsLevels { None, Message, Alarm };

    public class Alarm : INotifyPropertyChanged, IEquatable<Alarm>
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
        /// Ids dos alarmes da IHM.
        /// </summary>
        public enum AlarmsIds
        {
            None = 0,
            NoComm = 1000, Emg = 1001, RefTimeout = 1001, PositiveHardwareLimite = 1003, NegativeHardwareLimite = 1004,
            MsgPositiveLimite = 2000, MsgNegativeLimite = 2001
        }

        private AlarmsLevels _alarmLevel = AlarmsLevels.Alarm;
        /// <summary>
        /// Nível do alarme
        /// </summary>
        public AlarmsLevels AlarmLevel
        {
            get => _alarmLevel;
            set => SetField(ref _alarmLevel, value);
        }

        private AlarmsIds _identifier;
        /// <summary>
        /// Identificador, único, do alarme
        /// </summary>
        public AlarmsIds Identifier
        {
            get => _identifier;
            set => SetField(ref _identifier, value);
        }

        private string _text;
        /// <summary>
        /// Texto do alarme
        /// </summary>
        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        private DateTime _timeStamp;
        /// <summary>
        /// Momento em que o alarme foi obtido do CNC
        /// </summary>
        [XmlIgnore]
        public DateTime TimeStamp
        {
            get => _timeStamp;
            set => SetField(ref _timeStamp, value);
        }

        private Visibility _helpVisibility = Visibility.Collapsed;
        /// <summary>
        /// Visibilidade da seção de ajuda do alarme
        /// </summary>
        [XmlIgnore]
        public Visibility HelpVisibility
        {
            get => _helpVisibility;
            set => SetField(ref _helpVisibility, value);
        }

        private string _helpText;
        /// <summary>
        /// Texto que será apresentado na seção de ajuda do alarme
        /// </summary>
        public string HelpText
        {
            get => _helpText;
            set => SetField(ref _helpText, value);
        }

        /// <summary>
        /// Construtor para um alarme de acordo com um IO
        /// </summary>
        public Alarm(AlarmsIds identifier, string text, AlarmsLevels alarmLevel, string helpText)
        {
            Identifier = identifier;
            Text = text.Replace('\0', ' ').Trim();
            AlarmLevel = alarmLevel;
            HelpText = helpText;
            TimeStamp = DateTime.Now;
        }

        /// <summary>
        /// Construtor para serialização
        /// </summary>
        public Alarm()
        {

        }

        public bool Equals(Alarm other)
        {
            return Identifier.Equals(other.Identifier);
        }

        public Alarm Copy()
        {
            return (Alarm)this.MemberwiseClone();
        }

    }
}
