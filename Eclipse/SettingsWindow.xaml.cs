using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Eclipse
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            TextBoxMenuSize.Text = Properties.Settings.Default.MenuSize.ToString();
            TextBoxOpenDelay.Text = Properties.Settings.Default.OpenDelay.ToString();

            /*   Control Events   */
            TextBoxMenuSize.PreviewKeyDown += TextBox_PreviewKeyDown;
            TextBoxOpenDelay.PreviewKeyDown += TextBox_PreviewKeyDown;
            ButtonSave.Click += ButtonSave_Click;
            ButtonClear.Click += ButtonClear_Click;
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Clips.Clear();
            Properties.Settings.Default.Save();
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Regex regex = new Regex(@"[0-9+\-\/\*\(\)]");

            if ((!regex.IsMatch(e.Key.ToString()) && e.Key != Key.Back && e.Key != Key.Delete) || e.Key == Key.Space) { e.Handled = true; }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MenuSize = Int32.Parse(TextBoxMenuSize.Text);
            Properties.Settings.Default.OpenDelay = Int32.Parse(TextBoxOpenDelay.Text);
            Properties.Settings.Default.Save();
        }
    }
}
