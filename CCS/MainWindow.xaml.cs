using Microsoft.Win32;
using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Point> POIs;
        private List<Sensor> Sensors;
        private Random random;
        private int numSectors;
        private double sensorRange;
        private string sensorFile;

        public MainWindow()
        {
            InitializeComponent();
            POIs = new List<Point>();
            Sensors = new List<Sensor>();
            random = new Random();
            CreateFolders();
        }

        private void CreateFolders()
        {
            if (!Directory.Exists("INIT-RESULTS"))
            {
                Directory.CreateDirectory("INIT-RESULTS");
            }

            if (!Directory.Exists("m-RESULTS"))
            {
                Directory.CreateDirectory("m-RESULTS");
            }

            if (!Directory.Exists("DATA"))
            {
                Directory.CreateDirectory("DATA");
            }

            if (!Directory.Exists("RESULTS"))
            {
                Directory.CreateDirectory("RESULTS");
            }
        }

        private void SensorRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double.TryParse(SensorRange.Text, out double newValue);
                sensorRange = newValue;


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing value: {ex.Message}");
            }
        }

        private bool AreAllValidNumericChars(string str)
        {
            foreach (char c in str)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    return false;
                }
            }

            return true;
        }

        private void POIRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                this.numSectors = 36;
                switch (radioButton.Content)
                {
                    case "POI 36":
                        this.numSectors = 36;
                        break;
                    case "POI 121":
                        this.numSectors = 121;
                        break;
                    case "POI 441":
                        this.numSectors = 441;
                        break;
                }

                POIs = CreatePoints(this.numSectors);
            }

            DrawPoints();
            if (Sensors.Count > 0)
                DrawSensors();
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

            for (double i = 0; i < 100; i += sectors)
            {
                for (double j = 0; j < 100; j += sectors)
                {
                    points.Add(new Point(i + (sectors / 2), j + (sectors / 2)));
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
            }
        }

        private void readWSN_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Path.Combine(Environment.CurrentDirectory, "DATA");

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = openFileDialog.FileName;
                    sensorFile = GetFileNameFromPath(fileName);
                    string[] fileLines = File.ReadAllLines(fileName);

                    Sensors = new List<Sensor>();
                    for (int i = 1; i < fileLines.Length; i++)
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
        static string GetFileNameFromPath(string filePath)
        {
            return Path.GetFileName(filePath);
        }
        private void TextBox_PreviewNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0) &&
                e.Text != "." &&
                e.Text != "-" ||
                (e.Text == "-" && ((TextBox)sender).Text.Length > 0) ||
                ((TextBox)sender).Text.Contains(".") && e.Text == ".")
            {
                e.Handled = true;
            }
        }

        private void ActivateSensors(double probability)
        {
            foreach (Sensor sensor in Sensors)
            {
                double randomNumber = random.NextDouble();
                if (probability >= randomNumber)
                    sensor.IsWorking = true;
                else
                    sensor.IsWorking = false;
            }


        }

        private void DrawCircles(double radius)
        {
            SolidColorBrush strokeBrush = new SolidColorBrush(Colors.Green);
            strokeBrush.Opacity = .25d;
            foreach (Sensor sensor in Sensors)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.1
                };
                if (sensor.IsWorking)
                    ellipse.Fill = strokeBrush;

                Canvas.SetLeft(ellipse, sensor.X - radius);
                Canvas.SetTop(ellipse, sensor.Y - radius);

                myCanvas.Children.Add(ellipse);
            }
        }

        private void ShowWSN_Click(object sender, RoutedEventArgs e)
        {
            if (POIs.Count < 1 ||
                Sensors.Count < 1 ||
                SensorRange.Text == "" ||
                RandomlySensor.Text == "" ||
                double.Parse(SensorRange.Text, CultureInfo.InvariantCulture) < 0 ||
                double.Parse(RandomlySensor.Text, CultureInfo.InvariantCulture) < 0 ||
                double.Parse(RandomlySensor.Text, CultureInfo.InvariantCulture) > 1)
                return;

            DrawPoints();
            DrawSensors();
            ActivateSensors(double.Parse(RandomlySensor.Text, CultureInfo.InvariantCulture));
            DrawCircles(double.Parse(SensorRange.Text, CultureInfo.InvariantCulture));


        }

        public void AssignIDs()
        {
            var sortedSensors = Sensors.OrderByDescending(s => s.Y).ThenBy(s => s.X).ToList();

            for (int i = 0; i < sortedSensors.Count; i++)
            {
                sortedSensors[i].Index = i + 1;
            }

            Sensors = sortedSensors;
            foreach (var sensor in Sensors)
            {

                System.Diagnostics.Debug.WriteLine($"{sensor.Index} {sensor.X} {sensor.Y}");
            }


        }

        private void CalcSenorID_Click(object sender, RoutedEventArgs e)
        {
            AssignIDs();
            string filename = "INIT-RESULTS/sensorID-WSN-";
            filename += Sensors.Count.ToString() + ".txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine("#id x y");
                    foreach (var sensor in Sensors)
                    {
                        writer.WriteLine($"{sensor.Index} {sensor.X} {sensor.Y}");
                    }
                }

                Console.WriteLine($"Lista została zapisana do pliku: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd podczas zapisywania do pliku: {ex.Message}");
            }


            filename = "INIT-RESULTS/sensor-states-10.txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine("#id x y state");
                    foreach (var sensor in Sensors)
                    {
                        writer.WriteLine($"{sensor.Index} {sensor.X} {sensor.Y} {Convert.ToInt32(sensor.IsWorking)}");
                    }
                }

                Console.WriteLine($"Lista została zapisana do pliku: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd podczas zapisywania do pliku: {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }
/*
        Calculates the coverage as the fraction of PoI monitored by the sensors and then 
        calculates normalized quality values 
        for each sensor based on the provided formula.
*/
        private void Calc_singleQ_click(object sender, RoutedEventArgs e)
        {
            var numberOfSensors = Sensors.Count.ToString();


            string filepath = "INIT-RESULTS/single q WSN-";
            filepath += Sensors.Count.ToString() + ".txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {


                    // Write parameters of run
                    writer.WriteLine("#parameters of run:");
                    writer.WriteLine($"#Number of Sensors {numberOfSensors}");
                    writer.WriteLine($"#Sensor Range: {sensorRange}");
                    writer.WriteLine($"#POI: {this.numSectors}");
                    writer.WriteLine($"#Sensor from file: {sensorFile}");
                    writer.WriteLine($"#Sensor state from the file: sensor-states-10.txt");
                    writer.WriteLine($"#Sensor for file: {filepath}.txt");

                    // Write header
                    writer.Write("#\t q\t s");
                    for (int i = 1; i <= Sensors.Count; i++)
                    {
                        writer.Write($"\t s{i}");
                    }

                    for (int i = 1; i <= Sensors.Count; i++)
                    {
                        writer.Write($"\t q{i}");
                    }

                    writer.WriteLine("");

                    writer.Write("\t");
                    // Write data
                    double q = CalculateCoverage(Sensors);
                    int s = Sensors.Count;
                    List<double> qValues = CalculateNormalizedQValues(Sensors, q);

                    System.Diagnostics.Debug.WriteLine($"q: {q}");
                    writer.Write($"{q}");
                    System.Diagnostics.Debug.WriteLine($"s: {s}");
                    writer.Write($"\t{s*2}");
                    for (int i = 0; i < s; i++)
                    {
                        writer.Write($"\t{Convert.ToInt32(Sensors[i].IsWorking)}");
                    }

                    for (int i = 0; i < s; i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"q{i + 1}: {qValues[i]}");
                        writer.Write($"\t{Math.Round(qValues[i],2)}");
                    }
                    writer.WriteLine("");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }

        }

    

        //Convert.ToInt32(sensor.IsWorking)
        static double CalculateCoverage(List<Sensor> sensors)
        {
            int poiMonitored = 0;

            foreach (Sensor sensor in sensors)
            {
                if (Convert.ToInt32(sensor.IsWorking) == 1) // Assuming state 1 means the sensor is ON
                {
                    poiMonitored++;
                }
            }

            return (double)poiMonitored / sensors.Count;
        }

        static List<double> CalculateNormalizedQValues(List<Sensor> sensors, double coverage)
        {
            List<double> qValues = new List<double>();

            foreach (Sensor sensor in sensors)
            {
                double qValue = CalculateQValueForSensor(sensor, sensors, coverage);
                qValues.Add(qValue);
            }

            return qValues;
        }

        static double CalculateQValueForSensor(Sensor targetSensor, List<Sensor> sensors, double coverage)
        {
            double qValue = 0.0;

            foreach (Sensor otherSensor in sensors)
            {
                if (otherSensor != targetSensor)
                {
                    double distance = CalculateDistance(targetSensor, otherSensor);
                    qValue += Convert.ToInt32(otherSensor.IsWorking) / (distance + double.Epsilon); // Add epsilon to avoid division by zero
                }
            }

            if (coverage <= 0.0)
            {
                return 0;
            }
            else
            {
                return qValue / coverage;
            }
        }

        static double CalculateDistance(Sensor sensor1, Sensor sensor2)
        {
            return Math.Sqrt(Math.Pow(sensor1.X - sensor2.X, 2) + Math.Pow(sensor1.Y - sensor2.Y, 2));
        }
    }
}