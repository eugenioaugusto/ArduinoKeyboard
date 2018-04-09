using System;
using System.IO.Ports;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace ArduinoKeyboard
{
	public class ArduinoConnect
	{
		private SerialPort serialPort;
		private const Int32 MINUTE = 1 * 1000; // * 60;
		private InputSimulator inputSim;
		private Int32[] keyArray;
		private VirtualKeyCode[] keyCodeArray;
		private String comPort;
		AutoResetEvent stopEvent;
		bool running = true;
		private bool receivedData = false;
		private bool receivedPong = true;
        private int retry = 0;
		private Configs config;

		public string ComPort { get => this.comPort; set => this.comPort = value; }

		public ArduinoConnect(Configs config, String comPort)
		{
			this.config = config;
			this.ComPort = comPort;
			this.inputSim = new InputSimulator();
			this.stopEvent = new AutoResetEvent(false);
			this.keyArray = new Int32[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			this.keyCodeArray = new VirtualKeyCode[] {
				VirtualKeyCode.VK_1,
				VirtualKeyCode.VK_2,
				VirtualKeyCode.VK_3,
				VirtualKeyCode.VK_4,
				VirtualKeyCode.VK_5,
				VirtualKeyCode.VK_6,
				VirtualKeyCode.VK_7,
				VirtualKeyCode.VK_8,
				VirtualKeyCode.VK_9,
				VirtualKeyCode.VK_A,
				VirtualKeyCode.VK_B
			};
		}
		public void ReadFromPort()
		{
			//Inicializa a porta usando o com que recebeu no construtor
			this.serialPort = new SerialPort(this.ComPort)
			{
				BaudRate = 9600,
				Parity = Parity.None,
				StopBits = StopBits.One,
				DataBits = 8,
				Handshake = Handshake.None
			};

			// Subscribe to the DataReceived event.
			this.serialPort.DataReceived += this.SerialPortDataReceived;
			do
			{
				//dorme indefinido caso esteja conectado pois só recebe dado quando um botão é apertado
				int sleepTime = -1;
				try
				{
					if (!this.serialPort.IsOpen)
					{
						// Abre a porta
						this.serialPort.Open();
						this.LogInfo("Abriu a porta ");
						//envia ping para confirmar que está conectado ao arduino
						this.LogInfo("Vai enviar Ping");
						this.receivedPong = false;
						this.serialPort.WriteLine("ping");
						this.LogInfo("Ping enviado");
						sleepTime = 3000;
					}
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("does not exist."))
					{
						sleepTime = this.config.SleepNotExist;
						if (this.config.SleepNotExist < 0)
						{
							this.LogInfo("Saindo pois a porta não existe");
							this.ClosePort();
							return;
						}
					}
					else
					{
						this.LogSevere(ex.Message);
					}
				}
				if (sleepTime < 0)
				{
					this.LogInfo("Vai dormir até receber comando de parada");
				}
				else
				{
					this.LogInfo(String.Format("Vai dormir por {0} segundos", sleepTime/1000));
				}
				if(!this.stopEvent.WaitOne(sleepTime))
				{
					this.LogInfo("Acordou");
					if ( !this.receivedPong )
					{
                        if (this.receivedData)
                        {
                            this.LogSevere("Não recebeu pong mas recebeu dados corretos.");
                        }
                        else
                        {
                            this.LogSevere(String.Format("Não recebeu Pong depois de {0}s.", sleepTime / 1000));
                            if (this.retry < 3)
                            {
                                this.retry++;
                                this.serialPort.WriteLine("ping");
                                this.LogInfo(String.Format("Ping enviado [{0}]", this.retry));
                            }
                            else
                            {
                                this.LogSevere("Saindo pois não é a porta correta");
                                this.running = false;
                            }
                        }
					}
				}
			} while (this.running);
			this.LogInfo("Saindo");
			this.ClosePort();
		}
		public void ClosePort()
		{
			this.serialPort.Close();
		}
		public void Stop()
		{
			this.LogInfo("Recebeu comando stop");
			this.running = false;
			this.stopEvent.Set();
		}
		public bool IsConnected()
		{
			return this.serialPort.IsOpen && this.receivedData;
		}
		private void PressKeys()
		{
			for (int i = 0; i < this.keyArray.Length; i++)
			{
				Int32 keyvalue = this.keyArray[i];
				if (keyvalue != 0)
				{
					if (keyvalue == 1)
					{
						this.inputSim.Keyboard.KeyPress(this.keyCodeArray[i]);
					}
					else if (this.config.IsRepeat[i])
					{
						foreach (Int32 repeatValue in this.config.ListRepeticoes)
						{
							if (keyvalue < repeatValue)
							{
								break;
							}
							if (keyvalue == repeatValue ||
								keyvalue % repeatValue == 0)
							{
								this.inputSim.Keyboard.KeyPress(this.keyCodeArray[i]);
							}
						}
					}
				}
			}
		}
		private bool ReadButtons(String data)
		{
			if (data[0] != '#')
			{
				return false;
			}
			if (data.Length < this.keyArray.Length)
			{
				return false;
			}
			for (int i = 1; i < data.Length; i++)
			{
				if (data[i] == '0')
				{
					this.keyArray[i - 1] = 0;
				}
				else if (data[i] == '1')
				{
					this.keyArray[i - 1]++;
				}
				else if (data[i] == '$')
				{
					return true;
				}
				else
				{
					this.LogSevere("caractere [" + data[i] + "] inválido");
					return false;
				}
			}
			return true;
		}
		private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			var serialPort = (SerialPort)sender;
			// Read the data that's in the serial buffer.
			String serialdata = serialPort.ReadExisting().ToString();
			this.LogDataReceived(String.Format("Recebeu [{0}]", serialdata) );
			if (serialdata.Contains("pong"))
			{
				this.receivedData = true;
				this.receivedPong = true;
			}
			else
			{
				this.receivedData = ReadButtons(serialdata);
				if (this.receivedData)
				{
					this.PressKeys();
				}
			}
		}

		private void LogInfo(String log)
		{
			if (this.config.LogInfo)
			{
				this.LogSevere(log);
			}
		}
		private void LogDataReceived(String log)
		{
			if( this.config.LogDataReceived )
			{
				this.LogSevere(log);
			}
		}
		private void LogSevere(String log)
		{
			ArduinoKeyboardService.Log(this.ComPort, log);
		}
	}
}
