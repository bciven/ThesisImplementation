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
        private readonly Random _extrovertIndexGenerator;
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
            _extrovertIndexGenerator = new Random();
        }

        private UndirectedGraph GenerateEventGraph(int userNumber, int eventNumber)
        {
            var graph = new UndirectedGraph(userNumber);
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

        private UndirectedGraph GenerateSocialGraph(int userCount)
        {
            switch (_distDataParams.SocialNetworkModel)
            {
                case SocialNetworkModel.PowerLawModel:
                    return PowerLawModel(userCount);
                case SocialNetworkModel.BarabasiAlbertModel:
                    return BarabasiAlbertModel(userCount);
                case SocialNetworkModel.SymmetricErdosModel:
                    return ErdosModel(userCount, true);
                case SocialNetworkModel.AsymmetricErdosModel:
                    return ErdosModel(userCount, false);
                case SocialNetworkModel.ClusteredRandom:
                    return ClusteredRandomModel(userCount);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private UndirectedGraph BarabasiAlbertModel(int userCount)
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

        private UndirectedGraph ClusteredRandomModel(int userCount)
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

        private static UndirectedGraph CreateGraph(List<string> lines)
        {
            UndirectedGraph undirectedGraph = new UndirectedGraph();

            foreach (var line in lines)
            {
                var edge = line.Split(new[] { ',' });
                int nodeA = Convert.ToInt32(edge[0]) - 1;
                int nodeB = Convert.ToInt32(edge[1]) - 1;
                if (nodeA == nodeB)
                {
                    continue;
                }

                if (!undirectedGraph.Edges.ContainsKey(nodeA))
                {
                    undirectedGraph.Edges.Add(nodeA, new List<int>());
                }
                undirectedGraph.Edges[nodeA].Add(nodeB);
            }
            return undirectedGraph;
        }

        private UndirectedGraph ErdosModel(int userCount, bool symmetric)
        {
            UndirectedGraph undirectedGraph = new UndirectedGraph();
            var rand = new Random();
            for (int nodeA = 0; nodeA < userCount; nodeA++)
            {
                for (int nodeB = 0; nodeB < userCount; nodeB++)
                {
                    if (nodeA != nodeB)
                    {
                        if (rand.NextDouble() <= _distDataParams.SocialNetworkDensity)
                        {
                            if (!undirectedGraph.Edges.ContainsKey(nodeA))
                            {
                                undirectedGraph.Edges.Add(nodeA, new List<int>());
                            }
                            if (!undirectedGraph.Edges.ContainsKey(nodeB))
                            {
                                undirectedGraph.Edges.Add(nodeB, new List<int>());
                            }
                            undirectedGraph.Edges[nodeA].Add(nodeB);
                            if (symmetric)
                            {
                                undirectedGraph.Edges[nodeB].Add(nodeA);
                            }
                        }
                    }
                }
            }

            return undirectedGraph;
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

        private static UndirectedGraph PowerLawModel(int userCount)
        {
            List<string> lines;
            var minDegree = 1;
            using (WebClient client = new WebClient())
            {
                var ip = ConfigurationManager.AppSettings["IP"];
                var csv = client.DownloadString(ip + $"/powerlaw/{userCount}/{minDegree}");
                lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            //var lines = File.ReadAllLines("graph.csv");

            UndirectedGraph undirectedGraph = new UndirectedGraph();

            foreach (var line in lines)
            {
                var edge = line.Split(new[] { ',' });
                int nodeA = Convert.ToInt32(edge[0]) - 1;
                int nodeB = Convert.ToInt32(edge[1]) - 1;
                if (nodeA == nodeB)
                {
                    continue;
                }

                if (!undirectedGraph.Edges.ContainsKey(nodeA))
                {
                    undirectedGraph.Edges.Add(nodeA, new List<int>());
                }
                if (!undirectedGraph.Edges.ContainsKey(nodeB))
                {
                    undirectedGraph.Edges.Add(nodeB, new List<int>());
                }
                undirectedGraph.Edges[nodeA].Add(nodeB);
                undirectedGraph.Edges[nodeB].Add(nodeA);
            }
            return undirectedGraph;
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
                    Max = end,
                    Event = x
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
                    if (_distDataParams.SocialNetworkModel != SocialNetworkModel.BarabasiAlbertModel
                        && _distDataParams.SocialNetworkModel != SocialNetworkModel.AsymmetricErdosModel)
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

        private double GenerateExtrovertionIndex()
        {
            return _extrovertIndexGenerator.NextDouble();
        }

        public List<double> GenerateExtrovertIndeces(List<int> users)
        {
            var extrovertIndeces = new List<double>();
            foreach (var user in users)
            {
                extrovertIndeces.Add(GenerateExtrovertionIndex());
            }
            return extrovertIndeces;
        }
    }
}
