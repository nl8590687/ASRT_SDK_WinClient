using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ailemon.Asrt
{
    /// <summary>
    /// ASRT语音识别系统SDK
    /// </summary>
    public static class SDK
    {
        /// <summary>
        /// 获取一个语音识别接口调用实例化对象
        /// </summary>
        /// <param name="host">主机域名或IP</param>
        /// <param name="port">主机端口号</param>
        /// <param name="protocol">网络协议类型</param>
        /// <returns>一个语音识别接口调用实例化对象</returns>
        public static BaseSpeechRecognizer GetSpeechRecognizer(string host, string port, string protocol)
        {
            if(protocol.ToLower() == "http" || protocol.ToLower() == "https")
            {
                return new HttpSpeechRecognizer(host, port, protocol);
            }
            return null;
        }

        /// <summary>
        /// 从流中读取Wave数据
        /// </summary>
        /// <param name="istream">流</param>
        /// <returns>WaveData类型对象</returns>
        public static WaveData ReadWaveDatas(Stream istream)
        {
            WaveData wav = Common.WaveStreamRead(istream);
            return wav;
        }

        /// <summary>
        /// 从文件中读取Wave数据
        /// </summary>
        /// <param name="filename">wave文件名</param>
        /// <returns>WaveData类型对象</returns>
        public static WaveData ReadWaveDatasFromFile(string filename)
        {
            FileStream fstream = new FileStream(filename, FileMode.Open);
            WaveData wav = Common.WaveStreamRead(fstream);
            return wav;
        }
    }
}
