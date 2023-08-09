using LucasLauriHelpers.pages;
using LucasLauriHelpers.src;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TCC_CCA___Shaking_Table_Control_IHM.pages;
using TCC_CCA___Shaking_Table_Control_IHM.src.communication;

namespace TCC_CCA___Shaking_Table_Control_IHM.src
{
    public class Program : INotifyPropertyChanged
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

        public static string PathDataFolder = @"C:\ShakingTableControl\data";
        public static string PathLogFolder = @"C:\ShakingTableControl\log\";
        public static string PathDataContainerFile = $@"{PathDataFolder}\dataContainer.xml";

        /// <summary>
        /// Páginas da IHM
        /// </summary>
        public enum ProgramPages { Operation, Configurations};

        /// <summary>
        /// Obj estático desta classe
        /// </summary>
        public static Program s_Program { get; set; }

        /// <summary>
        /// Classe serializada para salvar dados em disco
        /// </summary>
        public DataContainer DataContainer { get; set; } = null;

        /// <summary>
        /// Comunicação entre o PLC e a IHM
        /// </summary>
        public PlcLink PlcLink { get; set; } = new PlcLink();

        #region Propriedades - Relacionadas as páginas

        /// <summary>
        /// Página principal do programa
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// Página para ações de DEBUG
        /// </summary>
        public DebugManagerPage DebugManagerPage { get; set; }

        /// <summary>
        /// Página de operação da mesa
        /// </summary>
        public OperationPage OperationPage { get; set; }

        /// <summary>
        /// Página para a configurações da mesa
        /// </summary>
        public ConfigurationsPage ConfigurationsPage { get; set; }

        /// <summary>
        /// Página para a visualização dos alarmes
        /// </summary>
        public AlarmsViewerPage AlarmsViewerPage { get; set; }

        /// <summary>
        /// Visibilidade das páginas da IHM
        /// </summary>
        public ObservableCollection<Visibility> PagesVisibilities { get; set; } = new ObservableCollection<Visibility>();

        #endregion

        private bool _debugMode = true;
        /// <summary>
        /// Se o programa esta em modo DEBUG
        /// </summary>
        public bool DebugMode
        {
            get => _debugMode;
            set => SetField(ref _debugMode, value);
        }

        private ProgramPages _currentPage = ProgramPages.Operation;
        /// <summary>
        /// Página em exibição
        /// </summary>
        public ProgramPages CurrentPage
        {
            get => _currentPage;
            set => SetField(ref _currentPage, value);
        }

        public Program()
        {
            s_Program = this;

            GraphData.ByteSize = Marshal.SizeOf(typeof(GraphData));

            Directory.CreateDirectory(PathDataFolder);
            Directory.CreateDirectory(PathLogFolder);

#if !DEBUG
            DebugMode = false;
#endif

            Logger.StartLogger(PathLogFolder);

            DataContainer = DataContainer.LoadData(PathDataContainerFile);

            for (int i = 0; i < Enum.GetNames(typeof(ProgramPages)).Length; i++)
                PagesVisibilities.Add(Visibility.Collapsed);

            PagesVisibilities[(int)CurrentPage] = Visibility.Visible;
        }

        public void Loaded()
        {
            MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        public void Closing()
        {
            if (DataContainer == null)
                return;

            DataContainer.SaveData(PathDataContainerFile);
        }

        /// <summary>
        /// Exibe uma página na IHM
        /// </summary>
        /// <param name="page"></param>
        public void ShowPage(ProgramPages page)
        {
            for(int i = 0; i < PagesVisibilities.Count;  i++)
            {
                PagesVisibilities[i] = i == (int)page ? Visibility.Visible : Visibility.Collapsed;
            }

            CurrentPage = page;
        }



        /// <summary>
        /// Alterna a visibildiade do gráfico em foco
        /// </summary>
        /// <param name="graphObjs"></param>
        public void ToggleFocusGraph(List<GraphObj> graphObjs = null)
        {
            //TODO: Implementar gráfico em tela cheia
            //if (FocusGraphVisibility == Visibility.Collapsed)
            //{
            //    FocusGraphData = graphObjs;

            //    if (FocusGraphData != null)
            //        foreach (GraphObj graphObj in FocusGraphData)
            //            graphObj.NeedsViewUpdate = true;

            //    FocusGraphVisibility = Visibility.Visible;
            //    IsGraphOnFocus = true;
            //}
            //else
            //{
            //    FocusGraphVisibility = Visibility.Collapsed;
            //    IsGraphOnFocus = false;
            //}
        }

    }

    public static class Helpers
    {
        /// <summary>
        /// Obtem a descrição de um enum (https://stackoverflow.com/questions/1415140/can-my-enums-have-friendly-names)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetEnumDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Obtem o tamanho de redenrização de uma textblock (https://stackoverflow.com/questions/9264398/how-to-calculate-wpf-textblock-width-for-its-known-font-size-and-characters)
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="textBlock"></param>
        /// <returns></returns>
        public static Size GetTextBlockSize(TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

        public static class StringCipher
        {
            private const string passPhrase = "LucasLauri - 10:14 16/05/2023";

            // This constant is used to determine the keysize of the encryption algorithm in bits.
            // We divide this by 8 within the code below to get the equivalent number of bytes.
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string Encrypt(string plainText)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt(string cipherText)
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    // Fill the array with cryptographically secure random bytes.
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
            }
        }
    }
}
