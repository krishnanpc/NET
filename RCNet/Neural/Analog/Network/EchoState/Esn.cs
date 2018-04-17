﻿using System;
using System.Collections.Generic;
using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.Neural.Data;
using RCNet.Neural.Analog.Reservoir;
using RCNet.Neural.Analog.Readout;

namespace RCNet.Neural.Analog.Network.EchoState
{
    /// <summary>
    /// Implements the Echo State Network.
    /// </summary>
    [Serializable]
    public class Esn
    {
        //Delegates
        /// <summary>
        /// Informative callback function to inform about predictors collection progress.
        /// </summary>
        /// <param name="totalNumOfInputs">Total number of inputs to be processed</param>
        /// <param name="numOfProcessedInputs">Number of processed inputs</param>
        /// <param name="userObject">An user object</param>
        public delegate void PredictorsCollectionCallbackDelegate(int totalNumOfInputs,
                                                                  int numOfProcessedInputs,
                                                                  Object userObject
                                                                  );
        //Attributes
        /// <summary>
        /// Esn settings used for instance creation.
        /// </summary>
        private EsnSettings _settings;
        /// <summary>
        /// Random generator
        /// </summary>
        private System.Random _rand;
        /// <summary>
        /// Collection of reservoir instances.
        /// </summary>
        private List<ReservoirInstance> _reservoirInstanceCollection;
        /// <summary>
        /// Number of Esn predictors
        /// </summary>
        private int _numOfPredictors;
        /// <summary>
        /// Readout layer.
        /// </summary>
        private ReadoutLayer _readoutLayer;

        //Constructor
        /// <summary>
        /// Constructs an instance of Echo State Network
        /// </summary>
        /// <param name="settings">Echo State Network settings</param>
        public Esn(EsnSettings settings)
        {
            _settings = settings.DeepClone();
            //Random object
            if (_settings.RandomizerSeek < 0) _rand = new System.Random();
            else _rand = new System.Random(_settings.RandomizerSeek);
            //Build structure
            //Reservoir instance(s)
            _numOfPredictors = 0;
            _reservoirInstanceCollection = new List<ReservoirInstance>(_settings.ReservoirInstanceDefinitionCollection.Count);
            foreach(EsnSettings.ReservoirInstanceDefinition instanceDefinition in _settings.ReservoirInstanceDefinitionCollection)
            {
                ReservoirInstance reservoirInstance = new ReservoirInstance(instanceDefinition, _settings.RandomizerSeek);
                _reservoirInstanceCollection.Add(reservoirInstance);
                _numOfPredictors += reservoirInstance.ReservoirObj.NumOfOutputPredictors;
            }
            if(_settings.RouteInputToReadout)
            {
                _numOfPredictors += _settings.InputFieldNameCollection.Count;
            }
            //Readout layer
            _readoutLayer = new ReadoutLayer(_settings.TaskType, _settings.ReadoutLayerConfig, _rand);
            return;
        }

        //Properties
        /// <summary>
        /// Collection of the error statistics.
        /// Each cluster of readout units related to output field  has one summary error statistics in the collection.
        /// Order is the same as the order of output fields.
        /// </summary>
        public List<ReadoutLayer.ClusterErrStatistics> ClusterErrStatisticsCollection { get { return _readoutLayer.ClusterErrStatisticsCollection; } }

        //Methods
        /// <summary>
        /// Sets Esn internal state to initial state
        /// </summary>
        /// <param name="resetStatistics">Specifies whether to reset internal statistics</param>
        private void Reset(bool resetStatistics)
        {
            foreach(ReservoirInstance reservoirInstanceData in _reservoirInstanceCollection)
            {
                reservoirInstanceData.Reset(resetStatistics);
            }
            return;
        }

