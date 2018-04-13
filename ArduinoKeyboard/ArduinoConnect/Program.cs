using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArduinoKeyboard
{
    class Program
    {
        static string DEFAULT_PATH = @"C:\Program Files (x86)\arduino keyboard\";
        static string LOG_FILE_NAME = DEFAULT_PATH + @"log\log";
        static string LOG_CRITICAL_FILE_NAME = DEFAULT_PATH + @"log\logCritical.txt";
        static string CNTL_PATH = DEFAULT_PATH + @"cntl\";
        static ArduinoConnect connect;
        static void Main(string[] args)
        {
            FileSystemWatcher watch;
            if (args.Length == 1)
            {
                LogQueue logQueue = new LogQueue(LOG_FILE_NAME + "_" + args[0]+".log");
                Configs config = LeConfiguracao();
                watch = new FileSystemWatcher(CNTL_PATH, "*.ini");
                watch.NotifyFilter = NotifyFilters.LastWrite;
                FileSystemEventHandler evento = new FileSystemEventHandler(OnIniChanged);

                watch.Changed += evento;
                watch.Created += evento;
                watch.Deleted += evento;
                watch.Renamed += new RenamedEventHandler(OnIniChanged);


                watch.EnableRaisingEvents = true;
                connect = new ArduinoConnect(config, args[0]);
                Thread t = new Thread(logQueue.LogFile);
                t.Start();
                t = new Thread(connect.ReadFromPort);
                t.Start();
                Thread.Sleep(-1);

            }
        }
        private static void OnIniChanged(object source, FileSystemEventArgs e)
        {
            string filename = Path.GetFileName(e.FullPath).ToLower();
            if (filename == "config.ini")
            {
                FileSystemWatcher fileSorce = (FileSystemWatcher)source;
                fileSorce.EnableRaisingEvents = false;
                TagLogData stLogData = new TagLogData();
                stLogData.DtCurrTime = DateTime.Now;
                try
                {
                    Configs config = LeConfiguracao();
                    //TODO set config no connect
                    stLogData.Text_data = "Detectou alteração no arquivo de configuração.";
                    LogQueue.QueueLogFile.Enqueue((TagLogData)stLogData.Clone());
                }
                catch (Exception ex)
                {
                    stLogData.Text_data = "Exceção ao ler arquivo de configuração : " + ex.Message + " - " + ex.StackTrace;
                    LogQueue.QueueLogFile.Enqueue((TagLogData)stLogData.Clone());
                }
                fileSorce.EnableRaisingEvents = true;
            }
        }
        private static Configs LeConfiguracao()
        {
            Configs config = new Configs();
            if (!File.Exists(CNTL_PATH + "config.ini"))
            {
                return null;
            }
            //segue lendo mesmo tendo criado agora
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(CNTL_PATH + "config.ini");
            String leitura;
            leitura = data["SERVICO"]["SLEEP_NOT_EXIST"].Trim();
            config.SleepNotExist = Int32.Parse(leitura) * 60 * 1000;
            leitura = data["SERVICO"]["SLEEP"].Trim();
            config.SleepTime = Int32.Parse(leitura) * 1000;
            leitura = data["SERVICO"]["NIVEL_LOG"].Trim();
            if (leitura.ToUpper().Equals("INFO"))
            {
                config.LogInfo = true;
            }
            else if (!leitura.ToUpper().Equals("SEVERE"))
            {
                data["SERVICO"]["NIVEL_LOG"] = "SEVERE";
            }
            leitura = data["SERVICO"]["LOG_DATA"].Trim();
            if (leitura.ToUpper().Equals("TRUE"))
            {
                config.LogDataReceived = true;
            }
            else if (!leitura.ToUpper().Equals("FALSE"))
            {
                data["SERVICO"]["LOG_DATA"] = "FALSE";
            }
            string[] array = data["BOTOES"]["BOTAO_REPETE"].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            config.IsRepeat = new bool[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                config.IsRepeat[i] = array[i].ToUpper().Trim().Equals("TRUE");
            }

            array = data["BOTOES"]["TEMPOS_REPETICAO"].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            config.ListRepeticoes = new Int32[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                config.ListRepeticoes[i] = Int32.Parse(array[i].Trim());
            }
            return config;
        }
    }
}
