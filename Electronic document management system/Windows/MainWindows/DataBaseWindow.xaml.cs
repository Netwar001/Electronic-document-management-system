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
using System.Timers;
using System.Threading;
using WIA;
using System.IO;

namespace Electronic_document_management_system
{
    static class HeadersData
    {
        public static EventHandler ValueChanged = delegate { };
        private static List<string> _text;

        public static List<string> Value
        {
            get { return _text; }
            set
            {
                _text = value;
                ValueChanged(null, EventArgs.Empty);
            }
        }
    }

    static class DataGridUpdater
    {
        public static EventHandler ValueChanged = delegate { };
        private static string _text;

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

    public static class User
    {
        public static string Name { get; set; }
        public static string Position { get; set; }
        public static string Subdivision { get; set; }
        public static string Email { get; set; }
        public static string AccessLevel { get; set; }
        public static bool UploadFilesToArchive { get; set; }
        public static bool ChangeInfoAboutFile { get; set; }
        public static bool ChangeFileInArchive { get; set; }
        public static bool CreateNewTable { get; set; }
        public static bool LoadingNewTemplates { get; set; }
        public static bool CreateAccounts { get; set; }
        public static bool WorkWithPositions { get; set; }
        public static bool AddSubdivisions { get; set; }
        public static bool WorkWithNomenclature { get; set; }
    }

    public static class Archive
    {
        public static DataBaseWindow Window { get; set; }
    }

    public partial class DataBaseWindow : Window
    {
        public class TableFilters
        {
            public string Column { get; set; }
            public TextBox EmptyTextBox { get; set; }
            public DatePicker EmptyDatePicker { get; set; }
            public ComboBox EmptyComboBox { get; set; }
        }

        DataTable mainTable;
        string filterByText = "";
        List<string> headers;
        List<string> necessaryTableNames;
        List<TableFilters> dataForFillFilters;
        private static System.Timers.Timer timer;
        private static System.Timers.Timer timerForExpiredDoc;
        string tableName;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public DataBaseWindow()
        {
            InitializeComponent();
            tableName = "Карточка электронного документа";

            var timerForUserCapabilities = new System.Timers.Timer(100);
            timerForUserCapabilities.Elapsed += UserCapabilities;
            timerForUserCapabilities.AutoReset = false;
            timerForUserCapabilities.Enabled = true;

            timer = new System.Timers.Timer(20000);
            timer.Elapsed += UpdateDataGridByTimer;
            timer.AutoReset = true;
            timer.Enabled = true;

            HeadersData.ValueChanged += HeadersChanged;
            DataGridUpdater.ValueChanged += DataGridUpdate;

            timerForExpiredDoc = new System.Timers.Timer(86400000); //раз в день
            timerForExpiredDoc.Elapsed += ExpiredDocByTimer;
            timerForExpiredDoc.AutoReset = true;
            timerForExpiredDoc.Enabled = true;
        }

