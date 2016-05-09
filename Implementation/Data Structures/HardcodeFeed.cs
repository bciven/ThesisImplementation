using System;
using System.Collections.Generic;
using System.Linq;

namespace Implementation.Data_Structures
{
    public class HardcodeFeed : IDataFeed
    {
        public List<Cardinality> GenerateCapacity(List<int> events, int numberOfUsers, int numberOfEvents)
        {
            if (numberOfUsers != 5 || numberOfEvents != 3)
            {
                throw new Exception("This method only supports 3 users and 2 events");
            }

            var result = new List<Cardinality>
            {
                new Cardinality(2, 3),
                new Cardinality(3, 3),
                new Cardinality(1, 3)
            };

            return result;
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            if (users.Count != 5 || events.Count != 3)
            {
                throw new Exception("This method only supports 3 users and 2 events");
            }
            var usersInterests = new List<List<double>>();
                                                      /*X     Y    Z*/
            usersInterests.Add(new List<double>() /*a*/{1,    1,   0});
            usersInterests.Add(new List<double>() /*b*/{1,    0,   0});
            usersInterests.Add(new List<double>() /*c*/{-0.2, 0.4, 1});
            usersInterests.Add(new List<double>() /*d*/{0,    0,   0});
            usersInterests.Add(new List<double>() /*e*/{0,    0,   0});

            return usersInterests;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            if (users.Count != 5)
            {
                throw new Exception("This method only supports 3 users");
            }

            var usersInterests = new double[,]
            {
                    /*a     b    c    d    e*/
                /*a*/{0,    0,   0,   0,   0},
                /*b*/{0,    0,   0,   0,   0},
                /*c*/{0.8,  0,   0,   0,   0},
                /*d*/{0,    0,   0,   0,   0},
                /*e*/{0,    0,   0,   0,   0}
            };

            return usersInterests;
        }
    }
}
