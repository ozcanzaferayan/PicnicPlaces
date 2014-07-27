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
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Win32;
using System.IO;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data;
using System.Data.Objects;

namespace PicnicProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Attributes

        // Çizim işlemi için kullandığımız polyline ve polygon
        MapPolyline polyline = null;
        MapPolygon polygon = null;      
        byte[] imageData;               // Görüntü dosyasını tutacağımız byte dizisi
        List<StackPanel> stackPanels = new List<StackPanel>();

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            SetUpPolyline();
            SetUpPolygon();
            ShowPlaces();
        }

        #endregion

        #region Inserting Place To Database

        public void InsertPlace()
        {
            using (var context = new piknik_7_mayisEntities())
            {
                var piknikYeri = new piknik_yerleri 
                {
                    name = txtPolygonName.Text,
                    location = System.Data.Spatial.DbGeography.PolygonFromText(GetPolygonText(polygon), 4326),
                    picture = imageData
                };
                context.piknik_yerleri.Add(piknikYeri);
                try
                {
                    context.SaveChanges();
                    MessageBox.Show(txtPolygonName.Text + " bölgesi eklendi!");
                    ImageLayer.Children.Clear();
                    ShowPlaces();
                }
                catch (Exception)
                {
                    MessageBox.Show("Beklenmedik bir hata oluştu!");
                }
            }
        }

        #endregion

        #region Drawing Place Line

        public void DrawLine()
        {
            if (polyline.Locations.Count >= 2) // Çizgi olabilmesi için en az iki nokta gerekir 
            {
                ShapeLayer.Children.Clear();
                ShapeLayer.Children.Add(polyline);
            }
        }

        #endregion

        #region Initializing Polygon and Polyline

        public void SetUpPolygon()
        {
            polygon = new MapPolygon();
            polygon.Fill = new SolidColorBrush(Colors.Blue);
            polygon.Opacity = 0.5;
            polygon.Stroke = new SolidColorBrush(Colors.BlueViolet);
            polygon.StrokeThickness = 5;
            polygon.Locations = new LocationCollection();
        }

        public void SetUpPolyline()
        {
            polyline = new MapPolyline();
            polyline.Locations = new LocationCollection();
            polyline.Stroke = new SolidColorBrush(Colors.Blue);
            polyline.StrokeThickness = 3;
            polyline.Opacity = 0.5;
        }

        #endregion

        #region Generatin Polygon Text From Polygon

        public string GetPolygonText(MapPolygon polygon)
        {
            string polygonText = "POLYGON((";
            for (int i = 0; i < polygon.Locations.Count; i++)
            {
                polygonText += polygon.Locations[i].Latitude + " " + polygon.Locations[i].Longitude + ", ";
            }
            polygonText += polygon.Locations[0].Latitude + " " + polygon.Locations[0].Longitude;
            polygonText += "))";

            return polygonText;
        }

        #endregion

        #region Map Double Clicking

        private void MyMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Point mousePosition = e.GetPosition(this);
            mousePosition.X = mousePosition.X - 163; // Expander genişliği olan 163 değeri çıkartılmalı
            Location polylinePointLocation = MyMap.ViewportPointToLocation(mousePosition);
            polyline.Locations.Add(polylinePointLocation);
            DrawLine();
        }

        #endregion

        private void btnGeneratePolygon_Click(object sender, RoutedEventArgs e)
        {
            if (polyline.Locations.Count < 3)
            {
                MessageBox.Show("Bir polygon oluşturmadınız!");
                return;
            }
            polygon.Locations = polyline.Locations;
            ShapeLayer.Children.Clear();
            ShapeLayer.Children.Add(polygon);

        }

        private void btnCleanPolygon_Click(object sender, RoutedEventArgs e)
        {
            ShapeLayer.Children.Clear();
            polygon = null;
            polyline = null;
            SetUpPolyline();
            SetUpPolygon();
        }

        private void btnUploadPolygon_Click(object sender, RoutedEventArgs e)
        {
            InsertPlace();
        }

        private void txtPolygonName_GotMouseCapture(object sender, MouseEventArgs e)
        {
            txtPolygonName.SelectAll();
        }

        public void UploadImage()
        {
            string filePath;
            int maxImageSize = 1000000; // 1MB maksimum resim büyüklüğü
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
            Nullable<bool> result = dialog.ShowDialog();
            if (result == true)
            {
                filePath = dialog.FileName;
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                if (fs.Length>1000000)
                {
                    MessageBox.Show("Max 1 MB büyüklüğünde dosya girmelisiniz");
                    return;
                }
                BinaryReader br = new BinaryReader(fs);
                imageData = br.ReadBytes(maxImageSize);
                txtPlacePicturename.Text = filePath;
            }
        }

        private void btnPictureUpload_Click(object sender, RoutedEventArgs e)
        {
            UploadImage();
        }

        public void ShowPlaces()
        {
            using (var context = new piknik_7_mayisEntities())
            {
                var piknikYerleri = from py in context.piknik_yerleri
                                    select py;
                IList<piknik_yerleri> piknikPlaceList = piknikYerleri.ToList<piknik_yerleri>();
                for (int i = 0; i < piknikPlaceList.Count; i++)
                {
                    if (piknikPlaceList[i].picture !=null)
                    {
                        byte[] piknikPicture = piknikPlaceList[i].picture;
                        System.Data.Spatial.DbGeography piknikPlacePolygon = piknikPlaceList[i].location;
                        MemoryStream ms = new MemoryStream(piknikPicture, 0, piknikPicture.Length);
                        ms.Write(piknikPicture, 0, piknikPicture.Length);
                        ms.Seek(0, SeekOrigin.Begin);
                        System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(ms);
                        BitmapSource bmsource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        Image image = new Image();
                        image.Source = bmsource;
                        Location l = new Location();
                        l.Latitude = piknikPlacePolygon.StartPoint.Longitude.Value;
                        l.Longitude = piknikPlacePolygon.StartPoint.Latitude.Value;
                        image.ToolTip = piknikPlaceList[i].name.ToString() + "\n" + l.Latitude;
                        StackPanel s = new StackPanel();
                        s.Height = 100;
                        s.Width = 100;
                        s.Children.Add(image);
                        Border myBorder1 = new Border();
                        myBorder1.Background = Brushes.White;
                        myBorder1.BorderBrush = Brushes.Black;
                        myBorder1.BorderThickness = new Thickness(1);
                        TextBlock txt1 = new TextBlock();
                        txt1.Foreground = Brushes.Black;
                        txt1.FontSize = 12;
                        txt1.Text = piknikPlaceList[i].name.ToString() + "\n" + l.Latitude;
                        myBorder1.Child = txt1;
                        MouseEventHandler m = new MouseEventHandler(ResizeImage);
                        s.MouseEnter += m;
                        s.MouseLeave += m;
                        s.Children.Add(myBorder1);
                        PositionOrigin position = PositionOrigin.Center;
                        stackPanels.Add(s);
                        ImageLayer.AddChild(s, l, position);
                    }
                }
            }
        }

        public void ResizeImage(object sender, EventArgs e)
        {
            StackPanel s = (StackPanel)sender;
            ImageLayer.Children.Remove(s);
            ImageLayer.Children.Add(s);
            if (s.Height==100)
            {
                s.Height = 300;
                s.Width = 300;
            }
            else
            {
                s.Height = 100;
                s.Width = 100;
            }
        }

    }
}
