using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        private List<Individual> Individuals;
        private double _CN, _CY, _DN, _DY;


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

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            provider.NumberGroupSeparator = ",";

            // it needs provider because PL-pl convertion
            double requestedCoverage = Convert.ToDouble(QReq.Text, provider);
            _CN = Convert.ToDouble(CNo.Text, provider);
            _CY = Convert.ToDouble(CYes.Text, provider);
            _DN = Convert.ToDouble(DNo.Text, provider);
            _DY = Convert.ToDouble(DYes.Text, provider);


            Individuals = new List<Individual>();
            int numberPermutations = 0;
            int individualId = 0;
            int generation = 0;
            int windowSize = Convert.ToInt32(NOnMax.Text) - Convert.ToInt32(NOnMin.Text);
            

            if ((bool)Debug1.IsChecked)
            {
                if (Sensors.Count >= 12)
                {
                    return;
                }

                List<string> permutations = GeneratePermutations();

                numberPermutations = permutations.Count;

                foreach(string permutation in permutations)
                {
                    if ((bool)F1.IsChecked)
                        Individuals.Add(CalculateFunctionF1(permutation, requestedCoverage));
                    else
                        Individuals.Add(CalculateFunctionF2(permutation, requestedCoverage));
                }

                if ((bool)F1.IsChecked)
                    saveAllF1(Individuals);
                else
                    saveRewards(Individuals);
            }
        }

        private Individual CalculateFunctionF1(string permutation, double requestedCoverage)
        {
            int numberOfTurnedOnSensors = 0;

            for (int i = 0; i < Sensors.Count; i++)
            {
                Sensors[i].IsWorking = permutation[i] == '1';
            }
            AssignPoIToSensors();
            List<Point> PoIInActiveArea = GetPointInActiveAreas(Sensors);
            double q = (double)PoIInActiveArea.Count / (double)POIs.Count;
            List<double> qValues = CalculateNormalizedQValues(Sensors, PoIInActiveArea);

            foreach (Sensor sensor in Sensors)
            {
                if (sensor.IsWorking)
                    numberOfTurnedOnSensors++;
            }

            if (q >= requestedCoverage)
                return new Individual(permutation, numberOfTurnedOnSensors, q, q + Sensors.Count - numberOfTurnedOnSensors);
            else
                return new Individual(permutation, numberOfTurnedOnSensors, q, q);
        }

        private Individual CalculateFunctionF2(string permutation, double requestedCoverage)
        {
            int sensorId = 0;
            List<double> localRewards = new List<double>();
            double avgPayOff = 0;
            double localCoverage = 0;
            int numberOfTurnedOnSensors = 0;

            for (int i = 0; i < Sensors.Count; i++)
            {
                Sensors[i].IsWorking = permutation[i] == '1';
            }

            AssignPoIToSensors();
            List<Point> PoIInActiveArea = GetPointInActiveAreas(Sensors);
            double globalCoverage = (double)PoIInActiveArea.Count / (double)POIs.Count;

            for(int i=0;i<Sensors.Count; i++)
            {
                if (!Sensors[i].IsWorking)
                {
                    List<double> qValues = CalculateNormalizedQValues(Sensors, PoIInActiveArea);
                    if (qValues[i]>=requestedCoverage)
                        localRewards.Add(_DY);
                    else
                        localRewards.Add(_DN);
                }
                else
                {
                    Sensors[i].IsWorking = false;
                    List<Point> newPoIInActiveArea = GetPointInActiveAreas(Sensors);
                    List<double> qValues = CalculateNormalizedQValues(Sensors, newPoIInActiveArea);
                    if (qValues[i]>=requestedCoverage)
                        localRewards.Add(_CY);
                    else
                        localRewards.Add(_CN);
                    Sensors[i].IsWorking = true;
                }
            }
            foreach (Sensor sensor in Sensors)
            {
                if (sensor.IsWorking)
                    numberOfTurnedOnSensors++;
            }

            bool nash = localRewards.Any(x => x == _CY);

            return new Individual(permutation, numberOfTurnedOnSensors, globalCoverage, localRewards, nash);
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

        private void saveAllF1(List<Individual> individuals)
        {
            string filepath = "INIT-RESULTS/ALL_F1.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {

                    writer.Write("#\t");
                    for (int i = 1; i < (Sensors.Count + 5); i++)
                    {
                        writer.Write($"{i}\t");
                    }

                    writer.WriteLine("");
                    writer.Write("#\tid\tq\tn_on\tf1\t");
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        writer.Write($"s{i + 1}\t");
                    }
                    writer.WriteLine("");
                    int id = 0;

                    foreach (var indi in individuals)
                    {

                        writer.Write($"\t{id}\t{Math.Round(indi.Coverage, 2)}\t{indi.NumberOfTurnedOnSensors}\t{Math.Round(indi.F1_result, 2)}\t");
                        for (int i = 0; i < Sensors.Count; i++)
                        {
                            writer.Write($"{indi.Permutation[i]}\t");
                        }
                        writer.WriteLine();
                        id++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }
        }

        private void saveRewards(List<Individual> individuals)
        {
            string filepath = "INIT-RESULTS/REWARD1.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {

                    writer.Write("#\t");
                    for (int i = 1; i <= (Sensors.Count + 4); i++)
                    {
                        writer.Write($"{i}\t");
                    }

                    writer.WriteLine("");
                    writer.Write("#\tid\tq\tn_on\tf2\t");
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        writer.Write($"s{i + 1}\t");
                    }
                    writer.WriteLine("");
                    int id = 0;

                    foreach (var indi in individuals)
                    {
                        writer.Write($"\t{id}\t{Math.Round(indi.Coverage, 2)}\t{indi.NumberOfTurnedOnSensors}\t{Math.Round(indi.F2_Rewards.Average(), 2)}\t");
                        for (int i = 0; i < Sensors.Count; i++)
                        {
                            writer.Write($"{indi.Permutation[i]}\t");
                        }
                        writer.WriteLine();
                        id++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }


            filepath = "INIT-RESULTS/REWARD2.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {

                    writer.Write("#\t");
                    for (int i = 1; i <= (Sensors.Count + 4); i++)
                    {
                        writer.Write($"{i}\t");
                    }

                    writer.WriteLine("");
                    writer.Write("#\tid\tq\tn_on\tf2\t");
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        writer.Write($"rew{i + 1}\t");
                    }
                    writer.Write($"Nash\t");
                    writer.WriteLine("");
                    int id = 0;

                    foreach (var indi in individuals)
                    {
                        writer.Write($"\t{id}\t{Math.Round(indi.Coverage, 2)}\t{indi.NumberOfTurnedOnSensors}\t{Math.Round(indi.F2_Rewards.Average(), 2)}\t");
                        for (int i = 0; i < Sensors.Count; i++)
                        {
                            writer.Write($"{indi.F2_Rewards[i]}\t");
                        }
                        writer.Write($"{indi.Nash}\t");
                        writer.WriteLine();
                        id++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing file: {ex.Message}");
            }
        }
    }
}
