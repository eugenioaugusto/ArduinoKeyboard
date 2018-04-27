using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using vJoyInterfaceWrap;

namespace ArduinoKeyboard
{
    public enum TipoBotao
    {
        btn_1 = 1,
        btn_2 = 2,
        btn_3 = 3,
        btn_4 = 4,
        btn_5 = 5,
        btn_6 = 6,
        btn_7 = 7,
        btn_8 = 8,
        btn_9 = 9,
        btn_10 = 10
    }
    public partial class ArduinoKeyboardService : ServiceBase
    {
        private const string LOG_FILE_NAME = @"C:\arduino keyboard\log\log.txt";
        private const string LOG_CRITICAL_FILE_NAME = @"C:\arduino keyboard\log\logCritical.txt";
        private const string CNTL_PATH = @"C:\arduino keyboard\cntl\";
        private const Int32 JOYSTICK_ID = 1;
        private LogQueue logQueue;
        private bool stop = false;
        private static ManualResetEvent g_ShutdownEvent;
        private static string g_strIniFileName = "config.ini";
        private static Dictionary<String, ArduinoConnect> mapConnects = new Dictionary<String, ArduinoConnect>();
        private static Configs config;
        private static vJoy joystick;
        private static vJoy.JoystickState iReport;
        FileSystemWatcher watch;

        public static ManualResetEvent G_ShutdownEvent { get => g_ShutdownEvent; set => g_ShutdownEvent = value; }

        public ArduinoKeyboardService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.ServiceInit();
        }

