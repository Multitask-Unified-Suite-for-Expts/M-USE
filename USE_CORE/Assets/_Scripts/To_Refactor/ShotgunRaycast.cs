using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
//using MGcommon;

public class ShotgunRaycast : MonoBehaviour
{

	//	public int[] monitorResolution = new int[2]{2560, 1440}; //pixel size of monitor - set for Apple Cinema display (needs to be in config)
	//	public float[] monitorSize = new float[2]{33.3f,59.4f}; //cm size of monitor
	public float DefaultRadiusDVA = 1;
	public float DefaultParticipantDistanceCm = 60f;
	public float DefaultRaycastSpacingDVA = 0.1f;
	public float DefaultRayLengthWorldUnits = 100f;
	public USE_DisplayManagement.MonitorDetails monitorDetails;

	private GameObject cylinder;
	private GameObject arena;
	public bool showCylinder = false;

	private Color[] rayColors;

	void Awake()
	{
		cylinder = GameObject.Find("Cylinder");
		//		if (!showCylinder) {
		//			cylinder.SetActive (false);
		//		}
		arena = GameObject.Find("Arena");
		rayColors = new Color[] {
			Color.black,
			Color.blue,
			Color.cyan,
			Color.gray,
			Color.green,
			Color.magenta,
			Color.red,
			Color.white,
			Color.yellow
		};
	}

