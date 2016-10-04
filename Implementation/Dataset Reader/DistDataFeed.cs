using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Implementation.Data_Structures;
using MathNet.Numerics.Distributions;

namespace Implementation.Dataset_Reader
{
    public class DistDataFeed : IDataFeed
    {
        private readonly DistDataParams _distDataParams;
        private readonly Normal _maxGenerator;
        private readonly Normal _innateNormalRandomGenerator;
        private readonly Normal _socialNormalRandomGenerator;
        private Random _rand;

        //public DistDataFeed()
        //{
        //    _rand = new Random();
        //    _capmean = 20;
        //    _capstddev = 10;
        //}

        public DistDataFeed(DistDataParams distDataParams)
        {
            _distDataParams = distDataParams;
            _rand = new Random();
            _maxGenerator = Normal.WithMeanVariance(_distDataParams.CapacityMean, _distDataParams.CapacityVariance, _rand);
            _innateNormalRandomGenerator = Normal.WithMeanVariance(1.5, 3, _rand);
            _socialNormalRandomGenerator = Normal.WithMeanVariance(1.5, 3, _rand);
        }

        private Graph GenerateEventGraph(int userNumber, int eventNumber)
        {
            var graph = new Graph(userNumber);
            var rand = new Random();
            for (int i = 0; i < userNumber; i++)
            {
                graph.Edges.Add(i, new List<int>(eventNumber));
                for (int j = 0; j < eventNumber; j++)
                {
                    if (rand.Next(1, 101) <= _distDataParams.EventInterestPerct)
                    {
                        graph.Edges[i].Add(j);
                    }
                }
            }
            return graph;
        }