        //получение списка таблиц
        public void GetTableNames()
        {
            var connection = new SqlConnection(connectionString);
            var tables = System.Configuration.ConfigurationManager.AppSettings["mainTables"].ToString();
            connection.Open();
            try
            {
                var getTableName = new SqlCommand("SELECT name FROM sys.objects WHERE type in (N'U') and name not in (" + tables + ")", connection);
                var readTableNames = getTableName.ExecuteReader();
                necessaryTableNames = new List<string>();
                while (readTableNames.Read())
                    necessaryTableNames.Add(readTableNames.GetString(0));
                readTableNames.Close();
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    TableNameComboBox.ItemsSource = necessaryTableNames;
                    if (TableNameComboBox.SelectedItem == null)
                        TableNameComboBox.SelectedItem = "Карточка электронного документа";
                }));
            }
            finally
            {
                connection.Close();
            }
        }

        //обновление по таймеру
        private void UpdateDataGridByTimer(Object source, ElapsedEventArgs e)
        {
            GetTableNames();
            WorkWithBD();
            Combine_TextFilter();
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
                var command = new SqlCommand("select * from [" + tableName + "] where [Уровень доступа] in (" + accessLevel + ")", connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(mainTable);
                Dispatcher.BeginInvoke(new ThreadStart(delegate { MainDataGrid.ItemsSource = mainTable.DefaultView; }));
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
            if (HeadersData.Value == null)
            {
                if (MainDataGrid.Columns.Count > 5)
                    e.Column.Visibility = Visibility.Hidden;
            }
            else
            {
                if (HeadersData.Value.Contains(e.Column.Header.ToString()))
                    e.Column.Visibility = Visibility.Visible;
                else e.Column.Visibility = Visibility.Hidden;
            }
        }

        //фильтр по строковым полям
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            StringBuilder str = new StringBuilder();
            foreach (DataColumn column in mainTable.DefaultView.Table.Columns)
            {
                if (column.DataType == typeof(String))
                    str.AppendFormat("[{0}] Like '%{1}%' OR ", column.ColumnName, (sender as TextBox).Text);
            }
            str.Remove(str.Length - 3, 3);
            filterByText = str.ToString();
            Combine_TextFilter();
        }

        //объединение фильтра по дате и по текстовым полям
        public void Combine_TextFilter()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
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
                if (filterByText.Length != 0 && str.Length == 0)
                    dataView.RowFilter = filterByText;
                else if (filterByText.Length == 0 && str.Length != 0)
                    dataView.RowFilter = str.Remove(str.Length - 4, 4).ToString();
                else if (filterByText.Length != 0 && str.Length != 0)
                    dataView.RowFilter = "(" + filterByText + ") and (" + str.Remove(str.Length - 4, 4).ToString() + ")";
                else
                    dataView.RowFilter = "";
                MainDataGrid.ItemsSource = dataView;
                MainDataGrid.Items.Refresh();
            }));
        }

        //открытие карточки документа при 2 щелчке ЛКМ по строке
        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataRowView = (DataRowView)MainDataGrid.SelectedItem;
            var row = dataRowView.Row.ItemArray;
            new ElectronicDocumentCard(headers, row, tableName).Show();
        }

        //выбор столбцов для вывода в datagrid
        private void ChooseColumns_Click(object sender, RoutedEventArgs e)
        {
            var visibleHeaders = new List<string>();
            foreach (var col in MainDataGrid.Columns)
                if (col.Visibility == Visibility.Visible)
                    visibleHeaders.Add(col.Header.ToString());
            new ChooseColumnsWindow(headers, visibleHeaders).Show();
        }

        //отображение выбранных столбцов
        public void HeadersChanged(object sender, EventArgs e)
        {
            if (HeadersData.Value != null)
                for (var i = 0; i < MainDataGrid.Columns.Count; i++)
                {
                    if (HeadersData.Value.Contains(MainDataGrid.Columns[i].Header.ToString()))
                        MainDataGrid.Columns[i].Visibility = Visibility.Visible;
                    else MainDataGrid.Columns[i].Visibility = Visibility.Hidden;
                }
        }

        //обновление информации в dataGrid при изменении записи
        public void DataGridUpdate(object sender, EventArgs e)
        {
            WorkWithBD();
            Combine_TextFilter();
        }

        //выбор таблицы для отображения в datagrid
        private void TableNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tableName = TableNameComboBox.SelectedItem.ToString();
            filterByText = "";
            HeadersData.Value = null;
            WorkWithBD();
            InsertFilters();
            Combine_TextFilter();
            UpdateHeadersList();
        }

        //обновление данных о столбцах
        private void UpdateHeadersList()
        {
            headers = new List<string>();
            foreach (var colName in mainTable.Columns)
                headers.Add(colName.ToString());
        }

        //выбор отрисовывваемых функций меню
        private void UserCapabilities(Object source, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                var menuItems = new List<MenuItem>() { Load, LoadTemplate, CreateTable, WorkWithPosition, AddSubdivision, WatchNomenclature };
                var capabilitiesList = new List<bool>() { User.UploadFilesToArchive, User.LoadingNewTemplates, User.CreateNewTable, User.WorkWithPositions, User.AddSubdivisions, User.WorkWithNomenclature };
                if (!User.CreateAccounts)
                {
                    CreateUser.Visibility = Visibility.Hidden;
                    ChangeUser.Visibility = Visibility.Hidden;
                }
                for (var i = 0; i < menuItems.Count; i++)
                {
                    if (!capabilitiesList[i])
                    {
                        menuItems[i].Visibility = Visibility.Hidden;
                    }
                }
            }));
            GetTableNames();
            WorkWithBD();
            UpdateHeadersList();
            MoveExpiredDoc();
        }

        //перемещение устаревших документов в архивную таблицу
        private void MoveExpiredDoc()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var dataTable = new DataTable();
                var commandForSelect = new SqlCommand("SELECT distinct [Индекс],[Заголовок дела],[Подразделение] FROM [Номенклатура дел] where DATEDIFF(Day, [Срок актуальности], GetDate()) >= 1 and Статус='Открыто'", connection);
                var adapter = new SqlDataAdapter(commandForSelect);
                adapter.Fill(dataTable);
                if (dataTable.Rows.Count != 0)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var listWithDoc = new List<ElectronicCaseCard.DocumentsData>();
                        var commandForSelectDocs = new SqlCommand("SELECT [id документа],[Название таблицы] FROM [Номенклатура дел]" +
                            " where [Индекс] = '" + row[0] + "'" + " and [Заголовок дела] = '" + row[1] + "'" + " and [Подразделение] = '" + row[2] + "'", connection);
                        var reader = commandForSelectDocs.ExecuteReader();
                        while (reader.Read())
                        {
                            if (listWithDoc.Find(x => x.TableName == reader.GetString(1)) == null)
                                listWithDoc.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { reader.GetInt32(0).ToString() }, TableName = reader.GetString(1) });
                            else
                                listWithDoc[listWithDoc.FindIndex(x => x.TableName == reader.GetString(1))].Id.Add(reader.GetInt32(0).ToString());
                        }
                        reader.Close();

                        for (var i = 0; i < listWithDoc.Count; i++)
                        {
                            foreach (var id in listWithDoc[i].Id)
                            {
                                SqlCommand cmd = connection.CreateCommand();
                                cmd.Connection = connection;
                                cmd.CommandText = new StringBuilder().AppendFormat("insert into [Архивные документы] " +
                                "select [Область действия],[Уровень доступа],[Тип документа],[Дата документа],[Тема],[Исполнители],[Адресаты],[FileName],[DocData] " +
                                "from [{0}] join Документы on Документы.[id документа] = [{0}].id where [Название таблицы] = '{0}' and [id документа] = {1}" +
                                " and Версия = (select Max(Версия) from Документы where [Название таблицы] = '{0}' and [id документа] = {1})", listWithDoc[i].TableName, id).ToString();
                                cmd.ExecuteNonQuery();

                                cmd.CommandText = new StringBuilder().AppendFormat("update [Номенклатура дел] set [Статус]='Закрыто',[id документа]=(select Max(id) from [Архивные документы]),[Название таблицы]='Архивные документы'" +
                                    " where [Индекс]='{0}' and [Заголовок дела]='{1}' and [Подразделение]='{2}' and [id документа]={3} and [Название таблицы]='{4}'",
                                    row[0], row[1], row[2], id, listWithDoc[i].TableName).ToString();
                                cmd.ExecuteNonQuery();
                            }
                            WorkWithEpireDoc(listWithDoc[i].Id,
                            new StringBuilder().AppendFormat("delete from [{0}] where [id] in (", listWithDoc[i].TableName), connection);
                            WorkWithEpireDoc(listWithDoc[i].Id,
                                new StringBuilder().AppendFormat("delete from [Документы] where [Название таблицы] = '{0}' and [id документа] in (", listWithDoc[i].TableName), connection);
                            WorkWithEpireDoc(listWithDoc[i].Id,
                                new StringBuilder().AppendFormat("delete from [Связи документов] where [Название таблицы] = '{0}' and [id документа] in (", listWithDoc[i].TableName), connection);
                            WorkWithEpireDoc(listWithDoc[i].Id,
                                new StringBuilder().AppendFormat("delete from [Связи документов] where [Название таблицы связываемого документа] = '{0}' and [id связываемого документа] in (", listWithDoc[i].TableName), connection);
                        }
                    }
                    WorkWithBD();
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public void WorkWithEpireDoc(List<string> listWithId, StringBuilder command, SqlConnection connection)
        {
            foreach (var id in listWithId)
            {
                command.AppendFormat("{0},", id);
            }
            command.Remove(command.Length - 1, 1).Append(")");
            SqlCommand cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.CommandText = command.ToString();
            cmd.ExecuteNonQuery();
        }

        //вызов методля для перемещения устаревших документов в архивную таблицу по таймеру
        private void ExpiredDocByTimer(Object source, ElapsedEventArgs e)
        {
            MoveExpiredDoc();
        }

        //добавление фильтров по всем столбцам
        private void InsertFilters()
        {
            dataForFillFilters = new List<TableFilters>();
            DataPanel.Children.Clear();
            var optionsFields = new List<string>();
            if (tableName != "Архивные документы")
            {
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
                        dataForFillFilters.Add(new TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyComboBox = comboBox });
                        DataPanel.Children.Add(comboBox);
                    }
                    else
                    {
                        var textBox = new TextBox() { Name = "column" + i };
                        textBox.TextChanged += TextBox_ChangedInFilters;
                        dataForFillFilters.Add(new TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyTextBox = textBox });
                        DataPanel.Children.Add(textBox);
                    }
                }
                else
                {
                    var wrapPanel = new WrapPanel();
                    var datePicker = new DatePicker() { Width = 180 };
                    var items = new List<string>() { ">", "=", "<" };
                    var comboBox = new ComboBox() { ItemsSource = items };
                    comboBox.SelectionChanged += ComboBox_ChangedInFilters;
                    datePicker.SelectedDateChanged += DatePicker_ChangedInFilters;
                    dataForFillFilters.Add(new TableFilters() { Column = mainTable.Columns[i].ToString(), EmptyDatePicker = datePicker, EmptyComboBox = comboBox });
                    wrapPanel.Children.Add(comboBox);
                    wrapPanel.Children.Add(datePicker);
                    DataPanel.Children.Add(wrapPanel);
                }
            }
        }

        //Изменения в фильтрах
        private void DatePicker_ChangedInFilters(object sender, SelectionChangedEventArgs e)
        {
            Combine_TextFilter();
        }

        private void TextBox_ChangedInFilters(object sender, TextChangedEventArgs e)
        {
            Combine_TextFilter();
        }

        private void ComboBox_ChangedInFilters(object sender, SelectionChangedEventArgs e)
        {
            Combine_TextFilter();
        }

        //полнотекстовый поиск в документах
        private void FullTextSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            new FullTextSearchWindow().Show();
        }

        //Кнопки меню
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            new CreateDocumentWindow().Show();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.EventOnCreateRelations.Value = null;
            new MainWindow().Show();
        }

        private void Load_Template_Click(object sender, RoutedEventArgs e)
        {
            new LoadTemplateWindow().Show();
        }

        private void Create_Table_Click(object sender, RoutedEventArgs e)
        {
            new CreateNewTableWindow().Show();
        }

        private void Watch_Nomenclature_Click(object sender, RoutedEventArgs e)
        {
            new WorkWithNomenclatureWindow("Создать дело").Show();
        }

        private void Create_User_Click(object sender, RoutedEventArgs e)
        {
            new CreateUserWindow().Show();
        }

        private void Change_User_Click(object sender, RoutedEventArgs e)
        {
            new ChangeUserWindow().Show();
        }

        private void Work_With_Position_Click(object sender, RoutedEventArgs e)
        {
            new WorkWithPositionWindow().Show();
        }

        private void Add_Subdivision_Click(object sender, RoutedEventArgs e)
        {
            new AddSubdivisionWindow().Show();
        }

        private void Watch_Account_Click(object sender, RoutedEventArgs e)
        {
            new ProfileWindow().Show();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.Restart();
            Environment.Exit(0);
        }

        private void Open_Scaner_Click(object sender, RoutedEventArgs e)
        {
            new ScanerWindow().Show();            
        }

        private void Serach_On_Relations_Click(object sender, RoutedEventArgs e)
        {
            new SearchOnRelationsWindow().Show();
        }
    }
}
