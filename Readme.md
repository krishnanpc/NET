# Reservoir Computing for .NET (RCNet)
The aim of the project is to make the [reservoir computing](https://en.wikipedia.org/wiki/Reservoir_computing) methods  easy to use and available for .net platform without dependency on external heterogeneous libraries.
Two main reservoir computing methods are called Echo State Network (ESN) and Liquid State Machine (LSM).
The implemented solution supports both of these methods. However, since ESN and LSM are based on very similar general principles, RCNet brings the option to combine them at the same time. It means the possibility to design complex "hybrid" recurrent networks with spiking and analog neurons working together. This approach, as I believe, opens up new interesting possibilities. This general implementation is called "**State Machine**" in the context of RCNet.
<br/>
<br/>
Main documentation is located on project's wiki (https://github.com/okozelsk/NET/wiki). The WiKi documentation is unfortunately still delayed and does not match the latest version.
<br/>
Questions, ideas and suggestions for improvements, usage experiences, bug alerts, constructive comments, etc.... are welcome.
<br/>
To contact me use my email address oldrich.kozelsky@email.cz

## Technical information
 - Source code is written in C# 6.0
 - Necessary components are serializable
 - Backward compatibility with earlier releases is not guaranteed, SW is still under dynamic development

## Demo application
Main RCNet functionality is demonstrated in a simple demo application (/Demo/DemoConsoleApp). Application has no startup parameters, all necessary settins are specified in DemoSettings.xml file. DemoSettings.xml has to be in the same folder as the executable DemoConsoleApp.exe. Application performs sequence of demo cases defined in DemoSettings.xml. The input data for each demo case must be stored in the csv file format. You can easily modify DemoSettings.xml and configure your own tasks or modify and tune existing ones.


## Overview of the main implemented components
### Signal generators
|Component|Description|
|--|--|
|ConstGenerator|Generates constant signal|
|MackeyGlassGenerator|Generates Mackey-Glass chaotic signal|
|RandomGenerator|Generates random signal|
|SinusoidalGenerator|Generates sinusoidal signal|

### XML
|Component|Description|
|--|--|
|ElemValidator|Implements ugly and unefficient method to validate element against specified xsd type. It complements the apparently missing .net method and is necessary to comply with the overall RCNet xml concept.|
|DocValidator|Provides the xml loading/validation functionalities|

### Math
|Component|Description|
|--|--|
|Normalizer|Supports data normalization/naturalization and Gaussian standardization|
|BasicStat|Implements the simple and thread safe statistics|
|ODENumSolver|Ordinary Differential Equations (ODE) Numerical Solver (Euler and RK4 methods)|
|Vector|Class represents the mathematical vector of double values supporting basic operations|
|Matrix|Class represents the mathematical matrix of double values supporting basic operations, LU and the Power Iteration method for the largest EV estimation|
|EVD|Full eigen values and vectors decomposition of a matrix|
|SVD|Singular values decomposition of a matrix|
|QRD|QR decomposition of a matrix|
|PhysUnit|Encaptualates SI physical unit|
|"RandomValue"|Supports Uniform and Gaussian distributions|
|Others|Interval, BinErrStat, BinDistribution, Bitwise, Combinatorics, Factorial, WeightedAvg, HurstExpEstim, ...|

### Data handling
|Component|Description|
|--|--|
|PatternBundle|Bundle of pattern of vectors and desired output vector. Supports upload from csv file|
|TimeSeriesBundle|Bundle of input vector and desired output vector. Supports upload from csv file|
|ValidationBundle|Bundle of computed vector and desired output vector|
|BundleNormalizer|Helper class for easy standardization and normalization/naturalization of sample data bundle|

### Analog neuron activation functions
|Component|Description|
|--|--|
|BentIdentity|Bent identity activation function|
|Elliot|Elliot activation function (aka Softsign)|
|Gaussian|Gaussian activation function|
|Identity|Identity activation function (aka Linear)|
|ISRU|ISRU (Inverse Square Root Unit) activation function|
|LeakyReLU|Leaky ReLU (Leaky Rectified Linear Unit) activation function|
|Sigmoid|Sigmoid activation function|
|Sinc|Sinc activation function|
|Sinusoid|Sinusoid activation function|
|SoftExponential|Soft exponential activation function|
|SoftPlus|Soft Plus activation function|
|TanH|TanH activation function|
See the [wiki pages.](https://en.wikipedia.org/wiki/Activation_function)

### Spiking neuron activation functions
|Component|Description|
|--|--|
|SimpleIF|Simple Integrate and Fire activation function|
|LeakyIF|Leaky Integrate and Fire activation function|
|ExpIF|Exponential Integrate and Fire activation function|
|AdExpIF|Adaptive Exponential Integrate and Fire activation function|
|IzhikevichIF|Izhikevich Integrate and Fire activation function (model "one fits all")|
See the [wiki pages.](https://en.wikipedia.org/wiki/Biological_neuron_model)

### Non-recurrent networks and trainers
|Component|Description|
|--|--|
|FeedForwardNetwork|Implements the feed forward network supporting multiple hidden layers|
|LinRegrTrainer|Implements the linear regression trainer of the feed forward network. This is the special case trainer for FF network having no hidden layers and Identity output activation function|
|RPropTrainer|Resilient propagation (iRPROP+) trainer of the feed forward network|
|ParallelPerceptron|Implements the parallel perceptron network|
|PDeltaRuleTrainer|P-Delta rule trainer of the parallel perceptron network|

### State Machine components
|Component|Description|
|--|--|
|StaticSynapse|Computes constantly weighted signal from source to target neuron. Supports signal delay|
|DynamicSynapse|Computes dynamically weighted signal from source to target neuron using pre-synaptic and/or post-synaptic "Short-Term-Plasticity". Supports signal delay|
|InputNeuron|Input neuron is the special type of very simple neuron. Its purpose is only to mediate input analog value for a synapse|
|AnalogNeuron|Analog neuron produces analog output according to its analog activation function. Main features: Retainment (leaky integrator), The second power as augmented readout state|
|SpikingNeuron|Spiking neuron produces spikes according to its spiking activation function. Main features: Membrane potential as primary readout state, Firing rate as augmented readout state|
|Reservoir|Implements recurrent network supporting analog and spiking neurons working directly together. Main features: SpectralRadius, Multiple 3D pools of neurons, Pool to pool connections. It can work as the Echo State Network reservoir, Liquid State Machine reservoir or Mixed reservoir|
|ReadoutUnit|Readout unit does the Forecast or Classification. Contains trained output unit and related important error statistics. Trained unit can be the Feed Forward Network or the Parallel Perceptron Network|
|ReadoutLayer|Class implements common readout layer concept for the reservoir computing methods. Supports x-fold cross validation method and clustering of the trained readout units.|
|StateMachine|Encaptulates the State Machine Network. Supports multiple internal reservoirs having multiple interconnected/cooperating analog and spiking neuron pools. Supports virtual input data associated with predefined signal generators. Supports two input feeding regimes: Continuous and Patterned|

