using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Threading;
//using System.Windows.Interop;


namespace UdpServerScreen
{
    class Program
    {
        static Socket srvSocket;
        static EndPoint clientEndPoint;

        static void Main(string[] args)
        {
            //1500 -- router
            srvSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            srvSocket.Bind(new IPEndPoint(
                IPAddress.Any, // 0.0.0.0.  (Loopback - 127.0.0.01)
                12345
                ));
            clientEndPoint = new IPEndPoint(0, 0);

            while (true)
            {
                byte[] buf = new byte[64 * 1024];
                int size = srvSocket.ReceiveFrom(buf, ref clientEndPoint);  //на запись clientEndPoint

                byte[] imageBytes = new byte[64 * 1024];
                imageBytes = CopyScreen();

                ThreadPool.QueueUserWorkItem(ThreadRoutine, imageBytes);

            }
            
        }

        static public byte[] CopyScreen()
        {
            var left = Screen.AllScreens.Min(screen => screen.Bounds.X);
            var top = Screen.AllScreens.Min(screen => screen.Bounds.Y);
            var right = Screen.AllScreens.Max(screen => screen.Bounds.X + screen.Bounds.Width);
            var bottom = Screen.AllScreens.Max(screen => screen.Bounds.Y + screen.Bounds.Height);
            var width = right - left;
            var height = bottom - top;

            using (var screenBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));

                    var ms = new MemoryStream();
                    screenBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] array = ms.ToArray();

                    return array;

                }
            }
        }

        static void ThreadRoutine(object obj)
        {
            int OneBlockSize = 4096;
            byte[] data;
            data = (byte[]) obj;
            MemoryStream ms = new MemoryStream(data);

            // количество посылок
            int cntRecive = (int)ms.Length / OneBlockSize;
            // размер в байтах последней посылки
            int Remainder = (int)ms.Length % OneBlockSize;

            // посылаем количество посылок
            int BlockCountToSend = cntRecive;
            if (Remainder > 0) BlockCountToSend++;
            data = BitConverter.GetBytes(BlockCountToSend);
            srvSocket.SendTo(data, clientEndPoint);

            // посылаем части файла
            byte[] buf = new byte[OneBlockSize];
            for (int i = 0; i < cntRecive; i++)
            {
                ms.Read(buf, 0, OneBlockSize);
                srvSocket.SendTo(buf, 0, OneBlockSize, SocketFlags.None, clientEndPoint);
                Thread.Sleep(20);  // без этой задержки большие файлы не пересылаются до конца
            }
            if (Remainder > 0)
            {
                ms.Read(buf, 0, Remainder);
                srvSocket.SendTo(buf, 0, Remainder, SocketFlags.None, clientEndPoint);
            }
            ms.Close();

        }
    }
}
