using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ailemon
{
    class WavRecorder
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(
         string lpstrCommand,
         string lpstrReturnString,
         int uReturnLength,
         int hwndCallback
        );

        private string _dName = "";

        private int bitspersample = 16; //每个样本的比特数，单位：位、比特、bit
        private int channels = 1; //声道数
        private int samplespersec = 16000; //采样频率，单位：Hz

        public WavRecorder(string dName)
        {
            _dName = dName;
        }

        public void Record()
        {
            //mciSendString("set wave bitspersample 16", "", 0, 0);

            //mciSendString("set wave samplespersec 16000", "", 0, 0);
            //mciSendString("set wave channels 1", "", 0, 0);
            //mciSendString("set wave bytespersec 256000", "", 0, 0);

            //mciSendString("set wave format tag pcm", "", 0, 0);
            //mciSendString("open new type WAVEAudio alias movie", "", 0, 0);

            //mciSendString("record movie", "", 0, 0);
            mciSendString("open new Type waveaudio Alias " + _dName, "", 0, 0);
            mciSendString("stop " + _dName, "", 0, 0);

            
            int bytespersec = bitspersample * channels * samplespersec / 8; //每秒的字节数
            int alignment = bitspersample * channels / 8; //每个时刻样本的字节数

            string command = "set " + _dName + " time format ms";
            command += " bitspersample " + bitspersample;
            command += " channels " + channels;
            command += " samplespersec " + samplespersec;
            command += " bytespersec " + bytespersec;
            command += " alignment " + alignment;
            //command += " set waveaudio format tag pcm";
            //command += " set file format wav";
            string _mciReturnData = "";
            int error = mciSendString(command, _mciReturnData, 0, 0);


            mciSendString("record " + _dName, "", 0, 0);
        }

        public void Stop()
        {
            mciSendString("stop " + _dName, "", 0, 0);
        }

        public void Save(string filename = "1.wav")
        {
            mciSendString("save " + _dName + " " + filename, "", 0, 0);
        }

        public void Close()
        {
            mciSendString("close " + _dName, "", 0, 0);
        }

        public void Play()
        {
            //long P1 = 0, P2 = 3000;
            //mciSendString("seek recsound to", P1.ToString(), 0, 0);
            mciSendString("seek " + _dName + " to start", "", 0, 0);
            mciSendString("play " + _dName, "", 0, 0);
        }
    }
}
