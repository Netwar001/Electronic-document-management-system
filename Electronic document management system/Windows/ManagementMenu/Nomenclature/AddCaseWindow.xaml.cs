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
    public partial class AddCaseWindow : Window
    {
        public static class EventOnAddDocumentsInCase
        {
            public static EventHandler DocumentsAdded = delegate { };
            private static List<ElectronicCaseCard.DocumentsData> _list;

            public static List<ElectronicCaseCard.DocumentsData> Value
            {
                get { return _list; }
                set
                {
                    _list = value;
                    DocumentsAdded(null, EventArgs.Empty);
                }
            }
        }

        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public AddCaseWindow(string subdivisionName)
        {
            InitializeComponent();
            SubdivisionTextBox.Text = subdivisionName;

            EventOnAddDocumentsInCase.DocumentsAdded += DocumentsAdded;
        }

        public void DocumentsAdded(object sender, EventArgs e)
        {
            if (EventOnAddDocumentsInCase.Value != null)
            {
                DataPanel.Children.Clear();
                for (var i = 0; i < EventOnAddDocumentsInCase.Value.Count; i++)
                {
                    foreach (var id in EventOnAddDocumentsInCase.Value[i].Id)
                    {
                        DataPanel.Children.Add(new Label() { Content = "id документа: " + id + ", Название таблицы: " + EventOnAddDocumentsInCase.Value[i].TableName });
                    }
                }
                VariableBtn.Content = "Очистить поле";
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EventOnAddDocumentsInCase.Value != null && IndexTextBox.Text != "" && CaseNametextBox.Text != "" && StorageAgeDatePicker.Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    var str = "insert into [Номенклатура дел] values ";
                    for (var i = 0; i < EventOnAddDocumentsInCase.Value.Count; i++)
                    {
                        foreach (var id in EventOnAddDocumentsInCase.Value[i].Id)
                        {
                            str += "('" + IndexTextBox.Text + "','" + CaseNametextBox.Text + "','" + SubdivisionTextBox.Text + "','Открыто','" +
                                DateTime.Today + "','" + StorageAgeDatePicker.Text + "','" + RemarkTextBox.Text + "','";
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
                MessageBox.Show("Дело успешно создано");
                WorkWithNomenclatureWindow.EventOnAddCase.Value = SubdivisionTextBox.Text;
                Close();
            }
        }

        private void VariableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VariableBtn.Content.ToString() == "Добавить документы")
            {
                var allDocList = new List<ElectronicCaseCard.DocumentsData>();
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
                            allDocList.Add(new ElectronicCaseCard.DocumentsData() { Id = new List<string>() { tableRow[0].ToString() }, TableName = tableRow[1].ToString() });
                        else
                            allDocList[allDocList.FindIndex(x => x.TableName == tableRow[1].ToString())].Id.Add(tableRow[0].ToString());
                    }
                }
                finally
                {
                    connection.Close();
                }
                new AddDocumentsInCaseWindow(new List<ElectronicCaseCard.DocumentsData>(), CaseNametextBox.Text, allDocList, "AddCaseWindow").Show();
            }
            else
            {
                EventOnAddDocumentsInCase.Value = null;
                DataPanel.Children.Clear();
                VariableBtn.Content = "Добавить документы";
            }
        }
    }
}
