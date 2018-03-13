using System;
using System.IO.Ports;
using WindowsInput.Native; 
using WindowsInput;
using System.Threading;

namespace ArduinoKeyboard {
    public class ArduinoConnect {
        private SerialPort serialPort;
        private const Int32 MINUTE = 1*1000*60;
        private InputSimulator inputSim;
        private boolean receivedPong = false;
        private Int32[] keyArray;
        private VirtualKeyCode[] keyCodeArray;
        private String comPort;
        private boolean running = true;
        public ArduinoConnect(String comPort)
        {
            this.comPort = comPort;
            inputSim = new InputSimulator();
            keyArray = new Int32[] {0,0,0,0,0,0,0,0,0,0,0};
            keyCodeArray = new VirtualKeyCode[] 
            {
                VirtualKeyCode.VK_1,
                VirtualKeyCode.VK_2,
                VirtualKeyCode.VK_3,
                VirtualKeyCode.VK_4,
                VirtualKeyCode.VK_5,
                VirtualKeyCode.VK_6,
                VirtualKeyCode.VK_7,
                VirtualKeyCode.VK_8,
                VirtualKeyCode.VK_9,
                VirtualKeyCode.VK_A
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

            // Now open the port.
            this.serialPort.Open ();
            
            do
            {
                //TODO alguma coisa para verifica se fica rodando
                //Thread.Sleep(MINUTE);
            }while(running);
            this.ClosePort();
        }
        public void ClosePort()
        {
            this.serialPort.Close();
        }
        public void Stop()
        {
            if(!this.serialPort.isOpen)
            {
                this.ClosePort();
            }
            this.running = false;
        }
        private void sendPing()
        {
            //TODO enviar ping
        }
        private void pressKeys()
        {
            for( int i=0;i< keyArray.Length;i++)
            {
                Int32 keyvalue = keyArray[i];
                if( keyvalue != 0 )
                {
                    if( keyvalue == 1 )
                    {
                        inputSim.Keyboard.KeyPress(keyCodeArray[i]);
                    }
                }
            }
        }
        private void readButtons(String data)
        {
            if( data[0] != '#' )
            {
                return;
            }
            for( int i=1;i<data.Length - 1; i++)
            {
                if( data[i] == '0' )
                {
                    keyArray[i-1] = 0;
                }
                else if( data[i] == '1' )
                {
                    keyArray[i-1]++;
                }
            }
            
        }
        private void SerialPortDataReceived (object sender, SerialDataReceivedEventArgs e) {
            var serialPort = (SerialPort) sender;

            // Read the data that's in the serial buffer.
            String serialdata = serialPort.ReadExisting ().ToString();
            readButtons(serialdata);
            // Write to debug output.
            Console.WriteLine (serialdata);
        }
    }
}
