using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace CCS
{
    /// <summary>
    /// Logika interakcji dla klasy GUI2Window.xaml
    /// </summary>
    public partial class GUI2Window : UserControl
    {
        private MainWindow mainWindow;
        public List<Point> POIs;
        public List<Sensor> Sensors;
        private double sensorRange = 0.0;

        public GUI2Window(MainWindow window)
        {
            InitializeComponent();

            mainWindow = window;
            POIs = new List<Point>();
            Sensors = new List<Sensor>();
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSecondPageToFirst();
        }

        public void SetDataFromPageOne(List<Point> pois, List<Sensor> sensors, double range)
        {
            POIs = new List<Point>(pois);
            Sensors = new List<Sensor>(sensors);
            sensorRange = range;
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

        private void Gui2Function(object sender, RoutedEventArgs e)
        {
            double requestedCoverage = 0.8;
            int numberPermutations = 0;
            int individualId = 0;
            int generation = 0;
            bool isDebug = true;
            bool f1 = true;
            Dictionary<string, double> individualDict;
            if (!isDebug)
            {
                return;
            }

            if (Sensors.Count >= 12)
            {
                return;
            }

            List<string> permutations = GeneratePermutations();

            numberPermutations = permutations.Count;
            Debug.WriteLine("Number of permutations: {0}", numberPermutations);
            foreach (string permutation in permutations)
            {
                Debug.WriteLine(permutation);
            }

            if (f1)
            {
                individualDict = CalculateFunctionF1(permutations, requestedCoverage);

            }


        }

        private Dictionary<string, double> CalculateFunctionF1(List<String> permutations, double requestedCoverage)
        {
            Dictionary<string, double> individual = new Dictionary<string, double>();



            List<Point> PoIInActiveArea = GetPointInActiveAreas(Sensors);
            foreach (string permutation in permutations)
            {

                int numberOfTurnedOnSensors = 0;

                for (int i = 0; i < Sensors.Count; i++)
                {
                    Sensors[i].IsWorking = permutation[i] == '1';
                }
                AssignPoIToSensors();

                double q = (double)PoIInActiveArea.Count / (double)POIs.Count;
                List<double> qValues = CalculateNormalizedQValues(Sensors, PoIInActiveArea);

                System.Diagnostics.Debug.WriteLine($"q: {q}");
                Debug.WriteLine($"{Math.Round(q, 2)}");
                foreach (Sensor sensor in Sensors)
                {
                    Debug.WriteLine($"\t{Convert.ToInt32(sensor.IsWorking)}");
                }

                foreach (double qs in qValues)
                {
                    Debug.WriteLine($"\t{Math.Round(qs, 2)}");
                }

                Debug.WriteLine("");
                foreach (Sensor sensor in Sensors)
                {
                    if (sensor.IsWorking)
                    {
                        numberOfTurnedOnSensors++;
                    }
                }

                if (q >= requestedCoverage)
                {
                    individual.Add(permutation, numberOfTurnedOnSensors * (q - requestedCoverage));
                }
                else
                {
                    individual.Add(permutation, (Sensors.Count - numberOfTurnedOnSensors) * (requestedCoverage - q));
                }

            }

            return individual;

        }

        private List<string> GeneratePermutations()
        {

            // Create a list to store the permutations
            List<string> permutations = new List<string>();

            // Generate all permutations of zeros and ones
            foreach (int i in Enumerable.Range(0, 1 << Sensors.Count))
            {
                StringBuilder permutation = new StringBuilder();

                for (int j = 0; j < Sensors.Count; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        permutation.Append('1');
                    }
                    else
                    {
                        permutation.Append('0');
                    }
                }

                permutations.Add(permutation.ToString());
            }

            // Return the list of permutations
            return permutations;

        }

    }
}
