﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using OKOSW.MathTools;
using OKOSW.Extensions;
using OKOSW.CSVTools;
using OKOSW.Neural.Activation;

namespace OKOSW.Neural.Reservoir.Analog
{
    /// <summary>
    /// Implements analog reservoir supporting several internal topologies and advanced features
    /// </summary>
    [Serializable]
    public class AnalogReservoir : IAnalogReservoir
    {
        //Attributes
        private string m_seqNum;
        private string m_configName;
        private Random m_rand;
        private ReservoirInputBlock m_inputBlock;
        private AnalogNeuron[] m_neurons;
        private List<int>[] m_partyNeuronsIdxs;
        private List<double>[] m_partyNeuronsWeights;
        private bool m_contextNeuronFeature;
        private AnalogNeuron m_contextNeuron;
        private double[] m_neurons2ContextWeights;
        private double[] m_context2NeuronsWeights;
        private bool m_feedbackFeature;
        private double[] m_feedback;
        private double[] m_feedbackWeights;
        private bool m_augmentedStatesFeature;

        /// <summary>
        /// Constructs analog computing reservoir.
        /// </summary>
        /// <param name="seqNum">Reservoir sequence identifier (together with reservoir configuration name should be unique within the parent)</param>
        /// <param name="inputValuesCount">Number of reservoir input values</param>
        /// <param name="feedbackValuesCount">Number of values to be fed back</param>
        /// <param name="settings">Reservoir initialization parameters</param>
        /// <param name="randomizerSeek">
        /// Calling constructor with the same randomizerSeek greater or equal to 0 ensures the same reservoir initialization (good for tuning).
        /// Specify randomizerSeek less than 0 for different initialization aech time the constructor will be called.
        /// </param>
        public AnalogReservoir(int seqNum, int inputValuesCount, int feedbackValuesCount, AnalogReservoirSettings settings, int randomizerSeek = -1)
        {
            //--------------------------------------------------------
            //Configuration name
            m_configName = settings.CfgName;
            //Reservoir ID
            m_seqNum = m_configName + "(" + seqNum.ToString() + ")";
            //--------------------------------------------------------
            //Random object initialization
            if (randomizerSeek < 0) m_rand = new Random();
            else m_rand = new Random(randomizerSeek);
            //--------------------------------------------------------
            //Input memory and connections
            int neuronsPerInput = Math.Max(1, (int)Math.Round((double)settings.Size * settings.InputConnectionDensity, 0));
            m_inputBlock = new ReservoirInputBlock(inputValuesCount, settings.Size, settings.BiasScale, settings.InputWeightScale, neuronsPerInput, m_rand);
            //--------------------------------------------------------
            //Reservoir neurons
            m_neurons = new AnalogNeuron[settings.Size];
            //Neurons retainment rates
            double[] retainmentRates = new double[m_neurons.Length];
            retainmentRates.Populate(0);
            int retainmentNeuronsCount = (int)Math.Round((double)m_neurons.Length * settings.RetainmentNeuronsDensity, 0);
            if (retainmentNeuronsCount > 0 && settings.RetainmentMaxRate > 0)
            {
                m_rand.FillUniform(retainmentRates, settings.RetainmentMinRate, settings.RetainmentMaxRate, 1, retainmentNeuronsCount);
                m_rand.Shuffle(retainmentRates);
            }
            //Neurons creation
            for (int n = 0; n < m_neurons.Length; n++)
            {
                m_neurons[n] = new AnalogNeuron(ActivationFactory.CreateAF(settings.ReservoirNeuronActivation), retainmentRates[n]);
            }
            //Helper array for neurons order randomization purposes
            int[] neuronsShuffledIndices = new int[settings.Size];
            //--------------------------------------------------------
            //Context neuron feature
            int contextNeuronFeedbacksCount = (int)Math.Round((double)m_neurons.Length * settings.ContextNeuronFeedbackDensity, 0);
            m_contextNeuron = null;
            m_neurons2ContextWeights = null;
            m_context2NeuronsWeights = null;
            m_contextNeuronFeature = (contextNeuronFeedbacksCount > 0);
            if (m_contextNeuronFeature)
            {
                m_contextNeuron = new AnalogNeuron(ActivationFactory.CreateAF(settings.ContextNeuronActivation), 0);
                //Weights from each res neuron to context neuron
                m_neurons2ContextWeights = new double[m_neurons.Length];
                m_rand.FillUniform(m_neurons2ContextWeights, -1, 1, settings.ContextNeuronInWeightScale);
                //Weights from context neuron to res neurons
                m_context2NeuronsWeights = new double[m_neurons.Length];
                m_context2NeuronsWeights.Populate(0);
                neuronsShuffledIndices.ShuffledIndices(m_rand);
                for (int i = 0; i < contextNeuronFeedbacksCount && i < m_neurons.Length; i++)
                {
                    m_context2NeuronsWeights[neuronsShuffledIndices[i]] = RandomWeight(m_rand, settings.ContextNeuronOutWeightScale);
                }
            }
            //--------------------------------------------------------
            //Feedback feature and weights
            int neuronsPerOutput = (int)Math.Round(settings.FeedbackConnectionDensity * (double)m_neurons.Length, 0);
            m_feedback = new double[feedbackValuesCount];
            m_feedback.Populate(0);
            m_feedbackWeights = null;
            m_feedbackFeature = (neuronsPerOutput > 0);
            if (m_feedbackFeature)
            {
                //Feedback weights
                m_feedbackWeights = new double[feedbackValuesCount * m_neurons.Length];
                m_feedbackWeights.Populate(0);
                for (int outNo = 0; outNo < feedbackValuesCount; outNo++)
                {
                    neuronsShuffledIndices.ShuffledIndices(m_rand);
                    for (int i = 0; i < neuronsPerOutput; i++)
                    {
                        m_feedbackWeights[outNo * m_neurons.Length + neuronsShuffledIndices[i]] = RandomWeight(m_rand, settings.FeedbackWeightScale);
                    }
                }
            }
            //--------------------------------------------------------
            //Reservoir topology -> schema of internal connections
            m_partyNeuronsIdxs = new List<int>[m_neurons.Length];
            m_partyNeuronsWeights = new List<double>[m_neurons.Length];
            for (int i = 0; i < m_neurons.Length; i++)
            {
                m_partyNeuronsIdxs[i] = new List<int>();
                m_partyNeuronsWeights[i] = new List<double>();
            }
            switch (settings.Topology)
            {
                case AnalogReservoirSettings.EnumReservoirTopology.Random:
                    SetupRandomTopology(settings.RandomTopologyCfg, settings.InternalWeightScale);
                    break;
                case AnalogReservoirSettings.EnumReservoirTopology.Ring:
                    SetupRingTopology(settings.RingTopologyCfg, settings.InternalWeightScale);
                    break;
                case AnalogReservoirSettings.EnumReservoirTopology.DTT:
                    SetupDTTTopology(settings.DTTTopologyCfg, settings.InternalWeightScale);
                    break;
            }
            //--------------------------------------------------------
            //Augmented states
            m_augmentedStatesFeature = settings.AugmentedStatesFeature;
            return;
        }

