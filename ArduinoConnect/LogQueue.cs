using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ArduinoConnect

{
	/// <summary>
	/// classe de LOG
	/// </summary>
	public class LogQueue
	{
		private bool bStop = false;
		//TODO arrumar a classe de log. criar uma mais simples
		private static ConcurrentQueue<TagLogData> queueLogFile = new ConcurrentQueue<TagLogData>();
		/// <summary>
		/// método usado na thread de log
		/// </summary>
		public void LogFile()
		{
			string fileLogName = "";
			StreamWriter file_log = new StreamWriter(new FileStream(default_filelog_name, System.IO.FileMode.Append));
			string text_buffer;
			TimeSpan delay_hig = new TimeSpan(0, 0, 0, 2, 0);
			while (!this.bStop)
			{
				while (queueLogFile.Count > 0)
				{
					if (!queueLogFile.TryDequeue(out stLogData))
					{
						Thread.Sleep(100);
						continue;
					}
					text_buffer = String.Empty;
					if (stLogData.Text_bInserirDataHora)
					{
						text_buffer = dtCurrTime.ToString("[ddMMyy HHmmss] ");
					}
					if (stLogData.Text_linha != 0)
					{
						text_buffer += "[" + stLogData.Text_linha.ToString("0###") + "] ";
					}
					if (stLogData.Text_function.Length > 0)
					{
						text_buffer += "[" + stLogData.Text_function + "] ";
					}
					if (stLogData.Text_data.Length > 0)
					{
						text_buffer += stLogData.Text_data;
					}
					if (stLogData.Text_bInserirQuebraLinha)
					{
						text_buffer += "\r\n";
					}
					file_log.Write(text_buffer);
					file_log.Flush();
				}
				//TODO criar evento na classe pai statico para quando deve sair
				/**
				if (ServidorComponentes.G_ShutdownEvent.WaitOne(delay_hig, true))
				{
					this.bStop = true;
				}*/
			}
		}
		public void Stop()
		{
			this.bStop = true;

		}
	}
}
