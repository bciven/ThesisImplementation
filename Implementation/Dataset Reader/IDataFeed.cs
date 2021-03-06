﻿using System.Collections.Generic;
using Implementation.Data_Structures;

namespace Implementation.Dataset_Reader
{
    public interface IDataFeed
    {
        List<Cardinality> GenerateCapacity(List<int> users, List<int> events);

        List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events);

        double[,] GenerateSocialAffinities(List<int> users);

        List<double> GenerateExtrovertIndeces(List<int> users, double[,] socialAffinities);
        void GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount);
    }
}
