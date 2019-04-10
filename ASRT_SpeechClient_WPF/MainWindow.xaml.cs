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

        public MainWindow()
        {
            InitializeComponent();

            MessageBox.Visibility = Visibility.Collapsed;
            asr = new SpeechRecognizer();
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
                asr = new SpeechRecognizer(textbox_url.Text, "qwertasd");
                asr.OnReceiveText += SpeechRecognizer_OnReceiveText;
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
