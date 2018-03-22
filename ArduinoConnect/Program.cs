using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace ArduinoKeyboard {
    class Program {
        private bool run = true;
        private AutoResetEvent stopEvent = new AutoResetEvent(false);

        Dictionary<String, ArduinoConnect> mapConnects = new Dictionary<String, ArduinoConnect> ();
        static void Main (string[] args) {
            Console.WriteLine ("Serial ports available:");
            Console.WriteLine ("-----------------------");
            Program program = new Program ();
            Thread thread = new System.Threading.Thread (program.StartPorts);
            thread.Start ();
            Console.WriteLine ("esperando comando");
            Console.Read ();
            Console.WriteLine ("Recebeu comando de saida");
            program.run = false;
            program.stopEvent.Set();
            Thread.Sleep (1000);
        }
        private void StartPorts () {
            ArduinoConnect arduinoConnect;
            do {

                foreach (string portName in SerialPort.GetPortNames ()) {
                    if (!mapConnects.ContainsKey (portName)) {
                        Console.WriteLine (portName);
                        arduinoConnect = new ArduinoConnect (portName);
                        mapConnects.Add (portName, arduinoConnect);

                        Thread thread = new System.Threading.Thread (new ThreadStart (arduinoConnect.ReadFromPort));
                        thread.Start ();
                    }
                }
                stopEvent.WaitOne(1000);
            } while (this.run);
            Console.WriteLine ("Programa saindo");
            foreach (ArduinoConnect connect in mapConnects.Values) {
                connect.Stop ();
            }
        }
    }
}