using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCS
{
    internal class Sensor
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Index { get; set; }
        public bool IsWorking { get;  set; }

        public Sensor(double x, double y) {
            X = x;
            Y = y;
        }
    }
}
