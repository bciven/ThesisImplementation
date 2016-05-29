using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    interface IDataFeed
    {
        List<Cardinality> GenerateCapacity(List<int> users, List<int> events);

        List<List<double>> GenerateInnateAffinities(List<int> users, List<int> events);

        double[,] GenerateSocialAffinities(List<int> users);

        void GetNumberOfUsersAndEvents(out int usersCount, out int eventsCount);
    }
}
