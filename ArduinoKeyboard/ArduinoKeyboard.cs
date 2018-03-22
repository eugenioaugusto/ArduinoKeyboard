using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArduinoKeyboard
{
	static class ArduinoKeyboard
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
#if !DEBUG
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[]
			{
				new ArduinoKeyboardService()
			};
			ServiceBase.Run(ServicesToRun);
#else
			ArduinoKeyboardService akService = new ArduinoKeyboardService();
			akService.ServiceInit();
			Thread.Sleep(-1);
#endif
		}
	}
}
