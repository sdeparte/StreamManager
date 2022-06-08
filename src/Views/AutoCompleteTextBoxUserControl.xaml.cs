//-----------------------------------------------------------------------  
// <copyright file="AutoCompleteTextBoxUserControl.xaml.cs" company="None">  
//     Copyright (c) Allow to distribute this code and utilize this code for personal or commercial purpose.  
// </copyright>  
// <author>Asma Khalid</author>  
//-----------------------------------------------------------------------  

using StreamManager.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views
{
    /// <summary>  
    /// Interaction logic for Autocomplete Text Box UserControl  
    /// </summary>  
    public partial class AutoCompleteTextBoxUserControl : UserControl
    {
        public string PlaceHolder { get; set; }

        public ObservableCollection<Object> AutoSuggestionList { get; set; } = new ObservableCollection<Object>();

        public string Text
        {
            get { return AutoTextBox.Text; }
            set { AutoTextBox.Text = value; }
        }

        public Object SelectedItem { get; set; }

        public event EventHandler<TextChangedEventArgs> TextChanged;

        public AutoCompleteTextBoxUserControl()
        {
            InitializeComponent();
        }

        private void OpenAutoSuggestionBox()
        {
            AutoListPopup.Visibility = Visibility.Visible;
            AutoListPopup.IsOpen = true;
            AutoList.Visibility = Visibility.Visible;
        }

        private void CloseAutoSuggestionBox()
        {
            AutoListPopup.Visibility = Visibility.Collapsed;
            AutoListPopup.IsOpen = false;
            AutoList.Visibility = Visibility.Collapsed;
        }

        private void TryToOpenAutoSuggestionBox()
        {
            if (string.IsNullOrEmpty(AutoTextBox.Text))
            {
                CloseAutoSuggestionBox();
                return;
            }

            OpenAutoSuggestionBox();
        }

        private void AutoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoTextBox.Text != SelectedItem?.ToString())
            {
                TryToOpenAutoSuggestionBox();
            }
            
            TextChanged?.Invoke(this, e);
        }

        private void AutoTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TryToOpenAutoSuggestionBox();
        }

        private void AutoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CloseAutoSuggestionBox();

            if (AutoList.SelectedIndex <= -1)
            {
                return;
            }

            SelectedItem = AutoList.SelectedItem;
            AutoTextBox.Text = AutoList.SelectedItem.ToString();
            AutoList.SelectedIndex = -1;
        }

        private void AutoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CloseAutoSuggestionBox();
        }
    }
}
