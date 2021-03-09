/// Name: VolatileProcess.cs
/// Description:
/// Authors: Multiple.
/// Copyright: Garry Sotnik

using SOSIEL.Enums;
using SOSIEL.Exceptions;

namespace SOSIEL.Processes
{
    public abstract class VolatileProcess
    {
        protected abstract void EqualToOrAboveFocalValue();
        protected abstract void Maximize();
        protected abstract void Minimize();
        protected abstract void MaintainAtValue();

        protected void SpecificLogic(GoalType goalType)
        {
            switch (goalType)
            {
                case GoalType.EqualToOrAboveFocalValue:
                    {
                        EqualToOrAboveFocalValue();
                        break;
                    }
               case GoalType.Maximize:
                    {
                        Maximize();
                        break;
                    }
                case GoalType.Minimize:
                    {
                        Minimize();
                        break;
                    }
                case GoalType.MaintainAtValue:
                    {
                        MaintainAtValue();
                        break;
                    }
                default:
                    throw new SosielAlgorithmException("Unknown managing of goal");
            }
        }
    }
}
