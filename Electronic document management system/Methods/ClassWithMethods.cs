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
using static Electronic_document_management_system.SearchOnRelationsWindow;

namespace Electronic_document_management_system.Methods
{
    public partial class ClassWithMethods
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;

        public Button SetImgOnBtn(Uri uri)
        {
            var btn = new Button()
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(-4)
            };
            Image img = new Image();
            img.Source = new BitmapImage(uri);
            StackPanel stackPnl = new StackPanel();
            stackPnl.Orientation = Orientation.Horizontal;
            stackPnl.Margin = new Thickness(10);
            stackPnl.Children.Add(img);
            btn.Content = stackPnl;
            return btn;
        }

        public List<GraphData> GetDocumentsInfo()
        {
            var dataList = new List<GraphData>();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var getTableName = new SqlCommand("SELECT name FROM sys.objects WHERE type in (N'U') and name not in ('Архивные документы'," +
                    System.Configuration.ConfigurationManager.AppSettings["mainTables"].ToString() + ")", connection);
                var readTableNames = getTableName.ExecuteReader();
                var tableNames = new List<string>();
                while (readTableNames.Read())
                    tableNames.Add(readTableNames.GetString(0));
                readTableNames.Close();

                foreach (var title in tableNames)
                {
                    var getDataFromTables = new SqlCommand("SELECT [id],'" + title + "',[Тема] FROM [" + title + "]", connection);
                    var readData = getDataFromTables.ExecuteReader();
                    while (readData.Read())
                        dataList.Add(new GraphData() { Id = readData.GetInt32(0), TableName = readData.GetString(1), Topic = readData.GetString(2) });
                    readData.Close();
                }
            }
            finally
            {
                connection.Close();
            }
            return dataList;
        }
    }
}