        /// <summary>
        /// Pushes input values into the reservoirs and returns the predictors
        /// </summary>
        /// <param name="inputValues">Esn input values</param>
        /// <param name="collectStatesStatistics">
        /// The parameter indicates whether the internal states may be included into the statistics
        /// </param>
        private double[] PushInput(double[] inputValues, bool collectStatesStatistics)
        {
            double[] predictors = new double[_numOfPredictors];
            int predictorsIdx = 0;
            //Compute reservoir(s)
            foreach (ReservoirInstance resInstance in _reservoirInstanceCollection)
            {
                double[] reservoirInput = new double[resInstance.InstanceDefinition.InputFieldMappingCollection.Count];
                for(int i = 0; i < resInstance.InstanceDefinition.InputFieldMappingCollection.Count; i++)
                {
                    reservoirInput[i] = inputValues[resInstance.InstanceDefinition.InputFieldMappingCollection[i]];
                }
                //Compute reservoir
                resInstance.ReservoirObj.Compute(reservoirInput, collectStatesStatistics);
                resInstance.ReservoirObj.CopyPredictorsTo(predictors, predictorsIdx);
                predictorsIdx += resInstance.ReservoirObj.NumOfOutputPredictors;
            }
            if(_settings.RouteInputToReadout)
            {
                inputValues.CopyTo(predictors, predictorsIdx);
            }
            return predictors;
        }

        /// <summary>
        /// Pushes input pattern into the reservoirs and returns the predictors
        /// </summary>
        /// <param name="inputPattern">Input pattern</param>
        private double[] PushInput(List<double[]> inputPattern)
        {
            double[] predictors = new double[_numOfPredictors];
            int predictorsIdx = 0;
            //Compute reservoir(s)
            foreach (ReservoirInstance resInstance in _reservoirInstanceCollection)
            {
                //Reset reservoir states but keep internal statistics
                resInstance.Reset(false);
                double[] reservoirInput = new double[resInstance.InstanceDefinition.InputFieldMappingCollection.Count];
                foreach (double[] inputVector in inputPattern)
                {
                    for (int i = 0; i < resInstance.InstanceDefinition.InputFieldMappingCollection.Count; i++)
                    {
                        reservoirInput[i] = inputVector[resInstance.InstanceDefinition.InputFieldMappingCollection[i]];
                    }
                    //Compute the reservoir
                    resInstance.ReservoirObj.Compute(reservoirInput, true);
                }
                resInstance.ReservoirObj.CopyPredictorsTo(predictors, predictorsIdx);
                predictorsIdx += resInstance.ReservoirObj.NumOfOutputPredictors;
            }
            return predictors;
        }

        /// <summary>
        /// Compute fuction for classification tasks.
        /// Processes given input pattern and computes the output.
        /// </summary>
        /// <param name="inputPattern">Input pattern</param>
        /// <returns>Computed output values</returns>
        public double[] Compute(List<double[]> inputPattern)
        {
            if(_settings.TaskType == CommonEnums.TaskType.Prediction)
            {
                throw new Exception("This version of Compute function is useable only for the classification or hybrid task type.");
            }
            double[] predictors = PushInput(inputPattern);
            //Compute output
            return _readoutLayer.Compute(predictors);
        }

        /// <summary>
        /// Compute fuction for time series prediction tasks.
        /// Processes given input values and computes (predicts) the output.
        /// </summary>
        /// <param name="inputVector">Input values</param>
        /// <returns>Computed output values</returns>
        public double[] Compute(double[] inputVector)
        {
            if (_settings.TaskType != CommonEnums.TaskType.Prediction)
            {
                throw new Exception("This version of Compute function is useable only for the prediction task type.");
            }
            //Push input into the Esn
            double[] predictors = PushInput(inputVector, true);
            //Compute output
            return _readoutLayer.Compute(predictors);
        }

        /// <summary>
        /// If feedback is defined in one or more of the reservoirs, the PushFeedback function must be called
        /// before calling the "Compute" function. The previous real values should be passed to the function, which is
        /// then processed in a similar way as the input values.
        /// The exception is the first call to Compute after network training. Before this first call, PushFeedback
        /// does not have to be called because it has already been called in the training.
        /// </summary>
        /// <param name="lastRealValues">
        /// Previous real values in the same order as the Esn output fields are defined.
        /// If you want to do more forward predictions, the previous real values are of course not available, and in
        /// this case use the previous values calculated by the network. However, it is very likely that the network
        /// error will grow steeply over time and the prediction ability will decrease.
        /// </param>
        public void PushFeedback(double[] lastRealValues)
        {
            if (_settings.TaskType != CommonEnums.TaskType.Prediction)
            {
                throw new Exception("PushFeedback function is useable only for the prediction task type.");
            }
            foreach (ReservoirInstance resInstance in _reservoirInstanceCollection)
            {
                if (resInstance.InstanceDefinition.ReservoirSettings.FeedbackFeature)
                {
                    double[] feedbackValues = new double[resInstance.InstanceDefinition.FeedbackFieldMappingCollection.Count];
                    for (int i = 0; i < resInstance.InstanceDefinition.FeedbackFieldMappingCollection.Count; i++)
                    {
                        feedbackValues[i] = lastRealValues[resInstance.InstanceDefinition.FeedbackFieldMappingCollection[i]];
                    }
                    resInstance.ReservoirObj.SetFeedback(feedbackValues);
                }
            }
            return;
        }

