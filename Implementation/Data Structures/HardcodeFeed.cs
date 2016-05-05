using System;
using System.Collections.Generic;
using System.Linq;

namespace Implementation.Data_Structures
{
    public class HardcodeFeed : IDataFeed
    {
        public List<Cardinality> GenerateCapacity(List<int> events, int numberOfUsers, int numberOfEvents)
        {
            if (numberOfUsers != 3 || numberOfEvents != 2)
            {
                throw new Exception("This method only supports 3 users and 2 events");
            }

            var result = new List<Cardinality>
            {
                new Cardinality(1, 2),
                new Cardinality(2, 2)
            };

            return result;
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            if (users.Count != 3 || events.Count != 2)
            {
                throw new Exception("This method only supports 3 users and 2 events");
            }
            var usersInterests = new List<List<double>>();

            usersInterests.Add(new List<double>() {0.2, 0});
            usersInterests.Add(new List<double>() {0, 0.2});
            usersInterests.Add(new List<double>() {0, 0});

            return usersInterests;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            if (users.Count != 3)
            {
                throw new Exception("This method only supports 3 users");
            }

            var usersInterests = new double[,]
            {
                {0, 0.1, 0},
                {0.1, 0, 0},
                {0, 0, 0}
            };

            return usersInterests;
        }
    }
}
