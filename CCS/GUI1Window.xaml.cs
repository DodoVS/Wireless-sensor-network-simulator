using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Shapes;

namespace CCS
{
    /// <summary>
    /// Logika interakcji dla klasy GUI1Window.xaml
    /// </summary>
    public partial class GUI1Window : UserControl
    {
        private List<Point> POIs;
        private List<Sensor> Sensors;
        private Random random;
        private int numSectors;
        private double sensorRange = 0.0;
        private double sensorProbability = 0.0;
        private string sensorFile;
        private bool isRadomly;
        private MainWindow mainWindow;

        public GUI1Window(MainWindow window)
        {
            InitializeComponent();
            POIs = new List<Point>();
            Sensors = new List<Sensor>();
            random = new Random();
            isRadomly = true;
            ReadWSNONOFFButton.IsEnabled = false;
            mainWindow = window;
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

        private void SensorProbability_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double.TryParse(SensorRange.Text, out double newValue);
                sensorProbability = newValue;


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing value: {ex.Message}");
            }
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

            for (double i = 0; i < 100 - (sectors / 2); i += sectors)
            {
                for (double j = 0; j < 100 - (sectors / 2); j += sectors)
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
            openFileDialog.InitialDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "DATA");

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
            return System.IO.Path.GetFileName(filePath);
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
                double.Parse(SensorRange.Text, CultureInfo.InvariantCulture) < 0)
                return;

            if (isRadomly)
            {
                if (RandomlySensor.Text == "" ||
                    double.Parse(RandomlySensor.Text, CultureInfo.InvariantCulture) < 0 ||
                    double.Parse(RandomlySensor.Text, CultureInfo.InvariantCulture) > 1)
                    return;
            }

            // Assing ID because there is chance that user forget that do manually
            AssignIDs();


            DrawPoints();
            DrawSensors();
            if (isRadomly)
                ActivateSensors(double.Parse(RandomlySensor.Text, CultureInfo.InvariantCulture));
            DrawCircles(double.Parse(SensorRange.Text, CultureInfo.InvariantCulture));

