using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace Kinect_Skeleton
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
                
                // フレーム更新イベントを登録
                kinect.SkeletonFrameReady +=new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

                // スケルトンを有効
                kinect.SkeletonStream.Enable();

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
        /// スケルトンのフレーム更新イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            try
            {
                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame != null)
                    {
                        DrawSkeleton(skeletonFrame);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        /// <summary>
        /// スケルトンの位置を描画する
        /// </summary>
        /// <param name="skeletonFrame"></param>
        private void DrawSkeleton(SkeletonFrame skeletonFrame)
        {
            // スケルトンのデータを取得する
            Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
            skeletonFrame.CopySkeletonDataTo(skeletons);

            SkeletonCanvas.Children.Clear();

            // スケルトンのジョイントを描画する
            foreach (Skeleton skeleton in skeletons)
            {
                // スケルトンがトラッキング状態の場合は、ジョイントを描画する
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // ジョイントを描画する
                    foreach (Joint joint in skeleton.Joints)
                    {
                        // ジョイントがトラッキングされていなければ次へ
                        if (joint.TrackingState == JointTrackingState.NotTracked)
                        {
                            continue;
                        }

                        // ジョイントの座標を描く
                        DrawEllipse(joint.Position);
                    }
                }
            }
        }

        /// <summary>
        /// ジョイントの円を描く
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="position"></param>
        private void DrawEllipse(SkeletonPoint position)
        {
            // 円の半径
            const int R = 5;

            // スケルトンの座標を、RGBカメラの座標に変換する
            ColorImagePoint point = kinect.MapSkeletonPointToColor(position, kinect.ColorStream.Format);

            // 円を描く
            SkeletonCanvas.Children.Add(new Ellipse()
            {
                Fill = new SolidColorBrush(Colors.Red),
                Margin = new Thickness(point.X - R, point.Y - R, 0, 0),
                Width = R * 2,
                Height = R * 2,
            });
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
                kinect.SkeletonFrameReady -= kinect_SkeletonFrameReady;

                kinect.Stop();          // Kinectの停止
                kinect.Dispose();       // ネイティブリソースを解放する
                kinect = null;
            }
        }
    }
}
