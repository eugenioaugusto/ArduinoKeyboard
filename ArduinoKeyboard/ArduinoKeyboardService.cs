using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoKeyboard
{
	public partial class ArduinoKeyboardService : ServiceBase
	{
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
		}
		/// <summary>
		/// Método que inicializa as threads de leitura
		/// </summary>
		public void ServiceInit()
		{
			
		}
		/// <summary>
		/// Método que monitora as portas e inicializa as conexões
		/// </summary>
		public void Run()
		{

		}
	}
}
