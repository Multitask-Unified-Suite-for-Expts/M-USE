# Tobii Pro SDK 

Welcome to the Tobii Pro SDK for .net developers. It allows easy access to configuration and data streams from Tobii Pro screen based eye trackers such as Tobii Pro Spectrum, Tobii Pro Fusion and Tobii Pro Nano, but also older trackers such as Tobii TX300 and Tobii Pro X2/X3, a comprehensive guide of supported devices is available [here](http://developer.tobiipro.com/tobiiprosdk/supportedeyetrackers.html).

## License

The usage of the Tobii Pro SDK is handled by the [Tobii Software Development License Agreement](license.md).

## Prerequisites

Your project needs to target one of the following:
* .NET Framework 4.6.1 or later
* .NET 5.0 or later
* .NET Core 2.0 or later

## Installation

The easiest way to get started is to use the NuGet package for Tobii Pro SDK

You can install using NuGet like this:

nuget install Tobii.Research.x64


Or select it from the NuGet packages UI on Visual Studio.

Alternatively, you can [download it](https://www.nuget.org/profiles/TobiiProSDK) directly.

## Documentation

The documentation for the Tobii Pro SDK is available at [developer.tobiipro.com](https://developer.tobiipro.com). The documentation site has reference documentation for all language bindings of the SDK (there are also python, matlab, c/c++ bindings). For .NET developers, the following sections are most relevant:

* [Getting started](http://developer.tobiipro.com/NET/dotnet-getting-started.html)
* [Reference Guide](http://developer.tobiipro.com/NET/dotnet-sdk-reference-guide.html)

## Usage

Here are some quick samples to get you started:

Finding all eye trackers
```csharp
var eyeTrackers = EyeTrackingOperations.FindAllEyeTrackers();
            foreach (var eyeTracker in eyeTrackers)
            {
                Console.WriteLine($"{eyeTracker.Address}, {eyeTracker.DeviceName}, {eyeTracker.Model}, {eyeTracker.SerialNumber}");
            }
```

Subscribe to gaze:
```
        private void SubscribeGazeData(IEyeTracker eyeTracker)
        {
            // Start listening to gaze data.
            eyeTracker.GazeDataReceived += EyeTracker_GazeDataReceived;
            // Wait for some data to be received.
            System.Threading.Thread.Sleep(2000);
            // Stop listening to gaze data.
            eyeTracker.GazeDataReceived -= EyeTracker_GazeDataReceived;
        }

        private void EyeTracker_GazeDataReceived(object sender, GazeDataEventArgs e)
        {
            if (e.leftEye.GazePoint.Validity == Validity.Valid)
            {
                var left = e.LeftEye.GazePoint.PositionOnDisplayArea;
                Console.WriteLine($"Left gaze: ({left.X}, {right.Y)");
            }
        }
```
