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
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
            var userInfo = new List<string>() { "ФИО: ", "Должность: ", "Подразделение: ", "e-mail: " };
            var userField = new List<object>() { User.Name, User.Position, User.Subdivision, User.Email };
            for (var i = 0; i < userInfo.Count; i++)
                DataPanel.Children.Add(new Label() { Content = userInfo[i] + userField[i] });
        }
    }
}
