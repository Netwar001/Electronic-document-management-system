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
    public partial class ElectronicCaseCard : Window
    {
        public class DocumentsData
        {
            public List<string> Id { get; set; }
            public string TableName { get; set; }
        }
        public static class EventOnAddDocumentsInCase
        {
            public static EventHandler DocumentsAdded = delegate { };
            private static List<DocumentsData> _list;

            public static List<DocumentsData> Value
            {
                get { return _list; }
                set
                {
                    _list = value;
                    DocumentsAdded(null, EventArgs.Empty);
                }
            }
        }
        public static class EventOnMoveDocumentsInCase
        {
            public static EventHandler DocumentsMoved = delegate { };
            private static List<string> _list;

            public static List<string> Value
            {
                get { return _list; }
                set
                {
                    _list = value;
                    DocumentsMoved(null, EventArgs.Empty);
                }
            }
        }

        private List<MainWindow.FillForm> dataForFill;
        private DataTable mainTable;
        private DataTable documentsTable;
        private object[] selectedRow;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public ElectronicCaseCard(object[] row, string subdivision)
        {
            InitializeComponent();
            AddElementsOnPanel(row, subdivision);
            AddDocumentsOnPanel();
            EventOnAddDocumentsInCase.DocumentsAdded += DocumentsAdded;
            EventOnMoveDocumentsInCase.DocumentsMoved += DocumentsMoved;
        }

        public void DocumentsAdded(object sender, EventArgs e)
        {
            if (EventOnAddDocumentsInCase.Value != null)
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    var rowOfMainTable = mainTable.Rows[0].ItemArray;
                    var str = "insert into [Номенклатура дел] values ";
                    for (var i = 0; i < EventOnAddDocumentsInCase.Value.Count; i++)
                    {
                        foreach (var id in EventOnAddDocumentsInCase.Value[i].Id)
                        {
                            str += "('";
                            foreach (var element in rowOfMainTable)
                            {
                                str += element + "','";
                            }
                            str += id + "','" + EventOnAddDocumentsInCase.Value[i].TableName + "'),";
                        }
                    }
                    str = str.Remove(str.Length - 1, 1);
                    SqlCommand insert = connection.CreateCommand();
                    insert.Connection = connection;
                    insert.CommandText = str;
                    insert.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                MessageBox.Show("Документы успешно добавлены в дело");
                AddDocumentsOnPanel();
            }
        }

        public void DocumentsMoved(object sender, EventArgs e)
        {
            if (EventOnMoveDocumentsInCase.Value != null)
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    var rowOfMainTable = mainTable.Rows[0].ItemArray;
                    var str = new StringBuilder();
                    str.AppendFormat("update [Номенклатура дел] set [Индекс]='{0}',[Заголовок дела]='{1}',[Подразделение]='{2}',[Статус]='{3}',[Начато]='{4}',[Срок актуальности]='{5}',[Примечание]='{6}'",
                        EventOnMoveDocumentsInCase.Value[0], EventOnMoveDocumentsInCase.Value[1], EventOnMoveDocumentsInCase.Value[2], EventOnMoveDocumentsInCase.Value[3],
                        EventOnMoveDocumentsInCase.Value[4], EventOnMoveDocumentsInCase.Value[5], EventOnMoveDocumentsInCase.Value[6]);
                    str.AppendFormat("where [Индекс]='{0}' and [Заголовок дела]='{1}' and [Подразделение]='{2}' and [id документа]='{3}' and [Название таблицы]='{4}'",
                        rowOfMainTable[0], rowOfMainTable[1], rowOfMainTable[2], selectedRow[0], selectedRow[1]);
                    SqlCommand update = connection.CreateCommand();
                    update.Connection = connection;
                    update.CommandText = str.ToString();
                    update.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                MessageBox.Show("Документ успешно перемещен в другое дело");
                AddDocumentsOnPanel();
            }
        }

        public void AddElementsOnPanel(object[] row, string subdivision)
        {
            mainStackPanel.Children.Clear();
            dataForFill = new List<MainWindow.FillForm>();
            var subdivisions = new List<string>();

            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select [Индекс],[Заголовок дела],[Подразделение],[Статус],[Начато],[Срок актуальности],[Примечание] from [Номенклатура дел]" +
                    " where [Индекс] = '" + row[0] + "'" + " and [Заголовок дела] = '" + row[1] + "'" + " and [Подразделение] = '" + subdivision + "'", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);

                var getName = new SqlCommand("SELECT [Наименование] FROM [Подразделения]", connection);
                var reader = getName.ExecuteReader();
                while (reader.Read())
                {
                    subdivisions.Add(reader.GetString(0));
                }
                reader.Close();
                subdivisions.Sort();
            }
            finally
            {
                connection.Close();
            }

            if (mainTable.Rows[0].ItemArray[3].ToString() == "Закрыто")
            {
                SaveButton.Visibility = Visibility.Hidden;
                AddDocBtn.Visibility = Visibility.Hidden;
                MoveDocBtn.Visibility = Visibility.Hidden;
            }

            var rowOfMainTable = mainTable.Rows[0].ItemArray;
            for (var i = 0; i < mainTable.Columns.Count; i++)
            {
                mainStackPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                if (mainTable.Columns[i].ToString() == "Подразделение")
                {
                    var comboBox = new ComboBox() { ItemsSource = subdivisions, SelectedItem = rowOfMainTable[i].ToString() };
                    dataForFill.Add(new MainWindow.FillForm() { Column = mainTable.Columns[i].ToString(), EmptyComboBox = comboBox });
                    mainStackPanel.Children.Add(comboBox);
                }
                else if (mainTable.Columns[i].DataType != typeof(DateTime))
                {
                    var textBox = new TextBox() { Text = rowOfMainTable[i].ToString() };
                    if (mainTable.Columns[i].ToString() == "Статус")
                        textBox.IsEnabled = false;
                    dataForFill.Add(new MainWindow.FillForm() { Column = mainTable.Columns[i].ToString(), EmptyTextBox = textBox });
                    mainStackPanel.Children.Add(textBox);
                }
                else
                {
                    var datePicker = new DatePicker() { Text = rowOfMainTable[i].ToString() };
                    if (mainTable.Columns[i].ToString() == "Начато")
                        datePicker.IsEnabled = false;
                    dataForFill.Add(new MainWindow.FillForm() { Column = mainTable.Columns[i].ToString(), EmptyDatePicker = datePicker });
                    mainStackPanel.Children.Add(datePicker);
                }
            }
        }

        public void AddDocumentsOnPanel()
        {
            documentsTable = new DataTable();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var rowOfMainTable = mainTable.Rows[0].ItemArray;
                var commandForAdapter = new SqlCommand("select [id документа],[Название таблицы] from [Номенклатура дел] where [Индекс] = '" +
                    rowOfMainTable[0] + "' and [Заголовок дела] = '" + rowOfMainTable[1] + "' and [Подразделение] = '" + rowOfMainTable[2] + "'", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                var tempTable = new DataTable();
                adapter.Fill(tempTable);

                var listWithDoc = new List<DocumentsData>();
                foreach (DataRow tableRow in tempTable.Rows)
                {
                    if (listWithDoc.Find(x => x.TableName == tableRow[1].ToString()) == null)
                        listWithDoc.Add(new DocumentsData() { Id = new List<string>() { tableRow[0].ToString() }, TableName = tableRow[1].ToString() });
                    else
                        listWithDoc[listWithDoc.FindIndex(x => x.TableName == tableRow[1].ToString())].Id.Add(tableRow[0].ToString());
                }
                for (var i = 0; i < listWithDoc.Count; i++)
                {
                    var strForCmd = "select [id],'" + listWithDoc[i].TableName + "' as 'Название таблицы',[Дата документа],[Тема] from [" +
                        listWithDoc[i].TableName + "] where [id] in (";
                    foreach(var id in listWithDoc[i].Id)
                    {
                        strForCmd += id + ",";
                    }
                    strForCmd = strForCmd.Remove(strForCmd.Length - 1, 1);
                    var command = new SqlCommand( strForCmd + ")", connection);
                    var newAdapter = new SqlDataAdapter(command);
                    var tableForFillDataGrid = new DataTable();
                    newAdapter.Fill(tableForFillDataGrid);
                    if (documentsTable.Rows.Count == 0)
                        documentsTable = tableForFillDataGrid;
                    else
                    {
                        foreach (DataRow row in tableForFillDataGrid.Rows)
                            documentsTable.Rows.Add(row.ItemArray);
                    }
                }
            }
            finally
            {
                connection.Close();
            }
            documentsDataGrid.ItemsSource = documentsTable.DefaultView;
        }

        private void documentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((DataRowView)documentsDataGrid.SelectedItem != null)
                selectedRow = ((DataRowView)documentsDataGrid.SelectedItem).Row.ItemArray;
        }

        private void documentsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "dd.MM.yyyy";
        }

        private void AddDocBtn_Click(object sender, RoutedEventArgs e)
        {
            var listWithDoc = new List<DocumentsData>();
            foreach (DataRow tableRow in documentsTable.Rows)
            {
                if (listWithDoc.Find(x => x.TableName == tableRow[1].ToString()) == null)
                    listWithDoc.Add(new DocumentsData() { Id = new List<string>() { tableRow[0].ToString() }, TableName = tableRow[1].ToString() });
                else
                    listWithDoc[listWithDoc.FindIndex(x => x.TableName == tableRow[1].ToString())].Id.Add(tableRow[0].ToString());
            }

            var allDocList = new List<DocumentsData>();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select [id документа],[Название таблицы] from [Номенклатура дел]", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                var tempTable = new DataTable();
                adapter.Fill(tempTable);
                foreach (DataRow tableRow in tempTable.Rows)
                {
                    if (allDocList.Find(x => x.TableName == tableRow[1].ToString()) == null)
                        allDocList.Add(new DocumentsData() { Id = new List<string>() { tableRow[0].ToString() }, TableName = tableRow[1].ToString() });
                    else
                        allDocList[allDocList.FindIndex(x => x.TableName == tableRow[1].ToString())].Id.Add(tableRow[0].ToString());
                }
            }
            finally
            {
                connection.Close();
            }

            EventOnAddDocumentsInCase.Value = null;
            new AddDocumentsInCaseWindow(listWithDoc, mainTable.Rows[0].ItemArray[1].ToString(), allDocList, "ElectronicCaseCard").Show();
        }

        private void MoveDocBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRow != null)
            {
                new WorkWithNomenclatureWindow("Подтвердить выбор").Show();
            }
            else
                MessageBox.Show("Выберете документ из списка");
        }

        private void OpenInfoAboutDocBtn_Click(object sender, RoutedEventArgs e)
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var rowOfMainTable = mainTable.Rows[0].ItemArray;
            var updateStr = "update [Номенклатура дел] set ";
            for (var i = 0; i < mainTable.Columns.Count; i++)
            {
                updateStr += "[" + mainTable.Columns[i].ToString() + "]='";
                if (dataForFill[i].EmptyTextBox != null)
                    updateStr += dataForFill[i].EmptyTextBox.Text + "'";
                else if (dataForFill[i].EmptyDatePicker != null)
                    updateStr += dataForFill[i].EmptyDatePicker.Text + "'";
                else
                    updateStr += dataForFill[i].EmptyComboBox.Text + "'";
                if (i != mainTable.Columns.Count - 1)
                    updateStr += ",";
            }
            updateStr += " where [Индекс]='" + rowOfMainTable[0] + "' and [Заголовок дела]='" + rowOfMainTable[1] +
                "' and [Подразделение]='" + rowOfMainTable[2] + "'";
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                SqlCommand command = connection.CreateCommand();
                command.Connection = connection;
                command.CommandText = updateStr;
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
            WorkWithNomenclatureWindow.EventOnAddCase.Value = dataForFill[2].EmptyComboBox.Text;
            var arr = new object[] { dataForFill[0].EmptyTextBox.Text, dataForFill[1].EmptyTextBox.Text };
            new ElectronicCaseCard(arr, dataForFill[2].EmptyComboBox.Text).Show();
            Close();
            MessageBox.Show("Информация о деле успешно обновлена");
        }
    }
}
