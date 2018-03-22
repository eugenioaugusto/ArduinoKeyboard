using System;
using System.IO.Ports;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace ArduinoKeyboard {
    public class ArduinoConnect {
        private SerialPort serialPort;
        private const Int32 MINUTE = 1 * 1000;// * 60;
        private InputSimulator inputSim;
        private Int32[] keyArray;
        private VirtualKeyCode[] keyCodeArray;
        private String comPort;
        AutoResetEvent stopEvent;
        bool running = true;
        private bool receivedData = false;
        public ArduinoConnect (String comPort) {
            this.comPort = comPort;
            inputSim = new InputSimulator ();
            stopEvent = new AutoResetEvent(false);
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
            int sleep = MINUTE;
            do {
                try {
                    if (!this.serialPort.IsOpen) {
                        // Now open the port.
                        this.serialPort.Open ();
                        Console.WriteLine ("Abriu a porta " + this.comPort);
                    }
                } catch (Exception ex) {
                    Console.WriteLine (ex.Message);
                    if( ex.Message.Contains("does not exist."))
                    {
                        sleep = MINUTE * 30;
                    }
                }
                Console.WriteLine ("Dormiu");
                //TODO alguma coisa para verifica se fica rodando
                stopEvent.WaitOne(sleep);
                
                Console.WriteLine ("Acordou");
                if (!receivedData) {
                    this.ClosePort ();
                } else {
                    receivedData = false;
                }
                Console.WriteLine (this.comPort + "running " + this.running.ToString ());
            } while (this.running);
            Console.WriteLine ("ARduinoConnecta da porta " + this.comPort + "Saindo");
            this.ClosePort ();
        }
        public void ClosePort () {
            this.serialPort.Close ();
        }
        public void Stop () {
            Console.WriteLine ("ARduinoConnecta recebeu comando stop");
            this.running = false;
            stopEvent.Set();
        }
        private void pressKeys () {
            for (int i = 0; i < keyArray.Length; i++) {
                Int32 keyvalue = keyArray[i];
                if (keyvalue != 0) {
                    if (keyvalue == 1
                    || keyvalue % 10 == 0) {
                        inputSim.Keyboard.KeyPress (keyCodeArray[i]);
                    }
                }
            }
        }
        private bool readButtons (String data) {
            Console.WriteLine(data);
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
                }else if( data[i] == '$') 
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("caractere ["+data[i]+"] inválido");
                    return false;
                }
            }
            return true;
        }
        private void SerialPortDataReceived (object sender, SerialDataReceivedEventArgs e) {
            Console.WriteLine (".");
            var serialPort = (SerialPort) sender;
            // Read the data that's in the serial buffer.
            String serialdata = serialPort.ReadExisting ().ToString ();
            this.receivedData = readButtons (serialdata);
            if( this.receivedData )
            {
                this.pressKeys();
            }
            if(!this.running)
            {
                this.ClosePort();
            }
            // Write to debug output.
        }
    }
}