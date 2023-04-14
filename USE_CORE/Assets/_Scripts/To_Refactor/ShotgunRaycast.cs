using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.EventSystems;
using USE_DisplayManagement;
//using MGcommon;

public class ShotgunRaycast : MonoBehaviour
{
	public float DefaultRadiusDVA = 360;
	public float DefaultParticipantDistanceCm = 60f;
	public float DefaultRaycastSpacingDVA = 0.3f;
	public float DefaultRayLengthWorldUnits = 100f;
	public USE_DisplayManagement.MonitorDetails monitorDetails;

	private GameObject cylinder;
	private GameObject arena;
	public bool showCylinder = false;

	private Color[] rayColors;


    private void Awake()
    {
		float dpi = Screen.dpi;
		float width = (Screen.currentResolution.width / dpi) * 2.54f;
		float height = (Screen.currentResolution.height / dpi) * 2.54f;
		Vector2 resolutionCM = new Vector2(width, height);
		monitorDetails = new USE_DisplayManagement.MonitorDetails(new Vector2(Screen.currentResolution.width, Screen.currentResolution.height), resolutionCM);
    }


    public List<DoubleRaycast> RaycastShotgun(Vector2 gazePoint, Camera cam, float? customRadiusDVA = null, 
		float? customRaycastSpacingDVA = null, float? customParticipantDistanceToScreen = null, float? customRaycastLengthWorldUnits = null, bool drawRays = false)
	{
		drawRays = true;

		//check for custom values
		float radiusDVA = customRadiusDVA == null ? DefaultRadiusDVA : customRadiusDVA.Value;
		float raycastSpacingDVA = customRaycastSpacingDVA == null ? DefaultRaycastSpacingDVA : customRaycastSpacingDVA.Value;
		float participantDistanceToScreenCm = customParticipantDistanceToScreen == null ? DefaultParticipantDistanceCm : customParticipantDistanceToScreen.Value;
		float raycastLengthWorldUnits = customRaycastLengthWorldUnits == null ? DefaultRayLengthWorldUnits : customRaycastLengthWorldUnits.Value;

		//find cm and pixel size of radius visualAngle at surface of screen
		float radCm = 2 * participantDistanceToScreenCm * (Mathf.Tan((Mathf.PI * radiusDVA / 180f) / 2));
		float radPix = 50;
		radPix = radCm * monitorDetails.PixelResolution[0] / monitorDetails.CmSize[0];

		//now define circles that subtend visualAngle degrees, directly on monitor and at end of rayLength
		Vector3[] centres = {cam.ScreenToWorldPoint (new Vector3 (gazePoint.x, gazePoint.y, cam.nearClipPlane)),
			cam.ScreenToWorldPoint (new Vector3 (gazePoint.x, gazePoint.y, raycastLengthWorldUnits))
		};

		//points on outside of circles
		Vector3[] outsidePoints = new Vector3[2]{cam.ScreenToWorldPoint (new Vector3 (gazePoint.x + radPix, gazePoint.y, cam.nearClipPlane)),
			cam.ScreenToWorldPoint (new Vector3 (gazePoint.x + radPix, gazePoint.y, raycastLengthWorldUnits))
		};

		//find radii of circles
		float[] radWorld = new float[2] { Vector3.Distance(centres[0], outsidePoints[0]), Vector3.Distance(centres[1], outsidePoints[1]) };

		Vector3 rot = new Vector3(cam.transform.rotation.eulerAngles.x + 90, cam.transform.rotation.eulerAngles.y, cam.transform.rotation.eulerAngles.z);

		//raycast....
		List<DoubleRaycast> raycastList = new List<DoubleRaycast>();
		DoubleRaycast doubleRay = DualRaycast(gazePoint);
		//DoubleRaycast doubleRay = DualRaycast(centres[0]);
		raycastList.Add(doubleRay);

		//Determine appropriate number of circles and increase in radius between them (in worldspace units, both at the screen and distance rayLength from it)
		int numCircles = (int)Mathf.Ceil(radWorld[1] / raycastSpacingDVA);
		float[] radStepSize = new float[2] { radWorld[0] / numCircles, radWorld[1] / numCircles };

		Vector3 normal = GeometryUtility.CalculateFrustumPlanes(cam)[4].normal;
		Vector3[] orthonormals = { Vector3.one, Vector3.one };
		Vector3.OrthoNormalize(ref normal, ref orthonormals[0], ref orthonormals[1]);

		int totalRaysCast = 0;
		int hitcount = 0;
		//iterate from the smallest circle to the largest
		for (int i = 0; i < numCircles; i++)
		{
			Debug.Log("CIRCLE " + i);
			//find radius of current circles - one at screen and one at distance rayLength
			float[] rad = new float[2] { radStepSize[0] * (i + 1), radStepSize[1] * (i + 1) };

			//determine appropriate number of rays to place around this circle,
			//arc distance between them (in worldspace units, both at the screen and distance rayLength from it),
			//and angle between them
			int numRays = (int)Mathf.Ceil((2 * Mathf.PI * rad[1]) / raycastSpacingDVA);
			float[] rayStepSize = new float[2] { (2 * Mathf.PI * rad[0]) / numRays, (2 * Mathf.PI * rad[1]) / numRays };
			float angleStepSize = rayStepSize[0] / rad[0];

			//jitter the starting point of raycasts on each circle so that they are as un-aligned as possible
			float angleJitter = UnityEngine.Random.Range(0f, angleStepSize);

			//iterate around the circle
			for (int j = 0; j < numRays; j++)
			{
				Debug.Log("RAY " + i);
				totalRaysCast++;

				float angle = angleStepSize * j + angleJitter;
				
				Debug.Log("ANGLE " + angle);

				//find start and end points of current ray - see https://stackoverflow.com/questions/27714014/3d-point-on-circumference-of-a-circle-with-a-center-radius-and-normal-vector
				Vector3 startPoint = new Vector3(centres[0].x + rad[0] * (orthonormals[0].x * Mathf.Cos(angle) + orthonormals[1].x * Mathf.Sin(angle)),
					centres[0].y + rad[0] * (orthonormals[0].y * Mathf.Cos(angle) + orthonormals[1].y * Mathf.Sin(angle)),
					centres[0].z + rad[0] * (orthonormals[0].z * Mathf.Cos(angle) + orthonormals[1].z * Mathf.Sin(angle)));
				Vector3 endPoint = new Vector3(centres[1].x + rad[1] * (orthonormals[0].x * Mathf.Cos(angle) + orthonormals[1].x * Mathf.Sin(angle)),
					centres[1].y + rad[1] * (orthonormals[0].y * Mathf.Cos(angle) + orthonormals[1].y * Mathf.Sin(angle)),
					centres[1].z + rad[1] * (orthonormals[0].z * Mathf.Cos(angle) + orthonormals[1].z * Mathf.Sin(angle)));


				//perform raycast
				if (drawRays)
					Debug.DrawLine(startPoint, endPoint, Color.cyan);

				DoubleRaycast doubleRaycast = DualRaycast(startPoint - endPoint);
				raycastList.Add(doubleRaycast);
				if (doubleRaycast.Go != null)
				{
					hitcount++;
					Debug.Log("HIT: " + hitcount);
				}
			}
		}
		return raycastList;
	}

