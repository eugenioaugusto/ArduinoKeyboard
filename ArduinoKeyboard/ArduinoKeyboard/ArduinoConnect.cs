using System;
using System.IO.Ports;
using System.Threading;

namespace ArduinoKeyboard
{
	public class ArduinoConnect
	{
		private SerialPort serialPort;
		private const Int32 MINUTE = 1 * 1000; // * 60;
		private Int32[] keyArray;
		private TipoBotao[] keyCodeArray;
		private String comPort;
		AutoResetEvent stopEvent;
		bool running = true;
		private bool receivedData = false;
		private bool receivedPong = true;
		private bool logouArrayErrado = false;
		private int retry = 0;
		private Configs config;

		public string ComPort { get => this.comPort; set => this.comPort = value; }

		public ArduinoConnect(Configs config, String comPort)
		{
			this.config = config;
			this.ComPort = comPort;
			this.stopEvent = new AutoResetEvent(false);
			this.keyCodeArray = new TipoBotao[] {
				TipoBotao.btn_1,
				TipoBotao.btn_2,
				TipoBotao.btn_3,
				TipoBotao.btn_4,
				TipoBotao.btn_5,
				TipoBotao.btn_6,
				TipoBotao.btn_7,
				TipoBotao.btn_8,
				TipoBotao.btn_9,
				TipoBotao.btn_10
			};
            this.keyArray = new Int32[keyCodeArray.Length];
		}
		public void ReadFromPort()
		{
			try
			{
				if (this.keyCodeArray.Length != this.config.IsRepeat.Length)
				{
					LogSevere(String.Format("Array de teclas com tamanho [{0}] diferente do Array de repetições[{1}]", this.keyCodeArray.Length, this.config.IsRepeat.Length));
				}
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
							this.LogException(ex, "No while principal da ReadFromPort");
						}
					}
					if (sleepTime < 0)
					{
						this.LogInfo("Vai dormir até receber comando de parada");
					}
					else
					{
						this.LogInfo(String.Format("Vai dormir por {0} segundos", sleepTime / 1000));
					}
                    if(ArduinoKeyboardService.G_ShutdownEvent.WaitOne(sleepTime, true))
                    {
                        this.Stop();
                    }
                    else
                    { 
						this.LogInfo("Acordou");
						if (!this.receivedPong)
						{
							if (this.receivedData)
							{
								this.LogInfo("Não recebeu pong mas recebeu dados corretos.");
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
				this.LogSevere("Saindo");
				this.ClosePort();
			}
			catch (Exception ex)
			{
				this.LogException(ex, "No método ReadFromPort");
			}
			finally
			{
				if (this.serialPort != null && this.serialPort.IsOpen)
				{
					this.serialPort.Close();
				}
				this.LogSevere("Saindo");
			}
		}
		public void ClosePort()
		{
			this.serialPort.Close();
		}
		public void Stop()
		{
			this.LogSevere("Recebeu comando stop");
			this.running = false;
			this.stopEvent.Set();
		}
		public bool IsConnected()
		{
			return this.serialPort.IsOpen && this.receivedData;
		}
		private void PressKeys()
		{
            bool pressionou = false;
			for (int i = 0; i < this.keyArray.Length; i++)
			{
				Int32 keyvalue = this.keyArray[i];
				if (keyvalue != 0)
				{
					if (keyvalue == 1)
					{
                        pressionou = ArduinoKeyboardService.pressButton(this.keyCodeArray[i], this.comPort);
						this.LogDataReceived(String.Format("Pressionando com {1} o botão {0}", this.keyCodeArray[i], pressionou?"Sucesso":"Falha"));
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
                                pressionou = ArduinoKeyboardService.pressButton(this.keyCodeArray[i], this.comPort);
								this.LogDataReceived(String.Format("Pressionando botão {0}", this.keyCodeArray[i], pressionou ? "Sucesso" : "Falha"));
							}
						}
					}
				}
			}
			Thread.Sleep(20);
			for (int i = 0; i < this.keyArray.Length; i++)
			{
				Int32 keyvalue = this.keyArray[i];
				if (this.config.IsRepeat[i] || keyvalue == 0)
				{
					ArduinoKeyboardService.releaseButton(this.keyCodeArray[i], this.comPort);
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
                this.LogSevere(String.Format("Recebeu menos dados[{0}] do que o minimo [{1}]", data.Length, this.keyArray.Length));
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
			try
			{
				var serialPort = (SerialPort)sender;
				// Read the data that's in the serial buffer.
				String serialdata = serialPort.ReadExisting().ToString();
				this.LogDataReceived(String.Format("Recebeu [{0}]", serialdata.Replace('\n', ' ')));
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
						if (!this.logouArrayErrado && this.keyCodeArray.Length != this.keyArray.Length)
						{
							LogSevere(String.Format("Array de teclas com tamanho [{0}] diferente do Array recebido[{1}]", this.keyCodeArray.Length, this.keyArray.Length));
						}
						this.PressKeys();
					}
				}
			}
			catch (Exception ex)
			{
				LogSevere(ex.Message);
                this.Stop();
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
			if (this.config.LogDataReceived)
			{
				this.LogSevere(log);
			}
		}
		private void LogSevere(String log)
		{
			ArduinoKeyboardService.Log(this.ComPort, log);
		}
		private void LogException(Exception ex, String msg)
		{
			ArduinoKeyboardService.Log(this.ComPort, String.Format("Exception capturada. {0}\nMensagem:{1}\nStack:{2}", msg, ex.Message, ex.StackTrace));
		}
	}
}