        /// <summary>
        /// Collects the key statistics of each reservoir instance.
        /// It is very important to follow these statistics and adjust the weights in the reservoirs so that the neurons
        /// in the reservoir are not oversaturated.
        /// </summary>
        /// <returns>Collection of key statistics of each reservoir instance</returns>
        private List<AnalogReservoirStat> CollectReservoirInstancesStatatistics()
        {
            List<AnalogReservoirStat> stats = new List<AnalogReservoirStat>();
            foreach(ReservoirInstance resInstance in _reservoirInstanceCollection)
            {
                stats.Add(resInstance.ReservoirObj.CollectStatistics());
            }
            return stats;
        }

        /// <summary>
        /// Prepares input for regression stage of Esn training for the classification task type.
        /// All input patterns are processed by internal reservoirs and the corresponding Esn predictors are recorded.
        /// </summary>
        /// <param name="dataSet">
        /// The bundle containing known sample input patterns and desired output vectors
        /// </param>
        /// <param name="informativeCallback">
        /// Function to be called after each processed input.
        /// </param>
        /// <param name="userObject">
        /// The user object to be passed to informativeCallback.
        /// </param>
        public RegressionStageInput PrepareRegressionStageInput(PatternBundle dataSet,
                                                                PredictorsCollectionCallbackDelegate informativeCallback = null,
                                                                Object userObject = null
                                                                )
        {
            if (_settings.TaskType == CommonEnums.TaskType.Prediction)
            {
                throw new Exception("This version of PrepareRegressionStageInput function is useable only for the classification or hybrid task type.");
            }
            //RegressionStageInput allocation
            RegressionStageInput rsi = new RegressionStageInput();
            rsi.PredictorsCollection = new List<double[]>(dataSet.InputPatternCollection.Count);
            rsi.IdealOutputsCollection = new List<double[]>(dataSet.OutputVectorCollection.Count);
            //Collection
            for (int dataSetIdx = 0; dataSetIdx < dataSet.InputPatternCollection.Count; dataSetIdx++)
            {
                //Push input data into the Esn
                double[] predictors = PushInput(dataSet.InputPatternCollection[dataSetIdx]);
                rsi.PredictorsCollection.Add(predictors);
                //Add desired outputs
                rsi.IdealOutputsCollection.Add(dataSet.OutputVectorCollection[dataSetIdx]);
                //Informative callback
                if(informativeCallback != null)
                {
                    informativeCallback(dataSet.InputPatternCollection.Count, dataSetIdx + 1, userObject);
                }
            }
            //Collect reservoirs statistics
            rsi.ReservoirStatCollection = CollectReservoirInstancesStatatistics();
            return rsi;
        }

