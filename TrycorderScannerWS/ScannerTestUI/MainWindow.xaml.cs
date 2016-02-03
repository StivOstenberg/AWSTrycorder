﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using AWSFunctions;



namespace ScannerTestUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
    AWSFunctions.ScanAWS Scanner = new ScanAWS();

        public MainWindow()
        {
            InitializeComponent();
            ProfilesComboBox.ItemsSource = Scanner.GetProfileNames();
            FillRegions();
        }

        private void FillRegions()
        {
            RegionListcomboBox.ItemsSource = Scanner.GetRegionNames(); 
        }
        private void GetProfilesClick(object sender, RoutedEventArgs e)
        {
            
             ProfilesComboBox.ItemsSource = Scanner.GetProfileNames();
            
            
        }

        private void ProfilesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
