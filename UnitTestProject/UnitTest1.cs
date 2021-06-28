using Microsoft.VisualStudio.TestTools.UnitTesting;
using Electronic_document_management_system;
using System.Collections.Generic;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //var headers = new List<string>() { 
            //    "id", "Область действия", "Уровень доступа", "Тип документа","Дата документа", 
            //    "Тема", "Исполнители","Адресаты", "Время получения", "Отвечающие за получение" };
            //var row = new object[] { 
            //    "2", "Внутренний", "Конфиденциально", "Приказ", "2021-05-10", 
            //    "Приказ о предоставлении отпуска работнику Галкину Максиму Игоревичу", 
            //    "ФИО: Красиков Владимир Дмитриевич, Должность: Дирректор, Подразделение: Отдел кадров;", "", "Вовремя", "" };
            //var window = new ElectronicDocumentCard(headers, row, "Новая таблица");
            //var epectedDoc = window.DownloadDoc(row);
            //Assert.IsTrue(@"C:\\EDMS_App\\Downloaded_Documents\\Приказ о предоставлении отпуска работнику.docx", epectedDoc);
        }
    }
}
