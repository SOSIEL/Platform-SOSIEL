/// Name: Goal.cs
/// Description:
/// Authors: Multiple.
/// Copyright: Garry Sotnik

﻿using System;

using SOSIEL.Enums;

namespace SOSIEL.Entities
{
    public class Goal: IEquatable<Goal>
    {
        public int Index { get; set; }

        public string Name { get; set; }

        public GoalType Type { get; set; }

        public string ReferenceVariable { get; set; }

        public double FocalValue { get; set; }

        public bool ChangeFocalValueOnPrevious { get; set; }

        public double ReductionPercent { get; set; }

        public string FocalValueReference { get; set; }

        public bool RankingEnabled { get; set; }

        public bool IsCumulative { get; set; }

        /// <summary>
        /// Initializes a new instance of the Goal class.
        /// </summary>
        public Goal()
        {
            RankingEnabled = true;
        }

        /// <summary>
        /// Compares two Goal objects
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Goal other)
        {
            return ReferenceEquals(this, other)
                || (other != null && Name == other.Name);
        }

        public override bool Equals(object obj)
        {
            //check on equality by object reference or goal name
            return base.Equals(obj) || Equals(obj as Goal);
        }

        public override int GetHashCode()
        {
            // Hash code MUST be implemented, because we use goal as key to Dictionary.
            return Name.GetHashCode();
        }

        public static bool operator ==(Goal a, Goal b)
        {
            if (Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Goal a, Goal b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
