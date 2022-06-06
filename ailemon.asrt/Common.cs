using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Net.Http;

namespace Ailemon.Asrt
{
    static class Common
    {
        private static Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        private static string versionText = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision.ToString("0000"));
        private static string userAgentName = "ASRT-SDK-Client";
        private static string userAgentVersionInfo = string.Format("{0}-csharp", versionText);

        public static async Task<string> HttpPostAsync(string url, string contentType, string body)
        {
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(body);

            content.Headers.Remove("Content-Type");
            content.Headers.Add("Content-Type", contentType);
            client.DefaultRequestHeaders.UserAgent.Clear();
            var userAgentObj = new System.Net.Http.Headers.ProductInfoHeaderValue(new System.Net.Http.Headers.ProductHeaderValue(userAgentName, userAgentVersionInfo));
            client.DefaultRequestHeaders.UserAgent.Add(userAgentObj);

            var response = await client.PostAsync(url, content);
            var rspContent = await response.Content.ReadAsStringAsync();
            return rspContent;
        }

        /// <summary>
        /// 读取wav文件
        /// </summary>
        /// <param name="filename"></param>
        public static WaveData WaveStreamRead(Stream istream)
        {
            try
            {
                byte[] riff = new byte[4];
                byte[] riffSize = new byte[4];
                byte[] waveID = new byte[4];
                byte[] junkID = new byte[4];
                bool hasjunk = false;
                byte[] junklength = new byte[4];

                byte[] fmtID = new byte[4];
                byte[] cksize = new byte[4];
                uint waveType = 0;
                byte[] channel = new byte[2];
                byte[] sample_rate = new byte[4];
                byte[] bytespersec = new byte[4];
                byte[] blocklen_sample = new byte[2];
                byte[] bitNum = new byte[2];
                byte[] unknown = new byte[2];
                byte[] dataID = new byte[4];  //52
                byte[] dataLength = new byte[4];  //56 个字节

                //string longFileName = filepath;

                //FileStream fstream = new FileStream(filename, FileMode.Open);


                BinaryReader bread = new BinaryReader(istream);
                //BinaryReader bread = new BinaryReader(fs);
                riff = bread.ReadBytes(4); // RIFF

                if (BitConverter.ToUInt32(bytesReserve(riff), 0) != 0x52494646)
                {
                    Exception e = new Exception("该文件不是WAVE文件");
                    throw e;
                }

                riffSize = bread.ReadBytes(4); // 文件剩余长度

                if (BitConverter.ToUInt32(riffSize, 0) != bread.BaseStream.Length - bread.BaseStream.Position)
                {
                    //Exception e = new Exception("该WAVE文件损坏，文件长度与标记不一致");
                    //throw e;
                }

                waveID = bread.ReadBytes(4);

                if (BitConverter.ToUInt32(bytesReserve(waveID), 0) != 0x57415645)
                {
                    Exception e = new Exception("该文件不是WAVE文件");
                    throw e;
                }

                byte[] tmp = bread.ReadBytes(4);

                if (BitConverter.ToUInt32(bytesReserve(tmp), 0) == 0x4A554E4B)
                {
                    //包含junk标记的wav
                    junkID = tmp;
                    hasjunk = true;
                    junklength = bread.ReadBytes(4);
                    uint junklen = BitConverter.ToUInt32(junklength, 0);
                    //将不要的junk部分读出
                    bread.ReadBytes((int)junklen);

                    //读fmt 标记
                    fmtID = bread.ReadBytes(4);
                }
                else if (BitConverter.ToUInt32(bytesReserve(tmp), 0) == 0x666D7420)
                {
                    fmtID = tmp;
                }
                else
                {
                    Exception e = new Exception("无法找到WAVE文件的junk和fmt标记");
                    throw e;
                }



                if (BitConverter.ToUInt32(bytesReserve(fmtID), 0) != 0x666D7420)
                {
                    //fmt 标记
                    Exception e = new Exception("无法找到WAVE文件fmt标记");
                    throw e;
                }

                cksize = bread.ReadBytes(4);
                uint p_data_start = BitConverter.ToUInt32(cksize, 0);
                int p_wav_start = (int)p_data_start + 8;

                waveType = bread.ReadUInt16();

                if (waveType != 1)
                {
                    // 非pcm格式，暂不支持
                    Exception e = new Exception("WAVE文件不是pcm格式，暂时不支持");
                    throw e;
                }

                //声道数
                channel = bread.ReadBytes(2);

                //采样频率
                sample_rate = bread.ReadBytes(4);
                int fs = (int)BitConverter.ToUInt32(sample_rate, 0);

                //每秒钟字节数
                bytespersec = bread.ReadBytes(4);

                //每次采样的字节大小，2为单声道，4为立体声道
                blocklen_sample = bread.ReadBytes(2);

                //每个声道的采样精度，默认16bit
                bitNum = bread.ReadBytes(2);

                tmp = bread.ReadBytes(2);
                //寻找da标记
                while (BitConverter.ToUInt16(bytesReserve(tmp), 0) != 0x6461)
                {
                    tmp = bread.ReadBytes(2);
                }
                tmp = bread.ReadBytes(2);

                if (BitConverter.ToUInt16(bytesReserve(tmp), 0) != 0x7461)
                {
                    //ta标记
                    Exception e = new Exception("无法找到WAVE文件data标记");
                    throw e;
                }

                //wav数据byte长度
                uint DataSize = bread.ReadUInt32();
                //计算样本数
                long NumSamples = (long)DataSize / 2;

                if (NumSamples == 0)
                {
                    NumSamples = (bread.BaseStream.Length - bread.BaseStream.Position) / 2;
                }
                //if (BitConverter.ToUInt32(notDefinition, 0) == 18)
                //{
                //    unknown = bread.ReadBytes(2);
                //}
                //dataID = bread.ReadBytes(4);

                Int16[] data = new Int16[NumSamples];

                for (int i = 0; i < NumSamples; i++)
                {
                    //读入2字节有符号整数
                    data[i] = bread.ReadInt16();
                }

                //s.Dispose();
                //fstream.Close();
                //istream.Dispose();
                //bread.Dispose(false);

                WaveData wave = new WaveData();
                wave.wavs = data;
                wave.byteWavs = IntArrToByteArr(data);
                wave.sampleRate = fs;
                wave.channels = (int)BitConverter.ToUInt16(channel, 0);
                wave.byteWidth = (int)BitConverter.ToUInt16(blocklen_sample, 0);
                return wave;
            }
            catch (System.Exception ex)
            {
                //return null;
                throw ex;
            }
        }

