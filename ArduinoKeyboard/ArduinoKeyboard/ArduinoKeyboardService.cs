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
		private const string LOG_FILE_NAME = @"c:\arduino keyboard\log\log.txt";
		private const string LOG_CRITICAL_FILE_NAME = @"c:\arduino keyboard\log\logCritical.txt";
		private const string CNTL_PATH = @"c:\arduino keyboard\cntl\";
		private LogQueue logQueue;
		private bool stop = false;
		private static ManualResetEvent g_ShutdownEvent;
		private static string g_strIniFileName = "config.ini";
		private static Dictionary<String, ArduinoConnect> mapConnects = new Dictionary<String, ArduinoConnect>();
		private static Configs config;

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
#if !DEBUG
            eventLog.WriteEntry(msg);
#endif
			FileSystemWatcher watch = new FileSystemWatcher();
			watch.Path = CNTL_PATH;
			watch.NotifyFilter = NotifyFilters.LastWrite;
			// Only watch ini files.
			watch.Filter = " *.ini";
			watch.Changed += new FileSystemEventHandler(OnIniChanged);
			watch.EnableRaisingEvents = true;
			try
			{
				if (!LeConfiguracao())
				{
					msg = "Falha não tratada ao ler arquivo de configuração.";
#if !DEBUG
					eventLog.WriteEntry(msg);
#endif
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
#if !DEBUG
				eventLog.WriteEntry(msg);
#endif
				if (!File.Exists(LOG_CRITICAL_FILE_NAME))
				{
					File.Create(LOG_CRITICAL_FILE_NAME);
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
				
			}
			//segue lendo mesmo tendo criado agora
			FileIniDataParser parser = new FileIniDataParser();
			IniData data = parser.ReadFile(CNTL_PATH + g_strIniFileName);
			//TODO criar arquivo se não existir
			//String leitura = data["GeneralConfiguration"]["setUpdate"];
			leitura = data["SERVICO"]["SLEEP_NOT_EXIST"];
			config.SleepNotExist = 30 * 60 * 1000;
			leitura = data["SERVICO"]["SLEEP"];
			config.SleepTime = 1 * 1000;
			leitura = data["SERVICO"]["NIVEL_LOG"];
			config.LogInfo = true;
			leitura = data["SERVICO"]["LOG_DATA"];
			config.LogDataReceived = true;
			leitura = data["BOTOES"]["BOTAO_REPETE"];
			config.IsRepeat = new bool[] { true, true, true, true, true, true, true, true, true, true, true };
			leitura = data["BOTOES"]["TEMPOS_REPETICAO"];
			config.ListRepeticoes = new Int32[] { 10, 20, 25, 2 };

			//Save the file
			parser.WriteFile(CNTL_PATH + g_strIniFileName, data);
			return true;
		}
		private static void CreateConfigFile()
		{
			File.Create(CNTL_PATH + g_strIniFileName);
			//data.Sections.AddSection("newSection");
			//data["newSection"].AddKey("newKey1", "value1");
		}
		public static void RemoveCom(ArduinoConnect connect)
		{
			mapConnects.Remove(connect.ComPort);
		}
		/// <summary>
		/// Método que monitora as portas e inicializa as conexões
		/// </summary>
		public void Run()
		{
			List<String> keys;
			while (!this.stop)
			{
				ArduinoConnect arduinoConnect;
				bool hasConnected = false;

				List<String> portNames = SerialPort.GetPortNames().ToList<String>();
				keys = mapConnects.Keys.ToList();
				//usa assim para poder remover
				foreach (String key in keys)
				{
					if (!portNames.Contains(key))
					{
						mapConnects[key].Stop();
						mapConnects.Remove(key);
					}
					else if (mapConnects[key].IsConnected())
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
							Console.WriteLine(portName);


							arduinoConnect = new ArduinoConnect(config, portName);
							mapConnects.Add(portName, arduinoConnect);

							Thread thread = new System.Threading.Thread(new ThreadStart(arduinoConnect.ReadFromPort));
							thread.Start();
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
