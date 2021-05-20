using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using USE_Utilities;
using FLU_Common_Namespace;

public class TokenManager : MonoBehaviour {
	
	public int numTargetTokens = 5;
	
	public int totalTokensCollected = 0, numResetTokens = 0;
	List<Image> images = new List<Image>(), tempImages = new List<Image>();

	public GameObject prefabTokenImage;
	public Transform tokenImageContainer, tempTokenImageContainer;
	public Image tokenBarImage;
	private GameObject[] tempTokenObjects;
    public Color colorCollected = Color.green;
	public Color colorDisabled = new Color(0.541f, 0.169f, 0.886f);//Color.gray;
	private Color currentTokenBarColor;
	public bool hideDisabled = false;
	public int MsRewardAnim = 1000, MsRewardAnimFlashes = 250;
	public Animation animAllTokensCollected;
	public bool movingTokens;
	private Vector3 tokenDestination;
	public bool animationPlaying;
	//private event Action ChangeTokenBarColour;
	private bool updateTokensNow;

	bool testInitDone = false;
	public bool initDone = false;
	public EventCodeManager eventCodeManager;
	public EventCodeConfig eventCodes;

	public void Init (int numTokens) {
		initDone = true;
		testInitDone = true;
		foreach (Transform child in tempTokenImageContainer) {
			if (child == prefabTokenImage.transform)
				continue;
            Destroy(child.gameObject);
		}
		foreach (Transform child in tokenImageContainer)
		{
			if (child == prefabTokenImage.transform)
				continue;
			Destroy(child.gameObject);
		}
		tempImages.Clear();
		images.Clear();
		numTargetTokens = numTokens;
		for(int i = 0; i < numTokens; i++){
			GameObject g = Instantiate(prefabTokenImage);
			Image img = g.GetComponent<Image>();
			img.transform.SetParent(tokenImageContainer, false);
			images.Add(img);
			g.SetActive(true);
			GameObject gT = Instantiate(prefabTokenImage);
			Image imgT = gT.GetComponent<Image>();
			imgT.transform.SetParent(tempTokenImageContainer, false);
			tempImages.Add(imgT);
			//gT.SetActive(false);
		}
		tokenBarImage = tokenImageContainer.GetComponent<Image>();
		tokenBarImage.enabled = true;

		//ChangeTokenBarColour += UpdateTokenBarColour;

		//Component[] tempComp = tempTokenImageContainer.GetComponentsInChildren(typeof(Transform), true);
		//tempTokenObjects = new GameObject[tempComp.Length];
		//for (int iC = 0; iC < tempComp.Length; iC++)
			//tempTokenObjects[iC] = tempComp[iC].gameObject;

		UpdateColors();
	}

	private void Update()
	{
		tokenBarImage.color = currentTokenBarColor;
		if (updateTokensNow)
		{
			ResetTokens();
			updateTokensNow = false;
		}
	}

	public int BoundaryCheck(int numTokens = 1){
        if (totalTokensCollected + numTokens > numTargetTokens)
        {
            numTokens = numTargetTokens - totalTokensCollected;
        }
        else if (totalTokensCollected + numTokens < 0)
        {
            numTokens = totalTokensCollected * -1;
        }
		return numTokens;
	}	

	public int AddTokens(int numTokens = 1)
    {
        numTokens = BoundaryCheck(numTokens);
        totalTokensCollected += numTokens;
		UpdateColors();
        return numTokens;
	}

	public void ActivateTempTokens(Vector3 location, int numTokens = 1)
	{
		tempTokenImageContainer.gameObject.SetActive(true);
		for (int iToken = 0; iToken < Mathf.Abs(numTokens); iToken++)
		{
			//tempTokenObjects[iToken].SetActive(true);
			tempImages[iToken].gameObject.SetActive(true);
			tempImages[iToken].enabled = true;
			if (numTokens > 0)
				tempImages[iToken].color = colorCollected;
			else
				tempImages[iToken].color = colorDisabled;
		}
		tokenDestination = new Vector3(0, 0, 0);
		if (numTokens > 0)
		{
			for (int iToken = totalTokensCollected; iToken < totalTokensCollected + numTokens; iToken++)
			{
				Vector3 tempV = images[iToken].transform.position;
				tempV.x += images[iToken].rectTransform.rect.width / 2;
				tokenDestination += tempV;
			}
			tokenDestination = tokenDestination / numTokens;
		}
		else if (numTokens < 0)
		{
			for (int iToken = totalTokensCollected - 1; iToken > totalTokensCollected + numTokens - 1; iToken--)
			{
				Vector3 tempV = images[iToken].transform.position;
				tempV.x += images[iToken].rectTransform.rect.width / 2;
				tokenDestination += tempV;
			}
			tokenDestination = tokenDestination / (numTokens * -1);
		}
		tempTokenImageContainer.position = location;
	}


