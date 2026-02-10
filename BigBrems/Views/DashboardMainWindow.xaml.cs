using System;
using System.Windows;
using BigBrems.ViewModels;

namespace BigBrems.Views
{
    public partial class DashboardMainWindow : Window
    {
        public DashboardMainWindow()
        {
            InitializeComponent();

            // Link the UI to the Logic!
            this.DataContext = new DashboardViewModel();
        }

        private void BtnEnglish_Click(object sender, RoutedEventArgs e)
        {
            SwitchLanguage("en");
        }

        private void BtnGerman_Click(object sender, RoutedEventArgs e)
        {
            SwitchLanguage("de");
        }

        private void SwitchLanguage(string languageCode)
        {
            var dict = new ResourceDictionary();
            dict.Source = new Uri($"pack://application:,,,/Resources/Strings.{languageCode}.xaml");
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}