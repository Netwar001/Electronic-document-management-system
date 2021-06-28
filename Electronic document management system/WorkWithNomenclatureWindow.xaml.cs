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
    public partial class WorkWithNomenclatureWindow : Window
    {
        public static class EventOnAddCase
        {
            public static EventHandler CaseAdded = delegate { };
            private static string _text;

            public static string Value
            {
                get { return _text; }
                set
                {
                    _text = value;
                    CaseAdded(null, EventArgs.Empty);
                }
            }
        }

        private List<DataBaseWindow.TableFilters> dataForFillFilters;
        private DataTable mainTable;
        private object[] selectedRow;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public WorkWithNomenclatureWindow(string btnName)
        {
            InitializeComponent();
            EventOnAddCase.CaseAdded += CaseAdded;
            VariantBtn.Content = btnName;
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
                subdivisions.Sort();
                NameOfSubdivisionComboBox.ItemsSource = subdivisions;
            }
            finally
            {
                connection.Close();
            }
        }

        public void CaseAdded(object sender, EventArgs e)
        {
            NameOfSubdivisionComboBox.SelectedItem = null;
            NameOfSubdivisionComboBox.SelectedItem = EventOnAddCase.Value;
        }

        private void VariantBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NameOfSubdivisionComboBox.SelectedItem != null)
            {
                if (VariantBtn.Content.ToString() == "Создать дело")
                    new AddCaseWindow(NameOfSubdivisionComboBox.Text).Show();
                else 
                {
                    if (selectedRow.Length != 0)
                    {
                        ElectronicCaseCard.EventOnMoveDocumentsInCase.Value = new List<string>() 
                        { 
                            selectedRow[0].ToString(),
                            selectedRow[1].ToString(),
                            NameOfSubdivisionComboBox.Text,
                            selectedRow[2].ToString(),
                            selectedRow[3].ToString(),
                            selectedRow[4].ToString(),
                            selectedRow[5].ToString(),
                        };
                        Close();
                    }
                    else
                        MessageBox.Show("Для перемещения документа выберете дело");
                }
            }
            else
                MessageBox.Show("Выберете подразделение");
        }

        private void MainDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "dd.MM.yyyy";
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VariantBtn.Content.ToString() == "Создать дело")
            {
                var dataRowView = (DataRowView)MainDataGrid.SelectedItem;
                var row = dataRowView.Row.ItemArray;
                new ElectronicCaseCard(row, NameOfSubdivisionComboBox.Text).Show();
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VariantBtn.Content.ToString() == "Подтвердить выбор")
                selectedRow = ((DataRowView)MainDataGrid.SelectedItem).Row.ItemArray;
        }

        //добавление фильтров по всем столбцам
        private void InsertFilters()
        {
            dataForFillFilters = new List<DataBaseWindow.TableFilters>();
            DataPanel.Children.Clear();
            for (var i = 0; i < mainTable.Columns.Count; i++)
            {
                DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                if (mainTable.Columns[i].DataType != typeof(DateTime))
                {
                    var textBox = new TextBox() { Name = "column" + i };
                    textBox.TextChanged += TextBox_ChangedInFilters;
                    dataForFillFilters.Add(new DataBaseWindow.TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyTextBox = textBox });
                    DataPanel.Children.Add(textBox);
                }
                else
                {
                    var wrapPanel = new WrapPanel();
                    var datePicker = new DatePicker();
                    var items = new List<string>() { ">", "=", "<" };
                    var comboBox = new ComboBox() { ItemsSource = items };
                    comboBox.SelectionChanged += ComboBox_ChangedInFilters;
                    datePicker.SelectedDateChanged += DatePicker_ChangedInFilters;
                    dataForFillFilters.Add(new DataBaseWindow.TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyDatePicker = datePicker, EmptyComboBox = comboBox });
                    wrapPanel.Children.Add(comboBox);
                    wrapPanel.Children.Add(datePicker);
                    DataPanel.Children.Add(wrapPanel);
                }
            }
        }

        //Изменения в фильтрах
        private void DatePicker_ChangedInFilters(object sender, SelectionChangedEventArgs e)
        {
            TextFilter();
        }

        private void TextBox_ChangedInFilters(object sender, TextChangedEventArgs e)
        {
            TextFilter();
        }

        private void ComboBox_ChangedInFilters(object sender, SelectionChangedEventArgs e)
        {
            TextFilter();
        }

        //объединение фильтра по дате и по текстовым полям
        public void TextFilter()
        {
            DataView dataView = mainTable.DefaultView;
            StringBuilder str = new StringBuilder();

            for (var i = 0; i < dataForFillFilters.Count; i++)
            {
                if (dataForFillFilters[i].EmptyDatePicker != null)
                {
                    if (dataForFillFilters[i].EmptyComboBox.Text != "" && dataForFillFilters[i].EmptyDatePicker.Text != "")
                    {
                        str.AppendFormat("[{0}] {1} '{2}'", dataForFillFilters[i].Column, dataForFillFilters[i].EmptyComboBox.Text, dataForFillFilters[i].EmptyDatePicker.Text);
                        str.Append(" and ");
                    }
                }
                else if (dataForFillFilters[i].EmptyComboBox != null)
                {
                    if (dataForFillFilters[i].EmptyComboBox.Text != "")
                    {
                        str.AppendFormat("[{0}] Like '{1}'", dataForFillFilters[i].Column, dataForFillFilters[i].EmptyComboBox.Text);
                        str.Append(" and ");
                    }
                }
                else if (dataForFillFilters[i].EmptyTextBox != null)
                {
                    if (dataForFillFilters[i].EmptyTextBox.Text != "")
                    {
                        if (dataForFillFilters[i].Column == "id" || dataForFillFilters[i].Column == "id документа")
                            str.AppendFormat("[{0}] = '{1}'", dataForFillFilters[i].Column, dataForFillFilters[i].EmptyTextBox.Text);
                        else
                            str.AppendFormat("[{0}] Like '%{1}%'", dataForFillFilters[i].Column, dataForFillFilters[i].EmptyTextBox.Text);
                        str.Append(" and ");
                    }
                }
            }
            if (str.Length != 0)
                dataView.RowFilter = str.Remove(str.Length - 4, 4).ToString();
            else
                dataView.RowFilter = "";
            MainDataGrid.ItemsSource = dataView;
            MainDataGrid.Items.Refresh();
        }

        private void NameOfSubdivisionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select distinct [Индекс],[Заголовок дела],[Статус],[Начато],[Срок актуальности],[Примечание] from [Номенклатура дел] where [Подразделение] = '" +
                    (sender as ComboBox).SelectedItem + "'", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);
                MainDataGrid.ItemsSource = mainTable.DefaultView;
            }
            finally
            {
                connection.Close();
            }
            InsertFilters();
        }
    }
}