    private static DoubleRaycast DualRaycast(Vector3 touchPos)
    {
		DoubleRaycast doubleRaycast = null;
        float distance2D = 0;
        float distance3D = 0;

        //3D
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(touchPos), out hit, Mathf.Infinity))
        {
			Debug.Log("3D HIT!!!!!!");
			distance3D = (hit.point - touchPos).magnitude;
            doubleRaycast = new DoubleRaycast(hit.transform.gameObject, distance3D);
        }

        //2D:
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject != null)
            {
                // Debug.Log("2D HIT!!!!!!");
                distance2D = (result.gameObject.transform.position - touchPos).magnitude;
                if (doubleRaycast == null || (distance3D != 0 && (distance2D < distance3D)))
                {
					doubleRaycast = new DoubleRaycast(result.gameObject, distance2D);
                    break;
                }
            }
        }
        return doubleRaycast;
    }


	public Dictionary<GameObject, float> RaycastShotgunProportions(Vector3 gazePoint, Camera cam,
		float? customRadiusDVA = null, float? customRaycastSpacingDVA = null, float? customParticipantDistanceToScreen = null,
		float? customRaycastLengthWorldUnits = null, bool drawRays = false)
	{

		//check for custom values
		float radiusDVA = customRadiusDVA == null ? DefaultRadiusDVA : customRadiusDVA.Value;
		float raycastSpacingDVA = customRaycastSpacingDVA == null ? DefaultRaycastSpacingDVA : customRaycastSpacingDVA.Value;
		float participantDistanceToScreenCm = customParticipantDistanceToScreen == null ? DefaultParticipantDistanceCm : customParticipantDistanceToScreen.Value;
		float raycastLengthWorldUnits = customRaycastLengthWorldUnits == null ? DefaultRayLengthWorldUnits : customRaycastLengthWorldUnits.Value;

		List<DoubleRaycast> doubleRays = RaycastShotgun(gazePoint, cam, radiusDVA, raycastSpacingDVA, participantDistanceToScreenCm, raycastLengthWorldUnits, drawRays);

		Dictionary<GameObject, int> hitCounts = new Dictionary<GameObject, int>();
		foreach (DoubleRaycast ray in doubleRays)
		{
			if (ray != null)
			{
				Debug.Log("RAY NOT NULL!");
				GameObject go = ray.Go;
				if (!hitCounts.ContainsKey(go))
				{
					Debug.Log("i am here");
					hitCounts.Add(go, 1);
				}
				else
				{
					Debug.Log("no, i am actually here");
					hitCounts[go]++;
				}
			}

		}
		Dictionary<GameObject, float> proportionHits = new Dictionary<GameObject, float>();
		List<GameObject> keys = new List<GameObject>(hitCounts.Keys);
		for (int i = 0; i < hitCounts.Count; i++)
		{
			proportionHits.Add(keys[i], hitCounts[keys[i]] / (float)doubleRays.Count);
		}
		return proportionHits;
	}

	public GameObject ModalShotgunTarget(Dictionary<GameObject,float> proportionHits)
    {
		GameObject modalTarget = null;
        float highestProp = 0f;

        foreach (GameObject key in proportionHits.Keys)
        {
			if (proportionHits[key] > highestProp)
			{
				modalTarget = key;
				highestProp = proportionHits[key];
			}
        }
        return modalTarget;
    }


	//public class FullShotgunOutput
	//{
	//	public string SimpleRaycastTarget { get; }
	//	public string ShotgunRaycastTargetsString { get; }
	//	public Dictionary<string, float> ShotgunRaycastTargetsDict { get; }
	//	public string ModalShotgunRaycastTarget { get; }

	//	public FullShotgunOutput(ShotgunRaycast sRaycast,
	//		Vector3 gazePos, float? customRadiusDVA = null, float? customRaycastSpacingDVA = null, 
	//		float? customParticipantDistanceToScreen = null, float? customRaycastLengthWorldUnits = null)
	//	{

	//		//check for custom values
	//		float radiusDVA = customRadiusDVA == null ? sRaycast.DefaultRadiusDVA : customRadiusDVA.Value;
	//		float raycastSpacingDVA = customRaycastSpacingDVA == null ? sRaycast.DefaultRaycastSpacingDVA : customRaycastSpacingDVA.Value;
	//		float participantDistanceToScreenCm = customParticipantDistanceToScreen == null ? sRaycast.DefaultParticipantDistanceCm : customParticipantDistanceToScreen.Value;
	//		float raycastLengthWorldUnits = customRaycastLengthWorldUnits == null ? sRaycast.DefaultRayLengthWorldUnits : customRaycastLengthWorldUnits.Value;

	//		RaycastHit hit = new RaycastHit();
	//		if (!float.IsNaN(gazePos.x))
	//		{
	//			if (Physics.Raycast(Camera.main.ScreenPointToRay(gazePos), out hit, Mathf.Infinity))
	//			{
	//				GameObject g = hit.transform.gameObject;
	//				StimObject so = g.GetComponent<StimObject>();
	//				if (so != null)
	//					SimpleRaycastTarget = so.stimDef.StimID;
	//				else
	//					SimpleRaycastTarget = hit.collider.name;
	//			}
	//			else
	//				SimpleRaycastTarget = "";

	//			ShotgunRaycastTargetsDict = sRaycast.RaycastShotgunProportions(new Vector2(gazePos.x, gazePos.y),
	//				Camera.main, radiusDVA, raycastSpacingDVA, participantDistanceToScreenCm, raycastLengthWorldUnits);
	//			ShotgunRaycastTargetsString = JsonConvert.SerializeObject(ShotgunRaycastTargetsDict);//.ToString(Formatting.None);
	//			ModalShotgunRaycastTarget = sRaycast.ModalShotgunTarget(ShotgunRaycastTargetsDict);
	//		}
	//		else
	//		{
	//			ShotgunRaycastTargetsDict = null;
	//			SimpleRaycastTarget = "";
	//			ShotgunRaycastTargetsString = "";
	//			ModalShotgunRaycastTarget = "";
	//		}
	//	}
	//}


	public class ObjectFixationDetails
	{
		public float DurationMs;
		public string Target { get; }
		public Vector3 GazeMeanLocation;

		private List<Vector3> GazeRawLocation;
		private float OnsetTime { get; }

		public ObjectFixationDetails(string target, Vector3 immediateGazePos)
		{
			Target = target;
			DurationMs = 0;
			OnsetTime = Time.time;
			GazeRawLocation = new List<Vector3>();
			UpdateDetails(immediateGazePos);
		}

		public void UpdateDetails(Vector3 immediateGazePos)
		{
			DurationMs = Time.time - OnsetTime;
			GazeRawLocation.Add(immediateGazePos);
			GazeMeanLocation = GazeRawLocation.Aggregate(Vector3.zero, (acc, v) => acc + v) / GazeRawLocation.Count;
		}
	}

	public class CurrentObjectFixationDetails
	{
		public float VisualAngle { get; }
		public int FrameWindow { get; }
		public USE_DisplayManagement.MonitorDetails MonitorDetails { get; }
		public ShotgunRaycast ShotgunRaycast;
		public ExternalDataManager ExternalDataManager;

		public List<ObjectFixationDetails> ObjectFixDetails;
		private List<string> CurrentlyFixatedObjectNames;

		public CurrentObjectFixationDetails(USE_DisplayManagement.MonitorDetails monitorDetails, ShotgunRaycast shotgunRaycast,
			ExternalDataManager externalDataManager, float? visualAngle = null)
		{
			if (visualAngle != null)
				VisualAngle = visualAngle.Value;
			//FrameWindow = frameWindow;
			MonitorDetails = monitorDetails;
			ShotgunRaycast = shotgunRaycast;
			ExternalDataManager = externalDataManager;

			ObjectFixDetails = new List<ObjectFixationDetails>();
			CurrentlyFixatedObjectNames = new List<string>();
		}

		public void UpdateDetails(IEnumerable<string> shotgunGazeTargets)
		{

			foreach (string objectName in shotgunGazeTargets)
			{
				if (CurrentlyFixating(objectName))
					ObjectFixDetails[CurrentlyFixatedObjectNames.IndexOf(objectName)].UpdateDetails(ExternalDataManager.gazePositionWorld);

				else
				{
					ObjectFixDetails.Add(new ObjectFixationDetails(objectName, ExternalDataManager.gazePositionWorld));
					CurrentlyFixatedObjectNames.Add(objectName);
				}

			}

			List<int> unfixated = new List<int>();
			foreach (string objectName in CurrentlyFixatedObjectNames)
			{
				if (!shotgunGazeTargets.Contains(objectName))
				{
					unfixated.Add(CurrentlyFixatedObjectNames.IndexOf(objectName));
				}
			}

			unfixated.Sort();
			unfixated.Reverse();


			foreach (int i in unfixated)
			{
				CurrentlyFixatedObjectNames.RemoveAt(i);
				ObjectFixDetails.RemoveAt(i);
			}
		}

		public bool CurrentlyFixating(string objectName)
		{
			return CurrentlyFixatedObjectNames.Contains(objectName);
		}

		public float GetFixationDuration(string objectName)
		{
			if (CurrentlyFixating(objectName))
				return ObjectFixDetails[CurrentlyFixatedObjectNames.IndexOf(objectName)].DurationMs;
			else
			{
				return -1;
			}
		}
	}

	public class CurrentScreenPointFixationDetails
	{
		public Vector2 TargetPointPixels { get; set; }
		public Vector2 TargetPointProportion { get; set; }
		public int MaxDistance_Pixels { get; set; }
		public float MaxDistance_Cm { get; set; }
		public float MaxDistance_DVA { get; set; }
		public float MaxDistance_Proportion { get; set; }
		public float DurationMs { get; }
		public float GazeMeanLocation { get; }
	}

}

public class DoubleRaycast
{
	public GameObject Go;
	public float Distance;

	public DoubleRaycast(GameObject go, float distance)
	{
		Go = go;
		Distance = distance;
	}
}
