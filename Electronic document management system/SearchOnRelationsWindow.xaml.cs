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
using GraphX.Common.Enums;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Controls;
using Electronic_document_management_system.Models;
using System.Data;
using System.Data.SqlClient;

namespace Electronic_document_management_system
{
    public partial class SearchOnRelationsWindow : Window
    {
        public class GraphData
        {
            public int Id { get; set; }
            public string TableName { get; set; }
            public string Topic { get; set; }
            public DataVertex Vertex { get; set; }
        }

        public class RelationsData
        {
            public string RelationsName { get; set; }
            public List<GraphData> GraphDataList { get; set; }
        }

        private List<GraphData> graphDataList;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;
        public SearchOnRelationsWindow()
        {
            InitializeComponent();
            ZoomControl.SetViewFinderVisibility(zoomctrl, Visibility.Visible);
            //Масштабирование на всем графике
            zoomctrl.ZoomToFill();
            //Настройка GraphArea
            GraphArea_Setup();

            Loaded += Window_Loaded;
            Area.VertexSelected += Area_VertexSelected;
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Генерация настроенного графа по объекту LogicCore
            //Первый параметр отвечает за автоматическую генерацию ребер графа, а второй за присвоение id объектам
            Area.GenerateGraph(true, true);
            //Стиль линий
            Area.SetEdgesDashStyle(EdgeDashStyle.Solid);
            //Видимость стрелок на краях
            Area.ShowAllEdgesArrows(true);
            //Видимость названий линий
            Area.ShowAllEdgesLabels(true);

            zoomctrl.ZoomSensitivity = 20;
            zoomctrl.ZoomToFill();
        }

        private void Area_VertexSelected(object sender, GraphX.Controls.Models.VertexSelectedEventArgs args)
        {
            var index = graphDataList.FindIndex(x => x.Vertex == args.VertexControl.Vertex);
            string accessLevel;
            if (User.AccessLevel == "Максимальный")
                accessLevel = "'Открытая информация', 'Конфиденциально', 'Строго конфиденциальная информация'";
            else if (User.AccessLevel == "Средний")
                accessLevel = "'Открытая информация', 'Конфиденциально'";
            else
                accessLevel = "'Открытая информация'";
            var mainTable = new DataTable();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var command = new SqlCommand("select * from [" + graphDataList[index].TableName + "] where [Уровень доступа] in (" + accessLevel + ") and " +
                    "id=" + graphDataList[index].Id, connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(mainTable);
            }
            finally
            {
                connection.Close();
            }
            if (mainTable.Rows.Count != 0)
            {
                var headers = new List<string>();
                foreach (var colName in mainTable.Columns)
                    headers.Add(colName.ToString());
                new ElectronicDocumentCard(headers, mainTable.Rows[0].ItemArray, graphDataList[index].TableName).Show();
            }
            else
                MessageBox.Show("Ваш уровень доступа не соответствует требуемому");
        }

        private GraphModel Graph_Setup()
        {
            var mainTable = new DataTable();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var command = new SqlCommand("select * from [Связи документов]", connection);
                var adapter = new SqlDataAdapter(command);
                adapter.Fill(mainTable);
            }
            finally
            {
                connection.Close();
            }

            var classWithMethods = new Methods.ClassWithMethods();
            var topicDataList = classWithMethods.GetDocumentsInfo();
            graphDataList = new List<GraphData>();
            //Экземпляр графа данных
            var dataGraph = new GraphModel();
            //Цикл для создания ребер и вершин
            for (int i = 0; i < mainTable.Rows.Count; i++)
            {
                var row = mainTable.Rows[i].ItemArray;
                for (var j = 0; j <= 3; j += 3)
                {
                    if (graphDataList.Find(x => (x.Id == Convert.ToInt32(row[j])) && (x.TableName == row[j + 1].ToString())) == null)
                    {
                        var topic = topicDataList.Where(x => (x.Id == Convert.ToInt32(row[j])) && (x.TableName == row[j + 1].ToString())).Select(x => x.Topic).FirstOrDefault();
                        var dataVertex = new DataVertex(topic);
                        graphDataList.Add(new GraphData() { 
                            Id = Convert.ToInt32(row[j]), 
                            TableName = row[j + 1].ToString(), 
                            Topic = topic,
                            Vertex = dataVertex 
                        });
                        dataGraph.AddVertex(dataVertex);
                    }
                    if (j == 3)
                    {
                        var dataEdge = new DataEdge(
                            graphDataList[graphDataList.FindIndex(x => (x.Id == Convert.ToInt32(row[0])) && (x.TableName == row[1].ToString()))].Vertex,
                            graphDataList[graphDataList.FindIndex(x => (x.Id == Convert.ToInt32(row[3])) && (x.TableName == row[4].ToString()))].Vertex
                            )
                        { Text = row[2].ToString() };
                        dataGraph.AddEdge(dataEdge);
                    }
                }
            }
            return dataGraph;
        }

        private void GraphArea_Setup()
        {
            //Логическое ядро, заполненного графа данных с ребрами и вершинами
            var logicCore = new GXLogicCore() { Graph = Graph_Setup() };
            //Алгоритм компоновки, который будет использоваться для расчета положения вершин
            logicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Tree;
            //Задание параметров для выбранного алгоритма с помощью свойства AlgorithmFactory
            logicCore.DefaultLayoutAlgorithmParams = logicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.Tree);
            //Изменение параметров алгоритма
            ((SimpleTreeLayoutParameters)logicCore.DefaultLayoutAlgorithmParams).Direction = LayoutDirection.TopToBottom;
            ((SimpleTreeLayoutParameters)logicCore.DefaultLayoutAlgorithmParams).SpanningTreeGeneration = SpanningTreeGeneration.BFS;
            ((SimpleTreeLayoutParameters)logicCore.DefaultLayoutAlgorithmParams).LayerGap = 100;
            //Алгоритм расположения вершин без наложения друг на друга
            logicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            logicCore.DefaultOverlapRemovalAlgorithmParams.HorizontalGap = 50;
            logicCore.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 50;
            //Алгоритм для построения путей маршрута
            logicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            logicCore.AsyncAlgorithmCompute = false;
            //Назначение логического ядра объекту GraphArea
            Area.LogicCore = logicCore;
        }

        private void AgreeBtn_Click(object sender, RoutedEventArgs e)
        {
            var edgeList = Area.EdgesList;
            foreach (var edge in edgeList)
            {
                edge.Value.Source.Background = Brushes.LightGray;
                edge.Value.Target.Background = Brushes.LightGray;
            }
            foreach (var edge in edgeList)
            {
                if (edge.Key.Text.ToLower().Contains(SearchTextBox.Text.ToLower()))
                {
                    edge.Value.Source.Background = Brushes.Red;
                    edge.Value.Target.Background = Brushes.Red;
                }
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            //Освобождение памяти, для обновления графа
            Area.Dispose();

            GraphArea_Setup();
            Window_Loaded(null, null);
        }
    }
}