        /// <summary>
        /// Prepares input for regression stage of Esn training for the time series prediction task.
        /// All input vectors are processed by internal reservoirs and the corresponding Esn predictors are recorded.
        /// </summary>
        /// <param name="dataSet">
        /// The bundle containing known sample input and desired output vectors (in time order)
        /// </param>
        /// <param name="numOfBootSamples">
        /// Number of boot samples from the beginning of all samples.
        /// The purpose of the boot samples is to ensure that the states of the neurons in the reservoir
        /// depend only on the time series data and not on the initial state of the neurons in the reservoir.
        /// The number of boot samples depends on the size and configuration of the reservoirs.
        /// It is usually sufficient to set the number of boot samples equal to the number of neurons in the largest reservoir.
        /// </param>
        /// <param name="informativeCallback">
        /// Function to be called after each processed input.
        /// </param>
        /// <param name="userObject">
        /// The user object to be passed to informativeCallback.
        /// </param>
        public RegressionStageInput PrepareRegressionStageInput(TimeSeriesBundle dataSet,
                                                                int numOfBootSamples,
                                                                PredictorsCollectionCallbackDelegate informativeCallback = null,
                                                                Object userObject = null
                                                                )
        {
            if (_settings.TaskType != CommonEnums.TaskType.Prediction)
            {
                throw new Exception("This version of PrepareRegressionStageInput function is useable only for the prediction task type.");
            }
            int dataSetLength = dataSet.InputVectorCollection.Count;
            //RegressionStageInput allocation
            RegressionStageInput rsi = new RegressionStageInput();
            rsi.PredictorsCollection = new List<double[]>(dataSetLength - numOfBootSamples);
            rsi.IdealOutputsCollection = new List<double[]>(dataSetLength - numOfBootSamples);
            //Reset the internal states and statistics
            Reset(true);
            //Collection
            for (int dataSetIdx = 0; dataSetIdx < dataSetLength; dataSetIdx++)
            {
                bool afterBoot = (dataSetIdx >= numOfBootSamples);
                //Push input data into the Esn
                double[] predictors = PushInput(dataSet.InputVectorCollection[dataSetIdx], afterBoot);
                //Is boot sequence passed? Collect predictors?
                if (afterBoot)
                {
                    //YES
                    rsi.PredictorsCollection.Add(predictors);
                    //Desired outputs
                    rsi.IdealOutputsCollection.Add(dataSet.OutputVectorCollection[dataSetIdx]);
                }
                PushFeedback(dataSet.OutputVectorCollection[dataSetIdx]);
                //An informative callback
                if (informativeCallback != null)
                {
                    informativeCallback(dataSetLength, dataSetIdx + 1, userObject);
                }
            }

            //Collect reservoirs statistics
            rsi.ReservoirStatCollection = CollectReservoirInstancesStatatistics();
            return rsi;
        }

        /// <summary>
        /// Trains the Esn network readout layer.
        /// </summary>
        /// <param name="rsi">
        /// RegressionStageInput object prepared by PrepareRegressionStageInput function
        /// </param>
        /// <param name="regressionController">
        /// Optional. see Regression.RegressionCallbackDelegate
        /// </param>
        /// <param name="regressionControllerData">
        /// Optional custom object to be passed to regressionController together with other standard information
        /// </param>
        public ValidationBundle RegressionStage(RegressionStageInput rsi,
                                                ReadoutUnit.RegressionCallbackDelegate regressionController = null,
                                                Object regressionControllerData = null
                                                )
        {
            return _readoutLayer.Build(rsi.PredictorsCollection,
                                       rsi.IdealOutputsCollection,
                                       regressionController,
                                       regressionControllerData
                                       );
        }


        //Inner classes
        /// <summary>
        /// Contains prepared data for regression stage and statistics of the reservoir(s)
        /// </summary>
        [Serializable]
        public class RegressionStageInput
        {
            /// <summary>
            /// Collection of Esn predictors
            /// </summary>
            public List<double[]> PredictorsCollection { get; set; } = null;
            /// <summary>
            /// Collection of the ideal outputs
            /// </summary>
            public List<double[]> IdealOutputsCollection { get; set; } = null;
            /// <summary>
            /// Collection of statistics of the Esn's reservoir(s)
            /// </summary>
            public List<AnalogReservoirStat> ReservoirStatCollection { get; set; } = null;

        }//RegressionStageInput


        /// <summary>
        /// Holds the instantiated reservoir together with its definition.
        /// </summary>
        [Serializable]
        private class ReservoirInstance
        {
            //Attribute properties
            /// <summary>
            /// Instance definition.
            /// </summary>
            public EsnSettings.ReservoirInstanceDefinition InstanceDefinition { get; }
            /// <summary>
            /// Instantiated reservoir.
            /// </summary>
            public AnalogReservoir ReservoirObj { get; }

            //Constructor
            public ReservoirInstance(EsnSettings.ReservoirInstanceDefinition instanceDefinition, int randomizerSeek)
            {
                //Store definition
                InstanceDefinition = instanceDefinition;
                //Create reservoir
                ReservoirObj = new AnalogReservoir(InstanceDefinition.InstanceName,
                                                   InstanceDefinition.InputFieldMappingCollection.Count,
                                                   InstanceDefinition.ReservoirSettings,
                                                   InstanceDefinition.AugmentedStates,
                                                   randomizerSeek
                                                   );
                return;
            }

            //Methods
            /// <summary>
            /// Resets reservoir internal state to the initial state.
            /// </summary>
            /// <param name="resetStatistics">Specifies whether to reset internal statistics</param>
            public void Reset(bool resetStatistics)
            {
                ReservoirObj.Reset(resetStatistics);
                return;
            }

        }//ReservoirInstance

    }//Esn
}//Namespace