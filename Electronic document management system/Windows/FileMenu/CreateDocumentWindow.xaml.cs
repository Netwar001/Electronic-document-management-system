using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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
using Excel = Microsoft.Office.Interop.Excel;

namespace Electronic_document_management_system
{
    public partial class CreateDocumentWindow : Window
    {
        private DataTable mainTable;
        private List<ComboBox> comboBox = new List<ComboBox>();
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;
        public CreateDocumentWindow()
        {
            InitializeComponent();
            ChooseComboBox.ItemsSource = new List<string>() { "Word", "Excel", "PowerPoint" };

            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select * from [Шаблоны документов]", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);
            }
            finally
            {
                connection.Close();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChooseComboBox.SelectedItem = null;
        }

        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("C:\\EDMS_App\\Created_Documents");
            if (comboBox.Count != 0)
            {
                if (comboBox[0].SelectedItem == null)
                    Create_File();
                else
                {
                    var connection = new SqlConnection(connectionString);
                    connection.Open();
                    try
                    {
                        SqlCommand getDoc = new SqlCommand("select [DocData] from [Шаблоны документов] where [Тип документа] = '" + ChooseComboBox.Text + "' and [Название] = '" +
                            comboBox[0].SelectedItem.ToString() + "'", connection);
                        var reader = getDoc.ExecuteReader();
                        byte[] data = new byte[0];
                        while (reader.Read())
                        {
                            data = (byte[])reader.GetValue(0);
                        }
                        if (data.Length > 0)
                        {
                            var filename = "";
                            switch (ChooseComboBox.Text)
                            {
                                case "Word":
                                    filename = DocNameTextBox.Text + ".docx";
                                    break;
                                case "Excel":
                                    filename = DocNameTextBox.Text + ".xlsx";
                                    break;
                                case "PowerPoint":
                                    filename = DocNameTextBox.Text + ".pptx";
                                    break;
                            }
                            using (FileStream fs = new FileStream("C:\\EDMS_App\\Created_Documents\\" + filename, FileMode.OpenOrCreate))
                            {
                                fs.Write(data, 0, data.Length);
                            }
                            Process.Start("C:\\EDMS_App\\Created_Documents\\" + filename);
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                Create_File();
            }
            Close();
        }

        private void Create_File()
        {
            var filename = "";
            switch (ChooseComboBox.Text)
            {
                case "Word":
                    filename = DocNameTextBox.Text + ".docx";
                    break;
                case "Excel":
                    filename = DocNameTextBox.Text + ".xlsx";
                    var ObjWorkExcel = new Excel.Application();
                    var ObjWorkBook = ObjWorkExcel.Workbooks.Add();
                    ObjWorkBook.SaveAs("C:\\EDMS_App\\Created_Documents\\" + filename);
                    ObjWorkBook.Close();
                    break;
                case "PowerPoint":
                    filename = DocNameTextBox.Text + ".pptx";
                    break;
                default:
                    filename = DocNameTextBox.Text + "." + PersonalTextBox.Text;
                    break;
            }
            if (ChooseComboBox.Text != "Excel")
                File.Create("C:\\EDMS_App\\Created_Documents\\" + filename).Dispose();
            Process.Start("C:\\EDMS_App\\Created_Documents\\" + filename);
        }

        private void ChooseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = new List<string>();
            if ((sender as ComboBox).SelectedItem != null)
            {
                items = mainTable.AsEnumerable().Where(row => row.Field<string>("Тип документа") == (sender as ComboBox).SelectedItem.ToString())
                    .Select(row => row.Field<string>("Название")).ToList();
                items.Sort();
            }
            if (items.Count != 0)
            {
                if (comboBox.Count != 0)
                {
                    DataPanel.Children.Remove(comboBox[0]);
                    comboBox.RemoveAt(0);
                }
                comboBox.Add(new ComboBox() { Width = 135, ItemsSource = items });
                DataPanel.Children.Add(comboBox[0]);
            }
            else
            {
                if (comboBox.Count != 0)
                {
                    DataPanel.Children.Remove(comboBox[0]);
                    comboBox.RemoveAt(0);
                }
            }

        }
    }
}
