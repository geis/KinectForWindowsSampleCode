using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace Kinect_Depth
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly int Bgr32BytesPerPixel = PixelFormats.Bgr32.BitsPerPixel / 8;

        KinectSensor kinect;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Kinectが接続されているかどうかを確認する
                if (KinectSensor.KinectSensors.Count == 0)
                {
                    throw new Exception("Kinectを接続してください");
                }

                // 現在認識されているKinectから一つを取り出す
                kinect = KinectSensor.KinectSensors[0];
                // デプスカメラを有効
                kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                // フレーム更新イベントを登録
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);

                // Kinectの動作を開始する
                kinect.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// デプスカメラのフレーム更新イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            try
            {
                // 距離カメラのフレームデータを取得する
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    if (depthFrame != null)
                    {
                        // 距離データを画像化して表示
                        DepthCameraImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96,
                            PixelFormats.Bgr32, null, ConvertDepthToColorImage(kinect, depthFrame),
                            depthFrame.Width * Bgr32BytesPerPixel);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 距離データをカラー画像に変換する
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="depthFrame"></param>
        /// <returns></returns>
        private byte[] ConvertDepthToColorImage(KinectSensor kinect, DepthImageFrame depthFrame)
        {
            DepthImageStream depthStream = kinect.DepthStream;

            // 距離カメラのピクセルごとのデータを取得する
            short[] depthPixel = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(depthPixel);

            byte[] depthColor = new byte[depthFrame.PixelDataLength * Bgr32BytesPerPixel];
            for (int index = 0; index < depthPixel.Length; index++)
            {
                // 距離カメラのデータから、距離を取得する
                int distance = depthPixel[index] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                int colorIndex = index * Bgr32BytesPerPixel;

                // 赤：サポート外 0-40cm
                if (distance == depthStream.UnknownDepth)
                {
                    depthColor[colorIndex] = 0;
                    depthColor[colorIndex + 1] = 0;
                    depthColor[colorIndex + 2] = 255;
                }
                // 緑：近すぎ 40cm-80cm(default mode)
                else if (distance == depthStream.TooNearDepth)
                {
                    depthColor[colorIndex] = 0;
                    depthColor[colorIndex + 1] = 255;
                    depthColor[colorIndex + 2] = 0;
                }
                // 青：遠すぎ 3m(Near),4m(Default)-8m
                else if (distance == depthStream.TooFarDepth)
                {
                    depthColor[colorIndex] = 255;
                    depthColor[colorIndex + 1] = 0;
                    depthColor[colorIndex + 2] = 0;
                }
                // 有効な距離データ
                else
                {
                    byte color = (byte)(255 * distance / depthStream.TooFarDepth);

                    depthColor[colorIndex] = 0;
                    depthColor[colorIndex + 1] = color;
                    depthColor[colorIndex + 2] = color;
                }
            }

            return depthColor;
        }

        /// <summary>
        /// Windowsが閉じられるときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Kinectの停止処理
            if (kinect.IsRunning)
            {
                // フレーム更新イベントを削除する
                kinect.DepthFrameReady -= kinect_DepthFrameReady;

                kinect.Stop();          // Kinectの停止
                kinect.Dispose();       // ネイティブリソースを解放する
                kinect = null;
            }
        }
    }
}
