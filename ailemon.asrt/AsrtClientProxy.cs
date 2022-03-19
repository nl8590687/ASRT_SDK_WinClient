using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO;

namespace Ailemon.Asrt
{
    /// <summary>
    /// ASRT语音识别调用调用SDK客户端代理
    /// </summary>
    public class AsrtClientProxy
    {
        /// <summary>
        /// 定义委托  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="result"></param>
        public delegate void ReceiveText(object sender, AsrtResult result);
        /// <summary>
        /// 当收到语音识别后的文本的时候触发事件
        /// </summary>
        public event ReceiveText OnReceiveText;

        private string _host = "";
        private string _port = "";
        private string _protocol = "";

        DispatcherTimer timer;//定义定时器
        NAudioRecorder _audioRecorder;
        /// <summary>
        /// 返回是否正在录音识别
        /// </summary>
        public bool IsRecognizing
        {
            get { return _isRecognizing; }
        }
        private bool _isRecognizing = false;
        private BaseSpeechRecognizer _speechRecognizer = null;

        public AsrtClientProxy(string host, string port, string protocol)
        {
            this._host = host;
            this._port = port;
            this._protocol = protocol;
            _speechRecognizer = SDK.GetSpeechRecognizer(host, port, protocol);

            if (NAudioRecorder.DeviceCount < 1)
            {
                throw new Exception("There is no record device found");
            }
        }

        /// <summary>
        /// 指定录音设备ID，默认为0
        /// </summary>
        /// <param name="deviceNumber"></param>
        public void SetRecorderDevice(int deviceNumber=0)
        {
            _audioRecorder = new NAudioRecorder(deviceNumber, 16000, 1, DataAvailableHandler);
        }

        private void DataAvailableHandler(float value)
        {
            //Console.WriteLine("data available handler value: {0}", value);
        }

        /// <summary>
        /// 启动并开始客户端调用ASRT语音识别
        /// </summary>
        public void Start()
        {
            if(_audioRecorder != null)
            {
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 6);
                timer.Tick += Timer_Tick;//每6秒触发这个事件，以刷新指针
                timer.Start();

                _isRecognizing = true;
                //启动录音
                _audioRecorder.Start();
                Console.WriteLine("Ailemon.Asrt.AsrtClientProxy: 已启动录音识别");
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            Console.WriteLine("Ailemon.Asrt.AsrtClientProxy: 录音周期流式传输");
            // 定时从缓存中读取wave数据，并送去语音识别
            Stream waveMemStream = _audioRecorder.PopMemoryStream();
            WaveData wav = SDK.ReadWaveDatas(waveMemStream);
            AsrtApiResponse rsp = (AsrtApiResponse)await _speechRecognizer.RecogniteAsync(wav.byteWavs, wav.sampleRate, wav.channels, wav.byteWidth);
            AsrtResult result = new AsrtResult((string)rsp.Result, true, rsp.StatusCode, rsp.StatusMessage);
            OnReceiveText(this, result);
        }

        /// <summary>
        /// 异步停止客户端调用ASRT语音识别
        /// </summary>
        public async void StopAsync()
        {
            if (_isRecognizing)
            {
                timer.Stop();
                Console.WriteLine("Ailemon.Asrt.AsrtClientProxy: 停止录音识别");

                try
                {
                    // 从缓存中读取wave数据，并送去语音识别
                    Stream waveMemStream = _audioRecorder.Stop();
                    WaveData wav = SDK.ReadWaveDatas(waveMemStream);
                    AsrtApiResponse rsp = (AsrtApiResponse)await _speechRecognizer.RecogniteAsync(wav.byteWavs, wav.sampleRate, wav.channels, wav.byteWidth);
                    AsrtResult result = new AsrtResult((string)rsp.Result, true, rsp.StatusCode, rsp.StatusMessage);
                    OnReceiveText(this, result); //产生事件  
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
                _isRecognizing = false;
            }
        }
    }
}
