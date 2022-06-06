using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ailemon.Asrt
{
    public class HttpSpeechRecognizer: BaseSpeechRecognizer
    {
        protected string _subPath = "";
        protected string _url = "";
        protected int _wavDataMaxLength = 16000 * 2 * 16;
        public HttpSpeechRecognizer(string host, string port, string protocol, string subPath="")
        {
            this._host = host;
            this._port = port;
            this._protocol = protocol;
            this._subPath = subPath;

            _url = protocol + "://" + host + ":" + port;
        }

        /// <summary>
        /// 异步调用ASRT语音识别完整功能
        /// </summary>
        /// <param name="wavData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="byteWidth"></param>
        /// <returns></returns>
        public override async Task<AsrtApiResponse> RecogniteAsync(byte[] wavData, int sampleRate, int channels, int byteWidth)
        {
            if (wavData.Length > _wavDataMaxLength)
            {
                string exceptMsg = string.Format("%s `%d`, %s `%d`",
                    "Too long wave sample byte length:", wavData.Length,
                    "the max length is", _wavDataMaxLength);
                throw new Exception(exceptMsg);
            }

            AsrtApiSpeechRequest requestBody = new AsrtApiSpeechRequest(wavData, sampleRate, channels, byteWidth);
            string rsp = await Common.HttpPostAsync(_url + _subPath + "/all", "application/json", requestBody.ToJson());
            AsrtApiResponse responseBody = new AsrtApiResponse();
            responseBody.FromJson(rsp);
            return responseBody;
        }

        /// <summary>
        /// 异步调用ASRT语音识别声学模型
        /// </summary>
        /// <param name="wavData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="byteWidth"></param>
        /// <returns></returns>
        public override async Task<AsrtApiResponse> RecogniteSpeechAsync(byte[] wavData, int sampleRate, int channels, int byteWidth)
        {
            if (wavData.Length > _wavDataMaxLength)
            {
                string exceptMsg = string.Format("%s `%d`, %s `%d`",
                    "Too long wave sample byte length:", wavData.Length,
                    "the max length is", _wavDataMaxLength);
                throw new Exception(exceptMsg);
            }

            AsrtApiSpeechRequest requestBody = new AsrtApiSpeechRequest(wavData, sampleRate, channels, byteWidth);
            string rsp = await Common.HttpPostAsync(_url + _subPath + "/speech", "application/json", requestBody.ToJson());
            AsrtApiResponse responseBody = new AsrtApiResponse();
            responseBody.FromJson(rsp);
            return responseBody;
        }

        /// <summary>
        /// 异步调用ASRT语音识别语言模型
        /// </summary>
        /// <param name="sequencePinyin"></param>
        /// <returns></returns>
        public override async Task<AsrtApiResponse> RecogniteLanguageAsync(string[] sequencePinyin)
        {
            AsrtApiLanguageRequest requestBody = new AsrtApiLanguageRequest(sequencePinyin);
            string rsp = await Common.HttpPostAsync(_url + _subPath + "/language", "application/json", requestBody.ToJson());
            AsrtApiResponse responseBody = new AsrtApiResponse();
            responseBody.FromJson(rsp);
            return responseBody;
        }

        /// <summary>
        /// 调用ASRT进行WAVE音频文件的语音识别
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public new async Task<List<AsrtApiResponse>> RecogniteFile(string filename)
        {
            FileStream fstream = new FileStream(filename, FileMode.Open);
            WaveData wav = Common.WaveStreamRead(fstream);

            if(wav.sampleRate != 16000)
            {
                string exceptMsg = string.Format("Unsupport wave sample rate `%d`", wav.sampleRate);
                throw new Exception(exceptMsg);
            }
            
            if(wav.channels != 1)
            {
                string exceptMsg = string.Format("Unsupport wave channels number `%d`", wav.channels);
                throw new Exception(exceptMsg);
            }

            if(wav.byteWidth != 2)
            {
                string exceptMsg = string.Format("Unsupport wave byte width `%d`", wav.byteWidth);
                throw new Exception(exceptMsg);
            }

            List<AsrtApiResponse> asrt_result = new List<AsrtApiResponse>();
            int duration = 2 * 16000 * 10;
            for(int index = 0; index<wav.byteWavs.Length / duration + 1; index++)
            {
                AsrtApiResponse rsp = await RecogniteAsync(wav.byteWavs, wav.sampleRate, wav.channels, wav.byteWidth);
                asrt_result.Add(rsp);
            }

            return asrt_result;
        }
    }
}
