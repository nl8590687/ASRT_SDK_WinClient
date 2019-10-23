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
using ailemon.asrt;

namespace ASRT_SpeechClient_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SpeechRecognizer asr;

        string token = "qwertasd";
        string filename_conf = "conf.txt";

        public MainWindow()
        {
            InitializeComponent();

            MessageBox.Visibility = Visibility.Collapsed;

            
            string url = "http://127.0.0.1:20000/";
            
            if (System.IO.File.Exists(filename_conf))
            {
                //url = System.IO.File.ReadAllText(filename_url);
                string[] conf_lines = System.IO.File.ReadAllLines(filename_conf);
                url = conf_lines[0];
                token = conf_lines[1];
            }
            asr = new SpeechRecognizer(url, token);
            asr.OnReceiveText += SpeechRecognizer_OnReceiveText;
        }
        
        private void btn_start_speech_input_Click(object sender, RoutedEventArgs e)
        {
            asr.Start();
            MessageBox.Visibility = Visibility.Visible;
        }

        private void btn_end_speech_input_Click(object sender, RoutedEventArgs e)
        {
            asr.StopAsync();
            MessageBox.Visibility = Visibility.Collapsed;
        }

        private async void btn_change_url_Click(object sender, RoutedEventArgs e)
        {
            //text_note.Text += await asr.RecogniteFromFile("1.wav");
            //text_note.ScrollToEnd();
            if (!asr.isRecognizing)
            {
                string url_new = textbox_url.Text;
                asr = new SpeechRecognizer(url_new, token);
                asr.OnReceiveText += SpeechRecognizer_OnReceiveText;

                if (!System.IO.File.Exists(filename_conf))
                {
                    System.IO.File.WriteAllText(filename_conf, url_new + "\n" + token);
                }
            }
        }

        private void SpeechRecognizer_OnReceiveText(object sender, string text)
        {
            //事件处理方法  
            text_note.Text += text;
            text_note.ScrollToEnd();
        }
    }
}
