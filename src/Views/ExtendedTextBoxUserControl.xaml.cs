//-----------------------------------------------------------------------  
// <copyright file="AutoCompleteTextBoxUserControl.xaml.cs" company="None">  
//     Copyright (c) Allow to distribute this code and utilize this code for personal or commercial purpose.  
// </copyright>  
// <author>Asma Khalid</author>  
//-----------------------------------------------------------------------  

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views
{
    /// <summary>  
    /// Interaction logic for Autocomplete Text Box UserControl  
    /// </summary>  
    public partial class ExtendedTextBoxUserControl : UserControl, INotifyPropertyChanged
    {
        public object PlaceHolder
        {
            get => TextBox.Tag;
            set => TextBox.Tag = value;
        }

        public string Text
        {
            get => TextBox.Text;
            set => TextBox.Text = value;
        }

        public TextWrapping TextWrapping
        {
            get => TextBox.TextWrapping;
            set => TextBox.TextWrapping = value;
        }

        private bool _hasError = false;
        public bool HasError
        {
            get => _hasError;
            set
            {
                if (value != _hasError)
                {
                    _hasError = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event EventHandler<TextChangedEventArgs> TextChanged;

        public ExtendedTextBoxUserControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
