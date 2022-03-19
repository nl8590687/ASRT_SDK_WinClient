using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Windows.Threading;

namespace ailemon.Compat.asrt
{
    public class SpeechRecognizer
    {
        public delegate void ReceiveText(object sender, string text); //定义委托  
        public event ReceiveText OnReceiveText; //当收到语音识别后的文本的时候触发事件

        WavRecorder[] recorder = new WavRecorder[2];

        string url = "http://127.0.0.1:20000/";
        string token = "qwertasd";

        int i_audioRecorder = 0;
        WavRecorder _audioRecorder;
        WavRecorder _audioRecorder_old;
        DispatcherTimer timer;//定义定时器
        string filepath = "";

        public bool isRecognizing
        {
            get { return _is_recognizing; }
        }

        private bool _is_recognizing = false;

        public SpeechRecognizer(string url_server = "http://127.0.0.1:20000/", string token_client = "qwertasd")
        {
            url = url_server;
            token = token_client;
            recorder[0] = new WavRecorder("recsound0");
            recorder[1] = new WavRecorder("recsound1");
            _audioRecorder = recorder[0];
        }

        public void Start()
        {
            //recorder.record();

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 6);
            timer.Tick += Timer_Tick;//每6秒触发这个事件，以刷新指针
            timer.Start();

            _is_recognizing = true;
            this._audioRecorder.Record();
        }

        public async Task StopAsync()
        {
            //recorder.Stop();
            //recorder.Save();
            //recorder.Close();

            timer.Stop();
            this._audioRecorder.Stop();
            string filename = "speechfile_end.wav";
            this._audioRecorder.Save(filename);
            this._audioRecorder.Close();

            string text = "";

            try
            {
                text = await SpeechRecognizeAsync(filename);
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

            OnReceiveText(this, text); //产生事件  

            // 识别完成后删除文件
            DelWavFile(filename);
            _is_recognizing = false;

        }

        public async Task<string> RecogniteFromFile(string filename)
        {
            return await SpeechRecognizeAsync(filename);
        }

        private async void Timer_Tick(object sender, object e)
        {
            this._audioRecorder_old = this._audioRecorder;

            //停止后切换对象立即继续接力录音
            i_audioRecorder++;
            this._audioRecorder = recorder[i_audioRecorder % 2];

            this._audioRecorder.Record();
            this._audioRecorder_old.Stop();

            recorder[(i_audioRecorder + 1) % 2] = new WavRecorder("recsound" + (i_audioRecorder + 1).ToString());

            //保存文件
            string filename = "speechfile" + i_audioRecorder.ToString() + ".wav";
            this._audioRecorder_old.Save(filename);
            this._audioRecorder_old.Close();


            //timer.Stop();


            string text = "";

            try
            {
                text = await SpeechRecognizeAsync(filename);
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

            //this.text_note.Text += text;

            OnReceiveText(this, text); //产生事件  
            // 识别完成后删除文件
            DelWavFile(filename);


        }

        private async void DelWavFile(string filename)
        {
            //删除wav文件
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

        }

        private async Task<string> SpeechRecognizeAsync(string filename)
        {
            Console.WriteLine(filename);
            //将wav文件post到服务器进行语音识别
            //将识别回来的文本写入文本框
            wav wave = WaveAccess(filename);
            Int16[] wavs = wave.wavs;

            int fs = wave.fs;
            string wavs_str = "";

            string[] tmp_strs = new string[wavs.Length];
            for (int i = 0; i < wavs.Length; i++)
            {
                //tmp_strs[i] = "&wavs=" + wavs[i].ToString();
                tmp_strs[i] = wavs[i].ToString();
            }
            wavs_str = string.Join("&wavs=", tmp_strs);


            //string r = await PostDataAsync(url, "qwertasd", wavs_str, fs.ToString());

            string r = await PostAsync(url, token, fs.ToString(), wavs_str);
            return r;
        }

        private async Task<string> PostAsync(string url, string token, string fs, string wavs)
        {
            string resultContent = "";

            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            //((HttpWebRequest)request).UserAgent = "SpeechRecognitionNote";
            //((HttpWebRequest)request). = "SpeechRecognitionNote";
            request.Method = "POST";
            string postdata = "token=" + token + "&fs=" + fs + "&wavs=" + wavs;
            //request.ContentLength = postdata.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            Stream dataStream = await request.GetRequestStreamAsync();
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postdata);
            dataStream.Write(byteArray, 0, byteArray.Length);
            //dataStream.Close();
            dataStream.Dispose();
            WebResponse response = await request.GetResponseAsync();
            Stream data = response.GetResponseStream();

            StreamReader sr = new StreamReader(data);
            resultContent = sr.ReadToEnd();
            response.Dispose();
            sr.Dispose();
            return resultContent;
        }

        /// <summary>
        /// 读取wav文件
        /// </summary>
        /// <param name="filename"></param>
        private wav WaveAccess(string filename)
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

                FileStream fstream = new FileStream(filename, FileMode.Open);
                //Windows.Storage.StorageFolder s = Windows.ApplicationModel.Package.Current.InstalledLocation;
                //FileStream fs;

                //StorageFolder storageFolder = Package.Current.InstalledLocation;
                //StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

                //StorageFile storageFile = await storageFolder.GetFileAsync(filename);

                //IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read);
                //Stream s = fileStream.AsStream();
                //Stream s = fs.ReadAsync


                BinaryReader bread = new BinaryReader(fstream);
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
                fstream.Dispose();
                bread.Dispose();

                wav wave = new wav();
                wave.wavs = data;
                wave.fs = fs;
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
        private byte[] bytesReserve(byte[] sbytes)
        {
            int length = sbytes.Length;
            byte[] nbytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                nbytes[i] = sbytes[length - i - 1];
            }
            return nbytes;
        }

        private class wav
        {
            public Int16[] wavs;
            public int fs;
        }
    }


}
