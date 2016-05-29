using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;

namespace Implementation.Data_Structures
{
    public class DistDataFeed : IDataFeed
    {
        private readonly Random _rand;

        public DistDataFeed()
        {
            _rand = new Random();
        }

        public List<Cardinality> GenerateCapacity(List<int> users, List<int> events)
        {
            var result = events.Select(x =>
            {
                var end = GenerateMaxCapacity(1);
                var start = GenerateMinCapacity(1, end);
                var c = new Cardinality
                {
                    Min = start,
                    Max = end
                };
                return c;
            }).ToList();

            return result;
        }

        private int GenerateMinCapacity(int min, int max)
        {
            return _rand.Next(min, max);
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            var usersInterests = new List<List<double>>();
            foreach (var user in users)
            {
                var userInterests = new List<double>();
                foreach (var @event in events)
                {
                    double r = 1.0 / Math.Pow(1 - _rand.NextDouble(), 1.5);
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
                        var r = GenerateRandom(0);
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

        void IDataFeed.GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount)
        {
            throw new NotImplementedException();
        }

        private double GenerateRandom(double minimum)
        {
            var normalDist = Normal.WithMeanStdDev(1.5, 3, _rand).Sample();
            return normalDist < Math.Pow(10, -5) ? minimum : normalDist;
        }


        private int GenerateMaxCapacity(int minimum)
        {
            var normalDist = MathNet.Numerics.Distributions.Normal.WithMeanStdDev(20, 10, _rand).Sample();
            var notmalDistInt = Convert.ToInt32(Math.Floor(normalDist));
            return notmalDistInt < minimum ? minimum : notmalDistInt;
        }
    }
}
