using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Common_Namespace;
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
 /*   public class CalibrationResult
    {
        public bool calibrationStatus;
        public List<CalibrationPointResult> results;
        public CalibrationResult()
        {
            this.calibrationStatus = false;
            this.results = new List<CalibrationPointResult>();
        }
        //		public List<List<
    }
*//*
    public class ScreenTransformations
    {
	    private int xRes;
	    private int yRes;
	    private int editorDistanceFromOrigin;

	    public Vector3 AdcsToScreenPoint(Vector2 adcsPoint)
	    {
		    if (SessionSettings.SettingExists("Session", "MonitorPixelResolution"))
		    {
			    xRes = (int)((Vector2)SessionSettings.Get("Session", "MonitorPixelResolution"))[0];
			    yRes = (int)((Vector2)SessionSettings.Get("Session", "MonitorPixelResolution"))[1];
		    }
		    else
			    Debug.Log("MonitorPixelResolution not defined in the Session Config, necessary for gaze tracking");
		    if (SessionSettings.SettingExists("Session", "editorDistanceFromOrigin"))
		    {
			    editorDistanceFromOrigin = (int)SessionSettings.Get("Session", "editorDistanceFromOrigin");
		    }
		    else
			    Debug.Log("Editor Distance From Origin not defined in Session Config, necessary for gaze tracking");
		    
		    Vector3 screenPoint = Vector3.zero;
		    if (Application.isEditor)
		    {
			    screenPoint = new Vector3(adcsPoint.x * xRes, (yRes - editorDistanceFromOrigin) - (adcsPoint.y * yRes),
				    Camera.main.nearClipPlane);
		    }
		    else
		    {
			    screenPoint = new Vector3(adcsPoint.x * xRes, yRes - (adcsPoint.y * yRes), Camera.main.nearClipPlane);
		    }
		    //		Vector3 screenPoint = new Vector3(adcsPoint.x * Screen.currentResolution.width, (Screen.currentResolution.height - editorDistanceFromOrigin) - (adcsPoint.y * Screen.currentResolution.height), cam.nearClipPlane);

		    return screenPoint;
	    }

	    public Vector2 ScreenToAdcsPoint(Vector3 screenPoint)
	    {
		    if (SessionSettings.SettingExists("Session", "MonitorPixelResolution"))
		    {
			    xRes = (int)((Vector2)SessionSettings.Get("Session", "MonitorPixelResolution"))[0];
			    yRes = (int)((Vector2)SessionSettings.Get("Session", "MonitorPixelResolution"))[1];
		    }
		    else
				Debug.Log("MonitorPixelResolution not defined in the Session Config, necessary for gaze tracking");
		    if (SessionSettings.SettingExists("Session", "editorDistanceFromOrigin"))
		    {
			    editorDistanceFromOrigin = (int)SessionSettings.Get("Session", "editorDistanceFromOrigin");
		    }
		    else
				Debug.Log("Editor Distance From Origin not defined in Session Config, necessary for gaze tracking");
		    
		    Vector2 adcsPoint = Vector2.zero;
		    if (Application.isEditor)
		    {
			    adcsPoint = new Vector2(screenPoint.x / xRes,
				    1.0f - screenPoint.y / yRes - editorDistanceFromOrigin / yRes);
		    }
		    else
		    {
			    adcsPoint = new Vector2(screenPoint.x / xRes, 1.0f - screenPoint.y / yRes);
		    }

		    return adcsPoint;
	    }*/

	    //private static Vector2 ProportionToPixel(Vector2 posProp, Vector2 resolution)
	    //{
	    //    return new Vector2(posProp.x * resolution.x, posProp.y * resolution.y);
	    //}

	    //private static Vector2 PixelToProportion(Vector2 posPix, Vector2 resolution)
	    //{
	    //    return new Vector2(posPix.x / resolution.x, posPix.y / resolution.y);
	    //}

	    //private static Vector2 ScreenToPlayerViewPoint(Vector2 screenPoint, bool NeedsScreenWidthCorrection)
	    //{
	    //    if (!Application.isEditor && NeedsScreenWidthCorrection)
	    //    {
	    //        screenPoint = screenPoint - new Vector2(Screen.width, 0f);
	    //    }
	    //    Vector2 posProp = PixelToProportion(screenPoint, mainDisplayRes);
	    //    return ProportionToPixel(posProp, playerViewRes);
	    //}
  //  }

}