        protected override void OnStop()
        {
            this.logQueue.Stop();
            g_ShutdownEvent.Set();
            Thread.Sleep(1000);
        }
        private static void LogInfo(String log)
        {
            LogInfo("Main", log);
        }
        private static void LogSevere(String log)
        {
            Log("Main", log);
        }
        public static void LogInfo(String comPort, String log)
        {
            if (config.LogInfo)
            {
                Log(comPort, log);
            }
        }
        public void LogDataReceived(String comPort, String log)
        {
            if (config.LogDataReceived)
            {
                Log(comPort, log);
            }
        }
        public static void Log(String comInfo, String log)
        {
            String msgFormatada = String.Format("ComPort {0}: {1}", comInfo, log);
            if (LogQueue.running)
            {
                TagLogData data = new TagLogData();
                data.Text_data = msgFormatada;
                data.DtCurrTime = DateTime.Now;
                LogQueue.QueueLogFile.Enqueue(data);
            }
            LogCritico(msgFormatada);
        }
        /// <summary>
        /// Método que inicializa as threads de leitura
        /// </summary>
        public void ServiceInit()
        {
            G_ShutdownEvent = new ManualResetEvent(false);
            //inicializa Thread de log
            this.logQueue = new LogQueue(LOG_FILE_NAME);
            Thread t = new Thread(this.logQueue.LogFile);
            t.Start();


            string msg = "Comando Start recebido.";
            LogSevere(msg);
            /*watch = new FileSystemWatcher(CNTL_PATH, "*.ini");
			watch.NotifyFilter = NotifyFilters.LastWrite;
			FileSystemEventHandler evento = new FileSystemEventHandler(OnIniChanged);

			watch.Changed += evento;
			watch.Created += evento;
			watch.Deleted += evento;
			watch.Renamed += new RenamedEventHandler(OnIniChanged);*/


            //watch.EnableRaisingEvents = true;
            try
            {
                if (!LeConfiguracao())
                {
                    msg = "Falha não tratada ao ler arquivo de configuração.";
                    LogCritico(msg);
                    base.Stop();
                    return;
                }
                LogInfo("Leu Arquivo de Configuração");
            }
            catch (Exception ex)
            {
                msg = String.Format("Exceção capturada ao ler arquivo de configuração : {0}\nStack:{1}", ex.Message, ex.StackTrace);
                LogCritico(ExceptionToString(ex, "Exceção capturada ao ler arquivo de configuração"));
                base.Stop();
                return;
            }
            try
            {
                LogSevere("Preparando joystick");
                joystick = new vJoy();
                iReport = new vJoy.JoystickState();
                if (!joystick.vJoyEnabled())
                {
                    LogSevere("Joystick não está funcionando");
                    base.Stop();
                    return;
                }
                if (!ResetJoystick())
                {
                    base.Stop();
                    return;
                }
                else
                {
                    LogSevere("Carregou o joystick " + JOYSTICK_ID);
                }
            }
            catch (Exception ex)
            {
                LogCritico(ExceptionToString(ex, "[le_configuracao()]Exceção ocorrida durante a inicialização do joystick,"));
                base.Stop();
                return;
            }
            //iniciliza método de leitura
            t = new Thread(this.Run);
            t.Start();
            LogInfo("Thread Principal iniciada");
        }
        public static bool ResetJoystick()
        {
            LogInfo("Tentando Reiniciar o Joystick");
            VjdStat status = joystick.GetVJDStatus(JOYSTICK_ID);
            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(JOYSTICK_ID))))
            {
                LogSevere("Falha ao carregar status do joystick 1");
                LogSevere(String.Format("Status = [{0}]", status));
                return false;
            }
            else
            {
                LogSevere("Carregou o joystick " + JOYSTICK_ID);
            }
            if (!joystick.ResetVJD(JOYSTICK_ID))
            {
                LogSevere("Falha no metodo ResetVJD");
                return false;
            }
            LogInfo("Reiniciado com sucesso");
            return true;
        }
        private static void OnIniChanged(object source, FileSystemEventArgs e)
        {
            string filename = Path.GetFileName(e.FullPath).ToLower();
            if (filename == g_strIniFileName)
            {
                FileSystemWatcher fileSorce = (FileSystemWatcher)source;
                fileSorce.EnableRaisingEvents = false;
                try
                {
                    TagLogData stLogData = new TagLogData();
                    stLogData.DtCurrTime = DateTime.Now;
                    LogSevere("Detectou alteração no arquivo de configuração.");
                    LeConfiguracao();
                }
                catch (Exception ex)
                {
                    LogSevere(ExceptionToString(ex,"Exceção ao ler arquivo de configuração :"));
                }
                fileSorce.EnableRaisingEvents = true;
            }
        }
        public static bool pressButton(TipoBotao tipoBotao, String comPort)
        {
            LogInfo("Servico recebeu comando de pressionar botão");
            bool retorno = joystick.SetBtn(true, JOYSTICK_ID, (uint)tipoBotao);
            LogInfo("Botão acionado com " + (retorno ? "Sucesso" : "Falha"));
            return retorno;
        }
        public static bool releaseButton(TipoBotao tipoBotao, String comPort)
        {
            LogInfo("Servico recebeu comando de pressionar botão");
            bool retorno = joystick.SetBtn(false, JOYSTICK_ID, (uint)tipoBotao);
            LogInfo("Botão acionado com " + (retorno ? "Sucesso" : "Falha"));
            return retorno;
        }
        private static bool LeConfiguracao()
        {
            if (config == null)
            {
                config = new Configs();
            }
            config.LogInfo = true;
            config.LogDataReceived = true;
            config.SleepNotExist = 30;
            config.SleepTime = 1;
            config.ListRepeticoes = new int[] { 10, 25, 2 };
            config.IsRepeat = new bool[] { false, false, false, false, false, false, false, false, false, false, false };
            return true;
            /*
            if ( !File.Exists(CNTL_PATH + g_strIniFileName))
			{
                CreateConfigFile();
			}
			//segue lendo mesmo tendo criado agora
			FileIniDataParser parser = new FileIniDataParser();
			IniData data = parser.ReadFile(CNTL_PATH + g_strIniFileName);
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
            else if( !leitura.ToUpper().Equals("SEVERE"))
            {
                data["SERVICO"]["NIVEL_LOG"] = "SEVERE";
            }
			leitura = data["SERVICO"]["LOG_DATA"].Trim();
            if(leitura.ToUpper().Equals("TRUE"))
            {
                config.LogDataReceived = true;
            }
            else if( !leitura.ToUpper().Equals("FALSE") )
            {
                data["SERVICO"]["LOG_DATA"] = "FALSE";
            }
            string[] array = data["BOTOES"]["BOTAO_REPETE"].Trim().Split(new char[] { ','}, StringSplitOptions.RemoveEmptyEntries);
            config.IsRepeat = new bool[array.Length];
            for(int i=0;i<array.Length;i++)
            {
                config.IsRepeat[i] = array[i].ToUpper().Trim().Equals("TRUE");
            }

            array = data["BOTOES"]["TEMPOS_REPETICAO"].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            config.ListRepeticoes = new Int32[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                config.ListRepeticoes[i] = Int32.Parse(array[i].Trim());
            }
            //Save the file
			parser.WriteFile(CNTL_PATH + g_strIniFileName, data);
			return true;*/
        }
        private static void CreateConfigFile()
        {
            LogInfo("Criando Arquivo de configuração novo");
            File.Create(CNTL_PATH + g_strIniFileName).Close();
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = new IniData();

            data.Sections.AddSection("SERVICO");
            data.Sections.AddSection("BOTOES");
            data["SERVICO"].AddKey("SLEEP_NOT_EXIST", "30");
            data["SERVICO"].AddKey("SLEEP", "1");
            data["SERVICO"].AddKey("NIVEL_LOG", "SEVERE");
            data["SERVICO"].AddKey("LOG_DATA", "FALSE");
            data["BOTOES"].AddKey("BOTAO_REPETE", "true, true, true, true, true, true, true, true, true, true, true");
            data["BOTOES"].AddKey("TEMPOS_REPETICAO", "10,25,2");
            parser.WriteFile(CNTL_PATH + g_strIniFileName, data);
            LogInfo("Arquivo de configuração criado com sucesso");
        }
        public static void RemoveCom(ArduinoConnect connect)
        {
            LogSevere(String.Format("Connecção {0} sendo removida", connect.ComPort));
            mapConnects.Remove(connect.ComPort);
        }
        /// <summary>
        /// Método que monitora as portas e inicializa as conexões
        /// </summary>
        public void Run()
        {
            LogSevere("Serviço iniciando");
            List<String> keys;
            while (!this.stop)
            {
                try
                {
                    ArduinoConnect arduinoConnect;
                    bool hasConnected = false;

                    List<String> portNames = SerialPort.GetPortNames().ToList<String>();
                    keys = mapConnects.Keys.ToList();
                    //usa assim para poder remover
                    foreach (String key in keys)
                    {
                        if( mapConnects[key] == null )
                        {
                            LogInfo(String.Format("Encontrou a porta {0} null, removendo.", key));
                            mapConnects.Remove(key);
                            continue;
                        }
                        if (!portNames.Contains(key) && mapConnects[key] != null)
                        {
                            LogSevere("Porta " + key + " Desconectada");

                            mapConnects[key].Stop();
                            mapConnects.Remove(key);
                        }
                        else if (mapConnects[key]!=null && mapConnects[key].IsConnected())
                        {
                            hasConnected = true;
                        }
                    }
                    //só verifica se não houver conexão já ativa
                    if (!hasConnected)
                    {
                        foreach (string portName in portNames)
                        {
                            if (!mapConnects.ContainsKey(portName))
                            {

                                LogSevere("Criando conexão para a porta " + portName);
                                arduinoConnect = new ArduinoConnect(config, portName);
                                mapConnects.Add(portName, arduinoConnect);

                                Thread thread = new System.Threading.Thread(new ThreadStart(arduinoConnect.ReadFromPort));
                                thread.Start();
                                LogSevere("Conexão criada para a porta " + portName);
                            }
                        }
                    }
                    if (ArduinoKeyboardService.G_ShutdownEvent.WaitOne(config.SleepTime))
                    {
                        LogSevere("Recebeu comando de parada");
                        this.stop = true;
                    }
                    
                }
                catch (Exception ex)
                {
                    LogCritico(String.Format("Exception capturada no método RUN: Mensagem: [{0}]\nStack:{1}", ex.Message, ex.StackTrace));
                }
            }
            LogSevere("Serviço saindo");
            ArduinoKeyboardService.G_ShutdownEvent.Set();
        }

        public static void LogCritico(String msg)
        {
            if (!File.Exists(LOG_CRITICAL_FILE_NAME))
            {
                File.Create(LOG_CRITICAL_FILE_NAME).Close();
            }
            StreamWriter file_log = new StreamWriter(new FileStream(LOG_CRITICAL_FILE_NAME, System.IO.FileMode.Append));
            file_log.WriteLine(msg);
            file_log.Close();
        }
        public static string ExceptionToString(Exception ex, String msgAdicional = "")
        {
            return String.Format("[Exception] {0} Message:{1}\nStack:{2}", msgAdicional, ex.Message, ex.StackTrace);
        }
    }
}
