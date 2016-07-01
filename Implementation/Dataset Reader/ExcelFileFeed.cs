using System;
using System.Collections.Generic;
using System.IO;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Dataset_Reader
{
    public class ExcelFileFeed : IDataFeed
    {
        private readonly string _filePath;

        public ExcelFileFeed(string filePath)
        {
            if (filePath == null)
            {
                throw new Exception("No file provided");
            }
            _filePath = filePath;
        }

        public List<Cardinality> GenerateCapacity(List<int> users, List<int> events)
        {
            var result = new List<Cardinality>();
            var fileInfo = new FileInfo(_filePath);
            var excel = new ExcelPackage(fileInfo);
            var ws = excel.Workbook.Worksheets[3];

            for (int i = 2; ; i++)
            {
                var value = ws.Cells[i, 1].Value;
                if (value == null)
                    break;
                var card = new Cardinality();
                card.Min = Convert.ToInt32(ws.Cells[i, 2].Value);
                card.Max = Convert.ToInt32(ws.Cells[i, 3].Value);

                result.Add(card);
            }

            return result;
        }

        public void GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount)
        {
            var fileInfo = new FileInfo(_filePath);
            var excel = new ExcelPackage(fileInfo);
            var ws = excel.Workbook.Worksheets[1];

            usersCount = 0;
            for (int i = 2; ; i++)
            {
                var value = ws.Cells[i, 1].Value;
                if (value == null)
                    break;
                usersCount++;
            }

            eventsCount = 0;
            for (int i = 2; ; i++)
            {
                var value = ws.Cells[1, i].Value;
                if (value == null)
                    break;
                eventsCount++;
            }
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            var fileInfo = new FileInfo(_filePath);
            var excel = new ExcelPackage(fileInfo);
            var ws = excel.Workbook.Worksheets[1];

            var result = ReadWorksheet(ws);

            return result;
        }

        private static List<List<double>> ReadWorksheet(ExcelWorksheet ws)
        {
            var result = new List<List<double>>();
            for (int i = 2; ; i++)
            {
                var value = ws.Cells[i, 1].Value;
                if (value == null)
                    break;
                var list = new List<double>();
                for (int j = 2; ; j++)
                {
                    value = ws.Cells[1, j].Value;
                    if (value == null)
                        break;
                    var v = Convert.ToDouble(ws.Cells[i, j].Value);
                    list.Add(v);
                }
                result.Add(list);
            }
            return result;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            var fileInfo = new FileInfo(_filePath);
            var excel = new ExcelPackage(fileInfo);
            var ws = excel.Workbook.Worksheets[2];
            var result = ReadWorksheet(ws);

            double[,] array = new double[result.Count, result[0].Count];
            for (int i = 0; i < result.Count; i++)
            {
                var list = result[i];
                for (int j = 0; j < list.Count; j++)
                {
                    array[i, j] = list[j];
                }
            }
            return array;
        }
    }
}
