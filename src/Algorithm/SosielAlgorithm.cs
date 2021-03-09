/// Name: SosielAlgorithm.cs
/// Description:
/// Authors: Multiple.
/// Copyright: Garry Sotnik

using System.Collections.Generic;
using System.Linq;
using SOSIEL.Configuration;
using SOSIEL.Entities;
using SOSIEL.Helpers;
using SOSIEL.Processes;

namespace SOSIEL.Algorithm
{
    public abstract class SosielAlgorithm<TDataSet> where TDataSet : IDataSet, new()
    {
        protected readonly TDataSet DefaultDataSet = new TDataSet();

        private int numberOfIterations;
        private int iterationCounter;
        private ProcessesConfiguration processConfiguration;

        protected int numberOfAgentsAfterInitialize;
        protected bool algorithmStoppage = false;
        protected AgentList agentList;
        protected LinkedList<Dictionary<IAgent, AgentState<TDataSet>>> iterations = new LinkedList<Dictionary<IAgent, AgentState<TDataSet>>>();

        protected Probabilities probabilities = new Probabilities();

        //processes
        protected IGoalPrioritizing gp;
        protected GoalSelecting gs = new GoalSelecting();
        protected AnticipatoryLearning<TDataSet> al = new AnticipatoryLearning<TDataSet>();
        protected CounterfactualThinking<TDataSet> ct = new CounterfactualThinking<TDataSet>();
        protected Innovation<TDataSet> innovation = new Innovation<TDataSet>();
        protected SocialLearning<TDataSet> sl = new SocialLearning<TDataSet>();
        protected Satisficing<TDataSet> satisficing = new Satisficing<TDataSet>();
        protected ActionTaking<TDataSet> at = new ActionTaking<TDataSet>();

        protected Demographic<TDataSet> demographic;

        public SosielAlgorithm(int numberOfIterations, ProcessesConfiguration processConfiguration, IGoalPrioritizing goalPrioritizing)
        {
            this.numberOfIterations = numberOfIterations;
            this.processConfiguration = processConfiguration;
            gp = goalPrioritizing != null ? goalPrioritizing : new DefaultGoalPrioritizing();
            iterationCounter = 0;
        }

        public SosielAlgorithm(int numberOfIterations, ProcessesConfiguration processConfiguration)
            : this(numberOfIterations, processConfiguration, null)
        {
        }

        /// <summary>
        /// Executes agent initializing. It's the first initializing step.
        /// </summary>
        protected abstract void InitializeAgents();


        /// <summary>
        /// Executes iteration state initializing. Executed after InitializeAgents.
        /// </summary>
        /// <returns></returns>
        protected abstract Dictionary<IAgent, AgentState<TDataSet>> InitializeFirstIterationState();

        protected virtual void UseDemographic()
        {
            processConfiguration.UseDemographicProcesses = true;
        }

        /// <summary>
        /// Executes last preparations before runs the algorithm. Executes after InitializeAgents and InitializeFirstIterationState.
        /// </summary>
        protected virtual void AfterInitialization() { }

        /// <summary>
        /// Executes in the end of the algorithm.
        /// </summary>
        protected virtual void AfterAlgorithmExecuted() { }


        /// <summary>
        /// Executes before any cognitive process is started.
        /// </summary>
        /// <param name="iteration"></param>
        protected virtual void PreIterationCalculations(int iteration) { }


        /// <summary>
        /// Executes after PreIterationCalculations
        /// </summary>
        /// <param name="iteration"></param>
        protected virtual void PreIterationStatistic(int iteration) { }


        /// <summary>
        /// Executes before action selection process
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="dataSet"></param>
        protected virtual void BeforeActionSelection(IAgent agent, TDataSet dataSet) { }


        /// <summary>
        /// Executes after action taking process
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="dataSet"></param>
        protected virtual void AfterActionTaking(IAgent agent, TDataSet dataSet) { }


        /// <summary>
        /// Befores the counterfactual thinking.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="dataSet"></param>
        protected virtual void BeforeCounterfactualThinking(IAgent agent, TDataSet dataSet) { }


        /// <summary>
        /// Executes after last cognitive process is finished
        /// </summary>
        /// <param name="iteration"></param>
        protected virtual void PostIterationCalculations(int iteration) { }

        /// <summary>
        /// Executes after PostIterationCalculations
        /// </summary>
        /// <param name="iteration"></param>
        protected virtual void PostIterationStatistic(int iteration) { }

        /// <summary>
        /// Executes agent deactivation logic.
        /// </summary>
        protected virtual void AgentsDeactivation() { }

        /// <summary>
        /// Executes after AgentsDeactivation.
        /// </summary>
        /// <param name="iteration"></param>
        protected virtual void AfterDeactivation(int iteration) { }

        /// <summary>
        /// Executes after Innovation.
        /// </summary>
        protected virtual void AfterInnovation(IAgent agent, TDataSet dataSet, DecisionOption newDecisionOption) { }

