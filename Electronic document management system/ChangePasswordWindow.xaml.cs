using System;
using System.Collections.Generic;
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
    public partial class ChangePasswordWindow : Window
    {
        private List<TextBox> textBoxes = new List<TextBox>();
        private static string randomCode;
        public ChangePasswordWindow(string email)
        {
            InitializeComponent();
            DataPanel.Children.Add(new Label() { Content = "Введите новый пароль, длиной не менее 6 символов" });
            var textBox = new TextBox() { Margin = new Thickness(5, 5, 5, 5) };
            textBoxes.Add(textBox);
            DataPanel.Children.Add(textBox);
            if (email != "")
            {
                SendEmail(email);
                DataPanel.Children.Add(new Label() { Content = "Введите код из письма" });
                var textBox1 = new TextBox() { Margin = new Thickness(5, 5, 5, 5) };
                textBoxes.Add(textBox1);
                DataPanel.Children.Add(textBox1);
            }
            var btn = new Button() { Content = "Подтвердить" };
            btn.Click += Btn_Click;
            DataPanel.Children.Add(btn);
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            if (randomCode == null)
            {
                if (textBoxes[0].Text != "" && textBoxes[0].Text.Length >= 6)
                {
                    EventOnChangePassword.Value = textBoxes[0].Text;
                    Close();
                }
                else
                    MessageBox.Show("Пароль не соответствует критериям");
            }
            else
            {
                if (textBoxes[1].Text == randomCode.ToString())
                {
                    if (textBoxes[0].Text != "" && textBoxes[0].Text.Length >= 6)
                    {
                        EventOnChangePassword.Value = textBoxes[0].Text;
                        Close();
                    }
                    else
                        MessageBox.Show("Пароль не соответствует критериям");
                }
                else
                    MessageBox.Show("Проверьте введенный код");
            }
        }

        private static void SendEmail(string email)
        {
            var length = 8;
            var random = new Random();
            randomCode = new String(Enumerable.Range(0, length).Select(n => (Char)(random.Next(32, 127))).ToArray());
            string fromEmail = System.Configuration.ConfigurationManager.AppSettings["fromEmail"].ToString();
            string passFromEmail = System.Configuration.ConfigurationManager.AppSettings["passwordFromEmail"].ToString();
            // отправитель - устанавливаем адрес и отображаемое в письме имя
            MailAddress from = new MailAddress(fromEmail, "Электронный архив");
            // кому отправляем
            MailAddress to = new MailAddress(email);
            // создаем объект сообщения
            MailMessage m = new MailMessage(from, to);
            // тема письма
            m.Subject = "Код для восстановления пароля";
            // текст письма
            m.Body = "<h2>Ваш код для восстановления пароля: " + randomCode.ToString() + "</h2>";
            // письмо представляет код html
            m.IsBodyHtml = true;
            // адрес smtp-сервера и порт, с которого будем отправлять письмо
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            // логин и пароль
            smtp.Credentials = new NetworkCredential(fromEmail, passFromEmail);
            smtp.EnableSsl = true;
            try
            {
                smtp.Send(m);
                MessageBox.Show("Код для восстановления пароля был отправлен на вашу почту");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Код для восстановления пароля не был отправлен, попробуйте позже");
            }
        }
    }
}
