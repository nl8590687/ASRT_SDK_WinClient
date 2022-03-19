using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ailemon.Asrt
{
    public class HttpSpeechRecognizer: BaseSpeechRecognizer
    {
        protected string _subPath = "";
        protected string _url = "";
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
        public override async Task<object> RecogniteAsync(byte[] wavData, int sampleRate, int channels, int byteWidth)
        {
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
        public override async Task<object> RecogniteSpeechAsync(byte[] wavData, int sampleRate, int channels, int byteWidth)
        {
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
        public override async Task<object> RecogniteLanguageAsync(string[] sequencePinyin)
        {
            AsrtApiLanguageRequest requestBody = new AsrtApiLanguageRequest(sequencePinyin);
            string rsp = await Common.HttpPostAsync(_url + _subPath + "/language", "application/json", requestBody.ToJson());
            AsrtApiResponse responseBody = new AsrtApiResponse();
            responseBody.FromJson(rsp);
            return responseBody;
        }
    }
}
