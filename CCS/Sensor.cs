using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCS
{
    internal class Sensor
    {
        public double X;
        public double Y;
        public int Index;
        public bool IsWorking;

        public Sensor(double x, double y) {
            X = x;
            Y = y;
        }
    }
}
