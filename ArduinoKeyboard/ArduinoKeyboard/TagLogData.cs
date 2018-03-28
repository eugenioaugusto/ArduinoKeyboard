using System;
namespace ArduinoKeyboard
{
    /// <summary>
    /// classe de LOG
    /// </summary>
    public class TagLogData : ICloneable
    {
        public DateTime DtCurrTime {get; set;}

        public Int32 Text_linha { get; internal set; }
        public string Text_function { get; internal set; }
        public string Text_data { get; internal set; }

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}
