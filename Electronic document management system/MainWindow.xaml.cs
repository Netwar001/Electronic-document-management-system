using System;
using System.IO;
using System.IO.Packaging;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Office.Interop.Word;
using System.Data.SqlClient;
using System.Data;

namespace Electronic_document_management_system
{
    static class Data
    {
        public static EventHandler ValueChanged = delegate { };
        private static string _text;
        public static string btnName;

        public static string Value
        {
            get { return _text; }
            set
            {
                _text = value;
                ValueChanged(null, EventArgs.Empty);
            }
        }
    }

    static class EventOnCreateTable
    {
        public static EventHandler TableCreated = delegate { };
        private static string _text;

        public static string Value
        {
            get { return _text; }
            set
            {
                _text = value;
                TableCreated(null, EventArgs.Empty);
            }
        }
    }

    public partial class MainWindow : System.Windows.Window
    {
        public static class EventOnCreateRelations
        {
            public static EventHandler RelationCreated = delegate { };
            private static List<string> _list;

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

        public class FillForm
        {
            public string Column { get; set; }
            public TextBox EmptyTextBox { get; set; }
            public DatePicker EmptyDatePicker { get; set; }
            public ComboBox EmptyComboBox { get; set; }
        }

        private string tableName;
        private string pathToFile;
        private List<FillForm> dataForFill;
        private DataTable mainTable;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public MainWindow()
        {
            InitializeComponent();
            GetTableNames();
            Data.ValueChanged += Data_ValueChanged;
            EventOnCreateTable.TableCreated += EventOnCreateTable_Created;
            EventOnCreateRelations.RelationCreated += EventOnCreateRelation;

            if (!User.CreateNewTable)
                CreateButton.Visibility = Visibility.Hidden;
        }

        public void GetTableNames()
        {
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
                TableNameComboBox.ItemsSource = tableNames;
                if (TableNameComboBox.SelectedItem == null)
                    TableNameComboBox.SelectedItem = "Карточка электронного документа";
                else if (EventOnCreateTable.Value != null)
                    TableNameComboBox.SelectedItem = EventOnCreateTable.Value;
                tableName = TableNameComboBox.SelectedItem.ToString();
            }
            finally
            {
                connection.Close();
            }
        }

