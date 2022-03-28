using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ailemon.Asrt;

namespace ASRT_SpeechClient_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        AsrtClientProxy _clientProxy;

        string filename_conf = "conf.txt";
        string host = "127.0.0.1";
        string port = "20001";
        string protocol = "http";

        string textBuffer = "";

        public MainWindow()
        {
            InitializeComponent();

            MessageBox.Visibility = Visibility.Collapsed;
            System.Threading.Thread.Sleep(1000); //延迟启动1秒，以便人们看清闪屏上的字

            if (System.IO.File.Exists(filename_conf))
            {
                string[] conf_lines = System.IO.File.ReadAllLines(filename_conf);
                protocol = conf_lines[0];
                host = conf_lines[1];
                port = conf_lines[2];
                textbox_url.Text = protocol + "/" + host + "/" + port;
            }
            _clientProxy = new AsrtClientProxy(host, port, protocol);
            _clientProxy.SetRecorderDevice(0);
            _clientProxy.OnReceiveText += SpeechRecognizer_OnReceiveText;
            Console.WriteLine("MainWindow运行");
        }
        
        private void btn_start_speech_input_Click(object sender, RoutedEventArgs e)
        {
            //asr.Start();
            _clientProxy.Start();
            MessageBox.Visibility = Visibility.Visible;
        }

        private void btn_end_speech_input_Click(object sender, RoutedEventArgs e)
        {
            _clientProxy.StopAsync();
            MessageBox.Visibility = Visibility.Collapsed;
        }

        private void btn_change_url_Click(object sender, RoutedEventArgs e)
        {
            if (!_clientProxy.IsRecognizing)
            {
                string url_new = textbox_url.Text;
                string[] config_arr = url_new.Split('/');
                protocol = config_arr[0];
                host = config_arr[1];
                port = config_arr[2];
                _clientProxy = new AsrtClientProxy(host, port, protocol);
                _clientProxy.SetRecorderDevice(0);
                _clientProxy.OnReceiveText += SpeechRecognizer_OnReceiveText;

                System.IO.File.WriteAllText(filename_conf, protocol + "\n" + host + "\n" + port);
            }
        }

        private void SpeechRecognizer_OnReceiveText(object sender, AsrtResult result)
        {
            //事件处理方法  
            if (result.Confirm)
            {
                textBuffer += result.Text;
                text_note.Text = textBuffer;
            }
            else
            {
                text_note.Text = textBuffer + result.Text;
            }

            text_note.ScrollToEnd();
            Console.WriteLine("recv: {0}, {1}", result.Confirm.ToString(), result.Text);
        }

        private async void btn_recognite_file_Click(object sender, RoutedEventArgs e)
        {
            string filename = "";
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "WAVE音频文件|*.wav";
            openFileDialog.DefaultExt = "WAVE音频文件|*.wav";
            if (openFileDialog.ShowDialog() == true)
            {
                filename = openFileDialog.FileName;

                Ailemon.Asrt.BaseSpeechRecognizer sr = Ailemon.Asrt.SDK.GetSpeechRecognizer(host, port, protocol);
                Ailemon.Asrt.AsrtApiResponse rsp = (Ailemon.Asrt.AsrtApiResponse)await sr.RecogniteFile(filename);
                System.Console.WriteLine((string)rsp.Result);
                AsrtResult result = new AsrtResult("\n" + (string)rsp.Result + "\n", true, rsp.StatusCode, rsp.StatusMessage);
                SpeechRecognizer_OnReceiveText(sender, result);
            }
        }
    }
}
