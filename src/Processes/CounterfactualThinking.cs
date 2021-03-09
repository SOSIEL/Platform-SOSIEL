/// Name: CounterfactualThinking.cs
/// Description: Counterfactual thinking follows goal prioritizing only in the
///   case that a mental (sub)model is modifiable, there is a lack of confidence
///   in relation to a goal, and the number of decision options matching conditions
///   in the prior period was equal to or greater than two. A loss of confidence,
///   which may occur during the process of anticipatory learning, triggers
///   counterfactual thinking as an effort to explain the discrepancy between the
///   anticipated and actual results of a decision. The aim of counterfactual
///   thinking is to check whether or not the agent would have behaved differently
///   (i.e., if an available alternate decision had been selected) had it known
///   in the prior period (which is represented by a prior set of conditions) what
///   it knows in the current (which is represented by updated anticipations). If
///   an alternative satisfactory decision is identified, then confidence is
///   regained and the agent moves on to the process of social learning. If, however,
///   an alternative decision is not identified, then the agent remains unconfident
///   and continues with individual learning by engaging in innovating, before moving
///   on to social learning. The process of counterfactual thinking consists of the
///   following two subprocesses: (a) search for a better decision option and
///   (b) assess the success of the search. The result of counterfactual thinking
///   is knowledge of whether a potentially better decision option is present in
///   the corresponding mental (sub)model and whether there is a potential change
///   to the state of uncertainty.
/// Authors: Multiple.
/// Copyright: Garry Sotnik

using System;
using System.Collections.Generic;
using System.Linq;
using SOSIEL.Entities;
using SOSIEL.Enums;

namespace SOSIEL.Processes
{
    /// <summary>
    /// Counterfactual thinking process implementation.
    /// </summary>
    public class CounterfactualThinking<TSite> : VolatileProcess
    {
        Goal selectedGoal;
        GoalState selectedGoalState;
        Dictionary<DecisionOption, Dictionary<Goal, double>> anticipatedInfluences;

        DecisionOption[] matchedDecisionOptions;
        DecisionOption activatedDecisionOption;

        #region Specific logic for tendencies
        protected override void EqualToOrAboveFocalValue()
        {
            if(selectedGoalState.PriorValue >= selectedGoalState.PriorFocalValue)
                return;

            DecisionOption[] selected = matchedDecisionOptions;

            if (matchedDecisionOptions.Length > 1)
            {
                selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluences[r][selectedGoal] - (selectedGoalState.PriorFocalValue - selectedGoalState.PriorValue))
                    .OrderBy(hg => hg.Key).First().ToArray();
            }

            selectedGoalState.Confidence = selected.Length > 0 && selected.Any(r => r != activatedDecisionOption);
        }

        protected override void Maximize()
        {
            if (matchedDecisionOptions.Length > 0)
            {
                DecisionOption[] selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluences[r][selectedGoal]).OrderByDescending(hg => hg.Key).First().ToArray();

                selectedGoalState.Confidence = selected.Length > 0 && selected.Any(r => r != activatedDecisionOption);
            }
        }

        protected override void Minimize()
        {
            if (matchedDecisionOptions.Length > 0)
            {
                DecisionOption[] selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluences[r][selectedGoal]).OrderBy(hg => hg.Key).First().ToArray();

                selectedGoalState.Confidence = selected.Length > 0 && selected.Any(r => r != activatedDecisionOption);
            }
        }

        protected override void MaintainAtValue()
        {
            if(selectedGoalState.PriorValue == selectedGoalState.PriorFocalValue)
                return;

            DecisionOption[] selected = matchedDecisionOptions;

            if (matchedDecisionOptions.Length > 1)
            {
                selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluences[r][selectedGoal] - Math.Abs(selectedGoalState.PriorFocalValue - selectedGoalState.PriorValue))
                    .OrderBy(hg => hg.Key).First().ToArray();
            }

            selectedGoalState.Confidence = selected.Length > 0 && selected.Any(r => r != activatedDecisionOption);
        }

        #endregion


        /// <summary>
        /// Executes counterfactual thinking about most important agent goal for specific site
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="lastIteration"></param>
        /// <param name="goal"></param>
        /// <param name="matched"></param>
        /// <param name="layer"></param>
        /// <param name="site"></param>
        /// <returns></returns>
        public bool Execute(IAgent agent, LinkedListNode<Dictionary<IAgent, AgentState<TSite>>> lastIteration, Goal goal,
            DecisionOption[] matched, DecisionOptionLayer layer, TSite site)
        {
            //Period currentPeriod = periodModel.Value;
            AgentState<TSite> priorIterationAgentState = lastIteration.Previous.Value[agent];

            selectedGoal = goal;

            selectedGoalState = lastIteration.Value[agent].GoalsState[selectedGoal];
            selectedGoalState.Confidence = false;

            DecisionOptionsHistory history = priorIterationAgentState.DecisionOptionsHistories[site];


            activatedDecisionOption = history.Activated.FirstOrDefault(r => r.Layer == layer);

            anticipatedInfluences = agent.AnticipationInfluence;

            matchedDecisionOptions = matched;

            SpecificLogic(selectedGoal.Type);


            return selectedGoalState.Confidence;
        }
    }
}