	public List<RaycastHit> RaycastShotgun(Vector2 gazePoint, Camera cam, float? customRadiusDVA = null, 
		float? customRaycastSpacingDVA = null, float? customParticipantDistanceToScreen = null, float? customRaycastLengthWorldUnits = null, bool drawRays = false)
	{
		//Debug.Log("Frame: " + Time.frameCount + ", GazePoint: " + gazePoint + "length: " + rayLength);
		//gazePoint = new Vector2(960, 540);
		//RaycastSpacingDVA = 0.05f;
		//		arena.SetActive (false);
		//RaycastShotgun spits out a list of *almost* all non-occluded objects within a truncated cone.

		//*Almost* because it actually does a series of raycasts that cover a great deal of territory within the cone,
		//but technically it is possible to miss an object if its collider is a really weird shape and just happens to meet the cone at exactly the right angle.
		//(To get around this problem, you can decrease maxBetweenRayDist, but this may lead to performance issues as it can add many rays).

		//Truncated cone is length rayLength, and projected directly out from the camera cam.

		//The inner angle is defined such that the circle projected onto the screen from any cross-section of the cone is of radius visualAngle.

		//Rays are cast around a series of concentric circles, which are evenly spaced such that each one's radius is no more than maxBetweenRayDist greater than one smaller.

		//On each circle, the rays are cast at evenly-spaced points such that the length of the arc between each point is no more than maxBetweenRayDist.

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

		//Debug.Log("Gazepoint: " + gazePoint + ", start: " + centres[0] + ", end: " + centres[1]);

		//points on outside of circles
		Vector3[] outsidePoints = new Vector3[2]{cam.ScreenToWorldPoint (new Vector3 (gazePoint.x + radPix, gazePoint.y, cam.nearClipPlane)),
			cam.ScreenToWorldPoint (new Vector3 (gazePoint.x + radPix, gazePoint.y, raycastLengthWorldUnits))
		};

		//find radii of circles
		float[] radWorld = new float[2] { Vector3.Distance(centres[0], outsidePoints[0]), Vector3.Distance(centres[1], outsidePoints[1]) };


		Vector3 rot = new Vector3(cam.transform.rotation.eulerAngles.x + 90, cam.transform.rotation.eulerAngles.y, cam.transform.rotation.eulerAngles.z);
		if (showCylinder)
		{
			cylinder.transform.rotation = Quaternion.Euler(rot);
		}
		if (Time.frameCount % 2 == 0)
		{
			float diam = 2f * radWorld[1];
			if (showCylinder)
			{
				cylinder.transform.position = centres[1];
				cylinder.transform.localScale = new Vector3(diam, cylinder.transform.localScale.y, diam);
			}
		}
		else
		{
			float diam = 2f * radWorld[0];
			if (showCylinder)
			{
				cylinder.transform.position = centres[0] + cam.transform.forward * 0.001f;
				cylinder.transform.localScale = new Vector3(diam, cylinder.transform.localScale.y, diam);
			}
		}

		//prepare list of raycast hits
		List<RaycastHit> hitList = new List<RaycastHit>();
		RaycastHit tempHit = new RaycastHit();

		//raycast from the centre.
		if (Physics.Raycast(cam.ScreenPointToRay(centres[0]), out tempHit, raycastLengthWorldUnits))
			hitList.Add(tempHit);
		else
			hitList.Add(new RaycastHit());


		//Determine appropriate number of circles and increase in radius between them (in worldspace units, both at the screen and distance rayLength from it)
		int numCircles = (int)Mathf.Ceil(radWorld[1] / raycastSpacingDVA);
		//		print (numCircles);
		float[] radStepSize = new float[2] { radWorld[0] / numCircles, radWorld[1] / numCircles };

		Vector3 normal = GeometryUtility.CalculateFrustumPlanes(cam)[4].normal;
		Vector3[] orthonormals = { Vector3.one, Vector3.one };
		Vector3.OrthoNormalize(ref normal, ref orthonormals[0], ref orthonormals[1]);

		int totalRaysCast = 0;

		//iterate from the smallest circle to the largest
		for (int i = 0; i < numCircles; i++)
		{

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
				totalRaysCast++;

				float angle = angleStepSize * j + angleJitter;

				//find start and end points of current ray - see https://stackoverflow.com/questions/27714014/3d-point-on-circumference-of-a-circle-with-a-center-radius-and-normal-vector
				Vector3 startPoint = new Vector3(centres[0].x + rad[0] * (orthonormals[0].x * Mathf.Cos(angle) + orthonormals[1].x * Mathf.Sin(angle)),
					centres[0].y + rad[0] * (orthonormals[0].y * Mathf.Cos(angle) + orthonormals[1].y * Mathf.Sin(angle)),
					centres[0].z + rad[0] * (orthonormals[0].z * Mathf.Cos(angle) + orthonormals[1].z * Mathf.Sin(angle)));
				Vector3 endPoint = new Vector3(centres[1].x + rad[1] * (orthonormals[0].x * Mathf.Cos(angle) + orthonormals[1].x * Mathf.Sin(angle)),
					centres[1].y + rad[1] * (orthonormals[0].y * Mathf.Cos(angle) + orthonormals[1].y * Mathf.Sin(angle)),
					centres[1].z + rad[1] * (orthonormals[0].z * Mathf.Cos(angle) + orthonormals[1].z * Mathf.Sin(angle)));

				//perform raycast
				if (drawRays)
					Debug.DrawLine(startPoint, endPoint, rayColors[Random.Range(0, rayColors.Length)]);
				if (Physics.Raycast(startPoint, endPoint - startPoint, out tempHit, raycastLengthWorldUnits))
					hitList.Add(tempHit);
				else
					hitList.Add(new RaycastHit());
				//Debug.Log("Frame " + Time.frameCount + ", Start: " + startPoint + ", End: " + endPoint + ", Vector: " + (endPoint - startPoint) + ", GazePoint: " + gazePoint);
			}
		}
		return hitList;
	}

	public Dictionary<string, float> RaycastShotgunProportions(Vector2 gazePoint, Camera cam, 
		float? customRadiusDVA = null, float? customRaycastSpacingDVA = null, float? customParticipantDistanceToScreen = null, 
		float? customRaycastLengthWorldUnits = null, bool drawRays = false)
	{

		//check for custom values
		float radiusDVA = customRadiusDVA == null ? DefaultRadiusDVA : customRadiusDVA.Value;
		float raycastSpacingDVA = customRaycastSpacingDVA == null ? DefaultRaycastSpacingDVA : customRaycastSpacingDVA.Value;
		float participantDistanceToScreenCm = customParticipantDistanceToScreen == null ? DefaultParticipantDistanceCm : customParticipantDistanceToScreen.Value;
		float raycastLengthWorldUnits = customRaycastLengthWorldUnits == null ? DefaultRayLengthWorldUnits : customRaycastLengthWorldUnits.Value;

		List<RaycastHit> hits = RaycastShotgun(gazePoint, cam, radiusDVA, raycastSpacingDVA, participantDistanceToScreenCm, raycastLengthWorldUnits, drawRays);
		Dictionary<string, int> hitCounts = new Dictionary<string, int>();
		foreach (RaycastHit hit in hits)
		{
			if (hit.transform != null)
			{
				GameObject g = hit.transform.gameObject;
				StimObject so = g.GetComponent<StimObject>();
				string objName = "";
				if (so != null)
					objName = so.stimDef.StimID;
				else
					objName = hit.collider.name;

				if (!hitCounts.ContainsKey(objName))
					hitCounts.Add(objName, 1);
				else
					hitCounts[objName]++;
			}
            else
			{
				if (!hitCounts.ContainsKey("NoHit"))
                    hitCounts.Add("NoHit", 1);
                else
                    hitCounts["NoHit"]++;
            }
        }
		Dictionary<string, float> proportionHits = new Dictionary<string, float>();
		//ProportionHit[] proportionHits = new ProportionHit[hitCounts.Count];
		List<string> keys = new List<string>(hitCounts.Keys);
		for (int i = 0; i < hitCounts.Count; i++)
		{
			proportionHits.Add(keys[i], hitCounts[keys[i]] / (float)hits.Count);
		}
		return proportionHits;
	}

    public string ModalShotgunTarget(Dictionary<string,float> proportionHits)
    {
        string modalTarget = "null";
        float highestProp = 0f;

        foreach (string key in proportionHits.Keys)
        {
			if (proportionHits[key] > highestProp)
			{
				modalTarget = key;
				highestProp = proportionHits[key];
			}
        }

        return modalTarget;
    }


	public class FullShotgunOutput
	{
		public string SimpleRaycastTarget { get; }
		public string ShotgunRaycastTargetsString { get; }
		public Dictionary<string, float> ShotgunRaycastTargetsDict { get; }
		public string ModalShotgunRaycastTarget { get; }

		public FullShotgunOutput(ShotgunRaycast sRaycast,
			Vector3 gazePos, float? customRadiusDVA = null, float? customRaycastSpacingDVA = null, 
			float? customParticipantDistanceToScreen = null, float? customRaycastLengthWorldUnits = null)
		{

			//check for custom values
			float radiusDVA = customRadiusDVA == null ? sRaycast.DefaultRadiusDVA : customRadiusDVA.Value;
			float raycastSpacingDVA = customRaycastSpacingDVA == null ? sRaycast.DefaultRaycastSpacingDVA : customRaycastSpacingDVA.Value;
			float participantDistanceToScreenCm = customParticipantDistanceToScreen == null ? sRaycast.DefaultParticipantDistanceCm : customParticipantDistanceToScreen.Value;
			float raycastLengthWorldUnits = customRaycastLengthWorldUnits == null ? sRaycast.DefaultRayLengthWorldUnits : customRaycastLengthWorldUnits.Value;

			RaycastHit hit = new RaycastHit();
			if (!float.IsNaN(gazePos.x))
			{
				if (Physics.Raycast(Camera.main.ScreenPointToRay(gazePos), out hit, Mathf.Infinity))
				{
					GameObject g = hit.transform.gameObject;
					StimObject so = g.GetComponent<StimObject>();
					if (so != null)
						SimpleRaycastTarget = so.stimDef.StimID;
					else
						SimpleRaycastTarget = hit.collider.name;
				}
				else
					SimpleRaycastTarget = "";

				ShotgunRaycastTargetsDict = sRaycast.RaycastShotgunProportions(new Vector2(gazePos.x, gazePos.y),
					Camera.main, radiusDVA, raycastSpacingDVA, participantDistanceToScreenCm, raycastLengthWorldUnits);
				ShotgunRaycastTargetsString = JsonConvert.SerializeObject(ShotgunRaycastTargetsDict);//.ToString(Formatting.None);
				ModalShotgunRaycastTarget = sRaycast.ModalShotgunTarget(ShotgunRaycastTargetsDict);
			}
			else
			{
				ShotgunRaycastTargetsDict = null;
				SimpleRaycastTarget = "";
				ShotgunRaycastTargetsString = "";
				ModalShotgunRaycastTarget = "";
			}
		}
	}


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

