using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LucasLauriHelpers.src.Logger;

namespace LucasLauriHelpers.src
{
    public class Logger
    {
        /// <summary>
        /// Tipos de mensagem que podem ser logadas
        /// </summary>
        public enum MessageLogTypes { Info, Warning, Error, Debug };

        /// <summary>
        /// Separador utilizado no CSV
        /// </summary>
        public static readonly string Delimiter = ";";

        /// <summary>
        /// Pasta onde os logs serão salvos
        /// </summary>
        public static string PathLogDirectory { get; set; }

        /// <summary>
        /// Semaforo da escrita de logs
        /// </summary>
        public static Semaphore WriteLogSemaphore { get; set; } = new Semaphore(1, 1);

        /// <summary>
        /// Caminho do arquivo sendo escrito
        /// </summary>
        public static string CurrentLogFilePath { get; set; } = $"{PathLogDirectory}log.csv";

        /// <summary>
        /// Data em que o logger foi iniciado
        /// </summary>
        public static DateTime StartLoggerDate { get; set; }


        /// <summary>
        /// Núemro de logs pendents 
        /// </summary>
        public static int PendentLogs { get; set; }

        /// <summary>
        /// Inicia o logger criando, se necessário, um arquivo csv novo.
        /// </summary>
        public static void StartLogger(string pathLogDirectory)
        {
            if (pathLogDirectory.Equals(""))
            {
#if DEBUG
                Debugger.Break();
#endif
            }

            PathLogDirectory = pathLogDirectory;

            if (!Directory.Exists(PathLogDirectory))
                Directory.CreateDirectory(PathLogDirectory);

            StartLoggerDate = DateTime.Now;
            CurrentLogFilePath = $"{PathLogDirectory}{StartLoggerDate:yyyy.MM.dd}.csv";


            LogItem startLogMessage = new("★★★★★★★★★★ Logger iniciado ★★★★★★★★★★", nameof(StartLogger), MessageLogTypes.Info);

            if (File.Exists(CurrentLogFilePath))
            {
                WriteLog(startLogMessage);

                ZipOldFiles();

                return;
            }


            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = Delimiter,
            };

            using var writer = new StreamWriter(CurrentLogFilePath);
            using var csv = new CsvWriter(writer, config);
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(new CsvHelper.TypeConversion.TypeConverterOptions { Formats = new[] { "dd/MM/yyyy HH:mm:ss.ffff" } });
            csv.WriteRecords(new List<LogItem>() { startLogMessage });

            ZipOldFiles();
        }

        /// <summary>
        /// Cria um zip com os arquivos mensais dos logs
        /// </summary>
        private static void ZipOldFiles()
        {
            string[] files = Directory.GetFiles(PathLogDirectory);

            List<DateTime> filesData = new();
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".zip")
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(file);

                if (DateTime.TryParse(fileName, out DateTime date))
                    filesData.Add(date);
            }

            if (filesData.Count == 0)
                return;

            filesData.Sort();

            List<List<DateTime>> filesRanges = new();

            foreach (DateTime date in filesData)
            {
                if (date.Year == DateTime.Now.Year && date.Month == DateTime.Now.Month) //Se o log for do mesmo mes atual...
                    continue;

                if (filesRanges.Count == 0)
                {
                    filesRanges.Add(new List<DateTime>());
                    filesRanges.Last().Add(date);
                    continue;
                }

                DateTime lastDate = filesRanges.Last().Last();
                if (date.Year == lastDate.Year && date.Month == lastDate.Month)
                {
                    filesRanges.Last().Add(date);
                }
                else
                {
                    filesRanges.Add(new List<DateTime>());
                    filesRanges.Last().Add(date);
                }
            }

            foreach (List<DateTime> range in filesRanges)
            {
                DateTime fistDate = range.First();

                string zipFile = $"{PathLogDirectory}{fistDate:yyyy.MM}.zip";
                using ZipArchive zip = ZipFile.Open(zipFile, File.Exists(zipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create);

                foreach (DateTime date in range)
                {
                    string logFile = $"{PathLogDirectory}{date:yyyy.MM.dd}.csv";
                    zip.CreateEntryFromFile(logFile, $"{date:yyyy.MM.dd}.csv");

                    File.Delete(logFile);
                }

                LogMessage($"{range.Count} arquivos de log foram compactados no arquivo '{zipFile}'", MessageLogTypes.Debug);
            }
        }

        /// <summary>
        /// Loga uma mensagem
        /// </summary>
        public static void LogMessage(string message, MessageLogTypes messageLogType, [CallerMemberName] string callerMember = "")
        {
            PendentLogs++;

            WriteLog(new LogItem(message, callerMember, messageLogType));
        }

        /// <summary>
        /// Loga um erro fatal
        /// </summary>
        public static void LogFatalError(string message)
        {
            LogMessage("Ocorreu um erro fatal na IHM!", MessageLogTypes.Error);

            string fatalErrorPath = $"{PathLogDirectory}FatalError - {DateTime.Now:yyyy.MM.dd - HH.ss.ffff}.txt";
            File.WriteAllText(fatalErrorPath, message);
        }

        private static void WriteLog(LogItem logItem)
        {
            WriteLogSemaphore.WaitOne();

            if (DateTime.Now.Day != StartLoggerDate.Day)
                StartLogger(PathLogDirectory);

            // Configura append
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false, //Não escrever cabeçalho
                Delimiter = Delimiter,
            };

#if DEBUG
            if (logItem.Message.Contains(Delimiter))
            {
                Debugger.Break();
                logItem.Message = logItem.Message.Replace(Delimiter, ".");
            }
#endif

            //Escreve no log
            try
            {
                using (var stream = File.Open(CurrentLogFilePath, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, config))
                {
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(new CsvHelper.TypeConversion.TypeConverterOptions { Formats = new[] { "dd/MM/yyyy HH:mm:ss.ffff" } });
                    csv.WriteRecords(new List<LogItem>() { logItem });
                }


#if DEBUG
                if (PendentLogs > 3)
                {
                    Debugger.Break();
                }
#endif

                PendentLogs--;

            }
            catch (Exception)
            {
                //catch genério para escrever o log novamente ao dar erro de escrita
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(100);
                    WriteLog(logItem);
                });
            }

            WriteLogSemaphore.Release();

        }
    }

    /// <summary>
    /// Classe representando um item a ser logado
    /// </summary>
    public class LogItem
    {
        /// <summary>
        /// Timestamp do log
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Nome do método que gerou o log
        /// </summary>
        public string CallerMember { get; set; }

        /// <summary>
        /// Mensagem a ser logada
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Tipo desta mensagem
        /// </summary>
        public MessageLogTypes MessageType { get; set; }

        public LogItem(string message, string callerMember, MessageLogTypes messageType)
        {
            TimeStamp = DateTime.Now;
            Message = message;
            CallerMember = callerMember;
            MessageType = messageType;
        }


        public LogItem(string message, string callerMember, MessageLogTypes messageType, DateTime timeStamp)
        {
            TimeStamp = timeStamp;
            Message = message;
            CallerMember = callerMember;
            MessageType = messageType;
        }
    }
}
