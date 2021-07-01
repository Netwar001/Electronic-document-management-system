using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
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
    public partial class LoadTemplateWindow : Window
    {

        private DataTable mainTable;
        private List<TextBox> textBoxes;
        private string previousName;
        private string previousType;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public LoadTemplateWindow()
        {
            InitializeComponent();
            ChooseComboBox.ItemsSource = new List<string>() { "Изменить существующий", "Добавить новый" };
            mainTable = new DataTable();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var command = new SqlCommand("select * from [Шаблоны документов]", connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(mainTable);
                DataTable tempTable = new DataTable();
                adapter.Fill(tempTable);
                tempTable.Columns.RemoveAt(tempTable.Columns.Count - 1);
                MainDataGrid.ItemsSource = tempTable.DefaultView;
            }
            finally
            {
                connection.Close();
            }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChooseComboBox.SelectedItem != null)
                if (ChooseComboBox.SelectedItem.ToString() == "Изменить существующий")
                {
                    var dataRowView = (DataRowView)MainDataGrid.SelectedItem;
                    var row = dataRowView.Row.ItemArray;
                    previousName = (string)row[1];
                    previousType = (string)row[2];
                    for (var i = 0; i < row.Length; i++)
                    {
                        textBoxes[i].Text = (string)row[i];
                    }
                }
        }

        private void ChooseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataPanel.Children.Clear();
            BtnPanel.Children.Clear();
            textBoxes = new List<TextBox>();
            if (ChooseComboBox.SelectedItem.ToString() == "Изменить существующий")
            {
                for (var i = 0; i < mainTable.Columns.Count - 1; i++)
                {
                    DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                    var textBox = new TextBox();
                    if (i == 0 || i == mainTable.Columns.Count - 2)
                        textBox.IsReadOnly = true;
                    textBoxes.Add(textBox);
                    DataPanel.Children.Add(textBox);
                }

                var changeBtn = new Button() { Content = "Изменить" };
                changeBtn.Click += ChangeBtn_Click;
                BtnPanel.Children.Add(changeBtn);
                var deleteBtn = new Button() { Content = "Удалить" };
                deleteBtn.Click += DeleteBtn_Click;
                BtnPanel.Children.Add(deleteBtn);
            }
            else
            {
                for (var i = 0; i < mainTable.Columns.Count; i++)
                {
                    if (i != mainTable.Columns.Count - 1)
                        DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ToString() });
                    else
                        DataPanel.Children.Add(new Label() { Content = "Местонахождение файла" });
                    var textBox = new TextBox();
                    if (i == 0 || i >= mainTable.Columns.Count - 2)
                        textBox.IsReadOnly = true;
                    if (i == 0)
                        textBox.Text = User.Email;
                    textBoxes.Add(textBox);
                    DataPanel.Children.Add(textBox);
                }

                var loadBtn = new Button() { Content = "Загрузить" };
                loadBtn.Click += LoadBtn_Click;
                BtnPanel.Children.Add(loadBtn);
                var saveBtn = new Button() { Content = "Сохранить" };
                saveBtn.Click += SaveBtn_Click;
                BtnPanel.Children.Add(saveBtn);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxes[1].Text != "" && textBoxes[textBoxes.Count - 1].Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    byte[] docData;
                    using (FileStream fs = new FileStream(textBoxes[textBoxes.Count - 1].Text, FileMode.Open))
                    {
                        docData = new byte[fs.Length];
                        fs.Read(docData, 0, docData.Length);
                    }
                    SqlCommand addTemplate = connection.CreateCommand();
                    addTemplate.Connection = connection;
                    var str = "insert into [Шаблоны документов] values ('";
                    for (var i = 0; i < mainTable.Columns.Count - 1; i++)
                    {
                        str += textBoxes[i].Text + "','";
                    }
                    str = str.Remove(str.Length - 1, 1) + "@DocData)";
                    addTemplate.CommandText = str;
                    addTemplate.Parameters.Add("@DocData", SqlDbType.Image, 1000000);
                    addTemplate.Parameters["@DocData"].Value = docData;
                    addTemplate.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                MessageBox.Show("Шаблон успешно загружен");
                new LoadTemplateWindow().Show();
                Close();
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Word|*.doc*|Excel|*.xls*|PowerPoint|*.ppt*";
            if (openFileDialog.ShowDialog() == true)
            {
                textBoxes[textBoxes.Count - 1].Text = openFileDialog.FileName;
            }
            if (System.IO.Path.GetExtension(textBoxes[textBoxes.Count - 1].Text).Contains("doc"))
                textBoxes[textBoxes.Count - 2].Text = "Word";
            else if (System.IO.Path.GetExtension(textBoxes[textBoxes.Count - 1].Text).Contains("xls"))
                textBoxes[textBoxes.Count - 2].Text = "Excel";
            else if (System.IO.Path.GetExtension(textBoxes[textBoxes.Count - 1].Text).Contains("ppt"))
                textBoxes[textBoxes.Count - 2].Text = "PowerPoint";
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                SqlCommand deleteTemplate = connection.CreateCommand();
                deleteTemplate.Connection = connection;
                var str = "DELETE FROM [Шаблоны документов] WHERE [Название] = '" + previousName + "' and [Тип документа] = '" + previousType + "'";
                deleteTemplate.CommandText = str;
                deleteTemplate.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
            MessageBox.Show("Шаблон успешно удален");
            new LoadTemplateWindow().Show();
            Close();
        }

        private void ChangeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxes[1].Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    SqlCommand changeTemplate = connection.CreateCommand();
                    changeTemplate.Connection = connection;
                    var str = "update [Шаблоны документов] set [Название] = '" + textBoxes[1].Text +
                        "' WHERE [Название] = '" + previousName + "' and [Тип документа] = '" + previousType + "'";
                    changeTemplate.CommandText = str;
                    changeTemplate.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                MessageBox.Show("Шаблон успешно изменен");
                new LoadTemplateWindow().Show();
                Close();
            }
        }
    }
}
