/// Name: Satisficing.cs
/// Description: Goal selecting is the first cognitive process activated during
///   the first time period and subsequently the first activated decision-making
///   process in the second and later time periods. The aim of goal selecting is to
///   generate a list of goals from which goals of focus can be selected during
///   decision-making and in which the goals are ordered by their importance levels
///   (which, during the second and later time periods are updated during anticipatory
///   learning). The reason a list of goals is generated (as opposed to a single
///   goal) is because not all mental (sub)models are associated with all goals.
///   The process of goal selecting consists of the following two subprocesses:
///   (a) generate the goal importance distribution, which constructs a distribution
///   of goals that reflects their importance levels; and (b) generate the goals
///   of focus list, which applies a uniform distribution to randomly select a list
///   of goals from the goal importance distribution. The result of the goal
///   selecting process is a list of goals, the goals of focus list, approximately
///   ordered by their level of importance. The ordering is only approximate because
///   the use of a uniform distribution to select goals implies that, at any point,
///   chance may lead to the selection of a less important goal, thereby introducing
///   a degree of uncertainty. The process of signaling interest in a collective
///   action follows satisficing when the selected decision during satisficing is
///   a collective action and interest in it has not yet been signaled to members
///   of associated social networks. The result of signaling interest in a collective
///   action is an updated list of agents committed to the collective action. After
///   all agents interested in collective action had a chance to express their
///   interest, the process of satisficing is reactivated. If a sufficient number
///   of agents have signaled interest in a collective action, then during
///   satisficing the collective action becomes their selected decision. If, however,
///   the number of agents signaling interest is not sufficient, then the collective
///   action is deactivated as a potential decision option during the current period
///   and the agents reengage in satisficing.
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
    /// Action selection process implementation.
    /// </summary>
    public class Satisficing<TDataSet> : VolatileProcess
    {
        Goal processedGoal;
        GoalState goalState;


        Dictionary<DecisionOption, Dictionary<Goal, double>> anticipatedInfluence;

        DecisionOption[] matchedDecisionOptions;


        DecisionOption priorPeriodActivatedDecisionOption;
        DecisionOption decisionOptionForActivating;

        #region Specific logic for tendencies
        protected override void EqualToOrAboveFocalValue()
        {
            if(goalState.Value >= goalState.FocalValue)
                return;

            DecisionOption[] selected = matchedDecisionOptions;

            if (matchedDecisionOptions.Length > 1)
            {
                selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluence[r][processedGoal] - (goalState.FocalValue - goalState.Value))
                    .OrderBy(hg => hg.Key).First().ToArray();
            }

            decisionOptionForActivating = selected.RandomizeOne();
        }

        protected override void Maximize()
        {
            if (matchedDecisionOptions.Length > 0)
            {
                DecisionOption[] selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluence[r][processedGoal]).OrderByDescending(hg => hg.Key).First().ToArray();

                decisionOptionForActivating = selected.RandomizeOne();
            }
        }

        protected override void Minimize()
        {
            if (matchedDecisionOptions.Length > 0)
            {
                DecisionOption[] selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluence[r][processedGoal]).OrderBy(hg => hg.Key).First().ToArray();

                decisionOptionForActivating = selected.RandomizeOne();
            }
        }

        protected override void MaintainAtValue()
        {
            if(goalState.Value == goalState.FocalValue)
                return;

            DecisionOption[] selected = matchedDecisionOptions;

            if (matchedDecisionOptions.Length > 1)
            {
                selected = matchedDecisionOptions.GroupBy(r => anticipatedInfluence[r][processedGoal] - Math.Abs(goalState.FocalValue - goalState.Value))
                  .OrderBy(hg => hg.Key).First().ToArray();
            }

            decisionOptionForActivating = selected.RandomizeOne();
        }
        #endregion

        /// <summary>
        /// Shares collective action among same household agents
        /// </summary>
        /// <param name="currentAgent"></param>
        /// <param name="decisionOption"></param>
        /// <param name="agentStates"></param>
        List<IAgent> SignalingInterest(IAgent currentAgent, DecisionOption decisionOption, Dictionary<IAgent, AgentState<TDataSet>> agentStates)
        {
            var scope = decisionOption.Scope;

            var agents = new List<IAgent>();

            foreach (IAgent neighbour in currentAgent.ConnectedAgents
                .Where(connected => scope == null || (connected.ContainsVariable(scope) && currentAgent.ContainsVariable(scope) 
                                                                                        && connected[scope] == currentAgent[scope])))
            {
                if (neighbour.AssignedDecisionOptions.Contains(decisionOption) == false)
                {
                    neighbour.AssignNewDecisionOption(decisionOption, currentAgent.AnticipationInfluence[decisionOption]);
                    agents.Add(neighbour);
                }
            }

            return agents;
        }

        /// <summary>
        /// Executes first part of action selection for specific agent and data set
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="lastIteration"></param>
        /// <param name="rankedGoals"></param>
        /// <param name="processedDecisionOptions"></param>
        /// <param name="dataSet"></param>
        public void ExecutePartI(IAgent agent, LinkedListNode<Dictionary<IAgent, AgentState<TDataSet>>> lastIteration, Goal[] rankedGoals, DecisionOption[] processedDecisionOptions, TDataSet dataSet)
        {
            decisionOptionForActivating = null;

            AgentState<TDataSet> agentState = lastIteration.Value[agent];
            AgentState<TDataSet> priorPeriod = lastIteration.Previous?.Value[agent];

            //adds new decisionOption history for specific data set if it doesn't exist
            if (agentState.DecisionOptionsHistories.ContainsKey(dataSet) == false)
                agentState.DecisionOptionsHistories.Add(dataSet, new DecisionOptionsHistory());

            DecisionOptionsHistory history = agentState.DecisionOptionsHistories[dataSet];

            processedGoal = rankedGoals.First(g => processedDecisionOptions.First().Layer.Set.AssociatedWith.Contains(g));
            goalState = agentState.GoalsState[processedGoal];

            matchedDecisionOptions = processedDecisionOptions.Except(history.Blocked).Where(h => h.IsMatch(agent)).ToArray();

            if (matchedDecisionOptions.Length == 0)
            {
                return;
            }

            if (matchedDecisionOptions.Length > 1)
            {
                if (priorPeriod != null)
                    priorPeriodActivatedDecisionOption = priorPeriod.DecisionOptionsHistories[dataSet].Activated.FirstOrDefault(r => r.Layer == processedDecisionOptions.First().Layer);

                //set anticipated influence before execute specific logic
                anticipatedInfluence = agent.AnticipationInfluence;

                SpecificLogic(processedGoal.Type);
            }
            else
                decisionOptionForActivating = matchedDecisionOptions[0];

            if (processedDecisionOptions.First().Layer.Set.Layers.Count > 1)
                decisionOptionForActivating.Apply(agent);

            if (decisionOptionForActivating != null)
            {
                history.Activated.Add(decisionOptionForActivating);
            }

            history.Matched.AddRange(matchedDecisionOptions);

            if (decisionOptionForActivating != null && decisionOptionForActivating.IsCollectiveAction)
            {
                var agents = SignalingInterest(agent, decisionOptionForActivating, lastIteration.Value);

                if (agents.Count > 0)
                {
                    foreach (var a in agents)
                    {
                        var agentHistory = lastIteration.Value[a].DecisionOptionsHistories[dataSet];
                        var layer = decisionOptionForActivating.Layer;
                        if (agentHistory.Activated.Any(h => h.Layer == layer))
                        {
                            //clean previous choice
                            agentHistory.Activated.RemoveAll(h => h.Layer == layer);
                            agentHistory.Matched.RemoveAll(h => h.Layer == layer);

                            var decisionOpts = a.AssignedDecisionOptions.Where(h => h.Layer == layer).ToArray();

                            ExecutePartI(a, lastIteration, rankedGoals, decisionOpts, dataSet);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Executes second part of action selection for specific data set
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="lastIteration"></param>
        /// <param name="rankedGoals"></param>
        /// <param name="processedDecisionOptions"></param>
        /// <param name="dataSet"></param>
        public void ExecutePartII(IAgent agent, LinkedListNode<Dictionary<IAgent, AgentState<TDataSet>>> lastIteration, Goal[] rankedGoals, DecisionOption[] processedDecisionOptions, TDataSet dataSet)
        {
            AgentState<TDataSet> agentState = lastIteration.Value[agent];

            DecisionOptionsHistory history = agentState.DecisionOptionsHistories[dataSet];

            DecisionOptionLayer layer = processedDecisionOptions.First().Layer;


            DecisionOption selectedDecisionOptions = history.Activated.SingleOrDefault(r => r.Layer == layer);

            if (selectedDecisionOptions == null) return;

            if (selectedDecisionOptions.IsCollectiveAction)
            {
                var scope = selectedDecisionOptions.Scope;

                //counting agents which selected this decision option
                int numberOfInvolvedAgents = agent.ConnectedAgents.Where(connected => agent[scope] == connected[scope] || scope == null)
                    .Count(a => lastIteration.Value[a].DecisionOptionsHistories[dataSet].Activated.Any(decisionOption => decisionOption == selectedDecisionOptions));

                int requiredParticipants = selectedDecisionOptions.RequiredParticipants - 1;

                //add decision option to blocked
                if (numberOfInvolvedAgents < requiredParticipants)
                {
                    history.Blocked.Add(selectedDecisionOptions);

                    history.Activated.Remove(selectedDecisionOptions);

                    ExecutePartI(agent, lastIteration, rankedGoals, processedDecisionOptions, dataSet);

                    ExecutePartII(agent, lastIteration, rankedGoals, processedDecisionOptions, dataSet);
                }
            }
        }
    }
}