        /// <summary>
        /// 字节序列转换，小端序列和大端序列相互转换
        /// </summary>
        /// <param name="sbytes"></param>
        /// <returns></returns>
        public static byte[] bytesReserve(byte[] sbytes)
        {
            int length = sbytes.Length;
            byte[] nbytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                nbytes[i] = sbytes[length - i - 1];
            }
            return nbytes;
        }

        public static byte[] IntArrToByteArr(Int16[] intArr)
        {
            int int16Size = sizeof(Int16) * intArr.Length;
            byte[] bytArr = new byte[int16Size];
            //申请一块非托管内存
            IntPtr ptr = Marshal.AllocHGlobal(int16Size);
            //复制int数组到该内存块
            Marshal.Copy(intArr, 0, ptr, intArr.Length);
            //复制回byte数组
            Marshal.Copy(ptr, bytArr, 0, bytArr.Length);
            //释放申请的非托管内存
            Marshal.FreeHGlobal(ptr);
            return bytArr;
        }
    }

    public class WaveData
    {
        public Int16[] wavs;
        public byte[] byteWavs;
        public int sampleRate;
        public int channels;
        public int byteWidth;
    }

    public class AsrtApiSpeechRequest
    {
        private byte[] _samples;
        private int _sampleRate;
        private int _channels;
        private int _byteWidth;

        public string Samples
        {
            get
            {
                string samples_base64 = Convert.ToBase64String(_samples);
                return samples_base64;
            }
        }
        public AsrtApiSpeechRequest(byte[] samples, int sampleRate, int channels, int byteWidth)
        {
            _samples = samples;
            _sampleRate = sampleRate;
            _channels = channels;
            _byteWidth = byteWidth;
        }

        public string ToJson()
        {
            string strJson = "{ \"samples\":\"" + Samples
                            + "\",\"sample_rate\":" + _sampleRate.ToString()
                            + ",\"channels\":" + _channels.ToString()
                            + ",\"byte_width\":" + _byteWidth.ToString()
                            + "}";
            return strJson;
        }
    }

    public class AsrtApiLanguageRequest
    {
        private string[] _sequence_pinyin;
        public string[] sequence_pinyin
        {
            get
            {
                return _sequence_pinyin;
            }
            set
            {
                _sequence_pinyin = value;
            }
        }
        public AsrtApiLanguageRequest(string[] sequence_pinyin)
        {
            _sequence_pinyin = sequence_pinyin;
        }

        public string ToJson()
        {
            string strJson = JsonConvert.SerializeObject(this);
            return strJson;
        }
    }

    public class AsrtApiResponse
    {
        private int _statusCode;
        private string _statusMessage;
        private object _result;

        public int StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                _statusCode = value;
            }
        }

        public string StatusMessage{
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
            }
        }

        public object Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
            }
        }

        public AsrtApiResponse()
        {

        }
        public AsrtApiResponse(int statusCode, string statusMessage, object result)
        {
            _statusCode = statusCode;
            _statusMessage = statusMessage;
            _result = result;
        }

        public void FromJson(string jsonText)
        {
            var model = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonText);
            foreach (var item in model)
            {
                if (item.Key == "status_code")
                {
                    _statusCode = (int)item.Value;
                }
                else if(item.Key == "status_message")
                {
                    _statusMessage = item.Value;
                }
                else if (item.Key == "result")
                {
                    _result = item.Value;
                }
            }
        }
    }

    public class AsrtResult
    {
        private string _text = "";
        private bool _confirm = false;
        private int _statusCode = 0;
        private string _statusMessage;

        public String Text
        {
            get
            {
                return _text;
            }
        }

        public bool Confirm
        {
            get
            {
                return _confirm;
            }
        }

        public int StatusCode
        {
            get
            {
                return _statusCode;
            }
        }

        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
        }

        public AsrtResult(string text, bool confirm, int statusCode, string statusMessage)
        {
            _text = text;
            _confirm = confirm;
            _statusCode = statusCode;
            _statusMessage = statusMessage;
        }
    }
}
