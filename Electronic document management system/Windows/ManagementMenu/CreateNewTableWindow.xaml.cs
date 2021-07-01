using System;
using System.Collections.Generic;
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
    public partial class CreateNewTableWindow : Window
    {
        class NewTable
        {
            public TextBox EmptyTextBox { get; set; }
            public bool CommonColumn { get; set; }
            public bool EmployeeColumn { get; set; }
            public bool DateColumn { get; set; }
            public bool OptionsColumn { get; set; }
            public Button EmptyButton { get; set; }
            public Border EmptyBorder { get; set; }
            public TextBox ListWithOptions { get; set; }
        }

        private string tableName;
        private List<NewTable> newTableList = new List<NewTable>();
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public CreateNewTableWindow()
        {
            InitializeComponent();
            var panel = new WrapPanel();
            panel.Children.Add(new Label() { Content = "Название таблицы: " });
            var textBox = new TextBox() { Width = 300 };
            textBox.TextChanged += TextBoxTableName_TextChanged;
            panel.Children.Add(textBox);
            DataPanel.Children.Add(panel);

            var panel1 = new WrapPanel();
            panel1.Children.Add(new Label() { Content = "Добавление столбцов: " });
            var comboBox = new ComboBox()
            {
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                ItemsSource = new List<string> { "Обычный столбец", "Столбец с датой", "Столбец с выбором", "Столбец с информацией о сотруднике" }
            };
            comboBox.SelectionChanged += ComboBoxAddNewField_SelectionChanged;
            panel1.Children.Add(comboBox);
            DataPanel.Children.Add(panel1);

            var connection = new SqlConnection(connectionString);
            var tableColumns = new List<string>();
            connection.Open();
            try
            {
                var getTableColumns = new SqlCommand("SELECT top (9) COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'Архивные документы'", connection);
                var readTable = getTableColumns.ExecuteReader();
                while (readTable.Read())
                    tableColumns.Add(readTable.GetString(0));
                readTable.Close();
                tableColumns.RemoveRange(0, 2);
            }
            finally
            {
                connection.Close();
            }
            for (var i = 0; i < tableColumns.Count; i++)
            {
                var border = new Border();
                var panel3 = new StackPanel();
                border.Margin = new Thickness(5);
                border.Padding = new Thickness(5);
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = new SolidColorBrush(Colors.MediumPurple);
                panel3.Children.Add(new Label() { Content = "Столбец: " + tableColumns[i] });
                switch (i)
                {
                    case 0:
                        panel3.Children.Add(new Label() { Content = "Варианты выбора: Внутренний, Входящий, Исходящий" });
                        newTableList.Add(new NewTable() { EmptyTextBox = new TextBox() { Text = tableColumns[i] }, OptionsColumn = true, ListWithOptions = new TextBox() { Text = "Внутренний, Входящий, Исходящий" } });
                        break;
                    case 1:
                        panel3.Children.Add(new Label() { Content = "Варианты выбора: Открытая информация, Конфиденциально, Строго конфиденциальная информация" });
                        newTableList.Add(new NewTable() { EmptyTextBox = new TextBox() { Text = tableColumns[i] }, OptionsColumn = true, ListWithOptions = new TextBox() { Text = "Открытая информация, Конфиденциально, Строго конфиденциальная информация" } });
                        break;
                    case 2:
                        panel3.Children.Add(new Label() { Content = "Варианты выбора (*через запятую): " });
                        var textBoxWithOptions = new TextBox() { Text = "Приказ, Заявление", Margin = new Thickness(3) };
                        textBoxWithOptions.PreviewTextInput += TextBoxWithOptions_PreviewTextInput;
                        panel3.Children.Add(textBoxWithOptions);
                        newTableList.Add(new NewTable() { EmptyTextBox = new TextBox() { Text = tableColumns[i] }, OptionsColumn = true, ListWithOptions = textBoxWithOptions });
                        break;
                    case 3:
                        newTableList.Add(new NewTable() { EmptyTextBox = new TextBox() { Text = tableColumns[i] }, DateColumn = true });
                        break;
                    case 4:
                        newTableList.Add(new NewTable() { EmptyTextBox = new TextBox() { Text = tableColumns[i] }, CommonColumn = true });
                        break;
                    default:
                        newTableList.Add(new NewTable() { EmptyTextBox = new TextBox() { Text = tableColumns[i] }, EmployeeColumn = true });
                        break;
                }
                border.Child = panel3;
                DataPanel.Children.Add(border);
            }
        }

        private void ComboBoxAddNewField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem != null)
            {
                var border = new Border();
                var panel = new StackPanel();
                var textBox = new TextBox();
                var wrapPanel = new WrapPanel();
                border.Margin = new Thickness(5);
                border.Padding = new Thickness(5);
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = new SolidColorBrush(Colors.MediumPurple);
                var classWithMethods = new Methods.ClassWithMethods();
                var btn = classWithMethods.SetImgOnBtn(new Uri(@"Icons/Del-icon.png", UriKind.Relative));
                btn.Click += BtnDeleteColumn_Click;
                wrapPanel.Children.Add(btn);
                panel.Children.Add(wrapPanel);
                panel.Children.Add(textBox);
                switch ((sender as ComboBox).SelectedItem)
                {
                    case "Обычный столбец":
                        wrapPanel.Children.Insert(0, new Label() { Content = "Название стандартного столбца: " });
                        newTableList.Add(new NewTable() { EmptyTextBox = textBox, CommonColumn = true, EmptyButton = btn, EmptyBorder = border });
                        break;
                    case "Столбец с датой":
                        wrapPanel.Children.Insert(0, new Label() { Content = "Название столбца с датой: " });
                        newTableList.Add(new NewTable() { EmptyTextBox = textBox, DateColumn = true, EmptyButton = btn, EmptyBorder = border });
                        break;
                    case "Столбец с выбором":
                        wrapPanel.Children.Insert(0, new Label() { Content = "Название столбца с выбором: " });
                        panel.Children.Add(new Label() { Content = "Варианты выбора (*через запятую): " });
                        var textBoxWithOptions = new TextBox();
                        textBoxWithOptions.Margin = new Thickness(2, 3, 2, 3);
                        textBoxWithOptions.PreviewTextInput += TextBoxWithOptions_PreviewTextInput;
                        panel.Children.Add(textBoxWithOptions);
                        newTableList.Add(new NewTable() { EmptyTextBox = textBox, OptionsColumn = true, ListWithOptions = textBoxWithOptions, EmptyButton = btn, EmptyBorder = border });
                        break;
                    case "Столбец с информацией о сотруднике":
                        wrapPanel.Children.Insert(0, new Label() { Content = "Название столбца с информацией о сотруднике: " });
                        newTableList.Add(new NewTable() { EmptyTextBox = textBox, EmployeeColumn = true, EmptyButton = btn, EmptyBorder = border });
                        break;
                }
                border.Child = panel;
                DataPanel.Children.Add(border);
                (sender as ComboBox).SelectedItem = null;
            }
        }

        private void TextBoxWithOptions_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text != "")
            {
                var symbol = e.Text.ToCharArray()[0];
                if (!Char.IsDigit(symbol) && symbol != 8 && symbol != 44 && symbol != 45 && !Char.IsLetter(symbol)) // цифры,клавиша BackSpace,запятая,тире и буквы
                {
                    e.Handled = true;
                }
            }
        }

        private void BtnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            var index = newTableList.FindIndex(x => x.EmptyButton == (sender as Button));
            DataPanel.Children.Remove(newTableList[index].EmptyBorder);
            newTableList.RemoveAt(index);
        }

        private void TextBoxTableName_TextChanged(object sender, TextChangedEventArgs e)
        {
            tableName = (sender as TextBox).Text;
        }

        private void AgreeButton_Click(object sender, RoutedEventArgs e)
        {
            var connection = new SqlConnection(connectionString);
            var exist = false;
            connection.Open();
            try
            {
                var getTableName = new SqlCommand("SELECT name FROM sys.objects WHERE type in (N'U')", connection);
                var readTableNames = getTableName.ExecuteReader();
                var tableNames = new List<string>();
                while (readTableNames.Read())
                    tableNames.Add(readTableNames.GetString(0));
                readTableNames.Close();
                if (tableNames.Contains(tableName))
                {
                    exist = true;
                    MessageBox.Show("Таблица с таким названием уже существует");
                }
                else
                {
                    SqlCommand createTable = connection.CreateCommand();
                    createTable.Connection = connection;
                    var commandToCreateTable = "create table [" + tableName + "] ( id int NOT NULL PRIMARY KEY, ";
                    var employeeInfo = "";
                    var optionsInfo = "";
                    for (var i = 0; i < newTableList.Count; i++)
                    {
                        commandToCreateTable += "[" + newTableList[i].EmptyTextBox.Text + "]";
                        if (newTableList[i].CommonColumn == true)
                        {
                            commandToCreateTable += " varchar(100), ";
                        }
                        else if (newTableList[i].EmployeeColumn == true)
                        {
                            commandToCreateTable += " varchar(1000), ";
                            employeeInfo += newTableList[i].EmptyTextBox.Text + ",";
                        }
                        else if (newTableList[i].DateColumn == true)
                        {
                            commandToCreateTable += " date, ";
                        }
                        else if (newTableList[i].OptionsColumn == true)
                        {
                            commandToCreateTable += " varchar(100), ";
                            optionsInfo += newTableList[i].EmptyTextBox.Text + ":" + newTableList[i].ListWithOptions.Text + ";";
                        }
                    }
                    createTable.CommandText = commandToCreateTable.Remove(commandToCreateTable.Length - 2, 2) + ")";
                    createTable.ExecuteNonQuery();

                    SqlCommand addInfoInTable = connection.CreateCommand();
                    addInfoInTable.Connection = connection;
                    addInfoInTable.CommandText = "insert into [Информация о таблицах] values ('" + tableName + "','" + User.Email + "','" + employeeInfo.Remove(employeeInfo.Length - 1, 1) + "','"
                        + optionsInfo + "')";
                    addInfoInTable.ExecuteNonQuery();
                }
            }
            finally
            {
                connection.Close();
            }
            if (exist == false)
            {
                EventOnCreateTable.Value = tableName;
                MessageBox.Show("Таблица успешно создана");
                Close();
            }
        }
    }
}
