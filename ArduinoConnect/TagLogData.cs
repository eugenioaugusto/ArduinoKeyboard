using System;
namespace ArduinoConnect

{
    /// <summary>
    /// classe de LOG
    /// </summary>
    public class TagLogData
    {
        public DateTime DtCurrTime {get; set;}

        public Int32 Text_linha { get; internal set; }
        public string Text_function { get; internal set; }
        public string Text_data { get; internal set; }
    }
}