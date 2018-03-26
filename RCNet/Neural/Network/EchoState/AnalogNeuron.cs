﻿using System;
using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.Neural.Activation;

namespace RCNet.Neural.Network.EchoState
{
    /// <summary>
    /// Implements the analog reservoir's neuron
    /// </summary>
    [Serializable]
    public class AnalogNeuron
    {
        //Constants
        //Maximum retainment rate
        public const double RetainmentMaxRate = 0.99;
        //Attributes
        /// <summary>
        /// Neuron's activation function
        /// </summary>
        private IActivationFunction _activation;
        /// <summary>
        /// Neuron's retainment rate
        /// </summary>
        private double _retainmentRate;
        /// <summary>
        /// Stored previous neuron's state
        /// </summary>
        private double _previousState;
        /// <summary>
        /// Neuron's current state.
        /// </summary>
        private double _currentState;
        //Attribute properties
        /// <summary>
        /// Neuron's states statistics
        /// </summary>
        public BasicStat StatesStat { get; }

        //Constructor
        /// <summary>
        /// Instantiates the neuron to be used in the analog reservoir.
        /// If retainmentRate is greater than 0, neuron is the leaky integrator.
        /// </summary>
        /// <param name="activation">Neuron's activation function</param>
        /// <param name="retainmentRate">Neuron's retainment rate</param>
        public AnalogNeuron(IActivationFunction activation, double retainmentRate = 0)
        {
            _activation = activation;
            _retainmentRate = retainmentRate.Bound(0, RetainmentMaxRate);
            _previousState = _currentState = 0;
            StatesStat = new BasicStat();
            return;
        }

        //Properties
        public double RetainmentRate { get { return _retainmentRate; } }
        public double CurrentState { get { return _currentState; } }
        public double PreviousState { get { return _previousState; } }

        /// <summary>
        /// Resets neuron to its initial state (0).
        /// </summary>
        public virtual void Reset()
        {
            _previousState = _currentState = 0;
            return;
        }

        /// <summary>
        /// Computes neuron's current state and updates statistics.
        /// </summary>
        public void Compute(double signal, bool collectStatistics)
        {
            _currentState = (_retainmentRate * _currentState) + (1d - _retainmentRate) * _activation.Compute(signal);
            if(collectStatistics)StatesStat.AddSampleValue(_currentState);
            return;
        }

        /// <summary>
        /// Stores current state to be accesible later.
        /// </summary>
        public void StoreCurrentState()
        {
            _previousState = _currentState;
            return;
        }
    }
}