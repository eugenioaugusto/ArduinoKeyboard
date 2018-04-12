using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoKeyboard
{
    class Program
    {
        static void Main(string[] args)
        {
            string DEFAULT_PATH = @"C:\Program Files (x86)\arduino keyboard\";
            string LOG_FILE_NAME = DEFAULT_PATH + @"log\log.txt";
            string LOG_CRITICAL_FILE_NAME = DEFAULT_PATH + @"log\logCritical.txt";
            string CNTL_PATH = DEFAULT_PATH + @"cntl\";
            LogQueue logQueue;
            if (args.Length == 1)
            {

                Configs config = new Configs();
                if (!File.Exists(CNTL_PATH + "config.ini"))
                {
                    return;
                }
                //segue lendo mesmo tendo criado agora
                FileIniDataParser parser = new FileIniDataParser();
                IniData data = parser.ReadFile(CNTL_PATH + "config.ini");
                String leitura;
                leitura = data["SERVICO"]["SLEEP_NOT_EXIST"].Trim();
                config.SleepNotExist = Int32.Parse(leitura) * 60 * 1000;
                leitura = data["SERVICO"]["SLEEP"].Trim();
                config.SleepTime = Int32.Parse(leitura) * 1000;
                leitura = data["SERVICO"]["NIVEL_LOG"].Trim();
                if (leitura.ToUpper().Equals("INFO"))
                {
                    config.LogInfo = true;
                }
                else if (!leitura.ToUpper().Equals("SEVERE"))
                {
                    data["SERVICO"]["NIVEL_LOG"] = "SEVERE";
                }
                leitura = data["SERVICO"]["LOG_DATA"].Trim();
                if (leitura.ToUpper().Equals("TRUE"))
                {
                    config.LogDataReceived = true;
                }
                else if (!leitura.ToUpper().Equals("FALSE"))
                {
                    data["SERVICO"]["LOG_DATA"] = "FALSE";
                }
                string[] array = data["BOTOES"]["BOTAO_REPETE"].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                config.IsRepeat = new bool[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    config.IsRepeat[i] = array[i].ToUpper().Trim().Equals("TRUE");
                }

                array = data["BOTOES"]["TEMPOS_REPETICAO"].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                config.ListRepeticoes = new Int32[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    config.ListRepeticoes[i] = Int32.Parse(array[i].Trim());
                }
            }
        }
    }
}
