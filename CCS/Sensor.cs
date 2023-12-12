using System.Windows;

namespace CCS
{
    public class Sensor
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Index { get; set; }
        public bool IsWorking { get;  set; }
        public List<Point> PoIs { get; set; }

        public Sensor(double x, double y) {
            X = x;
            Y = y;
        }
    }
}
