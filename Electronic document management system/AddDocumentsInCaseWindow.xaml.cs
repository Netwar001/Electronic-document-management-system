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
    public partial class AddDocumentsInCaseWindow : Window
    {
        public class PanelsData
        {
            public string Id { get; set; }
            public string TableName { get; set; }
            public WrapPanel Panels { get; set; }
            public Button Btn { get; set; }
        }

        private DataTable mainTable;
        private List<DataBaseWindow.TableFilters> dataForFillFilters;
        private List<ElectronicCaseCard.DocumentsData> selectedDocuments;
        private List<ElectronicCaseCard.DocumentsData> allDocuments;
        private List<PanelsData> listForBtnFunction = new List<PanelsData>();
        private string tableName;
        private string windowName;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public AddDocumentsInCaseWindow(List<ElectronicCaseCard.DocumentsData> selectedData, string nameOfCase, List<ElectronicCaseCard.DocumentsData> allData, string window)
        {
            InitializeComponent();
            GetTableNames();
            SelectedData.Children.Add(new Label() { Content = "Документы входящие в состав " + nameOfCase, FontWeight = FontWeights.Bold });
            if (selectedData.Count > 0)
            {
                for (var i = 0; i < selectedData.Count; i ++)
                {
                    foreach (var id in selectedData[i].Id)
                    {
                        var wrapPanel = new WrapPanel() { Margin = new Thickness(0, 5, 0, 5) };
                        wrapPanel.Children.Add(new Label() { Content = "id документа: " + id });
                        wrapPanel.Children.Add(new Label() { Content = ", Название таблицы: " + selectedData[i].TableName });
                        SelectedData.Children.Add(wrapPanel);
                    }
                }
            }
            selectedDocuments = new List<ElectronicCaseCard.DocumentsData>();
            allDocuments = allData;
            windowName = window;
        }


        //получение списка таблиц
        public void GetTableNames()
        {
            var connection = new SqlConnection(connectionString);
            var tables = System.Configuration.ConfigurationManager.AppSettings["mainTables"].ToString();
            connection.Open();
            try
            {
                var getTableName = new SqlCommand("SELECT name FROM sys.objects WHERE type in (N'U') and name not in ('Архивные документы'," + tables + ")", connection);
                var readTableNames = getTableName.ExecuteReader();
                var necessaryTableNames = new List<string>();
                while (readTableNames.Read())
                    necessaryTableNames.Add(readTableNames.GetString(0));
                readTableNames.Close();
                TableNameComboBox.ItemsSource = necessaryTableNames;
            }
            finally
            {
                connection.Close();
            }
        }

        //работа с столбцами таблицы
        private void MainDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "dd.MM.yyyy";
        }

        //выбор документа для связи
        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataRowView = (DataRowView)MainDataGrid.SelectedItem;
            var row = dataRowView.Row.ItemArray;
            var wrapPanel = new WrapPanel() { Margin = new Thickness(0, 5, 0, 10) };
            wrapPanel.Children.Add(new Label() { Content = "id документа: " + row[0] });
            wrapPanel.Children.Add(new Label() { Content = ", Название таблицы: " + tableName });
            var classWithMethods = new Methods.ClassWithMethods();
            var btn = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Del-icon.png", UriKind.Relative));
            btn.Click += DeleteBtn_Click;
            wrapPanel.Children.Add(btn);
            SelectedData.Children.Add(wrapPanel);

            if (selectedDocuments.Find(x => x.TableName == tableName) != null)
                selectedDocuments[selectedDocuments.FindIndex(x => x.TableName == tableName)].Id.Add(row[0].ToString());
            else
                selectedDocuments.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { row[0].ToString() }, TableName = tableName });

            if (allDocuments.Find(x => x.TableName == tableName) != null)
                allDocuments[allDocuments.FindIndex(x => x.TableName == tableName)].Id.Add(row[0].ToString());
            else
                allDocuments.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { row[0].ToString() }, TableName = tableName });

            listForBtnFunction.Add(new PanelsData() { Id = row[0].ToString(), TableName = tableName, Panels = wrapPanel, Btn = btn });
            WorkWithBD();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var indexForBtn = listForBtnFunction.FindIndex(x => x.Btn == (sender as Button));
            var indexForDoc = selectedDocuments.FindIndex(x => x.TableName == listForBtnFunction[indexForBtn].TableName);
            selectedDocuments[indexForDoc].Id.Remove(listForBtnFunction[indexForBtn].Id);
            if (selectedDocuments[indexForDoc].Id.Count == 0)
                selectedDocuments.RemoveAt(indexForDoc);
            SelectedData.Children.Remove(listForBtnFunction[indexForBtn].Panels);
            WorkWithBD();
        }

        //выбор таблицы для отображения в datagrid
        private void TableNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tableName = TableNameComboBox.SelectedItem.ToString();
            WorkWithBD();
            InsertFilters();
        }

        //заполнение datagrid данными
        public void WorkWithBD()
        {
            mainTable = new DataTable();
            string accessLevel;
            if (User.AccessLevel == "Максимальный")
                accessLevel = "'Открытая информация', 'Конфиденциально', 'Строго конфиденциальная информация'";
            else if (User.AccessLevel == "Средний")
                accessLevel = "'Открытая информация', 'Конфиденциально'";
            else
                accessLevel = "'Открытая информация'";
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var strForCommand = "";
                if (allDocuments.Count > 0 && allDocuments.Find(x => x.TableName == tableName) != null)
                {
                    strForCommand += "select * from [" + tableName + "] where [Уровень доступа] in (" + accessLevel + ") and [id] not in ( ";
                    var index = allDocuments.FindIndex(x => x.TableName == tableName);
                    foreach (var id in allDocuments[index].Id)
                        strForCommand += id + ",";
                    strForCommand = strForCommand.Remove(strForCommand.Length - 1, 1);
                    strForCommand += ")";
                }
                else
                    strForCommand += "select * from [" + tableName + "] where [Уровень доступа] in (" + accessLevel + ")";
                var command = new SqlCommand(strForCommand, connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(mainTable);
                MainDataGrid.ItemsSource = mainTable.DefaultView;
            }
            finally
            {
                connection.Close();
            }
        }

        //добавление фильтров по всем столбцам
        private void InsertFilters()
        {
            dataForFillFilters = new List<DataBaseWindow.TableFilters>();
            DataPanel.Children.Clear();
            var optionsFields = new List<string>();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForOptionsFields = new SqlCommand("select [Поля с вариантом выбора] from [Информация о таблицах] where [Название таблицы] = '" + tableName + "'", connection);
                var readFields = commandForOptionsFields.ExecuteReader();
                while (readFields.Read())
                {
                    optionsFields = readFields.GetString(0).Split(';').ToList();
                }
                readFields.Close();
            }
            finally
            {
                connection.Close();
            }
            for (var i = 0; i < mainTable.Columns.Count; i++)
            {
                DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                if (mainTable.Columns[i].DataType != typeof(DateTime))
                {
                    if (optionsFields.Any(l => l.Contains(mainTable.Columns[i].ToString())))
                    {
                        var strWithItems = optionsFields[optionsFields.FindIndex(x => x.Contains(mainTable.Columns[i].ToString()))].Split(':')[1];
                        var items = strWithItems.Split(',').Select(t => t.Trim()).ToList();
                        var comboBox = new ComboBox() { ItemsSource = items, Name = "column" + i };
                        comboBox.SelectionChanged += ComboBox_ChangedInFilters;
                        dataForFillFilters.Add(new DataBaseWindow.TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyComboBox = comboBox });
                        DataPanel.Children.Add(comboBox);
                    }
                    else
                    {
                        var textBox = new TextBox() { Name = "column" + i };
                        textBox.TextChanged += TextBox_ChangedInFilters;
                        dataForFillFilters.Add(new DataBaseWindow.TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyTextBox = textBox });
                        DataPanel.Children.Add(textBox);
                    }
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

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDocuments.Count != 0)
            {
                if (windowName == "ElectronicCaseCard")
                    ElectronicCaseCard.EventOnAddDocumentsInCase.Value = selectedDocuments;
                else
                    AddCaseWindow.EventOnAddDocumentsInCase.Value = selectedDocuments;
                Close();
            }
            else
                MessageBox.Show("Выберете документы, которые будут добавлены в дело");
        }
    }
}
