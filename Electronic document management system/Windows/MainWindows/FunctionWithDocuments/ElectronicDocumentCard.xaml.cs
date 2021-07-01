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
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace Electronic_document_management_system
{
    public partial class ElectronicDocumentCard : Window
    {
        public static class EventOnCreateRelations
        {
            public static EventHandler RelationCreated = delegate { };
            private static List<string> _list;
            public static string option;

            public static List<string> Value
            {
                get { return _list; }
                set
                {
                    _list = value;
                    RelationCreated(null, EventArgs.Empty);
                }
            }
        }

        public class DocumentsRelations
        {
            public string RelationsName { get; set; }
            public string MainId { get; set; }
            public string MainTableName { get; set; }
            public string SecondId { get; set; }
            public string SecondTableName { get; set; }
            public WrapPanel Panels { get; set; }
        }

        public class BtnData
        {
            public string BtnName { get; set; }
            public Button Btn { get; set; }
        }

        private List<MainWindow.FillForm> dataForFill;
        private List<DocumentsRelations> relationsData;
        private List<BtnData> btnData;
        private List<string> headersList;
        private List<object> rowList;
        private List<int> versions;
        private List<string> nameOfFile;
        private ComboBox comboBoxWithVersions;
        private ComboBox comboBoxForRecoveryDoc;
        private string tableName;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public ElectronicDocumentCard(List<string> headers, object[] row, string table)
        {
            InitializeComponent();
            tableName = table;
            rowList = new List<object>();
            if (tableName == "Архивные документы")
            {
                RelationsTabItem.Visibility = Visibility.Hidden;
                ChangeDocButton.Content = "Вернуть в документооборот";
                var tempList = row.ToList();
                tempList.RemoveRange(row.Length - 2, 2);
                row = tempList.ToArray();
            }

            foreach (var col in row)
            {
                if (col.GetType() == typeof(DateTime))
                {
                    if (col.ToString() == "01.01.1900 0:00:00")
                        rowList.Add("");
                    else rowList.Add((Convert.ToDateTime(col)).ToString("dd.MM.yyyy"));
                }
                else rowList.Add(col);
            }
            headersList = headers;

            if (!User.ChangeInfoAboutFile || tableName == "Архивные документы")
                SaveButton.Visibility = Visibility.Hidden;
            if (!User.ChangeFileInArchive)
                ChangeDocButton.Visibility = Visibility.Hidden;

            AddElementsOnPanel();
            AddElementsOnRelationsPanel();

            EventOnCreateRelations.RelationCreated += EventOnCreateRelation;
        }

        //Добавление элементов на форму
        public void AddElementsOnPanel()
        {
            mainStackPanel.Children.Clear();
            dataForFill = new List<MainWindow.FillForm>();
            var optionsFields = new List<string>();

            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                if (tableName != "Архивные документы")
                {
                    var commandForEmployeeFields = new SqlCommand("select [Поля с вариантом выбора] from [Информация о таблицах] where [Название таблицы] = '" + tableName + "'", connection);
                    var readFields = commandForEmployeeFields.ExecuteReader();
                    while (readFields.Read())
                    {
                        optionsFields = readFields.GetString(0).Split(';').ToList();
                    }
                    readFields.Close();
                }

                SqlCommand getVersions = new SqlCommand("select [Версия], [FileName] from [Документы] where [id документа] = '" + rowList[0].ToString() +
                    "' and [Название таблицы] = '" + tableName + "'", connection);
                var reader = getVersions.ExecuteReader();
                versions = new List<int>();
                nameOfFile = new List<string>();
                while (reader.Read())
                {
                    versions.Add(reader.GetInt32(0));
                    nameOfFile.Add(reader.GetString(1));
                }
                reader.Close();
                if (tableName != "Архивные документы")
                {
                    versions.Sort();
                    comboBoxWithVersions = new ComboBox() { ItemsSource = versions, SelectedItem = versions.Last() };
                    var wrapPanel = new WrapPanel();
                    wrapPanel.Children.Add(new Label() { Content = "Версия документа: " });
                    wrapPanel.Children.Add(comboBoxWithVersions);
                    mainStackPanel.Children.Add(wrapPanel);
                }
            }
            finally
            {
                connection.Close();
            }

            for (var i = 0; i < rowList.Count; i++)
            {
                mainStackPanel.Children.Add(new Label() { Content = headersList[i] });
                if (optionsFields.Any(l => l.Contains(headersList[i])))
                {
                    var strWithItems = optionsFields[optionsFields.FindIndex(x => x.Contains(headersList[i]))].Split(':')[1];
                    var items = strWithItems.Split(',').Select(t => t.Trim()).ToList();
                    var comboBox = new ComboBox() { ItemsSource = items, Name = "column" + i, SelectedItem = rowList[i].ToString() };
                    dataForFill.Add(new MainWindow.FillForm() { Column = headersList[i], EmptyComboBox = comboBox });
                    mainStackPanel.Children.Add(comboBox);
                }
                else
                {
                    var textBox = new TextBox() { Name = "column" + i, Text = rowList[i].ToString() };
                    if (i == 0)
                        textBox.IsReadOnly = true;
                    dataForFill.Add(new MainWindow.FillForm() { Column = headersList[i], EmptyTextBox = textBox });
                    mainStackPanel.Children.Add(textBox);
                }
            }
        }

        //Добавление элементов на форму связей
        public void AddElementsOnRelationsPanel()
        {
            RelationsStackPanel.Children.Clear();
            relationsData = new List<DocumentsRelations>();
            btnData = new List<BtnData>();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                SqlCommand getRelations = new SqlCommand("SELECT * FROM [Связи документов] where [id связываемого документа] = '" + rowList[0] + 
                    "' and [Название таблицы связываемого документа] = '" + tableName + "' or [id документа] = '" + rowList[0] + 
                    "' and [Название таблицы] = '" + tableName + "'", connection);
                var reader = getRelations.ExecuteReader();
                while (reader.Read())
                {
                    relationsData.Add(new DocumentsRelations()
                    {
                        MainId = reader.GetValue(0).ToString(),
                        MainTableName = reader.GetValue(1).ToString(),
                        RelationsName = reader.GetValue(2).ToString(),
                        SecondId = reader.GetValue(3).ToString(),
                        SecondTableName = reader.GetValue(4).ToString()
                    });
                }
                reader.Close();
                relationsData.Sort((x, y) => x.RelationsName.CompareTo(y.RelationsName));
            }
            finally
            {
                connection.Close();
            }

            var classWithMethods = new Methods.ClassWithMethods();
            var topicDataList = classWithMethods.GetDocumentsInfo();

            var previousRelationName = "";
            for (var i = 0; i < relationsData.Count; i++)
            {
                if (previousRelationName != relationsData[i].RelationsName)
                {
                    var relationsNameWrapPenel = new WrapPanel();
                    relationsNameWrapPenel.Children.Add(new Label() { Content = "Название связи: " + relationsData[i].RelationsName, Margin = new Thickness(0, 10, 0, 0) });
                    var addInExistRelations = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Add-icon.png", UriKind.Relative));
                    btnData.Add(new BtnData() { BtnName = relationsData[i].RelationsName, Btn = addInExistRelations });
                    addInExistRelations.Click += AddInExistRelations_Click;
                    relationsNameWrapPenel.Children.Add(addInExistRelations);
                    RelationsStackPanel.Children.Add(relationsNameWrapPenel);
                }

                var wrapPanel = new WrapPanel() { Margin = new Thickness(0, 5, 0, 10) };
                var activeBtn = new Button() { Background = Brushes.Gray, BorderThickness = new Thickness(0), Height = 50 };
                activeBtn.Click += OpenInfoAboutDoc_Click;
                if (relationsData[i].MainId == rowList[0].ToString() && relationsData[i].MainTableName == tableName)
                {
                    var topic = topicDataList.Where(x => (x.Id == Convert.ToInt32(relationsData[i].SecondId)) && (x.TableName == relationsData[i].SecondTableName)).Select(x => x.Topic).FirstOrDefault();
                    activeBtn.Content = "id: " + relationsData[i].SecondId + ", Таблица: " + relationsData[i].SecondTableName + ",\nТема: " + topic;
                    var deleteBtn = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Del-icon.png", UriKind.Relative));
                    deleteBtn.Click += DeleteBtn_Click;
                    wrapPanel.Children.Add(deleteBtn);
                }
                else
                {
                    var topic = topicDataList.Where(x => (x.Id == Convert.ToInt32(relationsData[i].MainId)) && (x.TableName == relationsData[i].MainTableName)).Select(x => x.Topic).FirstOrDefault();
                    activeBtn.Content = "id: " + relationsData[i].MainId + ", Таблица: " + relationsData[i].MainTableName + ",\nТема: " + topic;
                }
                wrapPanel.Children.Insert(0, activeBtn);
                RelationsStackPanel.Children.Add(wrapPanel);
                relationsData[i].Panels = wrapPanel;

                previousRelationName = relationsData[i].RelationsName;
            }
            var newBtn = new Button() { Content = "Создать новую связь", Margin = new Thickness(0,10,0,10) };
            newBtn.Click += AddNewRelation_Click;
            RelationsStackPanel.Children.Add(newBtn);
        }

        private void OpenInfoAboutDoc_Click(object sender, RoutedEventArgs e)
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
                var id = (sender as Button).Content.ToString().Replace("id: ", "").Split(',')[0];
                var tableName = (sender as Button).Content.ToString().Replace(" Таблица: ", "").Split(',')[1];
                var dataTable = new DataTable();
                var command = new SqlCommand("select * from [" + tableName + "] where [Уровень доступа] in (" + accessLevel + ") and " + 
                    "id = '" + id + "'", connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count != 0)
                {
                    var headers = new List<string>();
                    foreach (var colName in dataTable.Columns)
                        headers.Add(colName.ToString());

                    var row = dataTable.Rows[0].ItemArray;

                    new ElectronicDocumentCard(headers, row, tableName).Show();
                }
                else
                    MessageBox.Show("Ваш уровень доступа не соответствует требуемому");
            }
            finally
            {
                connection.Close();
            }
        }

        private void AddNewRelation_Click(object sender, RoutedEventArgs e)
        {
            EventOnCreateRelations.Value = null;
            EventOnCreateRelations.option = null;
            new AddRelationsWindow(EventOnCreateRelations.Value, Convert.ToInt32(rowList[0]), tableName, "ElectronicDocumentCard").Show();
        }

        private void AddInExistRelations_Click(object sender, RoutedEventArgs e)
        {
            EventOnCreateRelations.Value = null;
            EventOnCreateRelations.option = null;
            var relationName = btnData.Find(btn => btn.Btn == (sender as Button)).BtnName;
            var list = new List<string>() { relationName };
            foreach (var row in relationsData.FindAll(row => row.RelationsName == relationName))
            {
                if (row.MainId == rowList[0].ToString() && row.MainTableName == tableName)
                {
                    list.Add(row.SecondId);
                    list.Add(row.SecondTableName);
                }
            }
            new AddRelationsWindow(list, Convert.ToInt32(rowList[0]), tableName, "ElectronicDocumentCard").Show();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var index = relationsData.FindIndex(x => x.Panels.Children.Contains((sender as Button)));
            RelationsStackPanel.Children.Remove(relationsData[index].Panels);

            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                SqlCommand delete = connection.CreateCommand();
                delete.Connection = connection;
                delete.CommandText = "delete FROM [Связи документов] where [id документа] = '" + relationsData[index].MainId +
                    "' and [Название таблицы] = '" + relationsData[index].MainTableName + "' and [Название связи] = '" + relationsData[index].RelationsName + 
                    "' and [id связываемого документа] = '" + relationsData[index].SecondId +
                    "' and [Название таблицы связываемого документа] = '" + relationsData[index].SecondTableName + "'";
                delete.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }

            relationsData.RemoveAt(index);
            AddElementsOnRelationsPanel();
        }

        //событие при создании связи документов
        public void EventOnCreateRelation(object sender, EventArgs e)
        {
            if (EventOnCreateRelations.Value != null)
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    if (EventOnCreateRelations.option == "exist")
                    {
                        SqlCommand delete = connection.CreateCommand();
                        delete.Connection = connection;
                        delete.CommandText = "delete FROM [Связи документов] where [id документа] = '" + rowList[0] +
                            "' and [Название таблицы] = '" + tableName + "' and [Название связи] = '" + EventOnCreateRelations.Value[0] + "'";
                        delete.ExecuteNonQuery();
                    }
                    SqlCommand addRelations = connection.CreateCommand();
                    addRelations.Connection = connection;
                    var strForInsert = "insert into [Связи документов] values ";
                    for (var i = 1; i < EventOnCreateRelations.Value.Count; i += 2)
                    {
                        strForInsert += "('" + rowList[0] + "','" + tableName + "','" + EventOnCreateRelations.Value[0] +
                            "','" + EventOnCreateRelations.Value[i] + "','" + EventOnCreateRelations.Value[i + 1] + "'), ";
                    }
                    strForInsert = strForInsert.Remove(strForInsert.Length - 2, 2);
                    addRelations.CommandText = strForInsert;
                    addRelations.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                AddElementsOnRelationsPanel();
            }
        }

        //скачивание и открытие файла
        private void OpenDocButton_Click(object sender, RoutedEventArgs e)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            string filename = "";
            if (tableName != "Архивные документы")
                filename = "(ver " + comboBoxWithVersions.SelectedItem.ToString() + ") ";
            try
            {
                var str = "";
                if (tableName != "Архивные документы")
                    str = "select * from [Документы] where [id документа] = '" + dataForFill[0].EmptyTextBox.Text + "' and [Название таблицы] = '" +
                    tableName + "' and [Версия] = '" + comboBoxWithVersions.SelectedItem.ToString() + "'";
                else
                    str = "select [FileName],[DocData] from [Архивные документы] where [id] = '" + dataForFill[0].EmptyTextBox.Text + "'";
                SqlCommand getDoc = new SqlCommand(str, connection);
                var reader = getDoc.ExecuteReader();
                byte[] data = new byte[0];
                while (reader.Read())
                {
                    if (tableName != "Архивные документы")
                    {
                        filename += reader.GetString(4);
                        data = (byte[])reader.GetValue(5);
                    }
                    else
                    {
                        filename += reader.GetString(0);
                        data = (byte[])reader.GetValue(1);
                    }
                }
                if (data.Length > 0)
                {
                    Directory.CreateDirectory("C:\\EDMS_App\\Downloaded_Documents");
                    using (FileStream fs = new FileStream("C:\\EDMS_App\\Downloaded_Documents\\" + filename, FileMode.OpenOrCreate))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }
            }
            finally
            {
                connection.Close();
            }
            Process.Start("C:\\EDMS_App\\Downloaded_Documents\\" + filename);
        }

        //обновление строки в базе в соответствии с введенными изменениями
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var updateStr = "update [" + tableName + "] set ";
            for (var i = 1; i < rowList.Count; i++)
            {
                if (dataForFill[i].EmptyTextBox != null)
                    updateStr += "[" + headersList[i] + "]='" + dataForFill[i].EmptyTextBox.Text + "'";
                else
                    updateStr += "[" + headersList[i] + "]='" + dataForFill[i].EmptyComboBox.Text + "'";
                if (i != rowList.Count - 1)
                    updateStr += ",";
                else updateStr += " where id='" + dataForFill[0].EmptyTextBox.Text + "'";
            }

            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                SqlCommand update = connection.CreateCommand();
                update.Connection = connection;
                update.CommandText = updateStr;
                update.ExecuteNonQuery();

                var dataTable = new DataTable();
                var command = new SqlCommand("select * from [" + tableName + "] where id = '" + dataForFill[0].EmptyTextBox.Text + "'", connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(dataTable);

                DataGridUpdater.Value = updateStr;
                var headers = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    headers.Add(column.ColumnName);
                }
                new ElectronicDocumentCard(headers, dataTable.Rows[0].ItemArray, tableName).Show();
            }
            finally
            {
                connection.Close();
            }
            Close();
            MessageBox.Show("Карточка документа сохранена");
        }

        //обновление документа в базе
        private void ChangeDocButton_Click(object sender, RoutedEventArgs e)
        {
            if (tableName != "Архивные документы")
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.InitialDirectory = "C:\\EDMS_App\\Downloaded_Documents";
                openFileDialog.Filter = "Word Documents|*.doc*|Text files|*.txt|All files|*.*";
                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = openFileDialog.FileName;
                    byte[] docData;
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        docData = new byte[fs.Length];
                        fs.Read(docData, 0, docData.Length);
                    }

                    var connection = new SqlConnection(connectionString);
                    connection.Open();
                    try
                    {
                        SqlCommand addDoc = connection.CreateCommand();
                        addDoc.Connection = connection;
                        addDoc.CommandText = "insert into [Документы] values ('" + dataForFill[0].EmptyTextBox.Text + "','" + tableName + "','" +
                            (versions.Last() + 1) + "','" + nameOfFile[0] + "',@DocData)";
                        addDoc.Parameters.Add("@DocData", SqlDbType.Image, 1000000);
                        addDoc.Parameters["@DocData"].Value = docData;
                        addDoc.ExecuteNonQuery();
                    }
                    finally
                    {
                        connection.Close();
                    }
                    AddElementsOnPanel();
                    MessageBox.Show("Документ успешно обновлен");
                }
            }
            else
            {
                if (comboBoxForRecoveryDoc == null)
                {
                    mainStackPanel.Children.Add(new Label() { Content = "Выберете таблицу для восстановления документа", FontWeight = FontWeights.Bold });
                    var connection = new SqlConnection(connectionString);
                    var tables = System.Configuration.ConfigurationManager.AppSettings["mainTables"].ToString();
                    connection.Open();
                    try
                    {
                        var getTableName = new SqlCommand("SELECT name FROM sys.objects WHERE type in (N'U') and name not in ('Архивные документы'," + tables + ")", connection);
                        var readTableNames = getTableName.ExecuteReader();
                        var tableNames = new List<string>();
                        while (readTableNames.Read())
                            tableNames.Add(readTableNames.GetString(0));
                        readTableNames.Close();
                        var comboBox = new ComboBox() { ItemsSource = tableNames };
                        comboBoxForRecoveryDoc = comboBox;
                        mainStackPanel.Children.Add(comboBox);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                else if (comboBoxForRecoveryDoc.SelectedItem != null)
                {
                    var connection = new SqlConnection(connectionString);
                    connection.Open();
                    try
                    {
                        SqlCommand getLastId = new SqlCommand("Select top (1) [id] from [" + comboBoxForRecoveryDoc.Text + "] order by [id] desc", connection);
                        var readLastId = getLastId.ExecuteReader();
                        var newId = "";
                        while (readLastId.Read())
                            newId += readLastId.GetInt32(0) + 1;
                        if (newId.Length == 0)
                            newId += 1;
                        readLastId.Close();

                        SqlCommand addInfoAboutDoc = connection.CreateCommand();
                        addInfoAboutDoc.Connection = connection;
                        var str = "insert into [" + comboBoxForRecoveryDoc.Text + "] ([id],[Область действия],[Уровень доступа],[Тип документа],[Дата документа],[Тема],[Исполнители],[Адресаты]) values ('" + newId + "','";
                        for (var i = 1; i < dataForFill.Count; i++)
                        {
                            if (dataForFill[i].EmptyTextBox != null)
                                str += dataForFill[i].EmptyTextBox.Text;
                            else if (dataForFill[i].EmptyComboBox != null)
                                str += dataForFill[i].EmptyComboBox.Text;
                            else
                                str += dataForFill[i].EmptyDatePicker.Text;
                            if (i != dataForFill.Count - 1)
                                str += "','";
                            else str += "')";
                        }
                        addInfoAboutDoc.CommandText = str;
                        addInfoAboutDoc.ExecuteNonQuery();

                        
                        SqlCommand addDoc = connection.CreateCommand();
                        addDoc.Connection = connection;
                        addDoc.CommandText = "insert into [Документы] select '" + newId + "','" + comboBoxForRecoveryDoc.Text + 
                            "','1',[FileName],[DocData] from [Архивные документы] where id = " + dataForFill[0].EmptyTextBox.Text;
                        addDoc.ExecuteNonQuery();
                    }
                    finally
                    {
                        connection.Close();
                    }
                    DataGridUpdater.Value = comboBoxForRecoveryDoc.Text;
                    Close();
                    MessageBox.Show("Документ успешно возвращен в документооборот");
                }
                else
                    MessageBox.Show("Выберете таблицу для восстановления документа");
            }
        }
    }
}
