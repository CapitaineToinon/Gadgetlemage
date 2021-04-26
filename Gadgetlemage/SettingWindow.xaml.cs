using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;



namespace Gadgetlemage
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();

            tbxImageAutomatic.Text = Properties.Settings.Default.ImageAutomatic;
            tbxImageManual.Text = Properties.Settings.Default.ImageManual;
            tbxImageDestination.Text = Properties.Settings.Default.ImageDestination;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
        private OpenFileDialog openFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files(*.jpg, *.jpeg *.png) |*.jpg;*.jpeg;*.png";
                                    //"All Media Files(*.gif *.wav *.wma *.wmv *.avi *.mp4 *.mov *.mkv) |*.gif;*.wav;*.wma;*.wmv;*.avi;*.mp4;*.mov;*.mkv";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return openFileDialog;
            }
            return null;
        }

        private void btnImageAutomatic_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = openFileDialog();
            if (ofd != null)
            {
                Properties.Settings.Default.ImageAutomaticPath = ofd.FileName;
                Properties.Settings.Default.ImageAutomatic = ofd.SafeFileName;
                tbxImageAutomatic.Text = ofd.SafeFileName;
            }
        }

        private void btnImageManual_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = openFileDialog();
            if(ofd != null)
            {
                Properties.Settings.Default.ImageManualPath = ofd.FileName;
                Properties.Settings.Default.ImageManual = ofd.SafeFileName;
                tbxImageManual.Text = ofd.SafeFileName;
            }
        }

        private void btnImageDestination_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = openFileDialog();
            if (ofd != null)
            {
                Properties.Settings.Default.ImageDestinationPath = ofd.FileName;
                Properties.Settings.Default.ImageDestination = ofd.SafeFileName;
                tbxImageDestination.Text = ofd.SafeFileName;

            }
        }

        private void btnCreateHotkey_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteHotkey_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
