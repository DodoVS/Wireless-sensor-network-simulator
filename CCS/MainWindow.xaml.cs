using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Point> POIs;

        public MainWindow()
        {
            InitializeComponent();
            POIs = new List<Point>();
        }

        private void POIRadio_Checked(object sender, RoutedEventArgs e)
        {
            if(sender is RadioButton radioButton)
            {
                int numSectors = 36;
                switch (radioButton.Content)
                {
                    case "POI 36":
                        numSectors = 36;
                        break;
                    case "POI 121":
                        numSectors = 121;
                        break;
                    case "POI 441":
                        numSectors = 441;
                        break;
                }
                POIs = CreatePoints(numSectors);

                DrawPoints();
            }
        }

        private void DrawPoints()
        {
            myCanvas.Children.Clear();
            foreach (Point point in POIs)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 1,
                    Height = 1,
                    Fill = Brushes.Orange,
                };

                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                myCanvas.Children.Add(ellipse);
            }
        }

        private List<Point> CreatePoints(int numberOfPoints)
        {
            List<Point> points = new List<Point>();

            double sectors = 100 / Math.Sqrt(numberOfPoints);
            
            for(double i = 0; i < 100; i += sectors)
            {
                for(double j = 0; j < 100; j += sectors)
                {
                    points.Add(new Point(i+(sectors/2), j+(sectors/2))); 
                }
            }

            return points;
        }

        private void readWSN(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Get the selected file name
                    string fileName = openFileDialog.FileName;

                    // Read the contents of the selected file
                    string fileContent = File.ReadAllText(fileName);
                    MessageBox.Show("File content:\n" + fileContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}");
                }
            }
        }
    }
}