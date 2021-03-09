/// Name: IGoalPrioritizing.cs
/// Description: 
///   Goal prioritizing always follows anticipatory learning. the aim
///   of goal prioritizing is to use what was learned during anticipatory learning
///   to reevaluate the importance levels of goals and, if necessary, reprioritize
///   them. The process of goal prioritizing has a stabilizing effect on agent
///   behavior and has the option of being turned off as a mechanism if its
///   stabilizing effect contradicts reference behavior. The process of goal
///   prioritizing typicallt consists of the following two subprocesses:
///   (a) determine the relative difference between goal value and focal goal value
///   and (b) adjust the proportional importance levels of goals respectively.
///   The result of goal prioritizing is a reevaluated and, if appropriate,
///   a reprioritized set of proportional importance levels.
/// Authors: Multiple.
/// Copyright: Garry Sotnik

using System.Collections.Generic;
using SOSIEL.Entities;

namespace SOSIEL.Processes
{
    /// <summary>
    /// Goal prioritizing process interface.
    /// </summary>
    public interface IGoalPrioritizing
    {
        /// <summary>
        /// Prioritizes agent goals.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="goals">The goals.</param>
        void Prioritize(IAgent agent, IReadOnlyDictionary<Goal, GoalState> goals);
    }
}
