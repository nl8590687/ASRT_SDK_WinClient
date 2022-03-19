using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ailemon.Asrt
{
    /// <summary>
    /// ASRT语音识别功能C#接口
    /// </summary>
    public interface ISpeechRecognizer
    {
        Task<object> RecogniteAsync(byte[] wavData, int sampleRate, int channels, int byteWidth);
        Task<object> RecogniteSpeechAsync(byte[] wavData, int sampleRate, int channels, int byteWidth);
        Task<object> RecogniteLanguageAsync(string[] sequencePinyin);
        Task<object> RecogniteFile(string filename);
    }

    /// <summary>
    /// ASRT语音识别功能基类
    /// </summary>
    public abstract class BaseSpeechRecognizer : ISpeechRecognizer
    {
        protected string _host = "";
        protected string _port = "";
        protected string _protocol = "";

        public BaseSpeechRecognizer()
        {

        }
        public BaseSpeechRecognizer(string host, string port, string protocol)
        {
            this._host = host;
            this._port = port;
            this._protocol = protocol;
        }

        public BaseSpeechRecognizer(string host, string port, string protocol, string subPath)
        {
            this._host = host;
            this._port = port;
            this._protocol = protocol;
        }

        public abstract Task<object> RecogniteAsync(byte[] wavData, int sampleRate, int channels, int byteWidth);
        public abstract Task<object> RecogniteSpeechAsync(byte[] wavData, int sampleRate, int channels, int byteWidth);
        public abstract Task<object> RecogniteLanguageAsync(string[] sequencePinyin);

        /// <summary>
        /// 调用ASRT进行WAVE音频文件的语音识别
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<object> RecogniteFile(string filename)
        {
            FileStream fstream = new FileStream(filename, FileMode.Open);
            WaveData wav = Common.WaveStreamRead(fstream);
            return await RecogniteAsync(wav.byteWavs, wav.sampleRate, wav.channels, wav.byteWidth);
        }
    }
}
