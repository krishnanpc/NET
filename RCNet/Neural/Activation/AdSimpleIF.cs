﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.Extensions;
using RCNet.MathTools;

namespace RCNet.Neural.Activation
{
    /// <summary>
    /// Implements simple form of Adaptive Integrate and Fire neuron model
    /// </summary>
    [Serializable]
    public class AdSimpleIF : IActivationFunction
    {
        //Constants
        private const double Spike = 1d;
        private const double StimuliIncreaseBorderCoeff = 0.1d;
        private const double StimuliIncreaseAlpha = 1.1d;
        private const double StimuliDecreaseBorderCoeff = 0.5d;
        private const double StimuliDecreaseAlpha = 0.5d;

        //Attributes
        private static readonly Interval _outputRange = new Interval(0, 1);
        private readonly double _membraneResistance;
        private readonly double _membraneDecayRate;
        private readonly double _restV;
        private readonly double _resetV;
        private readonly double _firingThresholdV;
        private readonly double _initialStimuliCoeff;
        private double _stimuliCoeff;
        private double _membraneV;

        /// <summary>
        /// Constructs an initialized instance
        /// </summary>
        /// <param name="settings">Encapsulated arguments</param>
        public AdSimpleIF(AdSimpleIFSettings settings)
        {
            _membraneResistance = settings.Resistance;
            _membraneDecayRate = settings.DecayRate;
            _restV = 0;
            _resetV = Math.Abs(settings.ResetV);
            _firingThresholdV = Math.Abs(settings.FiringThresholdV);
            _initialStimuliCoeff = settings.StimuliCoeff;
            InternalStateRange = new Interval(_restV, _firingThresholdV);
            Reset();
            return;
        }

        //Properties
        /// <summary>
        /// Type of the output
        /// </summary>
        public ActivationFactory.FunctionOutputSignalType OutputSignalType { get { return ActivationFactory.FunctionOutputSignalType.Spike; } }

        /// <summary>
        /// Output signal range
        /// </summary>
        public Interval OutputSignalRange { get { return _outputRange; } }

        /// <summary>
        /// Specifies whether the activation function supports derivative calculation
        /// </summary>
        public bool SupportsComputeDerivativeMethod { get { return false; } }

        /// <summary>
        /// Specifies whether the activation function is depending on its previous states
        /// </summary>
        public bool Stateless { get { return false; } }

        /// <summary>
        /// Normal range of the internal state
        /// </summary>
        public Interval InternalStateRange { get; }

        /// <summary>
        /// Internal state
        /// </summary>
        public double InternalState { get { return _membraneV; } }

        //Methods
        /// <summary>
        /// Resets function to its initial state
        /// </summary>
        public void Reset()
        {
            _stimuliCoeff = _initialStimuliCoeff;
            _membraneV = _restV;
            return;
        }

        /// <summary>
        /// Updates state of the membrane according to an input stimuli and when the firing
        /// condition is met, it produces a spike
        /// </summary>
        /// <param name="x">Input stimuli (interpreted as an electric current)</param>
        public double Compute(double x)
        {
            x = (x * _stimuliCoeff).Bound();
            double spike = 0;
            if (_membraneV >= _firingThresholdV)
            {
                //Membrane potential after spike
                _membraneV = _resetV;
            }
            //Compute membrane new potential
            double inputVoltage = _membraneResistance * x;
            _membraneV = _restV + (_membraneV - _restV) * (1d - _membraneDecayRate) + inputVoltage;
            //Adaptation
            if (inputVoltage > 0)
            {
                if (inputVoltage >= (_firingThresholdV - _resetV) * StimuliDecreaseBorderCoeff)
                {
                    _stimuliCoeff *= StimuliDecreaseAlpha;
                }
                else if (inputVoltage <= (_firingThresholdV - _resetV) * StimuliIncreaseBorderCoeff)
                {
                    _stimuliCoeff *= StimuliIncreaseAlpha;
                }
            }
            //Output
            if (_membraneV >= _firingThresholdV)
            {
                spike = Spike;
                _membraneV = _firingThresholdV;
            }
            return spike;
        }

        /// <summary>
        /// Unsupported functionality!!!
        /// Computes derivative of the activation input (does not change internal state)
        /// </summary>
        /// <param name="c">The result of the activation (Compute method)</param>
        /// <param name="x">Activation input (x argument of the Compute method)</param>
        public double ComputeDerivative(double c = double.NaN, double x = double.NaN)
        {
            throw new NotImplementedException("ComputeDerivative is unsupported method in case of spiking activation.");
        }

    }//AdSimpleIF

}//Namespace