        /// <summary>
        /// Returns random weight within range -scale, +scale
        /// </summary>
        /// <param name="rand">Random object to be used.</param>
        /// <param name="scale">Determines the range within the weight has to be.</param>
        /// <returns></returns>
        public static double RandomWeight(Random rand, double scale)
        {
            return rand.NextBoundedUniformDouble(-1, 1) * scale;
        }

        /// <summary>
        /// Establishes connection between two reservoir neurons.
        /// </summary>
        /// <param name="targetNeuronIdx">Target neuron index</param>
        /// <param name="partyNeuronIdx">Party neuron index</param>
        /// <param name="weightScale">Connection weight scale</param>
        /// <param name="check">Check if the connection already exists?</param>
        /// <returns>Success/Unsuccess (connection already exists)</returns>
        private bool AddConnection(int targetNeuronIdx, int partyNeuronIdx, double weightScale, bool check = true)
        {
            if (!check || !m_partyNeuronsIdxs[targetNeuronIdx].Contains(partyNeuronIdx))
            {
                m_partyNeuronsIdxs[targetNeuronIdx].Add(partyNeuronIdx);
                m_partyNeuronsWeights[targetNeuronIdx].Add(RandomWeight(m_rand, weightScale));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Establishes connection between two reservoir neurons.
        /// </summary>
        /// <param name="connectionID">Position of the connection within flat representation.</param>
        /// <param name="weightScale">Connection weight scale</param>
        /// <param name="check">Check if the connection already exists?</param>
        /// <returns>Success/Unsuccess (connection already exists)</returns>
        private bool AddConnection(int connectionID, double weightScale, bool check = true)
        {
            return AddConnection(connectionID / m_neurons.Length, connectionID % m_neurons.Length, weightScale, check);
        }

        /// <summary>
        /// Connects all reservoir neurons to a ring shape.
        /// </summary>
        /// <param name="weightScale">Connection weight scale</param>
        /// <param name="biDirection">Bi direction ring?</param>
        /// <param name="check">Check if the connection already exists?</param>
        private void SetRingConnections(double weightScale, bool biDirection, bool check = true)
        {
            for (int i = 0; i < m_neurons.Length; i++)
            {
                int partyNeuronIdx = (i == 0) ? (m_neurons.Length - 1) : (i - 1);
                AddConnection(i, partyNeuronIdx, weightScale, check);
                if(biDirection)
                {
                    partyNeuronIdx = (i == m_neurons.Length - 1) ? (0) : (i + 1);
                    AddConnection(i, partyNeuronIdx, weightScale, check);
                }
            }
            return;
        }

        /// <summary>
        /// Sets randomly selected number of neurons (corresponding to density) to be self-connected
        /// </summary>
        /// <param name="density">How many neurons will be self-connected?</param>
        /// <param name="weightScale">Connection weight scale</param>
        /// <param name="check">Check if the connection already exists?</param>
        private void SetSelfConnections(double density, double weightScale, bool check = true)
        {
            int connectionsCount = (int)Math.Round((double)m_neurons.Length * density);
            int[] indices = new int[m_neurons.Length];
            indices.ShuffledIndices(m_rand);
            for (int i = 0; i < connectionsCount; i++)
            {
                AddConnection(indices[i], indices[i], weightScale, check);
            }
            return;
        }

        /// <summary>
        /// Sets random neurons inter connections.
        /// </summary>
        /// <param name="density">How many connections from Size x Size options will be randomly initialized?</param>
        /// <param name="weightScale">Connection weight scale</param>
        /// <param name="check">Check if the connection already exists?</param>
        private void SetInterConnections(double density, double weightScale, bool check = true)
        {
            int connectionsCount = (int)Math.Round((double)((m_neurons.Length - 1) * m_neurons.Length) * density);
            int[] randomConnections = new int[(m_neurons.Length - 1) * m_neurons.Length];
            int indicesPos = 0;
            for(int n1Idx = 0; n1Idx < m_neurons.Length; n1Idx++)
            {
                for(int n2Idx = 0; n2Idx < m_neurons.Length; n2Idx++)
                {
                    if(n1Idx != n2Idx)
                    {
                        randomConnections[indicesPos] = n1Idx * m_neurons.Length + n2Idx;
                        ++indicesPos;
                    }
                }
            }
            m_rand.Shuffle(randomConnections);
            for (int i = 0; i < connectionsCount; i++)
            {
                AddConnection(randomConnections[i], weightScale, check);
            }
            return;
        }

        /// <summary>
        /// Initializes random topology connection schema
        /// </summary>
        /// <param name="cfg">Configuration parameters</param>
        /// <param name="weightScale">Connection weight scale</param>
        private void SetupRandomTopology(AnalogReservoirSettings.RandomTopologyConfig cfg, double weightScale)
        {
            //Fully random connections setup
            int connectionsCount = (int)Math.Round((double)m_neurons.Length * (double)m_neurons.Length * cfg.ConnectionsDensity);
            int[] randomConnections = new int[m_neurons.Length * m_neurons.Length];
            randomConnections.ShuffledIndices(m_rand);
            for (int i = 0; i < connectionsCount; i++)
            {
                AddConnection(randomConnections[i], weightScale, false);
            }
            return;
        }

        /// <summary>
        /// Initializes ring shape topology connection schema
        /// </summary>
        /// <param name="cfg">Configuration parameters</param>
        /// <param name="weightScale">Connection weight scale</param>
        private void SetupRingTopology(AnalogReservoirSettings.RingTopologyConfig cfg, double weightScale)
        {
            //Ring connections part
            SetRingConnections(weightScale, cfg.BiDirection, false);
            //Self connections part
            SetSelfConnections(cfg.SelfConnectionsDensity, weightScale, false);
            //Inter connections part
            SetInterConnections(cfg.InterConnectionsDensity, weightScale, true);
            return;
        }

        /// <summary>
        /// Initializes doubly twisted thoroidal shape topology connection schema
        /// </summary>
        /// <param name="cfg">Configuration parameters</param>
        /// <param name="weightScale">Connection weight scale</param>
        private void SetupDTTTopology(AnalogReservoirSettings.DTTTopologyConfig cfg, double weightScale)
        {
            //HTwist part (single direction ring)
            SetRingConnections(weightScale, false);
            //VTwist part
            int step = (int)Math.Floor(Math.Sqrt(m_neurons.Length));
            for (int partyNeuronIdx = 0; partyNeuronIdx < m_neurons.Length; partyNeuronIdx++)
            {
                int targetNeuronIdx = partyNeuronIdx + step;
                if (targetNeuronIdx > m_neurons.Length - 1)
                {
                    int left = partyNeuronIdx % step;
                    targetNeuronIdx = (left == 0) ? (step - 1) : (left - 1);
                }
                AddConnection(targetNeuronIdx, partyNeuronIdx, weightScale, false);
            }
            //Self connections part
            SetSelfConnections(cfg.SelfConnectionsDensity, weightScale, false);
            return;
        }


        #region IReservoir implementation

        //Properties
        /// <summary>
        /// Reservoir ID.
        /// </summary>
        public string SeqNum { get { return m_seqNum; } }

        /// <summary>
        /// Reservoir configuration name (together with ID should be unique).
        /// </summary>
        public string ConfigName { get { return m_configName; } }

        /// <summary>
        /// Reservoir size. (Reservoir neurons count)
        /// </summary>
        public int Size { get { return m_neurons.Length; } }

        /// <summary>
        /// Number of reservoir's output predictors (Size or Size*2 when augumented states are enabled).
        /// </summary>
        public int OutputPredictorsCount { get { return m_augmentedStatesFeature ? m_neurons.Length * 2 : m_neurons.Length; } }

        /// <summary>
        /// Reservoir neurons.
        /// </summary>
        public AnalogNeuron[] Neurons { get { return m_neurons; } }

        //Methods
        /// <summary>
        /// Resets all reservoir neurons to their initial state (before boot state).
        /// Function does not affect weights or internal structure of the resservoir.
        /// </summary>
        public void Reset()
        {
            foreach (AnalogNeuron neuron in m_neurons)
            {
                neuron.Reset();
            }
            if(m_contextNeuronFeature)m_contextNeuron.Reset();
            m_feedback.Populate(0);
            return;
        }

        /// <summary>
        /// Computes reservoir neurons new states and returns new set of reservoir output predictors.
        /// </summary>
        /// <param name="input">Array of new input values.</param>
        /// <param name="outputPredictors">Array to be filled with output predictors values. Array has to be sized to OutputPredictorsCount reservoir property.</param>
        /// <param name="collectStatistics">Switch dictates, if to collect statistics. Typical usage is FALSE within boot phase and TRUE after boot phase.</param>
        public void Compute(double[] input, double[] outputPredictors, bool collectStatistics)
        {
            //Update input memory
            m_inputBlock.Update(input);
            //Store all reservoir neurons states
            foreach(AnalogNeuron neuron in m_neurons)
            {
                neuron.StoreCurrentState();
            }
            //Compute new states of all reservoir neurons and fill output array of predictors
            Parallel.For(0, m_neurons.Length, (neuronIdx) =>
            {
                //----------------------------------------------------
                //Input signal
                double inputSignal = m_inputBlock.GetInputSignal(neuronIdx);
                //----------------------------------------------------
                //Signal from reservoir neurons
                double reservoirSignal = 0;
                //Add reservoir neurons signal
                for (int j = 0; j < m_partyNeuronsIdxs[neuronIdx].Count; j++)
                {
                    reservoirSignal += m_partyNeuronsWeights[neuronIdx][j] * m_neurons[m_partyNeuronsIdxs[neuronIdx][j]].PreviousState;
                }
                //Add context neuron signal if allowed
                reservoirSignal += m_contextNeuronFeature ? m_context2NeuronsWeights[neuronIdx] * m_contextNeuron.CurrentState : 0;
                //----------------------------------------------------
                //Feedback signal
                double feedbackSignal = 0;
                if (m_feedbackFeature)
                {
                    for (int outpIdx = 0; outpIdx < m_feedback.Length; outpIdx++)
                    {
                        feedbackSignal += m_feedback[outpIdx] * m_feedbackWeights[outpIdx * m_neurons.Length + neuronIdx];
                    }
                }
                //----------------------------------------------------
                //Set new state of reservoir neuron
                m_neurons[neuronIdx].NewState(inputSignal + reservoirSignal + feedbackSignal, collectStatistics);
                //----------------------------------------------------
                //Set neuron state to output predictors
                outputPredictors[neuronIdx] = m_neurons[neuronIdx].CurrentState;
                //----------------------------------------------------
                //Set neuron augmented state to output predictors
                if (m_augmentedStatesFeature)
                {
                    outputPredictors[m_neurons.Length + neuronIdx] = outputPredictors[neuronIdx] * outputPredictors[neuronIdx];
                }
            });
            //----------------------------------------------------
            //New state of context neuron if allowed
            if (m_contextNeuronFeature)
            {
                double res2ContextSignal = 0;
                for (int neuronIdx = 0; neuronIdx < m_neurons.Length; neuronIdx++)
                {
                    res2ContextSignal += m_neurons2ContextWeights[neuronIdx] * m_neurons[neuronIdx].CurrentState;
                }
                m_contextNeuron.NewState(res2ContextSignal, collectStatistics);
            }
            return;
        }

        /// <summary>
        /// Sets feedback values for next computation round
        /// </summary>
        /// <param name="feedback">Feedback values.</param>
        public void SetFeedback(double[] feedback)
        {
            feedback.CopyTo(m_feedback, 0);
            return;
        }


        #endregion

        //Inner classes
        /// <summary>
        /// Implements input component of the analog reservoir
        /// </summary>
        [Serializable]
        private class ReservoirInputBlock
        {
            //Constants
            //Attributes
            private int m_inputValuesCount;
            private double[] m_inputBiases;
            private double[] m_inputValues;
            private int m_reservoirNeuronsCount;
            private List<InputConnection>[] m_connections;

            //Constructor
            public ReservoirInputBlock(int inputValuesCount,
                                       int reservoirNeuronsCount,
                                       double biasScale,
                                       double inputWeightScale,
                                       int neuronsPerInput,
                                       Random rand
                                       )
            {
                m_inputValuesCount = inputValuesCount;
                m_reservoirNeuronsCount = reservoirNeuronsCount;
                //Input biases
                m_inputBiases = new double[m_reservoirNeuronsCount];
                rand.FillUniform(m_inputBiases, -1, 1, biasScale);
                //Input values
                m_inputValues = new double[m_inputValuesCount];
                m_inputValues.Populate(0);
                //Connections to reservoir neurons
                m_connections = new List<InputConnection>[m_reservoirNeuronsCount];
                for (int i = 0; i < m_reservoirNeuronsCount; i++)
                {
                    m_connections[i] = new List<InputConnection>(m_inputValuesCount * neuronsPerInput);
                }
                int[] neuronIdxs = new int[m_reservoirNeuronsCount];
                for (int fieldIdx = 0; fieldIdx < m_inputValuesCount; fieldIdx++)
                {
                    neuronIdxs.ShuffledIndices(rand);
                    for (int i = 0; i < neuronsPerInput; i++)
                    {
                        InputConnection connection = new InputConnection();
                        connection.FieldIdx = fieldIdx;
                        connection.Weight = AnalogReservoir.RandomWeight(rand, inputWeightScale);
                        m_connections[neuronIdxs[i]].Add(connection);
                    }
                }
                return;
            }
            //Properties
            public int InputValuesCount { get { return m_inputValuesCount; } }

            //Methods
            public void Update(double[] newInputValues)
            {
                newInputValues.CopyTo(m_inputValues, 0);
                return;
            }

            /// <summary>
            /// Computes input signal from input fields to be processed by specified neuron
            /// </summary>
            /// <param name="reservoirNeuronIdx">Reservoir neuron index</param>
            public double GetInputSignal(int reservoirNeuronIdx)
            {
                double signal = 0;
                if (m_connections[reservoirNeuronIdx].Count > 0)
                {
                    signal += m_inputBiases[reservoirNeuronIdx];
                    foreach (InputConnection connection in m_connections[reservoirNeuronIdx])
                    {
                        signal += m_inputValues[connection.FieldIdx] * connection.Weight;
                    }
                }
                return signal;
            }

            //Inner classes
            [Serializable]
            private class InputConnection
            {
                public int FieldIdx { get; set; } = 0;
                public double Weight { get; set; } = 0;
            }

        }//ReservoirInputBlock

    }//AnalogReservoir
}//Namespace
