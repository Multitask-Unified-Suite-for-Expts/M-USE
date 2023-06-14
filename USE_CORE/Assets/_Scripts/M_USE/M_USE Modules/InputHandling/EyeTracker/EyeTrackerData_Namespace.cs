using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_DisplayManagement;
using USE_Settings;

namespace EyeTrackerData_Namespace
{
    public class TobiiGazeSample
    {
        public string rightPupilValidity;
        public string leftPupilValidity;

        public string rightGazeOriginValidity;
        public string leftGazeOriginValidity;

        public string rightGazePointValidity;
        public string leftGazePointValidity;

        public Vector2 rightGazePointOnDisplayArea;
        public Vector2 leftGazePointOnDisplayArea;
        
		public Vector3 rightGazeOriginInUserCoordinateSystem;
        public Vector3 leftGazeOriginInUserCoordinateSystem;
        
		public Vector3 rightGazePointInUserCoordinateSystem;
        public Vector3 leftGazePointInUserCoordinateSystem;
        
		public Vector3 rightGazeOriginInTrackboxCoordinateSystem;
        public Vector3 leftGazeOriginInTrackboxCoordinateSystem;

        public float rightPupilDiameter;
        public float leftPupilDiameter;

        public float systemTimeStamp;
    }
}
