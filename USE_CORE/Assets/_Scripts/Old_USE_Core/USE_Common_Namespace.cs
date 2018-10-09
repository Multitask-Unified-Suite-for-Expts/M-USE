using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using USE_Utilities;


namespace USE_Common_Namespace {
	public class Datum<T>{
		//adapted from http://stackoverflow.com/questions/2980463/how-do-i-assign-by-reference-to-a-class-field-in-c
		public Func<T> getVal;

		public Datum(Func<T> getVal){
			this.getVal = getVal;
		}
		public T CurrentValue { get { return getVal (); }}
	}

	public class GazeDetails{
		public string gazeTarget;
		public string gazeTargetPath;
		public float[] distances;

		public GazeDetails(Vector2 gazePoint, GameObject[] objects){
			this.distances = new float[objects.Length];
			if (!float.IsNaN(gazePoint[0]) && gazePoint[0] != -1){
				Ray ray = Camera.main.ScreenPointToRay(gazePoint);
				int len = 500; //BoardManager.instance.

				RaycastHit hit;
				bool hitBool = Physics.Raycast(ray, out hit, len);

				//get the name of the object
				if (hitBool) {
					this.gazeTarget = hit.transform.name;
					this.gazeTargetPath = getChildPath (hit.transform);

				} else {
					this.gazeTarget = "Far";
					this.gazeTargetPath = "Null";
				}
				//Debug.Log ("fixating: " + objectName);			
				for (int i = 0; i < objects.Length; i++) {
					Vector3 objScreenPos = Camera.main.WorldToScreenPoint (objects [i].transform.position);
//					if (objects[i].GetComponent<StimDetails>().isVisible) {
//						Vector2 objAdcsPos = ScreenTransformations.ScreenToAdcsPoint (objScreenPos);
//						this.distances [i] = Vector2.Distance (objAdcsPos, ScreenTransformations.ScreenToAdcsPoint(gazePoint));
//					} else {
//						this.distances [i] = -1;
//					}
				}
			}else{
				this.gazeTarget = "Null";
				this.gazeTargetPath = "Null";
				for (int i = 0; i < objects.Length; i++) {
					this.distances [i] = -1;
				}
			}
		}


		private string getChildPath(Transform child){
			if (child.parent != null) {
				string str = getChildPath (child.parent.transform) + "/" + child.parent.name;
				return str;
			} else {
				return "/";
			}
		}
	}

	public static class ScreenTransformations{
		public static Vector3 AdcsToScreenPoint(Vector2 adcsPoint){
			//int yRes = 0;
			//int editorDistanceFromOrigin = 0;
			//if (SceneManager.GetActiveScene ().name == "Dynamic") {
			////	yRes = Screen.currentResolution.height;
			//	yRes = ConfigReader.sessionSettings.MonitorDetails ["monitorDetails"].pixelRes [1];
			//	editorDistanceFromOrigin = ConfigReader.sessionSettings.Int ["editorDistanceFromOrigin"];
			//} else if (SceneManager.GetActiveScene ().name == "Static" || SceneManager.GetActiveScene().name == "Static_Touch") {
			//	yRes = Screen.currentResolution.height;
			int xRes = ConfigReader.sessionSettings.MonitorDetails["monitorDetails"].PixelRes[0];
			int yRes = ConfigReader.sessionSettings.MonitorDetails ["monitorDetails"].PixelRes [1];
			int editorDistanceFromOrigin = ConfigReader.sessionSettings.Int ["editorDistanceFromOrigin"];
			//}
			Vector3 screenPoint = Vector3.zero;
			if (Application.isEditor) {
				screenPoint = new Vector3 (adcsPoint.x * xRes, (yRes - editorDistanceFromOrigin) - (adcsPoint.y * yRes), Camera.main.nearClipPlane);
			} else {
				screenPoint = new Vector3 (adcsPoint.x * xRes, yRes - (adcsPoint.y * yRes), Camera.main.nearClipPlane);
			}
			//		Vector3 screenPoint = new Vector3(adcsPoint.x * Screen.currentResolution.width, (Screen.currentResolution.height - editorDistanceFromOrigin) - (adcsPoint.y * Screen.currentResolution.height), cam.nearClipPlane);

			return screenPoint;
		}

		public static Vector2 ScreenToAdcsPoint(Vector3 screenPoint){
			int xRes = ConfigReader.sessionSettings.MonitorDetails["monitorDetails"].PixelRes[0];
			int yRes = ConfigReader.sessionSettings.MonitorDetails["monitorDetails"].PixelRes[1];
			int editorDistanceFromOrigin = ConfigReader.sessionSettings.Int["editorDistanceFromOrigin"];
			//int yRes = 0;
			//int editorDistanceFromOrigin = 0;
			//if (SceneManager.GetActiveScene ().name == "Dynamic") {
			//	yRes = ConfigReader.sessionSettings.MonitorDetails ["monitorDetails"].PixelRes [1];
			//	editorDistanceFromOrigin = ConfigReader.sessionSettings.Int ["editorDistanceFromOrigin"];
   //         } else if (SceneManager.GetActiveScene().name == "Static" || SceneManager.GetActiveScene().name == "Static_Touch") {
			//	yRes = ConfigReader.sessionSettings.MonitorDetails ["monitorDetails"].PixelRes [1];
			//	editorDistanceFromOrigin = ConfigReader.sessionSettings.Int ["editorDistanceFromOrigin"];
			//}
			Vector2 adcsPoint = Vector2.zero;
			if (Application.isEditor) {
				adcsPoint = new Vector2 (screenPoint.x / xRes, 1.0f - screenPoint.y / yRes - editorDistanceFromOrigin / yRes);
			} else {
				adcsPoint = new Vector2 (screenPoint.x / xRes, 1.0f - screenPoint.y / yRes);
			}
			return adcsPoint;
		}
	}


	public class CalibrationResult{
		public bool calibrationStatus;
		public List<CalibrationPointResult> results;
		public CalibrationResult(){
			this.calibrationStatus = false;
			this.results = new List<CalibrationPointResult> ();
		}
		//		public List<List<
	}

	public class CalibrationPointResult{
		public Vector2 testPoint;
		public List<CalibrationSample> leftSamples{get; set;}
		public List<CalibrationSample> rightSamples{ get; set;}
		public GameObject resultDisplay{ get; set; }
		public int leftValid{ get; set; }
		public int rightValid{ get; set; }

		public CalibrationPointResult(){
			testPoint = new Vector2(-1f, -1f);
			leftSamples = new List<CalibrationSample> ();
			rightSamples = new List<CalibrationSample> ();
		}

		public void AddCalibSample(string eye, bool validity, Vector2 samplePos){
			if (eye.Equals ("Left")) {
				leftSamples.Add (new CalibrationSample (validity, samplePos));
				if (validity) {
					leftValid++;
				}
			} else if (eye.Equals ("Right")) {
				rightSamples.Add (new CalibrationSample (validity, samplePos));
				if (validity) {
					rightValid++;
				}
			}
		}
	}

	public class CalibrationSample{
		public bool validity {get; set;}
		public Vector2 sample { get; set;}
		public CalibrationSample(bool validity, Vector2 sample){
			this.validity = validity;
			this.sample = sample;
		}
	}

	public class MonitorDetails{
		public int[] PixelRes{ get; set; }
		public float[] CmSize{ get; set; }
	}
}