        //работа с загруженным документом 
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Word Documents|*.doc*|Text files|*.txt|All files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;
                pathToFile = fileName;
                if (System.IO.Path.GetExtension(fileName).Contains("doc"))
                {
                    Directory.CreateDirectory("C:\\EDMS_App\\Temp");
                    string newXPSDocumentName = String.Concat("C:\\EDMS_App\\Temp\\", System.IO.Path.GetFileNameWithoutExtension(fileName), ".xps");
                    ConvertWordDocToXPSDoc(fileName, newXPSDocumentName);
                }
            }
        }

        //конвертация doc документа в xps для вывода в окне
        private void ConvertWordDocToXPSDoc(string wordDocName, string xpsDocName)
        {
            var wordApplication = new Microsoft.Office.Interop.Word.Application();
            wordApplication.Documents.Add(wordDocName);
            try
            {
                wordApplication.ActiveDocument.SaveAs(xpsDocName, WdSaveFormat.wdFormatXPS);
                wordApplication.Quit(WdSaveOptions.wdDoNotSaveChanges);
                XpsDocument xpsDoc = new XpsDocument(xpsDocName, FileAccess.Read);
                docViewer.Document = xpsDoc.GetFixedDocumentSequence();
                xpsDoc.Close();
            }

            catch (Exception exp)
            {
                string str = exp.Message;
            }
        }

        //добавление информации о сотрудниках
        private void AddInfo_Click(object sender, RoutedEventArgs e)
        {
            Data.btnName = (sender as Button).Name;
            new WindowForAddInfo().Show();
        }

        //выбор поля для записи введенных данных
        public void Data_ValueChanged(object sender, EventArgs e)
        {
            var btnIndex = Convert.ToInt32(Data.btnName.Substring(3)) - 1;
            var box = dataForFill[btnIndex].EmptyTextBox;
            if (box.Text.Length != 0)
                box.Text += "\n";
            box.Text += Data.Value;
        }

        //сохранение введенной информации в БД
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (pathToFile.Length != 0)
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    SqlCommand getLastId = new SqlCommand("Select top (1) [id] from [" + tableName + "] order by [id] desc", connection);
                    var readLastId = getLastId.ExecuteReader();
                    var newId = "";
                    while (readLastId.Read())
                        newId += readLastId.GetInt32(0) + 1;
                    if (newId.Length == 0)
                        newId += 1;
                    readLastId.Close();

                    SqlCommand addInfoAboutDoc = connection.CreateCommand();
                    addInfoAboutDoc.Connection = connection;
                    var str = "insert into [" + tableName + "] values ('" + newId + "','";
                    for (var i = 0; i < mainTable.Columns.Count - 1; i++)
                    {
                        if (dataForFill[i].EmptyTextBox != null)
                            str += dataForFill[i].EmptyTextBox.Text;
                        else if (dataForFill[i].EmptyComboBox != null)
                            str += dataForFill[i].EmptyComboBox.Text;
                        else
                            str += dataForFill[i].EmptyDatePicker.Text;
                        if (i != mainTable.Columns.Count - 2)
                            str += "','";
                        else str += "')";
                    }
                    addInfoAboutDoc.CommandText = str;
                    addInfoAboutDoc.ExecuteNonQuery();

                    var filename = pathToFile.Substring(pathToFile.LastIndexOf('\\') + 1);
                    byte[] docData;
                    using (FileStream fs = new FileStream(pathToFile, FileMode.Open))
                    {
                        docData = new byte[fs.Length];
                        fs.Read(docData, 0, docData.Length);
                    }
                    SqlCommand addDoc = connection.CreateCommand();
                    addDoc.Connection = connection;
                    addDoc.CommandText = "insert into [Документы] values ('" + newId + "','" + tableName + "','1','" + filename + "',@DocData)";
                    addDoc.Parameters.Add("@DocData", SqlDbType.Image, 1000000);
                    addDoc.Parameters["@DocData"].Value = docData;
                    addDoc.ExecuteNonQuery();

                    if (EventOnCreateRelations.Value != null)
                    {
                        if (EventOnCreateRelations.Value.Count - 1 >= 1)
                        {
                            SqlCommand addRelations = connection.CreateCommand();
                            addRelations.Connection = connection;
                            var strForInsert = "insert into [Связи документов] values ";
                            for (var i = 1; i < EventOnCreateRelations.Value.Count; i += 2)
                            {
                                strForInsert += "('" + newId + "','" + tableName + "','" + EventOnCreateRelations.Value[0] +
                                    "','" + EventOnCreateRelations.Value[i] + "','" + EventOnCreateRelations.Value[i + 1] + "'), ";
                            }
                            strForInsert = strForInsert.Remove(strForInsert.Length - 2, 2);
                            addRelations.CommandText = strForInsert;
                            addRelations.ExecuteNonQuery();
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }
                MessageBox.Show("Файл успешно загружен в архив");
                Archive.Window.Show();
                DataGridUpdater.Value = pathToFile;
                Close();
            }
        }

        //заполнение формы полями таблицы
        private void TableNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tableName = TableNameComboBox.SelectedItem.ToString();
            var employeeFields = new List<string>();
            var optionsFields = new List<string>();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select * from [" + tableName + "]", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);

                var commandForEmployeeFields = new SqlCommand("select [Поля сотрудников], [Поля с вариантом выбора] from [Информация о таблицах] where [Название таблицы] = '" + tableName + "'", connection);
                var readFields = commandForEmployeeFields.ExecuteReader();
                while (readFields.Read())
                {
                    employeeFields = readFields.GetString(0).Split(',').ToList();
                    optionsFields = readFields.GetString(1).Split(';').ToList();
                }
                readFields.Close();
            }
            finally
            {
                connection.Close();
            }

            DataPanel.Children.Clear();
            dataForFill = new List<FillForm>();
            for (var i = 1; i < mainTable.Columns.Count; i++)
            {
                DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                if (mainTable.Columns[i].DataType != typeof(DateTime))
                {
                    if (employeeFields.Contains(mainTable.Columns[i].ToString()))
                    {
                        var wrapPanel = new WrapPanel();
                        var classWithMethods = new Methods.ClassWithMethods();
                        var btn = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Add-User-icon.png", UriKind.Relative));
                        btn.Name = "Btn" + i;
                        btn.Click += AddInfo_Click;
                        var textBox = new TextBox() { Width = DataPanel.Width - 40 };
                        dataForFill.Add(new FillForm() { Column = mainTable.Columns[i].ToString(), EmptyTextBox = textBox });
                        wrapPanel.Children.Add(textBox);
                        wrapPanel.Children.Add(btn);
                        DataPanel.Children.Add(wrapPanel);
                    }
                    else if (optionsFields.Any(l => l.Contains(mainTable.Columns[i].ToString())))
                    {
                        var strWithItems = optionsFields[optionsFields.FindIndex(x => x.Contains(mainTable.Columns[i].ToString()))].Split(':')[1];
                        var items = strWithItems.Split(',').Select(t => t.Trim()).ToList();
                        var comboBox = new ComboBox() { ItemsSource = items };
                        dataForFill.Add(new FillForm() { Column = mainTable.Columns[i].ToString(), EmptyComboBox = comboBox });
                        DataPanel.Children.Add(comboBox);
                    }
                    else
                    {
                        var textBox = new TextBox();
                        dataForFill.Add(new FillForm() { Column = mainTable.Columns[i].ToString(), EmptyTextBox = textBox });
                        DataPanel.Children.Add(textBox);
                    }
                }
                else
                {
                    var datePicker = new DatePicker();
                    if (mainTable.Columns[i].ColumnName == "Дата документа")
                        datePicker.SelectedDate = DateTime.Today;
                    else if (mainTable.Columns[i].ColumnName == "Срок актуальности")
                        datePicker.SelectedDate = DateTime.Today.AddYears(10);
                    dataForFill.Add(new FillForm() { Column = mainTable.Columns[i].ToString(), EmptyDatePicker = datePicker });
                    DataPanel.Children.Add(datePicker);
                }
            }
        }

        //выбор поля для записи введенных данных
        public void EventOnCreateTable_Created(object sender, EventArgs e)
        {
            GetTableNames();
        }

        //событие при создании связи документов
        public void EventOnCreateRelation(object sender, EventArgs e)
        {
            if (EventOnCreateRelations.Value != null)
                CountRelations.Content = "Кол-во связей: " + ((EventOnCreateRelations.Value.Count - 1) / 2);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            new CreateDocumentWindow().Show();
            Close();
        }

        private void Watch_Click(object sender, RoutedEventArgs e)
        {
            Archive.Window.Show();
            Close();
        }

        //создание новой таблицы
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            new CreateNewTableWindow().Show();
        }

        //Добавиление связи к документу
        private void AddRelationsBtn_Click(object sender, RoutedEventArgs e)
        {
            new AddRelationsWindow(EventOnCreateRelations.Value, 0, "", "MainWindow").Show();
        }
    }
}
