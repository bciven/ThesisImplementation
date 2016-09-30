using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public static class OutputFiles
    {
        public static string InnateAffinity
        {
            get { return "User Event Innate Affinities.csv"; }
        }

        public static string SocialAffinity
        {
            get { return "User User Social Affinity.csv"; }
        }

        public static string Cardinality
        {
            get { return "Event Min Max Cardinality.csv"; }
        }

        public static string UserAssignment
        {
            get { return "User Event Assignment.csv"; }
        }

        public static string Welfare
        {
            get { return "Welfare.csv"; }
        }

        public static string RegretRatio
        {
            get { return "User Regret Ratio.csv"; }
        }

        public static string EventAssignment
        {
            get { return "Event Min Count Max Users.csv"; }
        }

        public static string UserGain
        {
            get { return "User Event Innate Social Total Gain.csv"; }
        }

        public static string Parameters
        {
            get { return "Parameters.csv"; }
        }

        public static string Configs
        {
            get { return "Configs.csv"; }
        }
    }
}
