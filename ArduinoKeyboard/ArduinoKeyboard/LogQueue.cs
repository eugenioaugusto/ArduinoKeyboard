using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace ArduinoKeyboard
{
	/// <summary>
	/// classe de LOG
	/// </summary>
	public class LogQueue
	{
		private bool bStop = false;
		//TODO arrumar a classe de log. criar uma mais simples
		private static ConcurrentQueue<TagLogData> queueLogFile = new ConcurrentQueue<TagLogData>();
		private String filename;

		public static ConcurrentQueue<TagLogData> QueueLogFile { get => queueLogFile; set => queueLogFile = value; }

		public LogQueue(String filename)
		{
			this.filename = filename;
		}
		/// <summary>
		/// método usado na thread de log
		/// </summary>
		public void LogFile()
		{
			if (!File.Exists(this.filename))
			{
				File.Create(this.filename);
			}
			StreamWriter file_log = new StreamWriter(new FileStream(this.filename, System.IO.FileMode.Append));
			string text_buffer;
			TimeSpan delay_hig = new TimeSpan(0, 0, 0, 2, 0);
			TagLogData stLogData;
			while (!this.bStop)
			{
				while (QueueLogFile.Count > 0)
				{
					if (!QueueLogFile.TryDequeue(out stLogData))
					{
						Thread.Sleep(100);
						continue;
					}
					text_buffer = String.Empty;
					text_buffer = stLogData.DtCurrTime.ToString("[ddMMyy HHmmss] ");
					text_buffer += stLogData.Text_data;
					text_buffer += "\r\n";
					file_log.Write(text_buffer);
					file_log.Flush();
				}
				//TODO criar evento na classe pai statico para quando deve sair

				if (ArduinoKeyboardService.G_ShutdownEvent.WaitOne(delay_hig, true))
				{
					this.bStop = true;
				}
			}
		}
		public void Stop()
		{
			this.bStop = true;

		}
	}
}
