using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace UdpClientScreen
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Socket socketMain;
        static EndPoint clientEndPoint;
        static BitmapImage imgsource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            socketMain = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            string ipSrv = "127.0.0.1";
            int port = 12345;
            // куда мы отправляем посылку
            clientEndPoint = new IPEndPoint(
                IPAddress.Parse(ipSrv), port);
            SendProc(null);

        }
        public async void SendProc(object obj)
        {
            byte[] bytes = await ThreadSendReceiveAsync();

            MemoryStream memorystream = new MemoryStream();
            memorystream.Write(bytes, 0, (int)bytes.Length);

            imgsource = new BitmapImage();
            imgsource.BeginInit();
            imgsource.StreamSource = memorystream;
            imgsource.EndInit();
            imageScreen.Source = imgsource; // реальный Image
        }

        // определение асинхронного метода
        static async Task<byte[]> ThreadSendReceiveAsync()
        {
            return await Task.Run(() => ThreadRoutine());
        }

        static byte[] ThreadRoutine()
        {
            int size = socketMain.SendTo(
                Encoding.UTF8.GetBytes("Hello"),
                clientEndPoint);

            byte[] buf = new byte[64 * 1024];  // рекомендуется для UDP
            MemoryStream ms = new MemoryStream();
            //            FileStream outFile = new FileStream(FileName, FileMode.Create);

            // получить кол-во посылок
            socketMain.ReceiveFrom(buf, ref clientEndPoint);
            int cntRecive = BitConverter.ToInt32(buf, 0);
            //MessageBox.Show($"cntRecive = {cntRecive}");

            // прием частей файла
            int fullSize = 0;
            for (int i = 0; i < cntRecive; i++)
            {
                int recSize = socketMain.ReceiveFrom(buf, ref clientEndPoint);
                if (recSize <= 0) break;
                ms.Write(buf, 0, recSize);
                fullSize += recSize;
            }

            byte[] bytes = ms.GetBuffer();

            ms.Flush();  // принудительный сброс данных
            ms.Close();  // принудительное закрытие потоков
            return bytes;

        }
    }
}
