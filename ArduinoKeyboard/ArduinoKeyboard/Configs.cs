using System;
namespace ArduinoKeyboard
{
    public class Configs
    {
        private Int32[] listRepeticoes;
        private bool[] isRepeat;
        private Int32 sleepTime;
        private Int32 sleepNotExist;
		private bool logInfo;
		private bool logDataReceived;
        private Int32 nKeys;


		public int[] ListRepeticoes { get => this.listRepeticoes; set => this.listRepeticoes = value; }
        public bool[] IsRepeat { get => this.isRepeat; set => this.isRepeat = value; }
        public int SleepTime { get => this.sleepTime; set => this.sleepTime = value; }
        public int SleepNotExist { get => this.sleepNotExist; set => this.sleepNotExist = value; }
		public bool LogInfo { get => this.logInfo; set => this.logInfo = value; }
		public bool LogDataReceived { get => this.logDataReceived; set => this.logDataReceived = value; }
        public int NKeys { get => nKeys; set => nKeys = value; }
    }
}
