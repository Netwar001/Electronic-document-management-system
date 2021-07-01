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
using System.Security.Cryptography;
using System.Data.SqlClient;
using System.Data;

namespace Electronic_document_management_system
{
    static class EventOnChangePassword
    {
        public static EventHandler PasswordChanged = delegate { };
        private static string _text;

        public static string Value
        {
            get { return _text; }
            set
            {
                _text = value;
                PasswordChanged(null, EventArgs.Empty);
            }
        }
    }

    public partial class AuthorizationWindow : Window
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EDMSConnectionString"].ConnectionString;
        List<string> userInfo;

        public AuthorizationWindow()
        {
            InitializeComponent();
            EventOnChangePassword.PasswordChanged += ChangePassword;
        }

        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] buffer1;
            if (hashedPassword == null)
                return false;
            if (password == null)
                throw new ArgumentNullException("password");
            byte[] source = Convert.FromBase64String(hashedPassword);
            if ((source.Length != 0x31) || (source[0] != 0))
                return false;
            byte[] destination = new byte[0x10];
            Buffer.BlockCopy(source, 1, destination, 0, 0x10);
            byte[] buffer = new byte[0x20];
            Buffer.BlockCopy(source, 0x11, buffer, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, destination, 0x3e8))
                buffer1 = bytes.GetBytes(0x20);
            return buffer.SequenceEqual(buffer1);
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            userInfo = new List<string>();
            var connection = new SqlConnection(connectionString);
            var pass = "";
            connection.Open();
            try
            {
                var getPass = new SqlCommand("SELECT * FROM [Учетные записи] WHERE Email = '" + EmailTextBox.Text + "'", connection);
                var reader = getPass.ExecuteReader();
                while (reader.Read())
                {
                    userInfo.Add(reader.GetString(0));
                    userInfo.Add(reader.GetString(1));
                    userInfo.Add(reader.GetString(2));
                    userInfo.Add(reader.GetString(3));
                    pass = reader.GetString(4);
                }
                reader.Close();
            }
            finally
            {
                connection.Close();
            }
            if (userInfo.Count != 0)
            {
                if (pass.Length == 7)
                {
                    var window = new ChangePasswordWindow("");
                    window.Show();
                }
                else
                {
                    if (VerifyHashedPassword(pass, PasswordTextBox.Text))
                    {
                        var window = new DataBaseWindow();
                        Archive.Window = window;
                        UploadInfoAboutUser();
                        window.Show();
                        Close();
                    }
                    else
                        MessageBox.Show("Неверно введен пароль");
                }
            }
            else
                MessageBox.Show("Неверно введен e-mail");
        }

        public static string HashPassword(string password)
        {
            byte[] salt;
            byte[] buffer;
            if (password == null)
                throw new ArgumentNullException("password");
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer = bytes.GetBytes(0x20);
            }
            var destination = new byte[0x31];
            Buffer.BlockCopy(salt, 0, destination, 1, 0x10);
            Buffer.BlockCopy(buffer, 0, destination, 0x11, 0x20);
            return Convert.ToBase64String(destination);
        }

        private void ChangePassword(object sender, EventArgs e)
        {
            var newPass = HashPassword(EventOnChangePassword.Value);
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                SqlCommand changePass = connection.CreateCommand();
                changePass.Connection = connection;
                changePass.CommandText = "Update [Учетные записи] set [Пароль] = '" + newPass + "' WHERE Email = '" + EmailTextBox.Text + "'";
                changePass.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
            var window = new DataBaseWindow();
            Archive.Window = window;
            UploadInfoAboutUser();
            window.Show();
            Close();
        }

        private void UploadInfoAboutUser()
        {
            var capabilities = new List<bool>();
            string accessLevel;
            var connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                var commandForAdapter = new SqlCommand("SELECT * FROM [Должности] where [Должность] = '" + userInfo[1] + "'", connection);
                var adapter = new SqlDataAdapter(commandForAdapter);
                var mainTable = new DataTable();
                adapter.Fill(mainTable);
                var items = mainTable.Rows[0].ItemArray.ToList();
                accessLevel = items[1].ToString();
                items.RemoveRange(0,2);
                capabilities = items.AsEnumerable().Select(x => (bool)x).ToList();
            }
            finally
            {
                connection.Close();
            }
            User.Name = userInfo[0];
            User.Position = userInfo[1];
            User.Subdivision = userInfo[2];
            User.Email = userInfo[3];
            User.AccessLevel = accessLevel;
            User.UploadFilesToArchive = capabilities[0];
            User.ChangeInfoAboutFile = capabilities[1];
            User.ChangeFileInArchive = capabilities[2];
            User.CreateNewTable = capabilities[3];
            User.LoadingNewTemplates = capabilities[4];
            User.CreateAccounts = capabilities[5];
            User.WorkWithPositions = capabilities[6];
            User.AddSubdivisions = capabilities[7];
            User.WorkWithNomenclature = capabilities[8];
        }

        private void ForgotenBtn_Click(object sender, RoutedEventArgs e)
        {
            var connection = new SqlConnection(connectionString);
            bool correct;
            connection.Open();
            try
            {
                var getPass = new SqlCommand("SELECT * FROM [Учетные записи] WHERE Email = '" + EmailTextBox.Text + "'", connection);
                var reader = getPass.ExecuteReader();
                correct = reader.HasRows;
            }
            finally
            {
                connection.Close();
            }
            if (correct)
            {
                var window = new ChangePasswordWindow(EmailTextBox.Text);
                window.Show();
            }
            else
                MessageBox.Show("Неверно введен e-mail");
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                LoginBtn_Click(null, null);
            }
        }
    }
}