        protected virtual TDataSet[] FilterManagementDataSets(IAgent agent, TDataSet[] orderedDataSets)
        {
            return orderedDataSets;
        }

        /// <summary>
        /// Executes reproduction logic.
        /// </summary>
        /// <param name="minAgentNumber"></param>
        protected virtual void Reproduction(int minAgentNumber)
        {

        }

        /// <summary>
        /// Executes maintenance logic.
        /// </summary>
        protected virtual void Maintenance()
        {
            agentList.ActiveAgents.ForEach(a =>
            {
                //increment decision option activation freshness
                a.DecisionOptionActivationFreshness.Keys.ToList().ForEach(k =>
                {
                    a.DecisionOptionActivationFreshness[k] += 1;
                });
            });
        }


        /// <summary>
        /// Executes SOSIEL Algorithm
        /// </summary>
        /// <param name="activeDataSets"></param>
        protected void RunSosiel(ICollection<TDataSet> activeDataSets)
        {
            for (int i = 1; i <= numberOfIterations; i++)
            {
                iterationCounter++;

                PreIterationCalculations(iterationCounter);
                PreIterationStatistic(iterationCounter);

                Dictionary<IAgent, AgentState<TDataSet>> currentIteration;

                if (iterationCounter > 1)
                    currentIteration = iterations.AddLast(new Dictionary<IAgent, AgentState<TDataSet>>()).Value;
                else
                    currentIteration = iterations.AddLast(InitializeFirstIterationState()).Value;

                Dictionary<IAgent, AgentState<TDataSet>> priorIteration = iterations.Last.Previous?.Value;

                IAgent[] orderedAgents = agentList.ActiveAgents.Randomize(processConfiguration.AgentRandomizationEnabled).ToArray();

                var agentGroups = orderedAgents.GroupBy(a => a.Archetype.NamePrefix).OrderBy(group => group.Key).ToArray();

                orderedAgents.ForEach(a =>
                {
                    if (iterationCounter > 1)
                        currentIteration.Add(a, priorIteration[a].CreateForNextIteration());

                    currentIteration[a].RankedGoals = a.AssignedGoals.ToArray();
                });

                if (processConfiguration.UseDemographicProcesses && iterationCounter > 1)
                {
                    demographic.ChangeDemographic(iterationCounter, currentIteration, agentList);
                }

                TDataSet[] orderedDataSets = activeDataSets.Randomize().ToArray();

                TDataSet[] notDataSetOriented = new TDataSet[] { DefaultDataSet };

                if (iterationCounter == 1)
                {
                    foreach (var agentGroup in agentGroups)
                    {
                        foreach (IAgent agent in agentGroup)
                        {
                            currentIteration[agent].RankedGoals = gs.SortByImportance(agent, currentIteration[agent].GoalsState).ToArray();
                        }
                    }
                }

                if (processConfiguration.AnticipatoryLearningEnabled && iterationCounter > 1)
                {
                    //1st round: AL, CT, IR
                    foreach (var agentGroup in agentGroups)
                    {
                        foreach (IAgent agent in agentGroup)
                        {
                            //anticipatory learning process
                            al.Execute(agent, iterations.Last);


                            var agentGoalState = currentIteration[agent].GoalsState;
                            //goal prioritizing
                            gp.Prioritize(agent, agentGoalState);

                            //goal selecting
                            currentIteration[agent].RankedGoals = gs.SortByImportance(agent, agentGoalState).ToArray();

                            if (processConfiguration.CounterfactualThinkingEnabled)
                            {
                                if (currentIteration[agent].RankedGoals.Any(g => currentIteration[agent].GoalsState.Any(kvp => kvp.Value.Confidence == false)))
                                {
                                    foreach (TDataSet dataSet in GetDataSets(agent, orderedDataSets, notDataSetOriented))
                                    {
                                        BeforeCounterfactualThinking(agent, dataSet);

                                        foreach (var set in agent.AssignedDecisionOptions.GroupBy(h => h.Layer.Set).OrderBy(g => g.Key.PositionNumber))
                                        {
                                            //optimization
                                            Goal selectedGoal = currentIteration[agent].RankedGoals.First(g => set.Key.AssociatedWith.Contains(g));

                                            GoalState selectedGoalState = currentIteration[agent].GoalsState[selectedGoal];

                                            if (selectedGoalState.Confidence == false)
                                            {
                                                foreach (var layer in set.GroupBy(h => h.Layer).OrderBy(g => g.Key.PositionNumber))
                                                {
                                                    if (layer.Key.LayerConfiguration.Modifiable || (!layer.Key.LayerConfiguration.Modifiable && layer.Any(r => r.IsModifiable)))
                                                    {
                                                        //looking for matched decision option in prior period
                                                        DecisionOption[] matchedDecisionOptions = priorIteration[agent].DecisionOptionsHistories[dataSet]
                                                                .Matched.Where(h => h.Layer == layer.Key).ToArray();

                                                        bool? CTResult = null;

                                                        //counterfactual thinking process
                                                        if (matchedDecisionOptions.Length >= 2)
                                                            CTResult = ct.Execute(agent, iterations.Last, selectedGoal, matchedDecisionOptions, layer.Key, dataSet);


                                                        if (processConfiguration.InnovationEnabled)
                                                        {
                                                            //innovation process
                                                            if (CTResult == false || matchedDecisionOptions.Length < 2)
                                                            {
                                                                DecisionOption decisionOption = innovation.Execute(agent, iterations.Last, selectedGoal, layer.Key, dataSet, probabilities);

                                                                AfterInnovation(agent, dataSet, decisionOption);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (processConfiguration.SocialLearningEnabled && iterationCounter > 1)
                {
                    //2nd round: SL
                    foreach (var agentGroup in agentGroups)
                    {

                        foreach (IAgent agent in agentGroup)
                        {
                            foreach (var set in agent.AssignedDecisionOptions.GroupBy(h => h.Layer.Set).OrderBy(g => g.Key.PositionNumber))
                            {
                                foreach (var layer in set.GroupBy(h => h.Layer).OrderBy(g => g.Key.PositionNumber))
                                {
                                    //social learning process
                                    sl.ExecuteLearning(agent, iterations.Last, layer.Key);
                                }
                            }
                        }
                    }

                }

                if (processConfiguration.DecisionOptionSelectionEnabled)
                {
                    //AS part I
                    foreach (var agentGroup in agentGroups)
                    {
                        foreach (IAgent agent in agentGroup)
                        {
                            foreach (TDataSet dataSet in GetDataSets(agent, orderedDataSets, notDataSetOriented))
                            {
                                foreach (var set in agent.AssignedDecisionOptions.GroupBy(h => h.Layer.Set).OrderBy(g => g.Key.PositionNumber))
                                {
                                    foreach (var layer in set.GroupBy(h => h.Layer).OrderBy(g => g.Key.PositionNumber))
                                    {
                                        BeforeActionSelection(agent, dataSet);

                                        //satisficing
                                        satisficing.ExecutePartI(agent, iterations.Last, currentIteration[agent].RankedGoals, layer.ToArray(), dataSet);
                                    }
                                }
                            }
                        }
                    }


                    if (processConfiguration.DecisionOptionSelectionPart2Enabled && iterationCounter > 1)
                    {
                        //4th round: AS part II
                        foreach (var agentGroup in agentGroups)
                        {
                            foreach (IAgent agent in agentGroup)
                            {
                                foreach (TDataSet dataSet in GetDataSets(agent, orderedDataSets, notDataSetOriented))
                                {
                                    foreach (var set in agent.AssignedDecisionOptions.GroupBy(r => r.Layer.Set).OrderBy(g => g.Key.PositionNumber))
                                    {
                                        foreach (var layer in set.GroupBy(h => h.Layer).OrderBy(g => g.Key.PositionNumber))
                                        {
                                            BeforeActionSelection(agent, dataSet);

                                            //action selection process part II
                                            satisficing.ExecutePartII(agent, iterations.Last, currentIteration[agent].RankedGoals, layer.ToArray(), dataSet);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (processConfiguration.ActionTakingEnabled)
                {
                    //5th round: TA
                    foreach (var agentGroup in agentGroups)
                    {
                        foreach (IAgent agent in agentGroup)
                        {
                            foreach (TDataSet dataSet in GetDataSets(agent, orderedDataSets, notDataSetOriented))
                            {
                                at.Execute(agent, currentIteration[agent], dataSet);

                                AfterActionTaking(agent, dataSet);
                            }
                        }
                    }
                }

                if (processConfiguration.AlgorithmStopIfAllAgentsSelectDoNothing && iterationCounter > 1)
                {
                    if (!currentIteration.SelectMany(kvp => kvp.Value.DecisionOptionsHistories.Values.SelectMany(rh => rh.Activated)).Any())
                    {
                        algorithmStoppage = true;
                    }
                }

                PostIterationCalculations(iterationCounter);

                PostIterationStatistic(iterationCounter);

                if (processConfiguration.AgentsDeactivationEnabled && iterationCounter > 1)
                {
                    AgentsDeactivation();
                }

                AfterDeactivation(iterationCounter);

                if (processConfiguration.ReproductionEnabled && iterationCounter > 1)
                {
                    Reproduction(0);
                }

                if (algorithmStoppage || agentList.ActiveAgents.Length == 0)
                    break;

                Maintenance();
            }
        }

        private TDataSet[] GetDataSets(IAgent agent, TDataSet[] orderedDataSets, TDataSet[] notDataSetOriented)
        {
            return agent.Archetype.IsDataSetOriented ? FilterManagementDataSets(agent, orderedDataSets) : notDataSetOriented;
        }
    }
}
