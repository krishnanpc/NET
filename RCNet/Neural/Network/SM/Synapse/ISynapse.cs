﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.Neural.Network.SM.Neuron;
using RCNet.MathTools;

namespace RCNet.Neural.Network.SM.Synapse
{
    /// <summary>
    /// Synapse is a transporter of the weighted signal from source neuron to target neuron.
    /// </summary>
    public interface ISynapse
    {
        //Properties
        /// <summary>
        /// Source neuron - signal emitor
        /// </summary>
        INeuron SourceNeuron { get; }

        /// <summary>
        /// Target neuron - signal receiver
        /// </summary>
        INeuron TargetNeuron { get; }

        /// <summary>
        /// Euclidean distance between SourceNeuron and TargetNeuron
        /// </summary>
        double Distance { get; }

        /// <summary>
        /// Weight of the synapse
        /// </summary>
        double Weight { get; }

        /// <summary>
        /// Signal delay
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Efficacy statistics of the synapse
        /// </summary>
        BasicStat EfficacyStat { get; }

        //Methods
        /// <summary>
        /// Resets synapse.
        /// </summary>
        /// <param name="statistics">Specifies whether to reset also internal statistics</param>
        void Reset(bool statistics);

        /// <summary>
        /// Rescales the synapse weight.
        /// </summary>
        /// <param name="scale">Scale factor</param>
        void Rescale(double scale);

        /// <summary>
        /// Sets the synapse delay
        /// </summary>
        /// <param name="delay">Signal delay (reservoir cycles)</param>
        void SetDelay(int delay);

        /// <summary>
        /// Returns signal to be delivered to target neuron.
        /// </summary>
        /// <param name="collectStatistics">Specifies whether to update internal statistics</param>
        double GetSignal(bool collectStatistics);

    }//ISynapse

}//Namespace
