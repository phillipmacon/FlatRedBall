﻿using FlatRedBall.PlatformerPlugin.Data;
using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Utilities;
using System;
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
using Xceed.Wpf.Toolkit;

namespace FlatRedBall.PlatformerPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        PlatformerEntityViewModel ViewModel =>
            DataContext as PlatformerEntityViewModel;

        public MainControl()
        {
            InitializeComponent();
        }

        private void AddEmptyPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Unnamed";
            AddPlatformerValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddDefaultGroundPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Ground";
            AddPlatformerValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddDefaultAirPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Air";
            AddPlatformerValues(name);

            AddControlButtonInstance.IsOpen = false;
        }

        private void AddPlatformerValues(string predefinedName)
        {
            var values = PredefinedPlatformerValues.GetValues(predefinedName);

            string newItemName = predefinedName;
            while(ViewModel.PlatformerValues.Any(item=>item.Name == newItemName))
            {
                newItemName = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(newItemName);
            }

            values.Name = newItemName;

            ViewModel.PlatformerValues.Add(values);
        }

        private void PlatformerValuesXClick(object sender, RoutedEventArgs e)
        {
            var valuesViewModel = (sender as UserControl).DataContext as PlatformerValuesViewModel;

            bool contains = ViewModel.PlatformerValues.Contains(valuesViewModel);

            if(contains)
            {
                ViewModel.PlatformerValues.Remove(valuesViewModel);
            }
        }
    }
}
