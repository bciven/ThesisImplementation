using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Data_Structures;

namespace Implementation.Dataset_Reader
{
    public class MeetupReader : IReader
    {
        public void CalculateSocialAffinity()
        {
            var eventGroupFile = @"D:\Graphs\user_tag.csv";
            var userTags = ReadGroups(eventGroupFile);
            var userEventsFile = @"D:\Graphs\users_events2.csv";
            var userEvents = ReadGroups(userEventsFile);
            var users = userEvents.Select(x => x.EntityId).Distinct().ToList();

            HashSet<int> hashset = new HashSet<int>(users);
            userTags = userTags.Where(x => hashset.Contains(x.EntityId)).ToList();
            Dictionary<int, HashSet<int>> dic = new Dictionary<int, HashSet<int>>();
            var tagsLookup = userTags.ToLookup(x => x.EntityId, x=>x.GroupId);
            foreach (var user in hashset)
            {
                dic.Add(user, new HashSet<int>(tagsLookup[user]));
            }

            var crossedJoin = users.SelectMany(user1 => users, (user1, user2) => new { user1, user2 });
            int i = 0;
            using (var file = new StreamWriter(@"D:\Graphs\user_user.csv"))
            {
                    foreach (var item in crossedJoin)
                    {
                        i++;
                        if (item.user1 != item.user2)
                        {
                            var user1Tags = dic[item.user1];
                            var user2Tags = dic[item.user2];
                            var union = user1Tags.Union(user2Tags).Count();
                            var intersect = user1Tags.Intersect(user2Tags).Count();
                            if (union > 0 && intersect > 0)
                            {
                                double interest = (double)intersect / union;

                                file.WriteLine("{0},{1},{2}", item.user1, item.user2, interest);
                            }
                        }
                    }
            }

        }

        public void FillInnateInterests()
        {
            var eventGroupFile = @"C:\Users\Behrad\Google Drive\Concordia\Thesis\Social Event Organization\Dataset Maker\Datasets\Meetup_network\event_group.csv";
            var userGroupFile = @"C:\Users\Behrad\Google Drive\Concordia\Thesis\Social Event Organization\Dataset Maker\Datasets\Meetup_network\user_group.csv";

            var eventGroups = ReadGroups(eventGroupFile);
            var userGroups = ReadGroups(userGroupFile);

            /*for (int i = 0; i < eventGroups.Count; i++)
            {
                var eventGroup = eventGroups[i];
                for (int j = 0; j < userGroups.Count; j++)
                {
                    var userGroup = userGroups[j];
                    if (eventGroup.GroupId == userGroup.GroupId)
                    {
                        graph.Add(new Tuple<int, int>(userGroup.EntityId, eventGroup.EntityId));
                    }
                }
            }*/
            var result = from eventGroup in eventGroups
                         join userGroup in userGroups
                 on eventGroup.GroupId equals userGroup.GroupId
                         select new
                         {
                             userId = userGroup.EntityId,
                             eventId = eventGroup.EntityId
                         };


            using (var file = new StreamWriter(@"graph.txt"))
            {
                foreach (var edge in result)
                {
                    file.WriteLine(edge.userId + "," + edge.eventId);
                }
            }
        }

        //public void FillInnateInterests()
        //{
        //    var eventLocations = ReadLocations(@"Datasets\Meetup_geo\event_lon_lat.csv");
        //    var userLocations = ReadLocations(@"Datasets\Meetup_geo\user_lon_lat.csv");


        //}

        private static List<Group> ReadGroups(string file)
        {
            List<Group> groups = new List<Group>();
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var values = line.Split(',');
                        var location = new Group();
                        location.EntityId = int.Parse(values[0]);
                        location.GroupId = int.Parse(values[1]);
                        groups.Add(location);
                    }
                }
                reader.Close();
            }
            return groups;
        }

        public void FillSocialInterests()
        {
            throw new NotImplementedException();
        }

        public void FixFile()
        {
            var graphFile = @"D:\graph.txt";

            var file = new StreamWriter(@"D:\userevent.csv");
            using (var reader = new StreamReader(File.OpenRead(graphFile)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var values = line.Split(',');
                        file.WriteLine("\"{0}\",\"{1}\"", values[0], values[1]);
                    }
                }
            }
            file.Close();
        }

        public void CompactFile()
        {
            var graphFile = @"D:\graph.txt";
            int minUser = int.MaxValue;
            int minEvent = int.MaxValue;

            using (var reader = new StreamReader(File.OpenRead(graphFile)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var values = line.Split(',');
                        var userId = int.Parse(values[0]);
                        var eventId = int.Parse(values[1]);

                        if (userId < minUser)
                        {
                            minUser = userId;
                        }
                        if (eventId < minEvent)
                        {
                            minEvent = eventId;
                        }

                    }
                }
            }
            var file = new StreamWriter(@"D:\Graphs\compact.csv");

            using (var reader = new StreamReader(File.OpenRead(graphFile)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var values = line.Split(',');
                        var userId = int.Parse(values[0]);
                        var eventId = int.Parse(values[1]);
                        file.WriteLine("{0},{1}", userId - minUser, eventId - minEvent);
                    }
                }
            }

            file.Close();

        }

        public void SplitFile()
        {
            var graphFile = @"D:\graph.txt";
            int row = 0;
            int group = 0;

            using (var reader = new StreamReader(File.OpenRead(graphFile)))
            {
                var file = new StreamWriter(@"D:\Graphs\graph0.txt");

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (group * 4361670 + row == (group + 1) * 4361670)
                        {
                            group++;
                            file.Close();
                            file = new StreamWriter(@"D:\Graphs\graph" + group + ".txt");
                        }
                        file.WriteLine(line);
                        row++;
                    }
                }
                file.Close();
            }
        }
    }
}
