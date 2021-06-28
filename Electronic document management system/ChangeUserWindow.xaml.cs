using System;
using System.Collections.Generic;
using System.Data;
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
    public partial class ChangeUserWindow : Window
    {
        private DataTable mainTable;
        private string oldEmail;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public ChangeUserWindow()
        {
            InitializeComponent();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select [ФИО],[Должность],[Подразделение],[Email] from [Учетные записи]", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);
                MainDataGrid.ItemsSource = mainTable.DefaultView;

                var command = new SqlCommand("select * from [Должности]", connection);
                var adapter1 = new SqlDataAdapter(command);
                var positionTable = new DataTable();
                adapter1.Fill(positionTable);
                var items = positionTable.AsEnumerable().Select(row => row.Field<string>("Должность")).ToList();
                items.Sort();
                PositionComboBox.ItemsSource = items;

                var subdivisions = new List<string>();
                var getName = new SqlCommand("SELECT [Наименование] FROM [Подразделения]", connection);
                var reader = getName.ExecuteReader();
                while (reader.Read())
                {
                    subdivisions.Add(reader.GetString(0));
                }
                reader.Close();
                subdivisions.Sort();
                SubdivisionComboBox.ItemsSource = subdivisions;
            }
            finally
            {
                connection.Close();
            }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataRowView = (DataRowView)MainDataGrid.SelectedItem;
            var row = dataRowView.Row.ItemArray;
            NameTextBox.Text = (string)row[0];
            PositionComboBox.SelectedItem = (string)row[1];
            SubdivisionComboBox.SelectedItem = (string)row[2];
            EmailTextBox.Text = (string)row[3];
            oldEmail = (string)row[3];
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text != "" && PositionComboBox.Text != "" && EmailTextBox.Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    SqlCommand update = connection.CreateCommand();
                    update.Connection = connection;
                    StringBuilder str = new StringBuilder();
                    str.AppendFormat("update [Учетные записи] set [{0}]='{1}', [{2}]='{3}', [{4}]='{5}', [{6}]='{7}' where [{6}]='{8}'",
                        mainTable.Columns[0].ToString(), NameTextBox.Text, mainTable.Columns[1].ToString(), PositionComboBox.Text, 
                        mainTable.Columns[2].ToString(), SubdivisionComboBox.Text, mainTable.Columns[3].ToString(), EmailTextBox.Text, oldEmail);
                    update.CommandText = str.ToString();
                    update.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                MessageBox.Show("Учетная запись успешно изменена");
                new ChangeUserWindow().Show();
                Close();
            }
            else
                MessageBox.Show("Заполните все поля");
        }
    }
}
