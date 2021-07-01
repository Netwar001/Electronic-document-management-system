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
    public partial class FullTextSearchWindow : Window
    {
        private DataTable mainTable;
        private object[] selectedRow;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;
        public FullTextSearchWindow()
        {
            InitializeComponent();
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                mainTable = new DataTable();
                var command = new SqlCommand("select [id документа],[Название таблицы],[Версия],[FileName] as 'Название файла' from [Документы] where CONTAINS (DocData, N'\"" + 
                    SearchingTextBox.Text + "\"')", connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(mainTable);
                MainDataGrid.ItemsSource = mainTable.DefaultView;
            }
            finally
            {
                connection.Close();
            }
        }

        private void OpenInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRow != null)
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    string accessLevel;
                    if (User.AccessLevel == "Максимальный")
                        accessLevel = "'Открытая информация', 'Конфиденциально', 'Строго конфиденциальная информация'";
                    else if (User.AccessLevel == "Средний")
                        accessLevel = "'Открытая информация', 'Конфиденциально'";
                    else
                        accessLevel = "'Открытая информация'";
                    var command = new SqlCommand("select * from [" + selectedRow[1].ToString() + "] where [Уровень доступа] in (" + accessLevel + ") and " +
                        "[id] = '" + selectedRow[0].ToString() + "'", connection);
                    var adapter = new SqlDataAdapter(command);
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count != 0)
                    {
                        var headers = new List<string>();
                        foreach (DataColumn column in table.Columns)
                        {
                            headers.Add(column.ColumnName);
                        }

                        new ElectronicDocumentCard(headers, table.Rows[0].ItemArray, selectedRow[1].ToString()).Show();
                    }
                    else
                        MessageBox.Show("Ваш уровень доступа не соответствует требуемому");
                }
                finally
                {
                    connection.Close();
                }
            }
            else
                MessageBox.Show("Выберете документ из списка");
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((DataRowView)MainDataGrid.SelectedItem != null)
                selectedRow = ((DataRowView)MainDataGrid.SelectedItem).Row.ItemArray;
        }
    }
}