        private Graph GenerateSocialGraph(int userCount)
        {
            switch (_distDataParams.SocialNetworkModel)
            {
                case SocialNetworkModel.PowerLawModel:
                    return PowerLawModel(userCount);
                case SocialNetworkModel.BarabasiAlbertModel:
                    return BarabasiAlbertModel(userCount);
                case SocialNetworkModel.ErdosModel:
                    return ErdosModel(userCount);
                case SocialNetworkModel.ClusteredRandom:
                    return ClusteredRandomModel(userCount);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Graph BarabasiAlbertModel(int userCount)
        {
            List<string> lines;
            using (WebClient client = new WebClient())
            {
                var ip = ConfigurationManager.AppSettings["IP"];
                var csv = client.DownloadString(ip + $"/barabasialbert/{userCount}");
                lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var graph = CreateGraph(lines);
            return graph;
        }

        private Graph ClusteredRandomModel(int userCount)
        {
            List<string> lines;
            using (WebClient client = new WebClient())
            {
                var ip = ConfigurationManager.AppSettings["IP"];
                var csv = client.DownloadString(ip + $"/clusteredrandom/{userCount}");
                lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            //var lines = File.ReadAllLines("graph.csv");

            var graph = CreateGraph(lines);
            return graph;
        }

        private static Graph CreateGraph(List<string> lines)
        {
            Graph graph = new Graph();

            foreach (var line in lines)
            {
                var edge = line.Split(new[] { ',' });
                int nodeA = Convert.ToInt32(edge[0]) - 1;
                int nodeB = Convert.ToInt32(edge[1]) - 1;
                if (nodeA == nodeB)
                {
                    continue;
                }

                if (!graph.Edges.ContainsKey(nodeA))
                {
                    graph.Edges.Add(nodeA, new List<int>());
                }
                graph.Edges[nodeA].Add(nodeB);
            }
            return graph;
        }

        private Graph ErdosModel(int userCount)
        {
            Graph graph = new Graph();
            var rand = new Random();
            for (int nodeA = 0; nodeA < userCount; nodeA++)
            {
                for (int nodeB = 0; nodeB < userCount; nodeB++)
                {
                    if (nodeA != nodeB)
                    {
                        if (rand.NextDouble() <= _distDataParams.SocialNetworkDensity)
                        {
                            if (!graph.Edges.ContainsKey(nodeA))
                            {
                                graph.Edges.Add(nodeA, new List<int>());
                            }
                            if (!graph.Edges.ContainsKey(nodeB))
                            {
                                graph.Edges.Add(nodeB, new List<int>());
                            }
                            graph.Edges[nodeA].Add(nodeB);
                            graph.Edges[nodeB].Add(nodeA);
                        }
                    }
                }
            }

            return graph;
        }

        //private Graph ErdosModel(int userCount)
        //{
        //    List<string> lines;
        //    using (WebClient client = new WebClient())
        //    {
        //        var ip = ConfigurationManager.AppSettings["IP"];
        //        var csv = client.DownloadString(ip + $"/erdos/{userCount}" + "/" + _distDataParams.SocialNetworkDensity + "/false");
        //        lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        //    }

        //    //var lines = File.ReadAllLines("graph.csv");

        //    Graph graph = new Graph();

        //    foreach (var line in lines)
        //    {
        //        var edge = line.Split(new[] { ',' });
        //        int nodeA = Convert.ToInt32(edge[0]) - 1;
        //        int nodeB = Convert.ToInt32(edge[1]) - 1;
        //        if (nodeA == nodeB)
        //        {
        //            continue;
        //        }

        //        if (!graph.Edges.ContainsKey(nodeA))
        //        {
        //            graph.Edges.Add(nodeA, new List<int>());
        //        }
        //        if (!graph.Edges.ContainsKey(nodeB))
        //        {
        //            graph.Edges.Add(nodeB, new List<int>());
        //        }
        //        graph.Edges[nodeA].Add(nodeB);
        //        graph.Edges[nodeB].Add(nodeA);
        //    }
        //    return graph;
        //}

        private static Graph PowerLawModel(int userCount)
        {
            List<string> lines;
            using (WebClient client = new WebClient())
            {
                var ip = ConfigurationManager.AppSettings["IP"];
                var csv = client.DownloadString(ip + $"/powerlaw/{userCount}");
                lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            //var lines = File.ReadAllLines("graph.csv");

            Graph graph = new Graph();

            foreach (var line in lines)
            {
                var edge = line.Split(new[] { ',' });
                int nodeA = Convert.ToInt32(edge[0]) - 1;
                int nodeB = Convert.ToInt32(edge[1]) - 1;
                if (nodeA == nodeB)
                {
                    continue;
                }

                if (!graph.Edges.ContainsKey(nodeA))
                {
                    graph.Edges.Add(nodeA, new List<int>());
                }
                if (!graph.Edges.ContainsKey(nodeB))
                {
                    graph.Edges.Add(nodeB, new List<int>());
                }
                graph.Edges[nodeA].Add(nodeB);
                graph.Edges[nodeB].Add(nodeA);
            }
            return graph;
        }

        public List<Cardinality> GenerateCapacity(List<int> users, List<int> events)
        {
            var result = events.Select(x =>
            {
                var end = GenerateMaxCapacity(1, users.Count);
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
            var ground = min;
            var rand = new Random();
            switch (_distDataParams.MinCardinalityOption)
            {
                case MinCardinalityOptions.Half:
                    ground = Convert.ToInt32(Math.Floor((double)max / 2));
                    break;
                case MinCardinalityOptions.Fourth:
                    ground = max - Convert.ToInt32(Math.Floor((double)max / 4));
                    break;
                case MinCardinalityOptions.Eighth:
                    ground = max - Convert.ToInt32(Math.Floor((double)max / 8));
                    break;
                case MinCardinalityOptions.Min:
                    return min;
                case MinCardinalityOptions.Random:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return rand.Next(ground, max);
        }

        public List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events)
        {
            var usersInterests = new List<List<double>>();
            var eventGraph = GenerateEventGraph(users.Count, events.Count);
            foreach (var user in users)
            {
                var userInterests = new List<double>();
                foreach (var @event in events)
                {
                    if (eventGraph.Edges[user].Contains(@event))
                    {
                        //double r = 1.0 / Math.Pow(1 - _rand.NextDouble(), 1.5);
                        //r = Math.Round(r, 2);
                        userInterests.Add(GenerateInnateAffinity(0));
                    }
                    else
                    {
                        userInterests.Add(0);
                    }
                }
                usersInterests.Add(userInterests);
            }
            return usersInterests;
        }

        public double[,] GenerateSocialAffinities(List<int> users)
        {
            var socialNetworkGraph = GenerateSocialGraph(users.Count);
            var usersInterests = new double[users.Count, users.Count];

            foreach (var edge in socialNetworkGraph.Edges)
            {
                var nodeA = edge.Key;
                foreach (var nodeB in edge.Value)
                {
                    var r = GenerateSocialAffinity(0);
                    //r = Math.Round(r, 2);
                    usersInterests[nodeA, nodeB] = r;
                    if (_distDataParams.SocialNetworkModel != SocialNetworkModel.BarabasiAlbertModel)
                    {
                        usersInterests[nodeB, nodeA] = r;
                    }
                }
            }

            return usersInterests;
        }

        void IDataFeed.GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount)
        {
            throw new NotImplementedException();
        }

        private double GenerateInnateAffinity(double minimum)
        {
            //var normalDist = _innateNormalRandomGenerator.Sample();
            //return normalDist < Math.Pow(10, -5) ? minimum : normalDist;

            double u1 = _rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = _rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            var variance = Math.Sqrt(3);
            double normalDist = 1.5 + variance * randStdNormal; //random normal(mean,stdDev^2)
            return normalDist < Math.Pow(10, -5) ? minimum : normalDist;
        }

        private double GenerateSocialAffinity(double minimum)
        {
            //var normalDist = _socialNormalRandomGenerator.Sample();
            //return normalDist < Math.Pow(10, -5) ? minimum : normalDist;
            double u1 = _rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = _rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            var variance = Math.Sqrt(3);
            double normalDist = 1.5 + variance * randStdNormal; //random normal(mean,stdDev^2)
            return normalDist < Math.Pow(10, -5) ? minimum : normalDist;
        }


        private int GenerateMaxCapacity(int minimum, int numberOfUsers)
        {
            if (_distDataParams.MaxCardinalityOption == MaxCardinalityOptions.Random)
            {
                var normalDist = _maxGenerator.Sample();
                var notmalDistInt = Convert.ToInt32(Math.Floor(normalDist));
                return notmalDistInt < minimum ? minimum : notmalDistInt;
            }

            if (_distDataParams.MaxCardinalityOption == MaxCardinalityOptions.Max)
            {
                return numberOfUsers;
            }

            throw new NotImplementedException("Max Cardinality Unknown");
        }
    }
}
