using System;
namespace ArduinoKeyboard
{
    /// <summary>
    /// classe de LOG
    /// </summary>
    public class TagLogData : ICloneable
    {
        public DateTime DtCurrTime {get; set;}

        public string Text_data { get; internal set; }

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}