using ImageGlass;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Project.BaseLib.DataStructures;
using Project.UI.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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


using Microsoft.Win32; // 네임스페이스 추가
using Microsoft.WindowsAPICodePack.Dialogs;
using Project.BaseLib.Utils;

namespace ImageBoxEx_Test
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private ByteImage _ByteImage;

        private ImageBoxEx _IGImageView;   // ImageGlass Bottom Object

        //private Mat _Display_image;

        public new void MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ImageBoxEx ImageBox = (sender as ImageBoxEx);       // 01.27 어떤 ImageBoxEx 객체에서 발생하였는지 확인하기 위한 객체.

            System.Windows.Point wPT = new System.Windows.Point();
            wPT.X = e.X;
            wPT.Y = e.Y;

            System.Drawing.Point dPT = new System.Drawing.Point((int)wPT.X, (int)wPT.Y);

            // Wafer or SelectedDie 객체 중 선택하여 이벤트 적용.
            //if (ImageView.Name == "ImageView")
            //    _IGImageView.ZoomWithMouseWheel(e.Delta, dPT);
        }

        public Mat BitmapToMat(Bitmap bmp)
        {
            Bitmap clone = bmp.Clone(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Mat mat = BitmapConverter.ToMat(clone);
            Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY); // 필요시 제거

            clone.Dispose();
            return mat;
        }

        public new void MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //// 07.04 Modify Start
            //SequenceContext cont = ContextList["WAFER"];
            //WaferSequenceContext waferContext = ContextList["WAFER"] as WaferSequenceContext;

            ImageBoxEx IGbox = (sender as ImageBoxEx);
            Bitmap bmp = IGbox.Image as Bitmap;

            if (bmp == null)
                return;

            


            System.Drawing.Point wPT = new System.Drawing.Point();  // wPT is Client Postion. System.Windows.Point -> System.Drawing.Point
            wPT.X = e.X;
            wPT.Y = e.Y;

            System.Drawing.Point iPT = new System.Drawing.Point();  // iPT is Image Position. System.Windows.Point -> System.Drawing.Point
            iPT = IGbox.PointToImage(wPT, true);


            System.Drawing.Color c = bmp.GetPixel(iPT.X, iPT.Y);

            //var pt = _Display_image.At<Vec3b>(iPT.Y, iPT.X);

            //string str = "Pixel Point:" + "(" + iPT.X.ToString() + "," + iPT.Y.ToString() + ")" + "\r\n"
            //    + "(R,G,B:" + pt.Item2.ToString() + "," + pt.Item1.ToString() + "," + pt.Item0.ToString() + ")";


            string str = "Pixel Point:" + "(" + iPT.X.ToString() + "," + iPT.Y.ToString() + ")" + "\r\n"
                + "(R,G,B:" + c.R + "," + c.G + "," + c.B + ")";




            IGbox.TextAlign = System.Drawing.ContentAlignment.TopLeft;          // 11.01
            IGbox.TextDisplayMode = ImageBoxGridDisplayMode.Image;
            IGbox.TextBackColor = System.Drawing.Color.Beige;

            IGbox.Text = str;
            //IGbox.Text = str + "\r\n" + GetXYCenterTomm(iPT.X, iPT.Y);    // 12.17 Insert (mouse mm Coordination)
            IGbox.Refresh();
            // 07.04 Modify End
        }


        public MainWindow()
        {
            InitializeComponent();






            
            //_IGImageView = new ImageBoxEx();
            //ImageView.Child = _IGImageView;

            //_IGImageView.Name = "ImageView";
            //_IGImageView.AllowClickZoom = true;
            //_IGImageView.ShowPixelGrid = true;

            //_IGImageView.VerticalScrollBarStyle = ImageBoxScrollBarStyle.Hide;
            //_IGImageView.HorizontalScrollBarStyle = ImageBoxScrollBarStyle.Hide;

            //_IGImageView.MouseWheel += MouseWheel;
            //_IGImageView.MouseMove += MouseMove;


            //_IGImageView.MouseDown += IGWaferImage_MouseDown;
            //_IGImageView.MouseMove += IGWaferImage_MouseMove;
            //_IGImageView.MouseUp += IGWaferImage_MouseUp;
            //_IGImageView.Paint += IGWaferImage_Paint;
        }
        //public unsafe System.Drawing.Color GetPixelFast(Bitmap bmp, int x, int y)
        //{
        //    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
        //    BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

        //    int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
        //    byte* ptr = (byte*)data.Scan0;

        //    byte* p = ptr + y * data.Stride + x * bytesPerPixel;

        //    byte b = p[0];
        //    byte g = p[1];
        //    byte r = p[2];
        //    byte a = (bytesPerPixel == 4) ? p[3] : (byte)255;

        //    bmp.UnlockBits(data);

        //    return System.Drawing.Color.FromArgb(a, r, g, b);
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                //ImageView.Image = System.Drawing.Image.FromFile(dlg.FileName);

                _ByteImage = new ByteImage(dlg.FileName);


                ImageView.SetImage(_ByteImage);

                //_ByteImage = ImageView.GetByteImage();

                //_ByteImage.Save("C:\\_ByteImage.bmp");



                //int left = (int)ImageView.ImageCenter.X - nSize;
                //int top  = (int)ImageView.ImageCenter.Y - nSize;

                //int right  = (int)ImageView.ImageCenter.X + nSize;
                //int bottom = (int)ImageView.ImageCenter.Y + nSize;

                //var pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5);

                //ImageView.DrawRectangle(pen, System.Drawing.Brushes.Transparent, left, top, right, bottom); // new DPointCoordinates(left, top), new DPointCoordinates(right, bottom));

                //_Display_image = BitmapToMat(_IGImageView.Image as Bitmap);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int left = 100;
            int top = 100;

            int right = 500;
            int bottom = 500;

            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5);

            ImageView.DrawRectangle(pen, System.Drawing.Brushes.Transparent, left, top, right, bottom);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int left = 1000;
            int top = 1000;

            int radius = 100;

            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Yellow, 5);

            ImageView.DrawCircle(pen, System.Drawing.Brushes.Transparent, left, top, radius);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            int left = 700;
            int top = 100;

            int right = 1000;
            int bottom = 200;
            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Blue, 2);

            ImageView.DrawLine(pen,left, top, right, bottom);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var image_size = ImageView.ImageSize;

            var x = image_size.Width / 2.0;
            var y = image_size.Height / 2.0;

            Font font = new Font("Arial", 12);
            ImageView.DrawString("data", "TestMessage", 
                font, System.Drawing.Brushes.Red, 
                x, y,
                StringAlignment.Near, StringAlignment.Near);


            x = image_size.Width / 3.0;
            y = image_size.Height / 2.0;

            ImageView.DrawString("data", "TestMessage222",
                font, System.Drawing.Brushes.Red,
                x, y,
                StringAlignment.Near, StringAlignment.Near);


            x = image_size.Width / 3.0 * 2;
            y = image_size.Height / 2.0;

            ImageView.DrawString("data", "TestMessage33333",
                font, System.Drawing.Brushes.Red,
                x, y,
                StringAlignment.Near, StringAlignment.Near);

        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Help;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            int cx = 1000;
            int cy = 500;

            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 2);

            ImageView.DrawCrossLine(pen, cx, cy, 10);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            if (ImageView.IsEnableSelected == true)
                ImageView.IsEnableSelected = false;
            else
                ImageView.IsEnableSelected = true;
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 2);

            ImageView.DrawArrowLine(pen, 500, 500, 700, 700, ArrowDirect.Start, 5);

        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 2);

            ImageView.DrawArrowLine(pen, 1000, 1000, 1200, 1200, ArrowDirect.End, 5);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            if (ImageView.IsEnableCenterLine == true)
                ImageView.IsEnableCenterLine = false;
            else
                ImageView.IsEnableCenterLine = true;

        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            ImageView.RemoveAllShape();
        }

        private async void Button_Click_12(object sender, RoutedEventArgs e)
        {
            await ImageView.SaveImageAsync("D:\\ImageBoxEx_OnlyImage.bmp", ImageSaveTypes.OnlyImage);

            await ImageView.SaveImageAsync("D:\\ImageBoxEx_WithShapes.bmp", ImageSaveTypes.WithShapes);
        }


        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "폴더를 선택하세요";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = "D:\\2_Projects\\FastHandlerProbbing\\Doc\\2026.02.04.AVDT_probe_card_수평_Non_saturation"; // 시작 경로

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dlg.FileName;
                //string folderPath = dlg.FolderName;

                // 3. 폴더 내 파일 가져오기 (예: 모든 파일)
                string[] files = Directory.GetFiles(selectedPath);


                var rectangles = ImageView.GetAllRoiRectangles();

                if(rectangles == null)
                {
                    MessageBox.Show("ROI is null.");
                    return;
                }

                if(rectangles.Count()!= 1)
                {
                    MessageBox.Show("ROI is too much");
                    return;
                }

                int left = rectangles[0].Left;
                int top = rectangles[0].Top;
                int right = rectangles[0].Right;
                int bottom = rectangles[0].Bottom;


                int i = 0;
                // 결과 확인을 위한 출력
                foreach (string file in files)
                {
                    ByteImage byte_image = new ByteImage(file);

                    RoiRectangle roi = new RoiRectangle(top, left, bottom, right);

                    var crop_image = byte_image.Crop(roi) as ByteImage;


                    string save_path = string.Format("D:\\crop_image_{0}.bmp", i);
                    crop_image.Save(save_path);


                    var sobel_image = crop_image.SobelEdge();
                    save_path = string.Format("D:\\sobel_image_{0}.bmp", i);
                    sobel_image.Save(save_path);


                    var sum = sobel_image.Sum();
                    AppLogger.Info()("Index : {0}, Sum Value : {1}", i, sum);


                    i++;
                }

            }


        }

        private void Button_Click_14(object sender, RoutedEventArgs e)
        {

        }
    }
}
