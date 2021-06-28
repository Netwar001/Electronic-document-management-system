using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
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
using System.Net;
using System.Net.Mail;

namespace Electronic_document_management_system
{
    public partial class CreateUserWindow : Window
    {
        private DataTable mainTable;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;
        public CreateUserWindow()
        {
            InitializeComponent();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("select * from [Должности]", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                mainTable = new DataTable();
                adapter.Fill(mainTable);

                var subdivisions = new List<string>();
                var getName = new SqlCommand("SELECT [Наименование] FROM [Подразделения]", connection);
                var reader = getName.ExecuteReader();
                while (reader.Read())
                {
                    subdivisions.Add(reader.GetString(0));
                }
                reader.Close();
                subdivisions.Sort();
                subdivisionComboBox.ItemsSource = subdivisions;
            }
            finally
            {
                connection.Close();
            }
            var items = mainTable.AsEnumerable().Select(row => row.Field<string>("Должность")).ToList();
            items.Sort();
            positionComboBox.ItemsSource = items;
        }

        private void GeneratePasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            var length = 7;
            var random = new Random();
            var randomPass = new String(Enumerable.Range(0, length).Select(n => (Char)(random.Next(32, 127))).ToArray());
            PasswordTextBox.Text = randomPass;
        }

        private void positionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataPanel.Children.Clear();
            var items = mainTable.AsEnumerable().Where(row => row.Field<string>("Должность") == (sender as ComboBox).SelectedItem.ToString()).ToList()[0].ItemArray;
            DataPanel.Children.Add(new Label() { Content = items[1] + " уровень доступа", FontWeight = FontWeights.Bold });
            for (var i = 2; i < items.Length; i++)
            {
                if ((bool)items[i] == true)
                    DataPanel.Children.Add(new Label() { Content = mainTable.Columns[i].ColumnName });
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text != "" && positionComboBox.Text != "" && subdivisionComboBox.Text != "" && EmailTextBox.Text != "" && PasswordTextBox.Text != "")
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                try
                {
                    SqlCommand command = connection.CreateCommand();
                    command.Connection = connection;
                    command.CommandText = "insert into [Учетные записи] values ('" + NameTextBox.Text + "','" + positionComboBox.Text + "','" +
                        subdivisionComboBox.Text + "','" + EmailTextBox.Text + "','" + PasswordTextBox.Text + "')";
                    command.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                SendEmail();
                new CreateUserWindow().Show();
                Close();
            }
            else
                MessageBox.Show("Заполните все поля");
        }

        private void SendEmail()
        {
            string fromEmail = System.Configuration.ConfigurationManager.AppSettings["fromEmail"].ToString();
            string passFromEmail = System.Configuration.ConfigurationManager.AppSettings["passwordFromEmail"].ToString();
            // отправитель - устанавливаем адрес и отображаемое в письме имя
            MailAddress from = new MailAddress(fromEmail, "Электронный архив");
            // кому отправляем
            MailAddress to = new MailAddress(EmailTextBox.Text);
            // создаем объект сообщения
            MailMessage mail = new MailMessage(from, to);
            // тема письма
            mail.Subject = "Пароль для первой авторизации в приложении";
            // текст письма
            mail.Body = "<h2>Ваш пароль для авторизации: " + PasswordTextBox.Text + "</h2>";
            // письмо представляет код html
            mail.IsBodyHtml = true;
            // адрес smtp-сервера и порт, с которого будем отправлять письмо
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            // логин и пароль
            smtp.Credentials = new NetworkCredential(fromEmail, passFromEmail);
            smtp.EnableSsl = true;
            try
            {
                smtp.Send(mail);
                MessageBox.Show("Пароль был отправлен на указанную почту");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Пароль не был отправлен на указанную почту");
            }
        }
    }
}
