﻿using Doki.Extensions;
using RenDisco;
using RenpyParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.RenpyUtils
{
    public class RenpyCondition
    {
        public int BeginningIndex = 0;
        public int EndingIndex = 0;
        public IfCondition IfCondition { get; set; }
        public List<ElifCondition> ElifConditions { get; set; } = new List<ElifCondition>();
        public ElseCondition ElseCondition { get; set; }

        public RenpyCondition(int beginningIndex = 0, IfCondition ifCondition = null, List<ElifCondition> elifConditions = null, ElseCondition elseCondition = null)
        {
            BeginningIndex = beginningIndex;
            IfCondition = ifCondition;
            ElifConditions = elifConditions ?? new List<ElifCondition>();
            ElseCondition = elseCondition;

            int totalConditionContentCount = 0;

            if (IfCondition != null)
                totalConditionContentCount += IfCondition.Content.Count() + 1;

            foreach (var elifCondition in ElifConditions)
                totalConditionContentCount += elifCondition.Content.Count() + 1;

            if (ElseCondition != null)
                totalConditionContentCount += ElseCondition.Content.Count() + 1;

            EndingIndex = BeginningIndex + totalConditionContentCount;
        }
    }
}