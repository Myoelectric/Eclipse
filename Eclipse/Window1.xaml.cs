using System;
using System.Windows;

namespace Eclipse
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            TextBoxMenuSize.Text = Properties.Settings.Default.MenuSize.ToString();
            TextBoxOpenDelay.Text = Properties.Settings.Default.OpenDelay.ToString();
        }
    }
}
