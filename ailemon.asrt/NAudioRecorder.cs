using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using System.Threading;

namespace Ailemon.Asrt
{
    class NAudioRecorder
    {
        private WaveIn wave;
        private WaveFileWriter waveWriter;//opening memoryStream and write in dataavailable event
        private Stream audioStream; //for save 
        private ReaderWriterLock wrl = new ReaderWriterLock();

        /// <summary>
        /// 返回可用录音设备数
        /// </summary>
        public static int DeviceCount
        {
            get
            {
                return WaveIn.DeviceCount;
            }
        }

        public NAudioRecorder(int device, int sampleRate, int channels, Action<float> dataAvailableHandler)
        {
            int waveInDevices = WaveIn.DeviceCount;
            if (waveInDevices < 1)
            {
                throw new Exception("there's no connectable devices in computer : AudioRecord constructor");
            }

            wave = new WaveIn();
            wave.DeviceNumber = device;
            wave.DataAvailable += (sender, e) =>
            {
                waveMaxSample(e, dataAvailableHandler);
            };
            wave.RecordingStopped += OnRecordingStopped;

            wave.WaveFormat = new WaveFormat(sampleRate, channels);
        }

        /// <summary>
        /// 启动录音
        /// </summary>
        public void Start()
        {
            audioStream = new MemoryStream();
            //WaveFileWriter with ignoredisposesream memorystream
            waveWriter = new WaveFileWriter(new IgnoreDisposeStream(audioStream), wave.WaveFormat);
            wave.StartRecording();
        }

        /// <summary>
        /// 停止录音
        /// </summary>
        /// <returns></returns>
        public Stream Stop()
        {
            if (waveWriter == null || audioStream == null)
                return null;

            wave.StopRecording();

            audioStream.Seek(0, SeekOrigin.Begin);
            return audioStream;
        }

        /// <summary>
        /// 提取音频流
        /// </summary>
        /// <returns></returns>
        public Stream PopMemoryStream()
        {
            if (waveWriter == null || audioStream == null)
                return null;

            Stream popedStream = audioStream;

            wrl.AcquireWriterLock(1000);
            wave.StopRecording();
            audioStream = new MemoryStream();
            //WaveFileWriter with ignoredisposesream memorystream
            waveWriter = new WaveFileWriter(new IgnoreDisposeStream(audioStream), wave.WaveFormat);
            wave.StartRecording();

            wrl.ReleaseWriterLock();
            popedStream.Seek(0, SeekOrigin.Begin);
            return popedStream;
        }

        private void waveMaxSample(WaveInEventArgs e, Action<float> handler)
        {
            if (waveWriter != null) //Aliving waveWrter(filewriter), write all bytes in buffer
            {
                waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                waveWriter.Flush();
            }

            float maxRecorded = 0.0f;
            for (int i = 0; i < e.BytesRecorded; i += 2) //loop for bytes
            {
                //convert to float
                short sample = (short)((e.Buffer[i + 1] << 8) |
                                e.Buffer[i + 0]);
                var sample32 = sample / 32768f;

                if (sample32 < 0) sample32 = -sample32; // alter to absolute value 
                if (sample32 > maxRecorded) maxRecorded = sample32; //update maximum 
            }

            handler(maxRecorded); //pass the handle
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            /*
            if (waveWriter != null)
            {
                waveWriter.Close();
                waveWriter = null;
            }
            */
        }

    }
}
