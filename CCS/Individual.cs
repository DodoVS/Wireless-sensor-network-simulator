﻿using System;
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
    }
}