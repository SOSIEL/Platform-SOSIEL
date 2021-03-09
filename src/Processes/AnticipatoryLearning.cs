/// Name: SosielVariables.cs
/// Description: Anticipatory learning is the first out of the learning
///   and decisionmaking processes that is activated in the second and later
///   time periods. the aim of the process is to use the change in the states of
///   goal variables to update the anticipated influences of decision options,
///   assess the success of decision options, and gauge confidence in attaining
///   goals. The process of anticipatory learning consists of the following three
///   subprocesses: (a) update the anticipated influence(s) on goal(s) of the
///   decision option(s) that was/were implemented in the prior period;
///   (b) assess the success of this/these decision option(s) in contributing to
///   the attainment of goal(s); and (c) establish whether, by consequence, the
///   agent is confident or unconfident in attaining the goal(s). The results of
///   anticipatory learning are updated goal-specific anticipated influences and
///   confidence states.
/// Authors: Multiple.
/// Copyright: Garry Sotnik



using System;
using System.Collections.Generic;
using System.Linq;
using SOSIEL.Entities;
using SOSIEL.Enums;
using SOSIEL.Helpers;

namespace SOSIEL.Processes
{
    /// <summary>
    /// Anticipatory learning process implementation.
    /// </summary>
    public class AnticipatoryLearning<TDataSet> : VolatileProcess
    {
        Goal currentGoal;
        GoalState currentGoalState;

        #region Specific logic for tendencies
        protected override void EqualToOrAboveFocalValue()
        {
            if (currentGoalState.Value >= currentGoalState.FocalValue
                 || currentGoalState.Value < currentGoalState.FocalValue
                     && currentGoalState.Value > currentGoalState.PriorValue
                     && currentGoalState.DiffCurrentAndPrior >= currentGoalState.DiffPriorAndTwicePrior)
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Stay;
                currentGoalState.Confidence = true;
            }
            else
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Up;
                currentGoalState.Confidence = false;
            }
        }

        protected override void Maximize()
        {
            if (currentGoalState.Value == currentGoalState.FocalValue
               || currentGoalState.Value > currentGoalState.PriorValue && currentGoalState.DiffCurrentAndPrior >= currentGoalState.DiffPriorAndTwicePrior)
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Stay;
                currentGoalState.Confidence = true;
            }
            else
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Up;
                currentGoalState.Confidence = false;
            }
        }

        protected override void Minimize()
        {
            if (currentGoalState.Value == currentGoalState.FocalValue
                || currentGoalState.Value < currentGoalState.PriorValue && currentGoalState.PriorValue - currentGoalState.Value >= currentGoalState.TwicePriorValue - currentGoalState.PriorValue)
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Stay;
                currentGoalState.Confidence = true;
            }
            else
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Down;
                currentGoalState.Confidence = false;
            }
        }

        protected override void MaintainAtValue()
        {
            if (currentGoalState.Value == currentGoal.FocalValue
                || Math.Abs(currentGoalState.Value - currentGoalState.FocalValue) < Math.Abs(currentGoalState.PriorValue - currentGoalState.PriorFocalValue))
            {
                currentGoalState.AnticipatedDirection = AnticipatedDirection.Stay;
                currentGoalState.Confidence = true;
            }
            else
            {
                currentGoalState.AnticipatedDirection = currentGoalState.DiffCurrentAndFocal > 0
                    ? AnticipatedDirection.Down
                    : AnticipatedDirection.Up;
                currentGoalState.Confidence = false;
            }
        }

        #endregion


        /// <summary>
        /// Executes anticipatory learning for specific agent and returns sorted by priority goals array
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="lastIteration"></param>
        /// <returns></returns>
        public void Execute(IAgent agent, LinkedListNode<Dictionary<IAgent, AgentState<TDataSet>>> lastIteration)
        {
            AgentState<TDataSet> currentIterationAgentState = lastIteration.Value[agent];
            AgentState<TDataSet> previousIterationAgentState = lastIteration.Previous.Value[agent];

            foreach (var goal in agent.AssignedGoals)
            {
                currentGoal = goal;
                currentGoalState = currentIterationAgentState.GoalsState[goal];
                var previousGoalState = previousIterationAgentState.GoalsState[goal];

                currentGoalState.Value = agent[goal.ReferenceVariable];
                currentGoalState.PriorValue = previousGoalState.Value;
                currentGoalState.TwicePriorValue = previousGoalState.PriorValue;

                if (goal.ChangeFocalValueOnPrevious)
                {
                    double reductionPercent = 1;

                    if (goal.ReductionPercent > 0d)
                        reductionPercent = goal.ReductionPercent;

                    currentGoalState.FocalValue = reductionPercent * currentGoalState.PriorValue;
                }

                currentGoalState.FocalValue = string.IsNullOrEmpty(goal.FocalValueReference) ? currentGoalState.FocalValue : agent[goal.FocalValueReference];

                currentGoalState.DiffCurrentAndFocal = currentGoalState.Value - currentGoalState.FocalValue;

                currentGoalState.DiffPriorAndFocal = currentGoalState.PriorValue - currentGoalState.FocalValue;

                currentGoalState.DiffCurrentAndPrior = currentGoalState.Value - currentGoalState.PriorValue;

                currentGoalState.DiffPriorAndTwicePrior = currentGoalState.PriorValue - previousGoalState.PriorValue;

                double anticipatedInfluence = 0;

                if (goal.IsCumulative)
                {
                    anticipatedInfluence = currentGoalState.Value - currentGoalState.PriorValue;
                }
                else
                {
                    anticipatedInfluence = currentGoalState.Value;
                }

                currentGoalState.AnticipatedInfluenceValue = anticipatedInfluence;


                //finds activated decision option for each site
                IEnumerable<DecisionOption> activatedInPriorIteration = previousIterationAgentState.DecisionOptionsHistories.SelectMany(rh => rh.Value.Activated);

                //update anticipated influences of found decision option
                activatedInPriorIteration.ForEach(r =>
                {
                    agent.AnticipationInfluence[r][goal] = anticipatedInfluence;
                });

                SpecificLogic(goal.Type);
            }

            //return SortByImportance(agent, currentIterationAgentState.GoalsState).ToArray();
        }
    }
}
