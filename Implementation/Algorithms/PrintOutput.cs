using System;
using System.Collections.Generic;
using System.IO;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Algorithms
{
    public class PrintOutput<T>
    {
        private readonly Algorithm<T> _algorithm;

        public PrintOutput(Algorithm<T> algorithm)
        {
            _algorithm = algorithm;
        }

        public void PrintToExcel(List<UserEvent> result, Welfare welfare, FileInfo output)
        {
            ExcelPackage excel = new ExcelPackage(output);
            var usereventsheet = excel.Workbook.Worksheets.Add("Innate Affinities");
            usereventsheet.Cells[1, 1].Value = @"User\Event";
            foreach (var @event in _algorithm.AllEvents)
            {
                usereventsheet.Cells[1, @event + 2].Value = @event + 1;
                foreach (var user in _algorithm.AllUsers)
                {
                    if (@event == 0)
                    {
                        usereventsheet.Cells[user + 2, 1].Value = user + 1;
                    }

                    usereventsheet.Cells[user + 2, @event + 2].Value = _algorithm.InAffinities[user][@event];
                }
            }
            usereventsheet.Cells[usereventsheet.Dimension.Address].AutoFitColumns();

            var socialaffinitiessheet = excel.Workbook.Worksheets.Add("Social Affinities");
            socialaffinitiessheet.Cells[1, 1].Value = @"User\User";
            foreach (var user1 in _algorithm.AllUsers)
            {
                socialaffinitiessheet.Cells[1, user1 + 2].Value = user1 + 1;
                foreach (var user2 in _algorithm.AllUsers)
                {
                    if (user1 == 0)
                    {
                        socialaffinitiessheet.Cells[user2 + 2, 1].Value = user2 + 1;
                    }

                    socialaffinitiessheet.Cells[user2 + 2, user1 + 2].Value = _algorithm.SocAffinities[user1, user2];
                }
            }
            socialaffinitiessheet.Cells[socialaffinitiessheet.Dimension.Address].AutoFitColumns();

            var cardinalitiessheet = excel.Workbook.Worksheets.Add("Cardinalities");
            cardinalitiessheet.Cells[1, 1].Value = "Event";
            cardinalitiessheet.Cells[1, 2].Value = "Min";
            cardinalitiessheet.Cells[1, 3].Value = "Max";
            for (int i = 0; i < _algorithm.EventCapacity.Count; i++)
            {
                var cap = _algorithm.EventCapacity[i];
                cardinalitiessheet.Cells[i + 2, 1].Value = i + 1;
                cardinalitiessheet.Cells[i + 2, 2].Value = cap.Min;
                cardinalitiessheet.Cells[i + 2, 3].Value = cap.Max;
            }
            cardinalitiessheet.Cells[cardinalitiessheet.Dimension.Address].AutoFitColumns();

            var assignmentssheet = excel.Workbook.Worksheets.Add("Assignments");
            assignmentssheet.Cells[1, 1].Value = "User";
            assignmentssheet.Cells[1, 2].Value = "Event";
            for (int i = 0; i < result.Count; i++)
            {
                var userEvent = result[i];
                assignmentssheet.Cells[i + 2, 1].Value = userEvent.User + 1;
                if (userEvent.Event >= 0)
                {
                    assignmentssheet.Cells[i + 2, 2].Value = userEvent.Event + 1;
                }
            }
            assignmentssheet.Cells[1, 4].Value = "Total Welfare";
            assignmentssheet.Cells[1, 5].Value = welfare.TotalWelfare;

            assignmentssheet.Cells[2, 4].Value = "Innate Welfare";
            assignmentssheet.Cells[2, 5].Value = welfare.InnateWelfare;

            assignmentssheet.Cells[3, 4].Value = "Social Welfare";
            assignmentssheet.Cells[3, 5].Value = welfare.SocialWelfare;
            assignmentssheet.Cells[assignmentssheet.Dimension.Address].AutoFitColumns();

            var regRatiosheet = excel.Workbook.Worksheets.Add("RegRatios");
            var ratios = _algorithm.CalcRegRatios(_algorithm.AllUsers);
            int index = 1;
            foreach (var ratio in ratios)
            {
                regRatiosheet.Cells[index, 1].Value = ratio.Key;
                regRatiosheet.Cells[index, 2].Value = ratio.Value;
                index++;
            }
            regRatiosheet.Cells[regRatiosheet.Dimension.Address].AutoFitColumns();

            _algorithm.Conf.PrintToExcel(excel, _algorithm._watches);

            var eventAssignmentsSheet = excel.Workbook.Worksheets.Add("Event Assignments");
            eventAssignmentsSheet.Cells[1, 1].Value = "Event";
            eventAssignmentsSheet.Cells[1, 2].Value = "Min";
            eventAssignmentsSheet.Cells[1, 3].Value = "Count";
            eventAssignmentsSheet.Cells[1, 4].Value = "Max";
            eventAssignmentsSheet.Cells[1, 5].Value = "Users";

            var sum = 0;
            int e = 0;
            for (e = 0; e < _algorithm.Assignments.Count; e++)
            {
                var assignment = _algorithm.Assignments[e];
                eventAssignmentsSheet.Cells[e + 2, 1].Value = e + 1;
                eventAssignmentsSheet.Cells[e + 2, 2].Value = _algorithm.EventCapacity[e].Min;
                eventAssignmentsSheet.Cells[e + 2, 3].Value = assignment.Count;
                sum += assignment.Count;
                eventAssignmentsSheet.Cells[e + 2, 4].Value = _algorithm.EventCapacity[e].Max;
                for (int j = 0; j < assignment.Count; j++)
                {
                    eventAssignmentsSheet.Cells[e + 2, j + 5].Value = assignment[j] + 1;
                }
            }
            eventAssignmentsSheet.Cells[e + 2, 3].Value = sum;
            eventAssignmentsSheet.Cells[eventAssignmentsSheet.Dimension.Address].AutoFitColumns();

            var userGainsSheet = excel.Workbook.Worksheets.Add("User Gains");
            userGainsSheet.Cells[1, 1].Value = "User";
            userGainsSheet.Cells[1, 2].Value = "Event";
            userGainsSheet.Cells[1, 3].Value = "Innate Gain";
            userGainsSheet.Cells[1, 4].Value = "Social Gain";
            userGainsSheet.Cells[1, 5].Value = "Total Gain";

            int u = 0;
            foreach (var user in _algorithm.AllUsers)
            {
                var userWelfare = _algorithm.CalculateSocialWelfare(_algorithm.Assignments, user);
                userGainsSheet.Cells[u + 2, 1].Value = user + 1;
                userGainsSheet.Cells[u + 2, 2].Value = _algorithm.UserAssignments[user];
                userGainsSheet.Cells[u + 2, 3].Value = userWelfare.InnateWelfare;
                userGainsSheet.Cells[u + 2, 4].Value = userWelfare.SocialWelfare;
                userGainsSheet.Cells[u + 2, 5].Value = userWelfare.TotalWelfare;
                u++;
            }
            userGainsSheet.Cells[userGainsSheet.Dimension.Address].AutoFitColumns();

            if (_algorithm.ExtrovertIndeces != null)
            {
                var extrovertIndecesSheet = excel.Workbook.Worksheets.Add("Extrovert Indeces");
                extrovertIndecesSheet.Cells[1, 1].Value = @"User";
                extrovertIndecesSheet.Cells[1, 2].Value = "Index";
                foreach (var user in _algorithm.AllUsers)
                {
                    extrovertIndecesSheet.Cells[user + 2, 1].Value = user;
                    extrovertIndecesSheet.Cells[user + 2, 2].Value = _algorithm.ExtrovertIndeces[user];
                }
                extrovertIndecesSheet.Cells[extrovertIndecesSheet.Dimension.Address].AutoFitColumns();
            }

            excel.Save();
        }

        public void PrintToText(List<UserEvent> result, Welfare welfare, FileInfo output)
        {
            var dir = Directory.CreateDirectory(output.FullName);
            var usereventFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.InnateAffinity));
            foreach (var user in _algorithm.AllUsers)
            {
                foreach (var @event in _algorithm.AllEvents)
                {

                    usereventFile.WriteLine("{0},{1},{2}", user + 1, @event + 1, _algorithm.InAffinities[user][@event]);
                }
            }
            usereventFile.Close();

            var socialAffinityFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.SocialAffinity));
            foreach (var user1 in _algorithm.AllUsers)
            {
                foreach (var user2 in _algorithm.AllUsers)
                {
                    socialAffinityFile.WriteLine("{0},{1},{2}", user1 + 1, user2 + 1, _algorithm.SocAffinities[user1, user2]);
                }
            }
            socialAffinityFile.Close();

            if (_algorithm.ExtrovertIndeces != null)
            {
                var extrovertIndecesFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.ExtrovertIndeces));
                foreach (var user1 in _algorithm.AllUsers)
                {
                    extrovertIndecesFile.WriteLine("{0},{1}", user1 + 1, _algorithm.ExtrovertIndeces[user1]);
                }
                extrovertIndecesFile.Close();
            }

            var cardinalitiesFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.Cardinality));
            for (int i = 0; i < _algorithm.EventCapacity.Count; i++)
            {
                var cap = _algorithm.EventCapacity[i];
                cardinalitiesFile.WriteLine("{0},{1},{2}", i + 1, cap.Min, cap.Max);
            }
            cardinalitiesFile.Close();

            var assignmentsFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.UserAssignment));
            for (int i = 0; i < result.Count; i++)
            {
                var userEvent = result[i];
                if (userEvent.Event >= 0)
                {
                    assignmentsFile.WriteLine("{0},{1}", userEvent.User + 1, userEvent.Event + 1);
                }
            }
            assignmentsFile.Close();

            var welfareFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.Welfare));
            welfareFile.WriteLine("{0},{1}", "Total Welfare", welfare.TotalWelfare);
            welfareFile.WriteLine("{0},{1}", "Innate Welfare", welfare.InnateWelfare);
            welfareFile.WriteLine("{0},{1}", "Social Welfare", welfare.SocialWelfare);
            welfareFile.Close();


            var regRatiosFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.RegretRatio));
            var ratios = _algorithm.CalcRegRatios(_algorithm.AllUsers);
            foreach (var ratio in ratios)
            {
                regRatiosFile.WriteLine("{0},{1}", ratio.Key, ratio.Value);
            }

            regRatiosFile.Close();

            _algorithm.Conf.PrintToText(dir, _algorithm._watches);

            var eventAssignmentsFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.EventAssignment));
            int e = 0;
            for (e = 0; e < _algorithm.Assignments.Count; e++)
            {
                var assignment = _algorithm.Assignments[e];
                eventAssignmentsFile.Write("{0},{1},{2},{3}", e + 1, _algorithm.EventCapacity[e].Min, assignment.Count, _algorithm.EventCapacity[e].Max);

                for (int j = 0; j < assignment.Count; j++)
                {
                    eventAssignmentsFile.Write(",{0}", assignment[j] + 1);
                }
                eventAssignmentsFile.Write(Environment.NewLine);
            }
            eventAssignmentsFile.Close();

            var userGainFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.UserGain));
            foreach (var user in _algorithm.AllUsers)
            {
                var userWelfare = _algorithm.CalculateSocialWelfare(_algorithm.Assignments, user);
                userGainFile.WriteLine("{0},{1},{2},{3},{4}", user + 1, _algorithm.UserAssignments[user], userWelfare.InnateWelfare, userWelfare.SocialWelfare, userWelfare.TotalWelfare);
            }
            userGainFile.Close();
        }
    }
}