            // Add PoI to the List inside Sensors to make somethinks easier but user need to
            // do showWSN every param change
            ClearPoIInSensors();
            AssignPoIToSensors();
        }

        public void AssignIDs()
        {
            var sortedSensors = Sensors.OrderByDescending(s => s.Y).ThenBy(s => s.X).ToList();

            for (int i = 0; i < sortedSensors.Count; i++)
            {
                sortedSensors[i].Index = i + 1;
            }

            Sensors = sortedSensors;
        }
        /*
                 Assign POI to every Sensor
        */
        private void AssignPoIToSensors()
        {
            foreach (var sensor in Sensors)
            {
                foreach (Point point in POIs)
                {
                    double distance = CalculateDistance(sensor.X, sensor.Y, point.X, point.Y);
                    if (distance <= sensorRange)
                        sensor.PoIs.Add(point);
                }
            }

        }

        private void ClearPoIInSensors()
        {
            foreach (var sensor in Sensors)
                sensor.PoIs = new List<Point>();
        }

        public void CalcSensorID_Click(object sender, RoutedEventArgs e)
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


            filename = "INIT-RESULTS/sensor-states-";
            filename += GetWorkingSensorsNumber() + ".txt";
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

        /*
                Creates number from binary construction of working states in sensors
         */
        private int GetWorkingSensorsNumber()
        {
            string binaryNumber = "";

            foreach (Sensor sensor in Sensors)
                binaryNumber += sensor.IsWorking ? "1" : "0";

            return Convert.ToInt32(binaryNumber, 2);
        }


        /*
                Calculates the coverage as the fraction of PoI monitored by the sensors and then 
                calculates normalized quality values 
                for each sensor based on the provided formula.
        */
        private void Calc_singleQ_click(object sender, RoutedEventArgs e)
        {
            var numberOfSensors = Sensors.Count.ToString();


            string filepath = "INIT-RESULTS/cov-single-WSN-";
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
                    writer.Write("#\t q");
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


                    List<Point> PoIInActiveArea = GetPointInActiveAreas(Sensors);
                    double q = (double)PoIInActiveArea.Count / (double)POIs.Count;
                    List<double> qValues = CalculateNormalizedQValues(Sensors, PoIInActiveArea);

                    writer.Write($"{Math.Round(q, 2)}");
                    foreach (Sensor sensor in Sensors)
                    {
                        writer.Write($"\t{Convert.ToInt32(sensor.IsWorking)}");
                    }

                    foreach (double qs in qValues)
                    {
                        writer.Write($"\t{Math.Round(qs, 2)}");
                    }
                    writer.WriteLine("");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }
        }

        private void Calc_allQ_click(object sender, RoutedEventArgs e)
        {
            var numberOfSensors = Sensors.Count.ToString();


            string filepath = "INIT-RESULTS/cov-all-WSN-";
            filepath += Sensors.Count.ToString() + ".txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {
                    // Write parameters of run
                    writer.WriteLine("#Parameters of run:");
                    writer.WriteLine($"#Number of Sensors {numberOfSensors}");
                    writer.WriteLine($"#Sensor Range: {sensorRange}");
                    writer.WriteLine($"#POI: {this.numSectors}");
                    writer.WriteLine($"#Sensor from file: {sensorFile}");
                    writer.WriteLine($"#Sensor state from the file: not applied");

                    // Write header
                    writer.Write("#\t q");
                    for (int i = 1; i <= Sensors.Count; i++)
                    {
                        writer.Write($"\t s{i}");
                    }

                    for (int i = 1; i <= Sensors.Count; i++)
                    {
                        writer.Write($"\t q{i}");
                    }

                    writer.WriteLine("");

                    // Write data

                    for (int i = 0; i < Math.Pow(2, Sensors.Count); i++)
                    {
                        List<Sensor> newSensors = ActiveSensorsFromNumber(Sensors, i);
                        List<Point> PoIInActiveArea = GetPointInActiveAreas(newSensors);
                        double q = (double)PoIInActiveArea.Count / (double)POIs.Count;
                        List<double> qValues = CalculateNormalizedQValues(newSensors, PoIInActiveArea);

                        writer.Write($"{i}\t {Math.Round(q, 2)}");
                        foreach (Sensor sensor in newSensors)
                        {
                            writer.Write($"\t{Convert.ToInt32(sensor.IsWorking)}");
                        }

                        foreach (double qs in qValues)
                        {
                            writer.Write($"\t{Math.Round(qs, 2)}");
                        }
                        writer.WriteLine("");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }
        }

        static List<Sensor> ActiveSensorsFromNumber(List<Sensor> sensors, int number)
        {
            List<Sensor> newActiveSensors = new List<Sensor>(sensors);
            string binaryNumber = Convert.ToString(number, 2).PadLeft(sensors.Count, '0');
            for (int j = 0; j < sensors.Count; j++)
            {
                newActiveSensors[j].IsWorking = ParseStringToBoolean(binaryNumber[j]);
            }
            return newActiveSensors;
        }

        static bool ParseStringToBoolean(char value)
        {
            if (value == '1')
                return true;
            else if (value == '0')
                return false;
            else
                return false;
        }

        /*
                Returns every Points that is in range of active sensors without repeating of points
        */
        static List<Point> GetPointInActiveAreas(List<Sensor> sensors)
        {
            List<Point> PoIsInActiveSensors = new List<Point>();

            foreach (var sensor in sensors)
                if (sensor.IsWorking)
                    PoIsInActiveSensors.AddRange(sensor.PoIs);

            return PoIsInActiveSensors.Distinct().ToList();
        }


        /*
                Returns every q values for specific sensor
        */
        static List<double> CalculateNormalizedQValues(List<Sensor> sensors, List<Point> PoIInActiveArea)
        {
            List<double> qValues = new List<double>();

            foreach (Sensor sensor in sensors)
            {
                List<Point> poIActiveInYourArea = sensor.PoIs.Intersect(PoIInActiveArea).ToList();
                qValues.Add((double)poIActiveInYourArea.Count / (double)sensor.PoIs.Count);
            }
            return qValues;
        }

        static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private void ReadWSNOnOff_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "INIT-RESULTS");

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = openFileDialog.FileName;
                    sensorFile = GetFileNameFromPath(fileName);
                    string[] fileLines = File.ReadAllLines(fileName);
                    for (int i = 1; i < fileLines.Length; i++)
                    {
                        string[] coordinates = fileLines[i].Split(' ');
                        double x = double.Parse(coordinates[1], CultureInfo.InvariantCulture);
                        double y = double.Parse(coordinates[2], CultureInfo.InvariantCulture);
                        int sensorIndex = Sensors.FindIndex(sensor => sensor.X == x && sensor.Y == y);

                        Sensors[sensorIndex].Index = int.Parse(coordinates[0]);
                        Sensors[sensorIndex].IsWorking = ParseStringToBoolean(coordinates[3][0]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}");
                }
            }
        }

        private void ActiveSensorRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                if (radioButton.Content.ToString() == "randomly")
                {
                    isRadomly = true;
                    ReadWSNONOFFButton.IsEnabled = false;
                    RandomlySensor.IsEnabled = true;
                }
                else
                {
                    isRadomly = false;
                    ReadWSNONOFFButton.IsEnabled = true;
                    RandomlySensor.IsEnabled = false;
                }

            }
        }





        public void Find_WSN_Graph_click(object sender, RoutedEventArgs e)
        {
            var numberOfSensors = Sensors.Count.ToString();


            string filepath = "INIT-RESULTS/neighborhood WSN-";
            filepath += Sensors.Count.ToString() + "-"; ;
            filepath += "r" + sensorRange.ToString() + ".txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {


                    // Write parameters of run
                    writer.WriteLine("#Parameters of run:");
                    writer.WriteLine($"#Number of Sensors {numberOfSensors}");
                    writer.WriteLine($"#POI: {this.numSectors}");
                    writer.WriteLine($"#Sensor from file: {sensorFile}");
                    writer.WriteLine($"#initialSensorProbablityOn: {sensorProbability}");
                    writer.WriteLine($"#activateSensorsRandomly: {isRadomly}");
                    writer.WriteLine($"#Sensor Range: {sensorRange}");
                    writer.WriteLine($"#Sensor for file: {filepath}.txt");

                    // Write header
                    writer.WriteLine("#id\tnum_of_neigh\tid-of_neighbors");

                    // Write data

                    // Create a dictionary to store neighbors for each sensor
                    Dictionary<int, List<int>> sensorNeighborsMap = new Dictionary<int, List<int>>();

                    // Iterate through each sensor and find neighbors
                    foreach (var sensor in Sensors)
                    {
                        int sensorId = sensor.Index;
                        sensorNeighborsMap[sensorId] = new List<int>();

                        foreach (var otherSensor in Sensors)
                        {
                            if (otherSensor.Index != sensorId)
                            {

                                double distance = CalculateDistance(sensor, otherSensor);

                                if (distance <= sensorRange * 2)
                                {
                                    sensorNeighborsMap[sensorId].Add(otherSensor.Index);
                                }
                            }
                        }
                    }

                    // Display the results
                    foreach (var kvp in sensorNeighborsMap)
                    {
                        writer.WriteLine($"\t{kvp.Key}\t{kvp.Value.Count()}\t{string.Join(" ", kvp.Value)}");
                    }
                    writer.WriteLine("");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }
        }

        static double CalculateDistance(Sensor sensor1, Sensor sensor2)
        {
            double deltaX = double.Abs(sensor1.X - sensor2.X);
            double deltaY = double.Abs(sensor1.Y - sensor2.Y);

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }


        public void Find_Sensor_Rank_click(object sender, RoutedEventArgs e)
        {

            var numberOfSensors = Sensors.Count.ToString();


            string filepath = "INIT-RESULTS/rank WSN-";
            filepath += Sensors.Count.ToString() + "-"; ;
            filepath += "r" + sensorRange.ToString() + ".txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {


                    // Write parameters of run
                    writer.WriteLine("#Parameters of run:");
                    writer.WriteLine($"#Number of Sensors {numberOfSensors}");
                    writer.WriteLine($"#POI: {this.numSectors}");
                    writer.WriteLine($"#Sensor from file: {sensorFile}");
                    writer.WriteLine($"#initialSensorProbablityOn: {sensorProbability}");
                    writer.WriteLine($"#activateSensorsRandomly: {isRadomly}");
                    writer.WriteLine($"#Sensor Range: {sensorRange}");
                    writer.WriteLine($"#Sensor for file: {filepath}.txt");

                    // Write header
                    writer.WriteLine("#id\tnum_of_neigh\tid-of_neighbors");

                    // Write data

                    // Create a dictionary to store neighbors for each sensor  
                    Dictionary<int, List<double>> sensorRankMap = new Dictionary<int, List<double>>();

                    // Iterate through each sensor and find neighbors
                    foreach (var sensor in Sensors)
                    {
                        int sensorId = sensor.Index;
                        sensorRankMap[sensorId] = new List<double>();

                        foreach (var otherSensor in Sensors)
                        {
                            if (otherSensor.Index != sensorId)
                            {

                                double distance = CalculateDistance(sensor, otherSensor);

                                if (distance <= sensorRange * 2)
                                {
                                    sensorRankMap[sensorId].Add(Math.Round(distance / (2 * sensorRange), 2));
                                }
                            }
                        }
                    }

                    // Display the results
                    foreach (var kvp in sensorRankMap)
                    {
                        writer.WriteLine($"\t{kvp.Key}\t{kvp.Value.Count()}\t{string.Join(" ", kvp.Value)}");
                    }
                    writer.WriteLine("");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            AssignIDs();
            ClearPoIInSensors();
            AssignPoIToSensors();
            mainWindow.ChangeFirstPageToSecond(POIs, Sensors, sensorRange);
        }
    }
}
