using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCS
{
    public class Individual
    {
        public string Permutation{ get; set; }
        public int NumberOfTurnedOnSensors { get; set; }
        public double Coverage { get; set; }

        public double F1_result { get; set; }
        public List<double> F2_Rewards { get; set; }
        public bool Nash {  get; set; }

        public Individual()
        {

        }

        public Individual(string permutation)
        {  
            Permutation = permutation;
        }

        public Individual(string permutation, int numberOfTurnedOnSensors)
        {
            Permutation = permutation;
            NumberOfTurnedOnSensors  = numberOfTurnedOnSensors;

        }

        public Individual(string permutation, double coverage)
        {
            Permutation = permutation;
            Coverage = coverage;

        }

        public Individual(string permutation, int numberOfTurnedOnSensors, double coverage, double f1_result)
        {
            Permutation = permutation;
            NumberOfTurnedOnSensors = numberOfTurnedOnSensors;
            Coverage = coverage;
            F1_result = f1_result;
        }

        public Individual(string permutation, int numberOfTurnedOnSensors, double coverage, List<double> f2_rewards, bool nash)
        {
            Permutation = permutation;
            NumberOfTurnedOnSensors = numberOfTurnedOnSensors;
            Coverage = coverage;
            F2_Rewards = new List<double>(f2_rewards);
            Nash = nash;
        }
    }
}
