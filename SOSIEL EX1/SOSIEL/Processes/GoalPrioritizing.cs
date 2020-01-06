﻿using System;
using System.Collections.Generic;
using System.Linq;
using SOSIEL.Entities;
using SOSIEL.Exceptions;
using SOSIEL.Helpers;

namespace SOSIEL.Processes
{
    /// <summary>
    /// Goal prioritizing process implementation.
    /// </summary>
    public class GoalPrioritizing
    {
        /// <summary>
        /// Prioritizes agent goals.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="goals">The goals.</param>
        public void Prioritize(IAgent agent, Dictionary<Goal, GoalState> goals)
        {
            if (goals.Count > 1)
            {
                var importantGoals = goals.Where(kvp => kvp.Value.Importance > 0).ToArray();

                var noConfidenceGoals = importantGoals.Where(kvp => kvp.Value.Confidence == false).ToArray();

                if (noConfidenceGoals.Length > 0 && agent.Archetype.UseImportanceAdjusting)
                {
                    var noConfidenceProportions = noConfidenceGoals.Select(kvp => new
                    {
                        Proportion = kvp.Value.Importance *
                                         (1 + CalculateRelativedDifference(agent, kvp.Key, kvp.Value)),
                        Goal = kvp.Key
                    })
                        .ToArray();

                    var confidenceGoals = goals.Where(kvp => kvp.Value.Confidence).ToArray();

                    double totalConfidenceUnadjustedProportions = confidenceGoals.Sum(kvp => kvp.Value.Importance);

                    double totalNoConfidenceAdjustedProportions = noConfidenceProportions.Sum(p => p.Proportion);

                    var confidenceProportions = confidenceGoals.Select(kvp => new
                    {
                        Proportion = kvp.Value.Importance * (1 - totalNoConfidenceAdjustedProportions) /
                                         totalConfidenceUnadjustedProportions,
                        Goal = kvp.Key
                    })
                        .ToArray();

                    Enumerable.Concat(noConfidenceProportions, confidenceProportions)
                        .ForEach(p =>
                        {
                            goals[p.Goal].AdjustedImportance = p.Proportion;

                        });
                }
                else
                {
                    goals.ForEach(kvp =>
                    {
                        kvp.Value.AdjustedImportance = kvp.Value.Importance;
                    });
                }
            }
        }

        /// <summary>
        /// Calculates normalized value for goal prioritizing.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="goal"></param>
        /// <param name="goalState"></param>
        /// <returns></returns>
        double CalculateRelativedDifference(IAgent agent, Goal goal, GoalState goalState)
        {
            //DecisionOptionLayerConfiguration layerConfiguration = agent.AssignedDecisionOptions
            //    .Where(kh => kh.Consequent.Param == goal.ReferenceVariable &&
            //                 (kh.Layer.LayerConfiguration.ConsequentValueInterval != null &&
            //                  kh.Layer.LayerConfiguration.ConsequentValueInterval.Length == 2))
            //    .Select(kh => kh.Layer.LayerConfiguration)
            //    .FirstOrDefault();

            //if (layerConfiguration != null)
            //{
            if (goal.Tendency == "Maximize" || goal.Tendency == "Minimize")
            {
                var decisionOptions = agent.AssignedDecisionOptions
                    .Where(kh => kh.Consequent.Param == goal.ReferenceVariable).ToList();

                double maxGoalValue = 0;

                if (decisionOptions.Any())
                {
                    maxGoalValue = decisionOptions
                        .Select(kh => string.IsNullOrEmpty(kh.Consequent.VariableValue)
                            ? (double)kh.Consequent.Value
                            : (double)agent[kh.Consequent.VariableValue])
                        .Max();
                }
                else
                {
                    maxGoalValue = string.IsNullOrEmpty(goal.MaxGoalReferenceVariable)
                        ? goal.MaxGoalValue
                        : (double)agent[goal.MaxGoalReferenceVariable];
                }
                
                return Math.Abs(goalState.DiffCurrentAndFocal / (goalState.FocalValue - maxGoalValue));
            }

            if (goal.Tendency == "EqualToOrAboveFocalValue")
            {
                var minGoalValue = 0;

                return Math.Abs(goalState.DiffCurrentAndFocal / (goalState.FocalValue - minGoalValue));
            }

            if (goal.Tendency == "MaintainAtValue")
            {
                var minGoalValue = string.IsNullOrEmpty(goal.MinGoalReferenceVariable)
                    ? goal.MinGoalValue
                    : agent[goal.MinGoalReferenceVariable];
                var maxGoalValue = string.IsNullOrEmpty(goal.MaxGoalReferenceVariable)
                    ? goal.MaxGoalValue
                    : agent[goal.MaxGoalReferenceVariable];

                var diffFocalAndMin = goalState.FocalValue - minGoalValue;
                var diffFocalAndMax = goalState.FocalValue - maxGoalValue;

                var max = Math.Min(diffFocalAndMax, diffFocalAndMin);

                return Math.Abs(goalState.DiffCurrentAndFocal / (goalState.FocalValue - max));
            }

            throw new SosielAlgorithmException(
                "Cannot calculate relative difference between goal value and focal goal value for tendency" +
                goal.Tendency);
            //}

            //throw new SosielAlgorithmException("Please fill out section layer configuration");
        }
    }
}