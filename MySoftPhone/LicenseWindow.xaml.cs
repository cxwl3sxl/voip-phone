using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MySoftPhone
{
    /// <summary>
    /// LicenseWindow.xaml interaction logic
    /// </summary>
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
        }

        private void Regist_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var fs = new StreamWriter("license.txt", false))
                {
                    fs.WriteLine(TextBoxUserName.Text);
                    fs.WriteLine(TextBoxLicense.Text);
                    fs.Close();
                }
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show("save failed！\n" + exception.Message);
            }
        }

        private void LicenseWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            string[] licenseInfo = null;
            if (File.Exists("license.txt"))
            {
                licenseInfo = File.ReadAllLines("license.txt");
            }
            if (licenseInfo == null) return;
            TextBoxUserName.Text = licenseInfo.Length > 0 ? licenseInfo[0] : "";
            TextBoxLicense.Text = licenseInfo.Length > 1 ? licenseInfo[1] : "";
        }
    }
}
