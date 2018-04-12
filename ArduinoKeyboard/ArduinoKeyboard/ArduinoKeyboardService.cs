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

namespace ArduinoKeyboard
{
	public partial class ArduinoKeyboardService : ServiceBase
	{
        private const string DEFAULT_PATH = @"C:\Program Files (x86)\arduino keyboard\";
        private const string LOG_FILE_NAME = DEFAULT_PATH + @"log\log.txt";
		private const string LOG_CRITICAL_FILE_NAME = DEFAULT_PATH + @"log\logCritical.txt";
		private const string CNTL_PATH = DEFAULT_PATH + @"cntl\";
		private LogQueue logQueue;
		private bool stop = false;
		private static ManualResetEvent g_ShutdownEvent;
		private static string g_strIniFileName = "config.ini";
		private static Dictionary<String, Process> mapConnects = new Dictionary<String, Process>();
		private static Configs config;
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
		public static void Log(String comInfo, String text)
		{
			TagLogData data = new TagLogData();
			data.Text_data = String.Format("ComPort {0}: {1}", comInfo, text);
			data.DtCurrTime = DateTime.Now;
			LogQueue.QueueLogFile.Enqueue(data);
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
			Log("Main", msg);
            watch = new FileSystemWatcher(CNTL_PATH, "*.ini");
			watch.NotifyFilter = NotifyFilters.LastWrite;
            FileSystemEventHandler evento = new FileSystemEventHandler(OnIniChanged);

            watch.Changed += evento;
            watch.Created += evento;
            watch.Deleted += evento;
            watch.Renamed += new RenamedEventHandler(OnIniChanged);


            watch.EnableRaisingEvents = true;
			try
			{
				if (!LeConfiguracao())
				{
					msg = "Falha não tratada ao ler arquivo de configuração.";
					StreamWriter file_log = new StreamWriter(new FileStream(LOG_CRITICAL_FILE_NAME, System.IO.FileMode.Append));
					file_log.WriteLine(msg);
					file_log.Close();
					base.Stop();
					return;
				}
			}
			catch (Exception ex)
			{
				msg = "Exceção capturada ao ler arquivo de configuração : " + ex.Message;
				if (!File.Exists(LOG_CRITICAL_FILE_NAME))
				{
					File.Create(LOG_CRITICAL_FILE_NAME).Close();
				}
				StreamWriter file_log = new StreamWriter(new FileStream(LOG_CRITICAL_FILE_NAME, System.IO.FileMode.Append));
				file_log.WriteLine("[Exception][le_configuracao()]Exceção ocorrida durante tentativa leitura de arquivo de configuração, causa : " + ex.ToString() + " - " + ex.StackTrace);
				file_log.Close();
				base.Stop();
				return;
			}
			//iniciliza método de leitura
			t = new Thread(this.Run);
			t.Start();
		}
		private static void OnIniChanged(object source, FileSystemEventArgs e)
		{
			string filename = Path.GetFileName(e.FullPath).ToLower();
			if (filename == g_strIniFileName)
			{
				FileSystemWatcher fileSorce = (FileSystemWatcher)source;
				fileSorce.EnableRaisingEvents = false;
				TagLogData stLogData = new TagLogData();
				stLogData.DtCurrTime = DateTime.Now;
				try
				{
					LeConfiguracao();
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
		private static bool LeConfiguracao()
		{
			if (config == null)
			{
				config = new Configs();
			}
			if( !File.Exists(CNTL_PATH + g_strIniFileName))
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
			return true;
		}
		private static void CreateConfigFile()
		{
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
        }
		/// <summary>
		/// Método que monitora as portas e inicializa as conexões
		/// </summary>
		public void Run()
		{
			List<String> keys;
			while (!this.stop)
			{
                Process arduinoConnect;
				bool hasConnected = false;

				List<String> portNames = SerialPort.GetPortNames().ToList<String>();
				keys = mapConnects.Keys.ToList();
				//usa assim para poder remover
				foreach (String key in keys)
				{
					if (!portNames.Contains(key))
					{
                        Log("Main", "Porta " + key + " Desconectada");

                        mapConnects[key].CloseMainWindow();
                        mapConnects[key].Close();
						mapConnects.Remove(key);
					}
					else if (mapConnects[key].Threads.Count > 0)
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

                            Log("Main", "Vai criar conexão para a porta " + portName);
                            
                            ProcessStartInfo startInfo = new ProcessStartInfo(DEFAULT_PATH + "ArduinoConnect.exe");
                            startInfo.Arguments = portName;
                            startInfo.UseShellExecute = false;
                            arduinoConnect = System.Diagnostics.Process.Start(startInfo);
                            mapConnects.Add(portName, arduinoConnect);
						}
					}
				}
				if (ArduinoKeyboardService.G_ShutdownEvent.WaitOne(config.SleepTime))
				{
					this.stop = true;
				}
			}
		}
	}
}
