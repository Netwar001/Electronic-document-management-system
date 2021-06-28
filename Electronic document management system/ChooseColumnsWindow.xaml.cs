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
using System.Windows.Shapes;

namespace Electronic_document_management_system
{
    public partial class ChooseColumnsWindow : Window
    {
        private List<CheckBox> checkBoxesList;
        public ChooseColumnsWindow(List<string> headers, List<string> visibleHeaders)
        {
            InitializeComponent();
            AddElementsOnPanel(headers, visibleHeaders);
        }

        //добавление элементов на панель
        public void AddElementsOnPanel(List<string> headers, List<string> visibleHeaders)
        {
            checkBoxesList = new List<CheckBox>();
            for (var i = 0; i < headers.Count; i++)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Content = headers[i];
                checkBox.Name = "checkBox" + i;
                if (visibleHeaders.Contains(headers[i]))
                    checkBox.IsChecked = true;
                checkBoxesList.Add(checkBox);
                mainStackPanel.Children.Add(checkBox);
            }
        }

        private void AgreeButton_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<string>();
            for (var i = 0; i < mainStackPanel.Children.Count; i++)
            {
                if (checkBoxesList[i].IsChecked == true)
                    list.Add(checkBoxesList[i].Content.ToString());
            }
            HeadersData.Value = list;
            Close();
        }
    }
}
