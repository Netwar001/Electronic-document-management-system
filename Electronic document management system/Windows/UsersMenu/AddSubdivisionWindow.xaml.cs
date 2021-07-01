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
    public partial class AddSubdivisionWindow : Window
    {

        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;
        public AddSubdivisionWindow()
        {
            InitializeComponent();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (subdivisionName.Text != "" && leaderName.Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    var subdivisions = new List<string>();
                    var getName = new SqlCommand("SELECT [Наименование] FROM [Подразделения]", connection);
                    var reader = getName.ExecuteReader();
                    while (reader.Read())
                    {
                        subdivisions.Add(reader.GetString(0));
                    }
                    reader.Close();

                    if (!subdivisions.Contains(subdivisionName.Text))
                    {
                        SqlCommand addSubdivision = connection.CreateCommand();
                        addSubdivision.Connection = connection;
                        addSubdivision.CommandText = "insert into [Подразделения] values ('" + subdivisionName.Text + "','" + leaderName.Text + "')";
                        addSubdivision.ExecuteNonQuery();
                    }
                    else
                        MessageBox.Show("Введенная должность уже существует в базе данных");
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}
