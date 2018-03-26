using System;
namespace ArduinoKeyboard
{
    public class Configs
    {
        private Int32[] listRepeticoes;
        private bool[] isRepeat;
        private Int32 sleepTime;
        private Int32 sleepNotExist;

        public int[] ListRepeticoes { get => listRepeticoes; set => listRepeticoes = value; }
        public bool[] IsRepeat { get => isRepeat; set => isRepeat = value; }
        public int SleepTime { get => sleepTime; set => sleepTime = value; }
        public int SleepNotExist { get => sleepNotExist; set => sleepNotExist = value; }
    }
}