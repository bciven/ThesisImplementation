﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class AlgorithmSpec
    {
        public ReassignmentEnum Reassignment { get; set; }

        public enum ReassignmentEnum
        {
            None = 0,
            Default = 1,
            Addition = 2,
            Reduction = 3,
            Greedy = 4,
            Power2Reduction = 5
        }

        public int? TakeChanceLimit { get; set; }
        public CommunityFixEnum CommunityFix { get; set; }

        public AlgorithmEnum Algorithm { get; set; }
        public bool DeficitFix { get; set; }
        public bool DoublePriority { get; set; }
        public bool LazyAdjustment { get; set; }
        public bool Swap { get; set; }
        public double SwapThreshold { get; set; }
        public double PreservePercentage { get; set; }

        public enum AlgorithmEnum
        {
            DG,
            PADG,
            PCADG,
            IRC,
            IR,
            Random,
            RandomPlus,
            OG,
            COG
        }
    }
}
