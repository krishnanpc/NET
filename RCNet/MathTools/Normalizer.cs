﻿using System;

namespace RCNet.MathTools
{
    /// <summary>
    /// Implements the thread safe normalizer and denormalizer. Scales the input to desired normalized range and vice versa.
    /// Normalizer supports gausse data standardization
    /// </summary>
    [Serializable]
    public class Normalizer
    {
        //Constants
        //Default borders of normalization range
        public const double DefaultNormRangeMin = -1;
        public const double DefaultNormRangeMax = 1;
        //Default is to use the gausse standardization
        public const bool DefaultStandardizeSwitch = true;

        //Attributes
        private bool _standardize;
        private double _reserveRatio;
        private BasicStat _samplesStat;
        private Interval _normRange;

        //Constructors
        /// <summary>
        /// The copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public Normalizer(Normalizer source)
        {
            _standardize = source._standardize;
            _reserveRatio = source._reserveRatio;
            _samplesStat = new BasicStat(source._samplesStat);
            _normRange = new Interval(source._normRange);
            return;
        }

        /// <summary>
        /// Instantiates a normalizer
        /// </summary>
        /// <param name="reserveRatio">
        /// The reserve kept by the normalizer to protect against overflow if the future data
        /// would grow from a known range.
        /// </param>
        /// <param name="standardize">
        /// A switch if to apply the data standardization
        /// </param>
        public Normalizer(double reserveRatio, bool standardize = DefaultStandardizeSwitch)
        {
            _standardize = standardize;
            _normRange = new Interval(DefaultNormRangeMin, DefaultNormRangeMax);
            _samplesStat = new BasicStat();
            _reserveRatio = reserveRatio;
            return;
        }

        /// <summary>
        /// Constructs normalizer
        /// </summary>
        /// <param name="normRange">
        /// The range of normalized values
        /// </param>
        /// <param name="reserveRatio">
        /// The reserve kept by the normalizer to protect against overflow if the future data
        /// would grow from a known range.
        /// </param>
        /// <param name="standardize">
        /// A switch if to apply the data standardization
        /// </param>
        public Normalizer(Interval normRange, double reserveRatio, bool standardize = DefaultStandardizeSwitch)
        {
            _standardize = standardize;
            _normRange = new Interval(normRange);
            _samplesStat = new BasicStat();
            _reserveRatio = reserveRatio;
            return;
        }

        //Properties
        public bool Initialized { get { return (_samplesStat.NumOfSamples > 0 && _samplesStat.Min != _samplesStat.Max); } }
        public double ReserveRatio { get { return _reserveRatio; } }
        public bool Standardize { get { return _standardize; } }
        public Interval NormRange { get { return _normRange; } }
        public BasicStat SamplesStat { get { return _samplesStat; } }
        private double VMin { get { return _samplesStat.Min - ((_samplesStat.Span * _reserveRatio) / 2); } }
        private double VMax { get { return _samplesStat.Max + ((_samplesStat.Span * _reserveRatio) / 2); } }

        //Methods
        /// <summary>
        /// Adapts to the sample value
        /// </summary>
        /// <param name="sampleValue">Sample value</param>
        public void Adjust(double sampleValue)
        {
            _samplesStat.AddSampleValue(sampleValue);
            return;
        }

        /// <summary>
        /// The standard normalization
        /// </summary>
        /// <param name="min">Samples min</param>
        /// <param name="max">Samples max</param>
        /// <param name="v">Natural (sample) value to be normalized</param>
        /// <returns>Normalized v</returns>
        private double Normalize(double min, double max, double v)
        {
            return _normRange.Min + _normRange.Span * ((v - min) / (max - min));
        }

        /// <summary>
        /// The standard denormalization
        /// </summary>
        /// <param name="min">Samples min</param>
        /// <param name="max">Samples max</param>
        /// <param name="n">Normalized value</param>
        /// <returns>Natural value</returns>
        private double Naturalize(double min, double max, double n)
        {
            return min + (max - min) * ((n - _normRange.Min) / _normRange.Span);
        }

        /// <summary>
        /// Checks normalizer readyness
        /// </summary>
        private void CheckInitiated()
        {
            if (!Initialized)
            {
                throw new Exception("Not properly initialized");
            }
            return;
        }

        /// <summary>
        /// Computes the half of a gausse interval
        /// </summary>
        /// <returns>The half of a gausse interval</returns>
        private double ComputeGausseHalfInterval()
        {
            double gausseLo = Math.Abs((VMin - _samplesStat.ArithAvg) / _samplesStat.StdDev);
            double gausseHi = Math.Abs((VMax - _samplesStat.ArithAvg) / _samplesStat.StdDev);
            return Math.Max(gausseLo, gausseHi);
        }

        /// <summary>
        /// Normalizes the given natural (sample) value
        /// </summary>
        /// <param name="naturalValue">Natural value to be normalized</param>
        /// <returns>Normalized value</returns>
        public double Normalize(double naturalValue)
        {
            //Check readiness
            CheckInitiated();
            //Value preprocessing
            if (_standardize)
            {
                //Gausse standardization
                double gausseHalfInt = ComputeGausseHalfInterval();
                double gausseValue = (naturalValue - _samplesStat.ArithAvg) / _samplesStat.StdDev;
                //Normalization
                return Normalize(-gausseHalfInt, gausseHalfInt, gausseValue);
            }
            else
            {
                //Normalization
                return Normalize(VMin, VMax, naturalValue);
            }
        }

        /// <summary>
        /// Denormalizes the given normalized value
        /// </summary>
        /// <param name="normValue">Normalized value to be denormalized</param>
        /// <returns>Natural (denormalized) value</returns>
        public double Naturalize(double normValue)
        {
            //Check readiness
            CheckInitiated();
            //Value preprocessing
            if (_standardize)
            {
                //Denormalization
                double gausseHalfInt = ComputeGausseHalfInterval();
                double standardizedGausse = Naturalize(-gausseHalfInt, gausseHalfInt, normValue);
                //Destandardization -> natural value
                return (standardizedGausse * _samplesStat.StdDev) + _samplesStat.ArithAvg;
            }
            else
            {
                //Denormalization -> natural value
                return Naturalize(VMin, VMax, normValue);
            }
        }

        /// <summary>
        /// Computes what part of the natural values range is affected by the piece of the normalized range
        /// </summary>
        /// <param name="normSpan">Piece of normalized range</param>
        /// <returns>Piece of natural range</returns>
        public double ComputeNaturalSpan(double normSpan)
        {
            CheckInitiated();
            return ((VMax - VMin) * Math.Abs(normSpan)) * (1 - _reserveRatio);
        }

    }//Normalizer
}//Namespace