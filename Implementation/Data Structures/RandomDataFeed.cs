using System;
using System.Collections.Generic;
using System.Linq;

namespace Implementation.Data_Structures
{
    public class RandomDataFeed : IDataFeed
    {
        private readonly Random _rand;

        public RandomDataFeed()
        {
            _rand = new Random();
        }

        public List<Cardinality> GenerateCapacity(List<int> events, int numberOfUsers, int numberOfEvents)
        {
            var result = events.Select(x =>
            {
                var n = numberOfUsers / numberOfEvents;
                var s = GenerateRandom(1, n);
                var l = GenerateRandom(1, n);
                var c = new Cardinality
                {
                    Min = s,
                    Max = s + l
                };
                return c;
            }).ToList();

            return result;
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            var usersInterests = new List<List<double>>();
            foreach (var user in users)
            {
                var userInterests = new List<double>();
                foreach (var @event in events)
                {
                    var r = GenerateRandom(0d, 1d);
                    r = Math.Round(r, 2);
                    userInterests.Add(r);
                }
                usersInterests.Add(userInterests);
            }
            return usersInterests;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            var usersInterests = new double[users.Count, users.Count];
            for (int i = 0; i < users.Count; i++)
            {
                var user1 = users[i];
                for (int j = 0; j < i; j++)
                {
                    var user2 = users[j];
                    usersInterests[i, j] = usersInterests[j, i];
                }
                for (int j = i; j < users.Count; j++)
                {
                    var user2 = users[j];
                    if (user1 != user2)
                    {
                        var r = GenerateRandom(0d, 1d);
                        r = Math.Round(r, 2);
                        usersInterests[i, j] = r;
                    }
                    else
                    {
                        usersInterests[i, j] = 0;
                    }
                }
            }
            return usersInterests;
        }

        private double GenerateRandom(double minimum, double maximum)
        {
            return _rand.NextDouble() * (maximum - minimum) + minimum;
        }


        private int GenerateRandom(int minimum, int maximum)
        {
            return _rand.Next(minimum, maximum + 1);
        }
    }
}
