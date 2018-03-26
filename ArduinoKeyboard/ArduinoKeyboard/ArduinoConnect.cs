using System;
using System.IO.Ports;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace ArduinoKeyboard {
    public class ArduinoConnect {
        private SerialPort serialPort;
        private const Int32 MINUTE = 1 * 1000; // * 60;
        private InputSimulator inputSim;
        private Int32[] keyArray;
        private VirtualKeyCode[] keyCodeArray;
        private String comPort;
        AutoResetEvent stopEvent;
        bool running = true;
        private bool receivedData = false;
        private Configs config;
        public ArduinoConnect (Configs config, String comPort) {
            this.config = config;
            this.comPort = comPort;
            inputSim = new InputSimulator ();
            stopEvent = new AutoResetEvent (false);
            keyArray = new Int32[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            keyCodeArray = new VirtualKeyCode[] {
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
        public void ReadFromPort () {
            // Initialise the serial port on COM3.
            // obviously we would normally parameterise this, but
            // this is for demonstration purposes only.
            this.serialPort = new SerialPort (this.comPort) {
                BaudRate = 9600,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None
            };

            // Subscribe to the DataReceived event.
            this.serialPort.DataReceived += SerialPortDataReceived;
            do {
                bool exist = true;
                try {
                    if (!this.serialPort.IsOpen) {
                        // Now open the port.
                        this.serialPort.Open ();
                        this.Log ("Abriu a porta ");
                    }
                } catch (Exception ex) {
                    this.Log (ex.Message);
                    if (ex.Message.Contains ("does not exist.")) {
                        exist = false;
                        if (this.config.SleepNotExist < 0) {
                            this.Log ("Saindo pois a porta não existe");
                            this.ClosePort ();
                            return;
                        }
                    }
                }
                this.Log ("Dormiu");
                //TODO alguma coisa para verifica se fica rodando
                if (exist) {
                    stopEvent.WaitOne (this.config.SleepTime);
                } else {
                    stopEvent.WaitOne (this.config.SleepNotExist);
                }
                this.Log ("Acordou");
                if (!receivedData) {
                    this.ClosePort ();
                } else {
                    receivedData = false;
                }
                this.Log (this.comPort + "running " + this.running.ToString ());
            } while (this.running);
            this.Log ("Saindo");
            this.ClosePort ();
        }
        public void ClosePort () {
            this.serialPort.Close ();
        }
        public void Stop () {
            this.Log ("Recebeu comando stop");
            this.running = false;
            stopEvent.Set ();
        }
        public bool IsConnected () {
            return this.serialPort.IsOpen && this.receivedData;
        }
        private void pressKeys () {
            for (int i = 0; i < keyArray.Length; i++) {
                Int32 keyvalue = keyArray[i];
                if (keyvalue != 0) {
                    foreach (Int32 repeatValue in this.config.ListRepeticoes) {
                        if( keyvalue < repeatValue )
                        {
                            break;
                        }
                        if (keyvalue == repeatValue ||
                            keyvalue % repeatValue == 0)
                            inputSim.Keyboard.KeyPress (keyCodeArray[i]);
                    }
                }
            }
        }
        private bool readButtons (String data) {
            this.Log (data);
            if (data[0] != '#') {
                return false;
            }
            if (data.Length < keyArray.Length) {
                return false;
            }
            for (int i = 1; i < data.Length; i++) {
                if (data[i] == '0') {
                    keyArray[i - 1] = 0;
                } else if (data[i] == '1') {
                    keyArray[i - 1]++;
                } else if (data[i] == '$') {
                    return true;
                } else {
                    this.Log ("caractere [" + data[i] + "] inválido");
                    return false;
                }
            }
            return true;
        }
        private void SerialPortDataReceived (object sender, SerialDataReceivedEventArgs e) {
            this.Log (".");
            var serialPort = (SerialPort) sender;
            // Read the data that's in the serial buffer.
            String serialdata = serialPort.ReadExisting ().ToString ();
            this.receivedData = readButtons (serialdata);
            if (this.receivedData) {
                this.pressKeys ();
            }
            if (!this.running) {
                this.ClosePort ();
            }
            // Write to debug output.
        }

        private void Log (String log) {
            Console.WriteLine (String.Format ("[{0}]:{1}", this.comPort, log));
        }
    }
}