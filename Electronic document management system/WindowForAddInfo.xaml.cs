using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
    public partial class WindowForAddInfo : Window
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public WindowForAddInfo()
        {
            InitializeComponent();
            var subdivisions = new List<string>();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var getName = new SqlCommand("SELECT [Наименование] FROM [Подразделения]", connection);
                var reader = getName.ExecuteReader();
                while (reader.Read())
                {
                    subdivisions.Add(reader.GetString(0));
                }
                reader.Close();
            }
            finally
            {
                connection.Close();
            }
            subdivisions.Sort();
            ComboBox1.ItemsSource = subdivisions;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var str = "";
            if (TextBox.Text != "")
            {
                str += "ФИО: " + TextBox.Text;
                if (TextBox1.Text == "" || ComboBox1.Text == "")
                    str += ";";
            }
            if (TextBox1.Text != "")
            {
                if (str != "")
                    str += ", ";
                str += "Должность: " + TextBox1.Text;
                if (ComboBox1.Text == "")
                    str += ";";
            }
            if (ComboBox1.Text != "")
            {
                if (str != "")
                    str += ", ";
                str += "Подразделение: " + ComboBox1.Text + ";";
            }
            Data.Value = str;
            Close();
        }
    }
}
