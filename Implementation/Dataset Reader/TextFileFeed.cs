using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Dataset_Reader
{
    public class TextFileFeed : IDataFeed
    {
        private readonly string _filePath;

        public TextFileFeed(string filePath)
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
            var lines = File.ReadAllLines(Path.Combine(_filePath, OutputFiles.Cardinality));
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var card = new Cardinality();
                card.Event = CsvReader.ReadIntValue(line, 0) - 1;
                card.Min = CsvReader.ReadIntValue(line, 1);
                card.Max = CsvReader.ReadIntValue(line, 2);
                result.Add(card);
            }

            return result;
        }

        public void GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount)
        {
            var userInnateLines = File.ReadAllLines(Path.Combine(_filePath, OutputFiles.InnateAffinity));
            usersCount = 0;
            foreach (var line in userInnateLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                usersCount++;
            }

            var cardinalityLines = File.ReadAllLines(Path.Combine(_filePath, OutputFiles.Cardinality));
            eventsCount = 0;
            foreach (var line in cardinalityLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                eventsCount++;
            }
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            var userInnateLines = File.ReadAllLines(Path.Combine(_filePath, OutputFiles.InnateAffinity));
            var result = new List<List<double>>();
            foreach (var line in userInnateLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var user = CsvReader.ReadIntValue(line, 0);
                var @event = CsvReader.ReadIntValue(line, 1);
                var affinity = CsvReader.ReadDoubleValue(line, 2);
                if (result.Count < user)
                {
                    result.Add(new List<double>());
                }
                
                result[user - 1].Add(affinity);
            }
            return result;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            var lines = File.ReadAllLines(Path.Combine(_filePath, OutputFiles.SocialAffinity));
            double[,] result = new double[users.Count, users.Count];
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var user1 = CsvReader.ReadIntValue(line, 0);
                var user2 = CsvReader.ReadIntValue(line, 1);
                var affinity = CsvReader.ReadDoubleValue(line, 2);
                result[user1 - 1, user2 - 1] = affinity;
            }
            return result;
        }

        public List<double> GenerateExtrovertIndeces(List<int> users, double[,] socialAffinities)
        {
            var extrovertIndecesLines = File.ReadAllLines(Path.Combine(_filePath, OutputFiles.ExtrovertIndeces));
            var result = new List<double>();
            foreach (var line in extrovertIndecesLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var user = CsvReader.ReadIntValue(line, 0);
                var extrovertIndex = CsvReader.ReadDoubleValue(line, 1);

                result.Add(extrovertIndex);
            }
            return result;
        }
    }
}
