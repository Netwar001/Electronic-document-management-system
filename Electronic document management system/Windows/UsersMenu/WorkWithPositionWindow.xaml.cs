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
    public partial class WorkWithPositionWindow : Window
    {
        private TextBox positionName;
        private ComboBox accessLevel;
        private string previousPositionName;
        private List<CheckBox> capabilities;
        private DataTable mainTable;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public WorkWithPositionWindow()
        {
            InitializeComponent();
            ChooseComboBox.ItemsSource = new List<string>() { "Изменить существующую должность", "Добавить новую должность" };
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select * from [Должности]", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);
            }
            finally
            {
                connection.Close();
            }
        }

        private void ChooseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataPanel.Children.Clear();
            PanelForComboBox.Children.Clear();
            if ((sender as ComboBox).SelectedItem.ToString() == "Изменить существующую должность")
            {
                var items = mainTable.AsEnumerable().Select(row => row.Field<string>("Должность")).ToList();
                items.Sort();
                var comboBox = new ComboBox() { ItemsSource = items, Width = 200 };
                comboBox.SelectionChanged += ComboBox_SelectPosition;
                PanelForComboBox.Children.Add(comboBox);
            }
            else
            {
                capabilities = new List<CheckBox>();
                for (var i = 0; i < mainTable.Columns.Count; i++)
                {
                    DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                    if (mainTable.Columns[i].ColumnName == "Должность")
                    {
                        var textBox = new TextBox();
                        positionName = textBox;
                        DataPanel.Children.Add(textBox);
                    }
                    else if (mainTable.Columns[i].ColumnName == "Уровень доступа")
                    {
                        var comboBox = new ComboBox() { ItemsSource = new List<string>() { "Максимальный", "Средний", "Минимальный" } };
                        accessLevel = comboBox;
                        DataPanel.Children.Add(comboBox);
                    }
                    else
                    {
                        var checkBox = new CheckBox();
                        capabilities.Add(checkBox);
                        DataPanel.Children.Add(checkBox);
                    }
                }
            }
        }

        private void ComboBox_SelectPosition(object sender, SelectionChangedEventArgs e)
        {
            DataPanel.Children.Clear();
            var items = mainTable.AsEnumerable().Where(row => row.Field<string>("Должность") == (sender as ComboBox).SelectedItem.ToString()).ToList()[0].ItemArray;
            capabilities = new List<CheckBox>();
            for (var i = 0; i < mainTable.Columns.Count; i++)
            {
                DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                if (mainTable.Columns[i].ToString() == "Должность")
                {
                    var textBox = new TextBox() { Text = (string)items[i] };
                    previousPositionName = (string)items[i];
                    positionName = textBox;
                    DataPanel.Children.Add(textBox);
                }
                else if (mainTable.Columns[i].ColumnName == "Уровень доступа")
                {
                    var comboBox = new ComboBox() { ItemsSource = new List<string>() { "Максимальный", "Средний", "Минимальный" }, SelectedItem = (string)items[i] };
                    accessLevel = comboBox;
                    DataPanel.Children.Add(comboBox);
                }
                else
                {
                    var checkBox = new CheckBox() { IsChecked = (bool)items[i] };
                    capabilities.Add(checkBox);
                    DataPanel.Children.Add(checkBox);
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (positionName.Text != "" && accessLevel.Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    if (PanelForComboBox.Children.Count == 0)
                    {
                        SqlCommand insert = connection.CreateCommand();
                        insert.Connection = connection;
                        var str = "insert into [Должности] values ('" + positionName.Text + "','" + accessLevel.Text + "','";
                        for (var i = 0; i < capabilities.Count; i++)
                        {
                            if (capabilities[i].IsChecked == true)
                                str += 1 + "','";
                            else
                                str += 0 + "','";
                        }
                        str = str.Remove(str.Length - 2, 2) + ")";
                        insert.CommandText = str;
                        insert.ExecuteNonQuery();
                        MessageBox.Show("Новая должность успешно добавлена");
                    }
                    else
                    {
                        SqlCommand update = connection.CreateCommand();
                        update.Connection = connection;
                        StringBuilder str = new StringBuilder();
                        str.AppendFormat("update [Должности] set [{0}]='{1}', ", mainTable.Columns[0].ToString(), positionName.Text);
                        str.AppendFormat("[{0}]='{1}', ", mainTable.Columns[1].ToString(), accessLevel.Text);
                        for (var i = 0; i < capabilities.Count; i++)
                        {
                            if (capabilities[i].IsChecked == true)
                                str.AppendFormat("[{0}]='{1}', ", mainTable.Columns[i + 2].ToString(), 1);
                            else
                                str.AppendFormat("[{0}]='{1}', ", mainTable.Columns[i + 2].ToString(), 0);
                        }
                        str.Remove(str.Length - 2, 2);
                        str.AppendFormat("where [{0}]='{1}'", mainTable.Columns[0].ToString(), previousPositionName);
                        update.CommandText = str.ToString();
                        update.ExecuteNonQuery();
                        MessageBox.Show("Должность успешно изменена");
                    }
                }
                finally
                {
                    connection.Close();
                }
                new WorkWithPositionWindow().Show();
                Close();
            }
            else
                MessageBox.Show("Заполните обязательные поля");
        }
    }
}
