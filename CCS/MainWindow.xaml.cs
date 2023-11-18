using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Point> POIs;
        private List<Sensor> Sensors;

        public MainWindow()
        {
            InitializeComponent();
            POIs = new List<Point>();
            Sensors = new List<Sensor>();
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
                if(Sensors.Count > 0)
                    DrawSensors();
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

        private void DrawSensors()
        {
            foreach (Sensor sensor in Sensors)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 1,
                    Height = 1,
                    Fill = Brushes.Red,
                };

                Canvas.SetLeft(ellipse, sensor.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, sensor.Y - ellipse.Height / 2);

                myCanvas.Children.Add(ellipse);
                Trace.WriteLine(sensor.X);
            }
        }

        private void readWSN(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = openFileDialog.FileName;
                    string[] fileLines = File.ReadAllLines(fileName);
                    
                    Sensors = new List<Sensor>();
                    for (int i = 1; i< fileLines.Length; i++)
                    {
                        string[] coordinates = fileLines[i].Split(' ');
                        Sensors.Add(
                            new Sensor(
                                double.Parse(coordinates[0], CultureInfo.InvariantCulture), 
                                double.Parse(coordinates[1], CultureInfo.InvariantCulture)
                            )
                        );
                    }
                    DrawPoints();
                    DrawSensors();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}");
                }
            }
            
        }
    }
}