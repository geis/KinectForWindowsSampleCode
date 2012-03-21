using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace Kinect_RGB
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
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
                // RGBカメラを有効
                kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                // フレーム更新イベントを登録
                kinect.ColorFrameReady +=                   
                  new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);

                // Kinectの動作を開始する
                kinect.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// RGBカメラのフレーム更新イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            try
            {
                // RGBカメラのフレームデータを取得する
                using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
                {
                    if (colorFrame != null)
                    {
                        // RGBカメラのピクセルデータを取得する
                        byte[] colorPixel = new byte[colorFrame.PixelDataLength];
                        colorFrame.CopyPixelDataTo(colorPixel);

                        // ピクセルデータをビットマップに変換する
                        RGBCameraImage.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96,
                            PixelFormats.Bgr32, null, colorPixel, colorFrame.Width * colorFrame.BytesPerPixel);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                kinect.ColorFrameReady -= kinect_ColorFrameReady;

                kinect.Stop();          // Kinectの停止
                kinect.Dispose();       // ネイティブリソースを解放する
                kinect = null;
            }
        }
    }
}
