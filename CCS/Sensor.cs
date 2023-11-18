using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCS
{
    internal class Sensor
    {
        public int X;
        public int Y;
        public int Index;
        public bool IsWorking;

        public Sensor(int x, int y) {
            X = x;
            Y = y;
        }
    }
}
