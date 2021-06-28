using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using WIA;

namespace Electronic_document_management_system
{
    public partial class ScanerWindow : Window
    {
        List<DeviceInfo> scannersAvailable;
        public ScanerWindow()
        {
            InitializeComponent();

            var deviceManager = new DeviceManager();
            scannersAvailable = new List<DeviceInfo>();
            // Цикл по списку устройств, для выбора доступных
            for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++)
            {
                // Добавление устройства если это сканер
                if (deviceManager.DeviceInfos[i].Type == WiaDeviceType.ScannerDeviceType)
                    scannersAvailable.Add(deviceManager.DeviceInfos[i]);
            }
            SelectScanerComboBox.ItemsSource = scannersAvailable;

            Directory.CreateDirectory("C:\\EDMS_App\\Scanned_Documents");
            FolderTextBox.Text = @"C:\\EDMS_App\\Scanned_Documents";
        }

        private void ChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    FolderTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectScanerComboBox.SelectedItem != null && FolderTextBox.Text != "" && FileNameTextBox.Text != "")
            {
                var image = ScanPNG();
                var path = FolderTextBox.Text + @"\\" + FileNameTextBox.Text + ".png";
                if (File.Exists(path))
                {
                    path = FolderTextBox.Text + @"\\" + FileNameTextBox.Text + " " + DateTime.Now.ToString() + ".png";
                }
                image.SaveFile(path);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                ImageBox.Source = bitmap;
            }
            else
                System.Windows.MessageBox.Show("Задайте все свойства");
        }

        public ImageFile ScanPNG()
        {
            // Подключение к выбранному сканеру
            var device = (SelectScanerComboBox.SelectedItem as DeviceInfo).Connect();
            CommonDialogClass dlg = new CommonDialogClass();
            var item = device.Items[1];
            try
            {
                object scanResult = dlg.ShowTransfer(item, FormatID.wiaFormatPNG, true);
                if (scanResult != null)
                {
                    var imageFile = (ImageFile)scanResult;
                    return imageFile;
                }
            }
            catch (COMException e)
            {
                uint errorCode = (uint)e.ErrorCode;
                // вывод ошибок при сканировании
                if (errorCode == 0x80210006)
                    System.Windows.MessageBox.Show("Сканер занят или не готов");
                else if (errorCode == 0x80210064)
                    System.Windows.MessageBox.Show("Процесс сканирования был отменен");
            }
            return new ImageFile();
        }
    }
}
