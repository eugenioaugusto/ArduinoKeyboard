using System;
using System.IO.Ports;
using System.Threading;


namespace ArduinoKeyboard
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Serial ports available:");
            Console.WriteLine("-----------------------");
            foreach(var portName in SerialPort.GetPortNames())
            {
                
                Console.WriteLine(portName);
            }

            ArduinoConnect connect = new ArduinoConnect();
            connect.ReadFromPort();

            Thread.Sleep(1000000);
        }
    }
}
