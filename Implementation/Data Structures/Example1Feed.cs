using System;
using System.Collections.Generic;
using System.Linq;

namespace Implementation.Data_Structures
{
    public class Example1Feed : IDataFeed
    {
        private const int NumberOfUsers = 8;
        private const int NumberOfEvents = 3;

        public List<Cardinality> GenerateCapacity(List<int> users, List<int> events)
        {
            if (users.Count != NumberOfUsers || events.Count != NumberOfEvents)
            {
                throw new Exception("This method only supports 3 users and 2 events");
            }

            var result = new List<Cardinality>
            {
                new Cardinality(2, 3),
                new Cardinality(3, 3),
                new Cardinality(3, 3)
            };

            return result;
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            if (users.Count != NumberOfUsers || events.Count != NumberOfEvents)
            {
                throw new Exception("This method only supports 3 users and 2 events");
            }
            var usersInterests = new List<List<double>>();
                                                      /*X     Y    Z*/
            usersInterests.Add(new List<double>() /*a*/{1,    1,   0});
            usersInterests.Add(new List<double>() /*b*/{1,    0,   0});
            usersInterests.Add(new List<double>() /*c*/{0,    0.4, 1});
            usersInterests.Add(new List<double>() /*d*/{0,    0,   0});
            usersInterests.Add(new List<double>() /*e*/{0,    0,   0});
            usersInterests.Add(new List<double>() /*f*/{0,    0,   0});
            usersInterests.Add(new List<double>() /*g*/{0,    0,   0});
            usersInterests.Add(new List<double>() /*h*/{0,    0,   0});

            return usersInterests;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            if (users.Count != NumberOfUsers)
            {
                throw new Exception("This method only supports 3 users");
            }

            var usersInterests = new double[,]
            {
                    /*a     b    c    d    e   f   g   h*/
                /*a*/{0,    0,   0,   0,   0,  0,  0,  0},
                /*b*/{0,    0,   0,   0,   0,  0,  0,  0},
                /*c*/{0.8,  0,   0,   0,   0,  0,  0,  0},
                /*d*/{0,    0,   1,   0,   0,  0,  0,  0},
                /*e*/{0,    0,   1,   0,   0,  0,  0,  0},
                /*f*/{0,    0,   0,   0,   0,  0,  0,  0},
                /*g*/{0,    0,   0,   0,   0,  0,  0,  0},
                /*h*/{0,    0,   0,   0,   0,  0,  0,  0}
            };

            return usersInterests;
        }


        public void GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount)
        {
            usersCount = NumberOfUsers;
            eventsCount = NumberOfEvents;
        }
    }
}
