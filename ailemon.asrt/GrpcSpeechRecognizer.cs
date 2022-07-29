using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc;
using Grpc.Core;
using Ailemon.Asrt.Grpc;
using System.IO;

namespace Ailemon.Asrt
{
    class GrpcSpeechRecognizer:BaseSpeechRecognizer
    {
        protected int _wavDataMaxLength = 16000 * 2 * 16;
        protected Channel grpcChannel;
        protected AsrtGrpcService.AsrtGrpcServiceClient _grpcClient;

        // 定义流式识别回调委托事件
        public delegate void StreamCallback(AsrtApiResponse response);
        public event StreamCallback OnReceiveStreamResponse;
        public delegate AsrtApiSpeechRequest StreamSendRequest();
        public event StreamSendRequest OnSendStreamRequest;
        public GrpcSpeechRecognizer(string host, string port, string protocol)
        {
            this._host = host;
            this._port = port;
            this._protocol = protocol;
            this.grpcChannel = new Channel(host, Convert.ToInt32(port), ChannelCredentials.Insecure);
            this._grpcClient = new AsrtGrpcService.AsrtGrpcServiceClient(this.grpcChannel);
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

            SpeechRequest request = new SpeechRequest();
            request.WavData.Samples = Google.Protobuf.ByteString.CopyFrom(wavData);
            request.WavData.SampleRate = sampleRate;
            request.WavData.Channels = channels;
            request.WavData.ByteWidth = byteWidth;

            TextResponse response = await this._grpcClient.AllAsync(request);
            AsrtApiResponse responseBody = new AsrtApiResponse(response.StatusCode, response.StatusMessage, response.TextResult);
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

            SpeechRequest request = new SpeechRequest();
            request.WavData.Samples = Google.Protobuf.ByteString.CopyFrom(wavData);
            request.WavData.SampleRate = sampleRate;
            request.WavData.Channels = channels;
            request.WavData.ByteWidth = byteWidth;

            SpeechResponse response = await this._grpcClient.SpeechAsync(request);
            AsrtApiResponse responseBody = new AsrtApiResponse(response.StatusCode, response.StatusMessage, response.ResultData);
            return responseBody;
        }

        /// <summary>
        /// 异步调用ASRT语音识别语言模型
        /// </summary>
        /// <param name="sequencePinyin"></param>
        /// <returns></returns>
        public override async Task<AsrtApiResponse> RecogniteLanguageAsync(string[] sequencePinyin)
        {
            LanguageRequest request = new LanguageRequest();
            request.Pinyins.Add(sequencePinyin);
            TextResponse response = await this._grpcClient.LanguageAsync(request);
            AsrtApiResponse responseBody = new AsrtApiResponse(response.StatusCode, response.StatusMessage, response.TextResult);
            return responseBody;
        }

        /// <summary>
        /// 启动ASRT进行WAVE音频文件的流式识别
        /// </summary>
        /// <param name="context"></param>
        public async void RecogniteStreamAsync(Dictionary<string, object> context)
        {
            if (this.OnReceiveStreamResponse == null)
            {
                throw new Exception("Callback Function is not defined for `OnReceiveStreamResponse`.");
            }

            if(this.OnSendStreamRequest == null)
            {
                throw new Exception("Callback Function is not defined for `OnSendStreamRequest`.");
            }

            while(!context.ContainsKey("close") || !Convert.ToBoolean(context["close"]))
            {
                SpeechRequest request = new SpeechRequest();
                AsrtApiSpeechRequest apiRequest = OnSendStreamRequest();
                request.WavData.Samples = Google.Protobuf.ByteString.CopyFrom(apiRequest.ByteSamples);
                request.WavData.SampleRate = apiRequest.SampleRate;
                request.WavData.Channels = apiRequest.Channels;
                request.WavData.ByteWidth = apiRequest.ByteWidth;

                var streamCall = this._grpcClient.Stream();
                await streamCall.RequestStream.WriteAsync(request);

                AsrtApiResponse responseBody = new AsrtApiResponse();
                responseBody.StatusCode = streamCall.ResponseStream.Current.StatusCode;
                responseBody.StatusMessage = streamCall.ResponseStream.Current.StatusMessage;
                responseBody.Result = streamCall.ResponseStream.Current.TextResult;

                OnReceiveStreamResponse(responseBody);
            }
        }

        /// <summary>
        /// 调用ASRT进行WAVE音频文件的长语音序列识别
        /// </summary>
        /// <param name="wavData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="byteWidth"></param>
        /// <returns></returns>
        public async Task<List<AsrtApiResponse>> RecogniteLongAsync(byte[] wavData, int sampleRate, int channels, int byteWidth)
        {
            if (sampleRate != 16000)
            {
                string exceptMsg = string.Format("Unsupport wave sample rate `%d`", sampleRate);
                throw new Exception(exceptMsg);
            }

            if (channels != 1)
            {
                string exceptMsg = string.Format("Unsupport wave channels number `%d`",channels);
                throw new Exception(exceptMsg);
            }

            if (byteWidth != 2)
            {
                string exceptMsg = string.Format("Unsupport wave byte width `%d`", byteWidth);
                throw new Exception(exceptMsg);
            }

            List<AsrtApiResponse> asrt_result = new List<AsrtApiResponse>();
            int duration = 2 * 16000 * 10;
            for (int index = 0; index < wavData.Length / duration + 1; index++)
            {
                byte[] wavDataSplit = new byte[duration];
                wavData.CopyTo(wavDataSplit, index);
                AsrtApiResponse rsp = await RecogniteAsync(wavDataSplit, sampleRate, channels, byteWidth);
                asrt_result.Add(rsp);
            }

            return asrt_result;
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

            return await this.RecogniteLongAsync(wav.byteWavs, wav.sampleRate, wav.channels, wav.byteWidth);
        }
    }
}
