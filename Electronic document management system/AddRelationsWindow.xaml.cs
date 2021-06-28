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
    public partial class AddRelationsWindow : Window
    {
        private DataTable mainTable;
        private TextBox textBoxData;
        private List<DataBaseWindow.TableFilters> dataForFillFilters;
        private List<WrapPanel> wrapPanelsList = new List<WrapPanel>();
        private List<ElectronicCaseCard.DocumentsData> allDocuments = new List<ElectronicCaseCard.DocumentsData>();
        private string nameOfWindow;
        private string tableName;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public AddRelationsWindow(List<string> selectedData, int chosenId, string chosenTableName, string windowName)
        {
            InitializeComponent();
            GetTableNames();
            SelectedData.Children.Add(new Label() { Content = "Название связи: " });
            nameOfWindow = windowName;
            allDocuments.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { chosenId.ToString() }, TableName = chosenTableName });
            if (selectedData != null)
            {
                var classWithMethods = new Methods.ClassWithMethods();
                var textBox = new TextBox() { Margin = new Thickness(0, 0, 0, 10), Text = selectedData[0], IsReadOnly = true };
                textBoxData = textBox;
                SelectedData.Children.Add(textBox);
                for (var i = 1; i < selectedData.Count; i += 2)
                {
                    var wrapPanel = new WrapPanel() { Margin = new Thickness(0, 5, 0, 10) };
                    wrapPanel.Children.Add(new Label() { Content = "id документа: " + selectedData[i] });
                    wrapPanel.Children.Add(new Label() { Content = "Название таблицы: " + selectedData[i + 1] });
                    var btn = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Del-icon.png", UriKind.Relative));
                    btn.Click += DeleteBtn_Click;
                    wrapPanel.Children.Add(btn);
                    wrapPanelsList.Add(wrapPanel);
                    SelectedData.Children.Add(wrapPanel);

                    if (allDocuments.Find(x => x.TableName == selectedData[i + 1]) != null)
                        allDocuments[allDocuments.FindIndex(x => x.TableName == selectedData[i + 1])].Id.Add(selectedData[i]);
                    else
                        allDocuments.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { selectedData[i] }, TableName = selectedData[i + 1] });
                }
                if (nameOfWindow == "ElectronicDocumentCard")
                    ElectronicDocumentCard.EventOnCreateRelations.option = "exist";
            }
            else
            {
                var textBox = new TextBox() { Margin = new Thickness(0, 0, 0, 10) };
                textBoxData = textBox;
                SelectedData.Children.Add(textBox);
            }
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
            var listForCheck = new List<string>();
            for (var i = 0; i < wrapPanelsList.Count; i++)
            {
                var listOfLabel = wrapPanelsList[i].Children.OfType<Label>().ToList();
                var id = listOfLabel[0].Content.ToString().Replace("id документа: ", "");
                var table = listOfLabel[1].Content.ToString().Replace("Название таблицы: ", "");
                listForCheck.Add(id + " " + table);
            }
            if (!listForCheck.Contains(row[0] + " " + tableName)) 
            {
                var wrapPanel = new WrapPanel() { Margin = new Thickness(0, 5, 0, 10) };
                wrapPanel.Children.Add(new Label() { Content = "id документа: " + row[0] });
                wrapPanel.Children.Add(new Label() { Content = "Название таблицы: " + tableName });
                var classWithMethods = new Methods.ClassWithMethods();
                var btn = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Del-icon.png", UriKind.Relative));
                btn.Click += DeleteBtn_Click;
                wrapPanel.Children.Add(btn);
                wrapPanelsList.Add(wrapPanel);
                SelectedData.Children.Add(wrapPanel);

                if (allDocuments.Find(x => x.TableName == tableName) != null)
                    allDocuments[allDocuments.FindIndex(x => x.TableName == tableName)].Id.Add(row[0].ToString());
                else
                    allDocuments.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { row[0].ToString() }, TableName = tableName });
                WorkWithBD();
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var index = wrapPanelsList.FindIndex(x => x.Children.Contains((sender as Button)));
            var id = (wrapPanelsList[index].Children[0] as Label).Content.ToString().Replace("id документа: ", "");
            var table = (wrapPanelsList[index].Children[1] as Label).Content.ToString().Replace("Название таблицы: ", "");
            allDocuments[allDocuments.FindIndex(x => x.TableName == table)].Id.Remove(id);
            SelectedData.Children.Remove(wrapPanelsList[index]);
            wrapPanelsList.RemoveAt(index);
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
                        var comboBox = new ComboBox() { ItemsSource = items };
                        comboBox.SelectionChanged += ComboBox_ChangedInFilters;
                        dataForFillFilters.Add(new DataBaseWindow.TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyComboBox = comboBox });
                        DataPanel.Children.Add(comboBox);
                    }
                    else
                    {
                        var textBox = new TextBox();
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
            if (textBoxData.Text != "" && wrapPanelsList.Count != 0)
            {
                var list = new List<string>();
                list.Add(textBoxData.Text);
                for (var i = 0; i < wrapPanelsList.Count; i++)
                {
                    var listOfLabel = wrapPanelsList[i].Children.OfType<Label>().ToList();
                    var id = listOfLabel[0].Content.ToString().Replace("id документа: ", "");
                    var table = listOfLabel[1].Content.ToString().Replace("Название таблицы: ", "");
                    list.Add(id);
                    list.Add(table);
                }
                if (nameOfWindow == "ElectronicDocumentCard")
                    ElectronicDocumentCard.EventOnCreateRelations.Value = list;
                else
                    MainWindow.EventOnCreateRelations.Value = list;
                Close();
            }
            else
                MessageBox.Show("Введите название связи и выберете документы");
        }
    }
}
