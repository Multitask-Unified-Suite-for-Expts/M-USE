using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EyeTrackerData_Namespace
{
    public class TobiiGazeSample
    {
        public int rightPupilValidity;
        public int leftPupilValidity;
        public double[] rightGazePointOnDisplayArea;
        public double[] leftGazePointOnDisplayArea;
        public int rightGazeOriginValidity;
        public int leftGazeOriginValidity;
        public double[] rightGazeOriginInUserCoordinateSystem;
        public double[] leftGazeOriginInUserCoordinateSystem;
        public double[] rightGazePointInUserCoordinateSystem;
        public double[] leftGazePointInUserCoordinateSystem;
        public double rightPupilDiameter;
        public double leftPupilDiameter;
        public double[] rightGazeOriginInTrackboxCoordinateSystem;
        public double[] leftGazeOriginInTrackboxCoordinateSystem;
        public int rightGazePointValidity;
        public int leftGazePointValidity;
        public double systemTimeStamp;
    }
    
}