﻿using System.IO;
using System.Windows;

namespace CCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public GUI1Window GUI1Window;
        public GUI2Window GUI2Window;
 
        public MainWindow()
        {
            InitializeComponent();
            CreateFolders();

            GUI1Window = new GUI1Window(this);
            GUI2Window = new GUI2Window(this);

            ViewControl.Content = GUI1Window.Content;

            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
        }

        private void CreateFolders()
        {
            if (!Directory.Exists("INIT-RESULTS"))
            {
                Directory.CreateDirectory("INIT-RESULTS");
            }

            if (!Directory.Exists("m-RESULTS"))
            {
                Directory.CreateDirectory("m-RESULTS");
            }

            if (!Directory.Exists("DATA"))
            {
                Directory.CreateDirectory("DATA");
            }

            if (!Directory.Exists("RESULTS"))
            {
                Directory.CreateDirectory("RESULTS");
            }
        }

        public void ChangeFirstPageToSecond(List<Point> pois, List<Sensor> sensors, double range, double propability)
        {
            ViewControl.Content = GUI2Window.Content;
            GUI2Window.SetDataFromPageOne(pois, sensors, range, propability);
        }

        public void ChangeSecondPageToFirst()
        {
            ViewControl.Content = GUI1Window.Content;
        }
    }
}