	public IEnumerator MoveTempTokensAtSpeed(float speed)
	{
		//Vector3 end = tokenImageContainer.transform.position;
		while (Vector3.Distance(tempTokenImageContainer.transform.position, tokenDestination) > speed * Time.deltaTime)
		{
			tempTokenImageContainer.transform.position =
				Vector3.MoveTowards(tempTokenImageContainer.transform.position, tokenDestination, speed * Time.deltaTime);
			yield return 0;
		}
		tempTokenImageContainer.transform.position = tokenDestination;
	}

	public void DeactivateTempTokens()
	{
		foreach (Image i in tempImages)
			i.gameObject.SetActive(false);
		//tempTokenImageContainer.gameObject.SetActive(false);
		//foreach (Transform t in tempTokenImageContainer)
		//{
		//	t.gameObject.SetActive(false);
		//}
	}

	//public void RemoveToken(int numTokens = 1){
	//       numTokens 
	//	numTokensCollected -= numTokens;
	//	UpdateColors();
	//}

	//public IEnumerator PlayAnimationAllTokensCollected(bool clearTokensAfterAnimation){
	//	animationPlaying = true;
	//	tokenImageContainer.GetComponent<Image>().enabled = true;
	//	animAllTokensCollected.Play();
	//	yield return new WaitForSeconds(timeAnimationAllTokensCollected);
	//	tokenImageContainer.GetComponent<Image>().enabled = false;
	//	if(clearTokensAfterAnimation)
	//		ClearTokens();
	//	animationPlaying = false;
	//}
	public void PlayAnimationAllTokensCollected(bool clearTokensAfterAnimation)
	{
		new Thread(() => PlayTokensCollectedThread(clearTokensAfterAnimation)).Start();//, tokenImageContainer.GetComponent<Image>())).Start();
		//animationPlaying = true;
		//tokenImageContainer.GetComponent<Image>().enabled = true;
		//animAllTokensCollected.Play();
		//Thread.Sleep(durationAnimAllTokensCollected);
		//tokenImageContainer.GetComponent<Image>().enabled = false;
		//if (clearTokensAfterAnimation)
		//	ClearTokens();
		//animationPlaying = false;
	}

	void PlayTokensCollectedThread(bool updateTokensAfterAnimation)//, Image img)
	{
		animationPlaying = true;
		//img.enabled = true;
		long animStartTime = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
		long colourChangeTime = animStartTime;
		Color[] colors = { new Color(0, 0, 1, 1), new Color(1, 0, 0, 1) };
		int currentColour = 0;
		currentTokenBarColor = colors[currentColour];
		//ChangeTokenBarColour.Invoke();

		eventCodeManager.SendCodeNextFrame(eventCodes.TokensCompletFbOn.Value);
		while (animationPlaying)
		{
			totalTokensCollected = numResetTokens;
			Thread.Sleep(MsRewardAnimFlashes);
			//long timestamp = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
			//if (timestamp - colourChangeTime >= durationAnimAllTokensCollectedColour * 10000)
			//{
			currentColour++;
			if (currentColour >= colors.Length)
				currentColour = 0;
			currentTokenBarColor = colors[currentColour];
			//ChangeTokenBarColour.Invoke();
			//}
			if (TimeStamp.ConvertToUnixTimestamp(DateTime.Now) - animStartTime >= MsRewardAnim * 10000)
				animationPlaying = false;
		}
		currentTokenBarColor = new Color(0, 0, 0, 0);
		eventCodeManager.SendCodeNextFrame(eventCodes.TokensCompletFbOff.Value);
		//ChangeTokenBarColour.Invoke();
		//animAllTokensCollected.Play();
		//Thread.Sleep(durationAnimAllTokensCollected);
		//img.enabled = false;
		if (updateTokensAfterAnimation)
		{
			updateTokensNow = true;
		}
		//animationPlaying = false;
		Thread.CurrentThread.Abort();
	}


	void UpdateTokenBarColour()
	{
		tokenBarImage.color = currentTokenBarColor;
	}

	public void UpdateColors(){
		if (!initDone)
			Init(numTargetTokens);

		for(int i = 0; i < images.Count; i++){
			if(i < totalTokensCollected){
				images[i].enabled = true;
				images[i].color = colorCollected;
			}
			else{
				// for disabled
				if(hideDisabled){
					images[i].enabled = false;
				}else
					images[i].color = colorDisabled;
			}
		}
	}

	public void ResetTokens(){
		totalTokensCollected = numResetTokens;
		UpdateColors();
	}

	public bool isAllTokensCollected(){
		return numTargetTokens == totalTokensCollected;
	}


	// JUST FOR TEST
	//public bool test = false;
	//void Update(){
	//	// UpdateColors();
	//	if(test)
	//	{
	//		if(!testInitDone){
	//			Init(2);
	//		}
	//		AddTokens();
	//		// UpdateColors();
	//		test = false;
	//		if(isAllTokensCollected()){
	//			StartCoroutine(PlayAnimationAllTokensCollected(true));
	//		}
	//	}
	//}
}

public class TokenReward
{
    public int NumTokens;
    public float Probability;
}
