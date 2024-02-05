using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;


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
        private Random random;
        private Individual currBestSol;


        public GUI2Window(MainWindow window)
        {
            InitializeComponent();

            mainWindow = window;
            POIs = new List<Point>();
            Sensors = new List<Sensor>();
            random = new Random();
            currBestSol = new Individual();
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
                sensor.PoIs.Clear();
                foreach (Point point in POIs)
                {
                    double distance = CalculateDistance(sensor.X, sensor.Y, point.X, point.Y);
                    if (distance < sensorRange)
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
        private List<Point> GetPointInActiveAreas(List<Sensor> sensors)
        {
            List<Point> PoIsInActiveSensors = new List<Point>();

            foreach (var sensor in Sensors)
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

            //mainWindow.GUI1Window.Find_WSN_Graph_click(null, null);
            //mainWindow.GUI1Window.Find_Sensor_Rank_click(null, null);
            //mainWindow.GUI1Window.CalcSensorID_Click(null, null);

            // it needs provider because PL-pl convertion
            double requestedCoverage = Convert.ToDouble(QReq.Text, provider);
            _CN = Convert.ToDouble(CNo.Text, provider);
            _CY = Convert.ToDouble(CYes.Text, provider);
            _DN = Convert.ToDouble(DNo.Text, provider);
            _DY = Convert.ToDouble(DYes.Text, provider);

            int popSize = Convert.ToInt32(PopSizeM.Text);


            Individuals = new List<Individual>();
            int numberPermutations = 0;
            int individualId = 0;
            int generation = 0;
            int orDeltaL = 0;
            int windowSize = Convert.ToInt32(NOnMax.Text) - Convert.ToInt32(NOnMin.Text);
            int l = Convert.ToInt32(NumOfGenL.Text);
            int deltaL = Convert.ToInt32(DeltaL.Text);



            if ((bool)Debug1.IsChecked)
            {
                if (Sensors.Count >= 12)
                {
                    return;
                }

                List<string> permutations = GeneratePermutations();

                numberPermutations = permutations.Count;

                foreach (string permutation in permutations)
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

            List<string> pop = initializePopulation(popSize);

            List<Individual> pop_fitt = new List<Individual>();
            foreach (string permutation in pop)
            {
                if ((bool)F1.IsChecked)
                    pop_fitt.Add(CalculateFunctionF1(permutation, requestedCoverage));
                else
                    pop_fitt.Add(CalculateFunctionF2(permutation, requestedCoverage));
            }
            currBestSol = findBestSolution(pop_fitt, requestedCoverage);
            saveBestSolut(currBestSol);
            saveGASolutions(pop_fitt, currBestSol, 0, requestedCoverage);


            for (int i = 1; i <= l; i++)
            {
                if (deltaL < 0)
                {
                    pop_fitt = applyGA(pop_fitt, requestedCoverage, i);
                }
                else
                {
                    if (deltaL != 0)
                    {
                        orDeltaL++;
                        if (orDeltaL < Convert.ToInt32(DeltaL.Text))
                        {
                            pop_fitt = applyGA(pop_fitt, requestedCoverage, i);
                        }
                        else
                        {
                            orDeltaL = 0;
                        }
                    }
                }
            }
            saveBestSolut(currBestSol);
        }

        private List<Individual> applyGA(List<Individual> temPop, double requestedCoverage, int gen)
        {
            List<Individual> tempSelectPop = TournamentSelection(temPop, Convert.ToInt32(TournameSize.Text));

            List<Individual> tempCrossPop = null;
            if ((bool)Cross1.IsChecked)
                tempCrossPop = CrossoverType1(tempSelectPop, 0.6);

            List<Individual> tempMutPop = null;
            if ((bool)Muta1.IsChecked)
                tempMutPop = CalculateMutation1(tempCrossPop, 0.1);

            List<Individual> pop = tempMutPop;

            List<Individual> pop_fitt = new List<Individual>();

            foreach (Individual permutation in pop)
            {
                if ((bool)F1.IsChecked)
                    pop_fitt.Add(CalculateFunctionF1(permutation.Permutation, requestedCoverage));
                else
                    pop_fitt.Add(CalculateFunctionF2(permutation.Permutation, requestedCoverage));
            };
            if ((bool)elilistStreetHillclimbing.IsChecked)
            {
                Individual newBestSollution = findBestSolution(pop_fitt, requestedCoverage);
                if (IsSolltionBetter(newBestSollution, currBestSol, requestedCoverage))
                {
                    int index = pop.IndexOf(pop.OrderBy(obj => obj.Coverage).FirstOrDefault());
                    pop[index] = newBestSollution;
                }
            }
            else
            {
                currBestSol = findBestSolution(pop_fitt, requestedCoverage);
            }
            if ((bool)elilistStreetHillclimbing.IsChecked)
            {
                int index = pop_fitt.IndexOf(pop_fitt.OrderBy(obj => obj.Coverage).FirstOrDefault());
                pop_fitt[index] = currBestSol;
            }
            saveGASolutions(pop_fitt, currBestSol, gen, requestedCoverage);
            return pop;
        }

        private bool IsSolltionBetter(Individual sol1, Individual sol2, double rq)
        {
            if (sol1.Coverage > sol2.Coverage)
            {
                if (sol1.NumberOfTurnedOnSensors < sol2.NumberOfTurnedOnSensors)
                    return true;
                if (sol1.NumberOfTurnedOnSensors == sol2.NumberOfTurnedOnSensors)
                    return true;
                return false;
            }
            else
            {
                if (sol1.NumberOfTurnedOnSensors < sol2.NumberOfTurnedOnSensors && sol1.Coverage > rq)
                    return true;
                return false;
            }
        }

        private List<Individual> CrossoverType1(List<Individual> temp_select_pop_old, double probability)
        {

            int i = 0;

            List<Individual> temp_cross_pop = new List<Individual>();
            List<Individual> temp_pop = new List<Individual>();
            Individual parent_one = new Individual();
            Individual parent_two = new Individual();


            int sensorsCount = Sensors.Count; // assume this is defined elsewhere


            for (int k = 0; k < temp_select_pop_old.Count; k++)
            {
                double r = new Random().NextDouble();

                if (r < probability)
                {
                    temp_cross_pop.Add(temp_select_pop_old[k]);
                }
                else
                {
                    temp_pop.Add(temp_select_pop_old[k]);
                    i += 1;
                }

            }


            for (int l = 0; l < Math.Floor(i / 2.0); l++)
            {
                parent_one = temp_pop[new Random().Next(temp_pop.Count)];
                temp_pop.Remove(parent_one);
                parent_two = temp_pop[new Random().Next(temp_pop.Count)];
                temp_pop.Remove(parent_two);

                int rand = new Random().Next(0, sensorsCount);

                // Create childOne using genes from parent_one and parent_two
                Individual childOne = new Individual();
                Individual childTwo = new Individual();
                // Copy genes from parent_one up to the random index

                childOne.Permutation += parent_one.Permutation.Substring(0, rand);
                childOne.Permutation +=
                    parent_two.Permutation.Substring(rand, (parent_two.Permutation.Length - rand));
                temp_cross_pop.Add(childOne);


                childTwo.Permutation += parent_two.Permutation.Substring(0, rand);
                childTwo.Permutation +=
                    parent_one.Permutation.Substring(rand, (parent_one.Permutation.Length - rand));
                temp_cross_pop.Add(childTwo);
            }

            if (temp_cross_pop.Count < Convert.ToInt32(PopSizeM.Text))
            {
                parent_one = temp_pop[0];
                temp_pop.Remove(parent_one);
                int rand = new Random().Next(0, sensorsCount);
                Individual childOne = new Individual();
                childOne.Permutation += parent_one.Permutation.Substring(0, rand);
                childOne.Permutation +=
                    parent_two.Permutation.Substring(rand, (parent_two.Permutation.Length - rand));
                temp_cross_pop.Add(childOne);
            }

            return temp_cross_pop;
        }

        private List<Individual> CalculateMutation1(List<Individual> temp_cross_pop, double probability)
        {

            int i = 0;
            int k = 0;

            List<Individual> temp_mut_pop = new List<Individual>();

            int sensorsCount = Sensors.Count; // assume this is defined elsewhere


            for (k = 0; k < temp_cross_pop.Count; k++)
            {
                for (; i < Sensors.Count; i++)
                {
                    double rand = new Random().NextDouble();
                    if (rand <= probability)
                    {
                        int x = 0;


                        if (i >= 0 && i < temp_cross_pop[k].Permutation.Length)
                        {
                            // Convert the string to a char array to make modifications
                            char[] charArray = temp_cross_pop[k].Permutation.ToCharArray();

                            // Change the character at the specified index
                            if (charArray[i] == '0')
                            {
                                charArray[i] = '1';
                            }
                            else
                            {
                                charArray[i] = '0';
                            }
                            // Create a new string from the modified char array
                            string modifiedString = new string(charArray);
                            temp_cross_pop[k].Permutation = modifiedString;
                        }
                        else
                        {
                            Console.WriteLine("Index is out of bounds.");
                        }
                    }
                    
                }
                temp_mut_pop.Add(temp_cross_pop[k]);
                i = 0;
            }
            return temp_mut_pop;
        }

        private List<Individual> TournamentSelection(List<Individual> temp_pop, int tournamentSize)
        {
            List<Individual> selectedPopulation = new List<Individual>();

            for (int i = 0; i < temp_pop.Count; i++)
            {
                // Randomly select individuals for the tournament
                List<Individual> tournamentParticipants = new List<Individual>();
                for (int j = 0; j < tournamentSize; j++)
                {
                    int randomIndex = new Random().Next(0, temp_pop.Count);
                    tournamentParticipants.Add(temp_pop[randomIndex]);
                }

                // Choose the best individual from the tournament based on coverage
                Individual winner = tournamentParticipants.OrderByDescending(individual => individual.Coverage).First();

                // Add the winner to the selected population
                selectedPopulation.Add(winner);
            }

            return selectedPopulation;
        }

        private void saveGASolutions(List<Individual> population, Individual best, int gen, double requestCoverage)
        {
            // GA_results.txt
            string GaResults = "INIT-RESULTS/GA-results.txt";
            string sollutions = $"{gen}\t{Math.Round(population.OrderByDescending(obj => obj.Coverage).FirstOrDefault().Coverage, 2)}\t {population.OrderByDescending(obj => obj.NumberOfTurnedOnSensors).FirstOrDefault().NumberOfTurnedOnSensors}\t";
            if ((bool)F1.IsChecked)
            {
                sollutions += $"{Math.Round(population.OrderByDescending(obj => obj.F1_result).FirstOrDefault().F1_result, 2)}\t";
                sollutions +=  $"{Math.Round(population.Max(obj => obj.F1_result),2)}\t";
                sollutions +=  $"{Math.Round(population.Average(obj => obj.F1_result),2)}\t";
                sollutions += $"{Math.Round(best.F1_result,2)}\t{best.NumberOfTurnedOnSensors}";
            }
            else
            {
                sollutions += $"{Math.Round(population.OrderByDescending(obj => obj.F2_Rewards.Average()).FirstOrDefault().F2_Rewards.Average(), 2)}\t";
                sollutions += $"{Math.Round(population.Max(obj => obj.F2_Rewards.Average()), 2)}\t";
                sollutions += $"{Math.Round(population.Average(obj => obj.F2_Rewards.Average()), 2)}\t";
                sollutions += $"{Math.Round(best.F2_Rewards.Average(), 2)}\t{best.NumberOfTurnedOnSensors}";
            }

            if (gen == 0)
            {
                using (StreamWriter writer = new StreamWriter(GaResults))
                {
                    writer.WriteLine("#gen\tbest_q\tbest_n_on\tbest_f1|f2\tbest_fitn\tav_fitn\tabs_best_f1|f2\tabs_best_n_on");
                    writer.WriteLine(sollutions);
                }
            }
            else
            {
                File.AppendAllText(GaResults, sollutions+ Environment.NewLine);
            }

            // GA_solutions.txt
            string GaSolutions = "INIT-RESULTS/GA-solutions.txt";
            sollutions = $"{gen}";
            foreach (char p in best.Permutation)
            {
                sollutions += $"\t{p}";
            }

            if (gen == 0)
            {
                using (StreamWriter writer = new StreamWriter(GaSolutions))
                {
                    string head = "#gen";
                    for (int i = 1; i <= best.Permutation.Length; i++)
                    {
                        head += $"\ts{i}";
                    }
                    writer.WriteLine(head);
                    writer.WriteLine(sollutions);
                }
            }
            else
            {
                File.AppendAllText(GaSolutions, sollutions + Environment.NewLine);
            }

            // GA_popul_structure.txt
            string GaPopul = "INIT-RESULTS/GA-popul_structure.txt";
            sollutions = $"{gen}";
            int min = Convert.ToInt32(NOnMin.Text);
            int max = Convert.ToInt32(NOnMax.Text);
            for (int i = min; i <= max; i++)
            {
                sollutions += $"\t{population.Count(obj => obj.NumberOfTurnedOnSensors == i)}";
            }
            if (gen == 0)
            {
                using (StreamWriter writer = new StreamWriter(GaPopul))
                {
                    string head = "#gen";
                    for (int i = min; i <= max; i++)
                    {
                        head += $"\t{i}";
                    }
                    writer.WriteLine(head);
                    writer.WriteLine(sollutions);
                }
            }
            else
            {
                File.AppendAllText(GaPopul, sollutions + Environment.NewLine);
            }
        }

        private void saveBestSolut(Individual best)
        {
            string filename = "INIT-RESULTS/GA-abs-best-solut-state.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine("#x y state");
                    for (int i = 0; i <= Sensors.Count; i++)
                    {
                        writer.WriteLine($"{Sensors[i].X} {Sensors[i].Y} {best.Permutation[i]}");
                    }
                }

                Console.WriteLine($"Lista została zapisana do pliku: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd podczas zapisywania do pliku: {ex.Message}");
            }
        }

        private Individual findBestSolution(List<Individual> population, double requestedCoverage)
        {
            Individual best = population[1];

            foreach (Individual individual in population)
            {
                if (individual.Coverage > requestedCoverage)
                {
                    if(best.Coverage < requestedCoverage && individual.Coverage >= requestedCoverage)
                        best = individual;

                    if (best.NumberOfTurnedOnSensors > individual.NumberOfTurnedOnSensors)
                        best = individual;
                }
            }
            return best;
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
            bool nash = false;

            for (int i = 0; i < Sensors.Count; i++)
            {
                if (!Sensors[i].IsWorking)
                {
                    List<double> qValues = CalculateNormalizedQValues(Sensors, PoIInActiveArea);
                    if (qValues[i] >= requestedCoverage)
                        localRewards.Add(_DY);
                    else
                        localRewards.Add(_DN);
                }
                else
                {
                    Sensors[i].IsWorking = false;
                    List<Point> newPoIInActiveArea = GetPointInActiveAreas(Sensors);
                    List<double> qValues = CalculateNormalizedQValues(Sensors, newPoIInActiveArea);
                    if (qValues[i] >= requestedCoverage)
                    {
                        localRewards.Add(_CY);
                    }

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
            if(localRewards.Average() > _CN)
                nash = true;

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

            permutations.Sort((x, y) => Convert.ToInt32(x, 2).CompareTo(Convert.ToInt32(y, 2)));
            return permutations;

        }

        private void saveAllF1(List<Individual> individuals)
        {
            string filepath = "INIT-RESULTS/ALL_F1.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {

                    writer.Write("#");
                    for (int i = 1; i < (Sensors.Count + 5); i++)
                    {
                        writer.Write($"{i}\t");
                    }

                    writer.WriteLine("");
                    writer.Write("#id\tq\tn_on\tf1\t");
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        writer.Write($"s{i + 1}\t");
                    }
                    writer.WriteLine("");
                    int id = 0;

                    foreach (var indi in individuals)
                    {

                        writer.Write($"{id}\t{Math.Round(indi.Coverage, 2)}\t{indi.NumberOfTurnedOnSensors}\t{Math.Round(indi.F1_result, 2)}\t");
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

                    writer.Write("#");
                    for (int i = 1; i <= (Sensors.Count + 4); i++)
                    {
                        writer.Write($"{i}\t");
                    }

                    writer.WriteLine("");
                    writer.Write("#id\tq\tn_on\tf2\t");
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        writer.Write($"s{i + 1}\t");
                    }
                    writer.WriteLine("");
                    int id = 0;

                    foreach (var indi in individuals)
                    {
                        writer.Write($"{id}\t{Math.Round(indi.Coverage, 2)}\t{indi.NumberOfTurnedOnSensors}\t{Math.Round(indi.F2_Rewards.Average(), 2)}\t");
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

                    writer.Write("#");
                    for (int i = 1; i <= (Sensors.Count + 4); i++)
                    {
                        writer.Write($"{i}\t");
                    }

                    writer.WriteLine("");
                    writer.Write("#id\tq\tn_on\tf2\t");
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        writer.Write($"rew{i + 1}\t");
                    }
                    writer.Write($"Nash\t");
                    writer.WriteLine("");
                    int id = 0;

                    foreach (var indi in individuals)
                    {
                        writer.Write($"{id}\t{Math.Round(indi.Coverage, 2)}\t{indi.NumberOfTurnedOnSensors}\t{Math.Round(indi.F2_Rewards.Average(), 2)}\t");
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

        private List<string> initializePopulation(int n)
        {
            var population = new List<string>();

            for (int i = 0; i < n; i++)
            {
                StringBuilder binaryString = new StringBuilder();

                for (int j = 0; j < Sensors.Count; j++)
                {
                    int randomBit = random.Next(2);
                    binaryString.Append(randomBit);
                }
                population.Add(binaryString.ToString());

            }
            return population;
        }
    }
}
