using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;


public class FlashPanelController : MonoBehaviour
{
	private Image panelImageL;
	private Image panelImageR;

	//public static bool runPattern;
	private int[] leftSequence;
	private int[] rightSequence;

	private int leftSequenceCount = 0;
	private int rightSequenceCount = 0;

	public int leftLuminanceFactor = 0;
	public int rightLuminanceFactor = 0;

	private int leftSegmentLength = 1;
	private int rightSegmentLength = 3;

	private int frameCounter = 0;
    public bool runPattern; 

	// Use this for initialization
	void Start()
	{
		panelImageL = GameObject.Find("FlashPanelL").GetComponent<Image>();
		panelImageR = GameObject.Find("FlashPanelR").GetComponent<Image>();
		leftSequence = MakeSequence(leftSegmentLength);
		rightSequence = MakeSequence(rightSegmentLength);
	}

	private void Update()
	{
		if (runPattern)
			RunPattern();
	}

	public void TurnOffFlashPanels()
    {
		panelImageL.gameObject.SetActive(false);
		panelImageR.gameObject.SetActive(false);
	    //GameObject.Find("FlashPanelL").SetActive(false);
	    //GameObject.Find("FlashPanelR").SetActive(false);
    }

	public void ReverseFlipBothSquares(){
		if (Time.frameCount % 1 == 0)
		{
			rightLuminanceFactor = leftLuminanceFactor;
			leftLuminanceFactor = Math.Abs(leftLuminanceFactor - 1);
			SetSquareColours(leftLuminanceFactor, rightLuminanceFactor);
		}
	}

	public void FlipBothSquares()
	{
		if (Time.frameCount % 1 == 0)
		{
			leftLuminanceFactor = Math.Abs(leftLuminanceFactor - 1);
			rightLuminanceFactor = leftLuminanceFactor;
			SetSquareColours(leftLuminanceFactor, rightLuminanceFactor);
		}
	}

	public void RunPattern()
	{
		if (Time.frameCount % 1 == 0)
		{
			//the mod should be left to 1 unless debugging, it indicates how many frames a single element of the sequences should last.
			//so this will usually be 1, but if you want to see each black/white shift you might change it to 20, or even 60 (in which
			//case a single element will last an entire second at 60FPS.
			leftLuminanceFactor = leftSequence[leftSequenceCount];
			rightLuminanceFactor = rightSequence[rightSequenceCount];
			rightSequenceCount++;
			leftSequenceCount++;
			if (leftSequenceCount == leftSequence.Length)
			{
				leftSequenceCount = 0;
			}

			if (rightSequenceCount == rightSequence.Length)
			{
				rightSequenceCount = 0;
			}

			//leftLuminanceFactor = 1f;
			//rightLuminanceFactor = 1f;
			//rightLuminanceFactor =  Math.Abs (leftLuminanceFactor - 1);
			SetSquareColours(leftLuminanceFactor, rightLuminanceFactor);
		}
	}


	public void SetSquareColours(float leftLum, float rightLum){
		Vector4 leftColour = new Vector4 (leftLum * 1, leftLum * 1, leftLum * 1, 1);
		Vector4 rightColour = new Vector4 (rightLum * 1, rightLum * 1, rightLum * 1, 1);

		panelImageL.color = leftColour;
		panelImageR.color = rightColour;
	}

	public void CalibratePhotodiodes() {
		leftLuminanceFactor = Mathf.Abs (leftLuminanceFactor - 1);
		Vector4 squareColour = new Vector4 (leftLuminanceFactor * 1, leftLuminanceFactor * 1, leftLuminanceFactor * 1, 1);
		panelImageL.color = squareColour;
		panelImageR.color = squareColour;
		//			calibratePhotodiodes = false;
		//			OptiExperimentController.newSession = true;
		//			runPattern = true;
		//			OptiUDPController.SendString ("ARDUINO CMD:CAF " + 10000 * freerunningCalbirationDuration);
	}


	int[] MakeSequence(int segmentLength){
		//Create a [2^N, N] binary table, where N = the length of each binary number
		//(and 2^N is the number of possible numbers composed of that length)
		//then we create a N x 2^N sequence, which is just each of the numbers in order.
		//So, e.g., if we want numbers of length 2, the sequence is 00011011 (0,1,2,3 in decimal)

		string seqString = "";
		for (int segCount = 0; segCount < (int)Math.Pow (2, segmentLength); segCount++) {
			seqString = seqString + ToBin (segCount, segmentLength);
		}

		int[] sequence = new int[seqString.Length];
		for (int eleCount = 0; eleCount < seqString.Length; eleCount++) {
			sequence [eleCount] = Convert.ToInt32(Char.ToString(seqString [eleCount]));
		}
		return sequence;
	}

	string ToBin(int value, int len) {
		string thisString = (len > 1 ? ToBin(value >> 1, len - 1) : null) + "01"[value & 1];
		return thisString;
	}
}
