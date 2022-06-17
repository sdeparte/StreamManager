//-----------------------------------------------------------------------  
// <copyright file="AutoCompleteTextBoxUserControl.xaml.cs" company="None">  
//     Copyright (c) Allow to distribute this code and utilize this code for personal or commercial purpose.  
// </copyright>  
// <author>Asma Khalid</author>  
//-----------------------------------------------------------------------  

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views
{
    /// <summary>  
    /// Interaction logic for Autocomplete Text Box UserControl  
    /// </summary>  
    public partial class AutoCompleteTextBoxUserControl : UserControl
    {
        private bool _selectionInProcess = false;
        private object _selectionItem = null;

        public ObservableCollection<Object> AutoSuggestionList { get; set; } = new ObservableCollection<Object>();

        public string Text
        {
            get => AutoTextBox.Text;
            set => AutoTextBox.Text = value;
        }

        public object SelectedItem
        {
            get => _selectionItem;

            set
            {
                _selectionInProcess = true;

                AutoTextBox.Text = value?.ToString();
                _selectionItem = value;
                AutoList.SelectedIndex = -1;

                _selectionInProcess = false;

                SelectionChanged?.Invoke(this, null);
            }
        }

        public event EventHandler<TextChangedEventArgs> TextChanged;

        public event EventHandler SelectionChanged;

        public AutoCompleteTextBoxUserControl()
        {
            InitializeComponent();
            DataContext = this;
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
            TextChanged?.Invoke(this, e);

            if (_selectionInProcess)
            {
                return;
            }

            _selectionItem = null;

            TryToOpenAutoSuggestionBox();

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
        }

        private void AutoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CloseAutoSuggestionBox();
        }
    }
}
