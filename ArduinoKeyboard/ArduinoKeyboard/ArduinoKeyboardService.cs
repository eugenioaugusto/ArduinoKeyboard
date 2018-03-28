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

namespace ArduinoKeyboard
{
	public partial class ArduinoKeyboardService : ServiceBase
	{
		private const string LOG_FILE_NAME = @"c:\arduino keyboard\log\log.txt";
		private LogQueue logQueue;
		private bool stop = false;
		private static ManualResetEvent g_ShutdownEvent;
		private static string g_strIniFileName = "config.ini";
		private Dictionary<String, ArduinoConnect> mapConnects = new Dictionary<String, ArduinoConnect>();
		private static Configs config;

		public static ManualResetEvent G_ShutdownEvent { get => g_ShutdownEvent; set => g_ShutdownEvent = value; }

		public ArduinoKeyboardService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			this.ServiceInit();
			G_ShutdownEvent = new ManualResetEvent(false);
			config = new Configs();
		}

		protected override void OnStop()
		{
			this.logQueue.Stop();
			g_ShutdownEvent.Set();
			Thread.Sleep(1000);
		}
		/// <summary>
		/// Método que inicializa as threads de leitura
		/// </summary>
		public void ServiceInit()
		{
			//inicializa Thread de log
			this.logQueue = new LogQueue(LOG_FILE_NAME);
			Thread t = new Thread(this.logQueue.LogFile);
			t.Start();


			string msg = "Comando Start recebido.";
#if !DEBUG
            eventLog.WriteEntry(msg);
#endif
			FileSystemWatcher watch = new FileSystemWatcher();
			watch.Path = @"c:\arduino keyboard\cntl\";
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
					StreamWriter file_log = new StreamWriter(new FileStream(LOG_FILE_NAME, System.IO.FileMode.Append));
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
				StreamWriter file_log = new StreamWriter(new FileStream(LOG_FILE_NAME, System.IO.FileMode.Append));
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
			//TODO criar arquivo se não existir
			config.IsRepeat = new bool[] { true, true, true, true, true, true, true, true, true, true, true };
			config.ListRepeticoes = new Int32[] { 10, 20, 25, 2 };
			config.SleepNotExist = 30 * 60 * 1000;
			config.SleepTime = 1 * 1000;
			return true;
		}
		/// <summary>
		/// Método que monitora as portas e inicializa as conexões
		/// </summary>
		public void Run()
		{
			while (!this.stop)
			{
				ArduinoConnect arduinoConnect;
				bool hasConnected = false;

				foreach( ArduinoConnect connect in this.mapConnects.Values )
				{
					if( connect.IsConnected() )
					{
						hasConnected = true;
						break;
					}
				}
				//só verifica se não houver conexão já ativa
				if (!hasConnected)
				{
					foreach (string portName in SerialPort.GetPortNames())
					{
						if (!this.mapConnects.ContainsKey(portName))
						{
							Console.WriteLine(portName);
							
							
							arduinoConnect = new ArduinoConnect(config, portName);
							this.mapConnects.Add(portName, arduinoConnect);

							Thread thread = new System.Threading.Thread(new ThreadStart(arduinoConnect.ReadFromPort));
							thread.Start();
						}
					}
				}
				if (ArduinoKeyboardService.G_ShutdownEvent.WaitOne(config.SleepTime* 1000))
				{
					this.stop = true;
				}
			}
		}
	}
}
