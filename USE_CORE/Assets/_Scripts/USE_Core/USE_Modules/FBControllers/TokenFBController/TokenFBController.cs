using UnityEngine;
using USE_Data;
using System.Collections;
using ConfigParsing;

public class TokenFBController : MonoBehaviour
{
    public int tokenSize = 100;
    public int tokenSpacing = 0;
    public int tokenBoxPadding = 0;
    public int tokenBoxYOffset = 10;
    public Texture2D tokenTexture;


    // Color Constants
    private readonly Color colorCollected = Color.green;
    private readonly Color colorUncollected = new Color(0.5f, 0.5f, 0.5f);
    private readonly Color colorFlashing1 = Color.blue;
    private readonly Color colorFlashing2 = Color.red;
    // Token Counts
    private int totalTokensNum = 5;
    private int numCollected = 0;
    private int numTokenBarFull = 0;
    private bool tokenBarFull = false;
    // Rendering
    private Rect tokenBoxRect;
    private GUIStyle whiteStyle;
    // Animation
    private int tokensChange;
    enum AnimationPhase { None, Show, Update, Flashing };
    private AnimationPhase animationPhase = AnimationPhase.None;
    private Vector2 animatedTokensPos;
    private Vector2 animatedTokensStartPos;
    private Vector2 animatedTokensEndPos;
    private int animatedTokensNum;
    private Color animatedTokensColor;
    private Color tokenBoxColor;
    private float animationStartTime;
    private float animationEndTime;
    private float revealTime = 0.4f; // How long to show the tokens before animating
    private float updateTime = 0.3f; // How long each token update animation should take
    private float flashingTime = 0.5f; // How long the token bar should flash when it fills up
    private int flashingNumBeeps = 3; //Num beeps for tokenbar flashing audio
    private float flashBeepInterval; //Length between beeps for tokenbar flashing audio
    // Audio
    AudioFBController audioFBController;

    public void Init(DataController trialData, DataController frameData, AudioFBController audioFBController)
    {
        trialData.AddDatum("TokenBarValue", () => numCollected);
        trialData.AddDatum("TokenChange", () => tokensChange == 0 ? null : (float?)tokensChange);
        trialData.AddDatum("TokenBarCompletedThisTrial", ()=> tokenBarFull);
        frameData.AddDatum("TokenBarValue", () => numCollected);
        frameData.AddDatum("TokenAnimationPhase", () => animationPhase.ToString());
        frameData.AddDatum("TokenBarVisibility", ()=> enabled);
        this.audioFBController = audioFBController;
        numCollected = 0;

        whiteStyle = new GUIStyle();
        whiteStyle.normal.background = Texture2D.whiteTexture;

        RecalculateTokenBox();

        SetPositiveShowAudioClip(audioFBController.GetClip("Positive"));
        SetNegativeShowAudioClip(audioFBController.GetClip("Negative"));
    }

    private void RecalculateTokenBox() {
        float width = CalcTokensWidth(totalTokensNum) + 2 * tokenBoxPadding;
        tokenBoxRect = new Rect(
            (Screen.width - width) / 2,
            tokenBoxYOffset,
            width,
            tokenSize + 2 * tokenBoxPadding
        );
    }

    public void AddTokens(GameObject gameObj, int numTokens)
    {
        AnimateTokens(Color.green, gameObj, numTokens);
    }

    public void RemoveTokens(GameObject gameObj, int numTokens)
    {
        AnimateTokens(Color.red, gameObj, -numTokens);
    }

    public void RemoveTokens(GameObject gameObj, int numTokens, Color color)
    {
        AnimateTokens(color, gameObj, -numTokens);
    }
    public void SetTokenBarValue(int value)
    {
        numCollected = value;
    }
    public int GetTokenBarValue()
    {
        return numCollected;
    }
    public void SetTokenBarFull(bool value)
    {
        tokenBarFull = value;
    }
    public bool isTokenBarFull()
    {
        return tokenBarFull;
    }
    public void OnGUI()
    {
        RenderTexture old = RenderTexture.active;
        Debug.Log("RENDERTEXTURE.ACTIVE: " + RenderTexture.active);
        if (Camera.main != null) {
            RenderTexture.active = Camera.main.targetTexture;
        }

        if (totalTokensNum < 0)
        {
            return;
        }

        GUI.BeginGroup(tokenBoxRect);
        Color oldBGColor = GUI.backgroundColor;
        Color oldColor = GUI.color;

        // Draw flashing box if needed
        if (animationPhase == AnimationPhase.Flashing)
        {
            GUI.backgroundColor = tokenBoxColor;
            GUI.Box(new Rect(0, 0, tokenBoxRect.width, tokenBoxRect.height), GUIContent.none, whiteStyle);
        }

        // Always draw the tokens
        Vector2 startPos = Vector2.one * tokenBoxPadding;
        GUI.color = colorCollected;
        startPos = DrawTokens(startPos, numCollected);
        GUI.color = colorUncollected;
        if (numCollected < 0) numCollected = 0;
        DrawTokens(startPos, totalTokensNum - numCollected);

        GUI.backgroundColor = oldBGColor;
        GUI.EndGroup();

        // Draw the animating tokens if needed
        if (animationPhase == AnimationPhase.Show || animationPhase == AnimationPhase.Update)
        {
            GUI.color = animatedTokensColor;
            DrawTokens(animatedTokensPos, animatedTokensNum);
        }

        GUI.color = oldColor;

        RenderTexture.active = old;
    }

    public bool IsAnimating()
    {
        return animationPhase != AnimationPhase.None || audioFBController.IsPlaying();
    }

    public string GetAnimationPhase()
    {
        return animationPhase.ToString();
    }

    public void Update()
    {
        if (animationPhase == AnimationPhase.None) return;

        // Switch to next animation phase if the current one ended
        if (Time.unscaledTime >= animationEndTime)
        {
            animationStartTime = Time.unscaledTime;
            animationEndTime = animationStartTime;
            switch (animationPhase)
            {
                case AnimationPhase.Show:
                    animationPhase = AnimationPhase.Update;
                    animationEndTime += updateTime;
                    break;
                case AnimationPhase.Update:
                   if (tokensChange < 0) {
                        audioFBController.Play("NegativeUpdate"); //not added
                    }
                    else {
                        audioFBController.Play("PositiveUpdate"); //not added
                    }
                    numCollected += tokensChange;
                    if (numCollected < 0) numCollected = 0; //set number to 0 if you lose more than you have, avoids neg tokens
                    animationPhase = AnimationPhase.None;
                    if (numCollected >= totalTokensNum)
                    {
                        animationPhase = AnimationPhase.Flashing;
                        StartCoroutine(FlashingBeeps(flashingNumBeeps)); //NT: put here instead of flashPhase, for it to be immediate. 
                        animationEndTime += flashingTime;
                    }
                    break;
                case AnimationPhase.Flashing:
                    //audioFBController.Play("Flashing"); //flashing clip doesn't exist
                    tokenBarFull = true;
                    numCollected = 0;
                    animationPhase = AnimationPhase.None;
                    break;
            }
        }

        // Set up the GUI state based on the animation phase
        float dt = Time.unscaledTime - animationStartTime;
        switch (animationPhase)
        {
            case AnimationPhase.Show:
                break;
            case AnimationPhase.Update:
                animatedTokensPos = Vector2.Lerp(animatedTokensStartPos, animatedTokensEndPos, dt / updateTime);
                break;
            case AnimationPhase.Flashing:
                if (dt < flashingTime / 2) tokenBoxColor = colorFlashing1;
                else tokenBoxColor = colorFlashing2;
                break;
        }
    }

    IEnumerator FlashingBeeps(int numBeeps)
    {
        while(numBeeps > 0)
        {
            audioFBController.Play("PositiveShow");
            numBeeps--;
            if(numBeeps > 0)
                yield return new WaitForSeconds(flashBeepInterval);
        }
    }

    public TokenFBController SetTotalTokensNum(int numTokens)
    {
        totalTokensNum = numTokens;
        RecalculateTokenBox();
        return this;
    }

    public TokenFBController SetRevealTime(float revealTime)
    {
        this.revealTime = revealTime;
        return this;
    }

    public TokenFBController SetUpdateTime(float updateTime)
    {
        this.updateTime = updateTime;
        return this;
    }

    public TokenFBController SetFlashingTime(float flashingTime)
    {
        this.flashingTime = flashingTime;
        return this;
    }
    
    public TokenFBController SetPositiveShowAudioClip(AudioClip clip) {
        audioFBController.AddClip("PositiveShow", clip);
        flashBeepInterval = clip.length;
        return this;
    }
    
    public TokenFBController SetNegativeShowAudioClip(AudioClip clip) {
        audioFBController.AddClip("NegativeShow", clip);
        return this;
    }

    public TokenFBController SetPositiveUpdateAudioClip(AudioClip clip) {
        audioFBController.AddClip("PositiveUpdate", clip);
        return this;
    }

    public TokenFBController SetNegativeUpdateAudioClip(AudioClip clip) {
        audioFBController.AddClip("NegativeUpdate", clip);
        return this;
    }

    public TokenFBController SetFlashingAudioClip(AudioClip clip) {
        audioFBController.AddClip("Flashing", clip);
        return this;
    }

    // gameObjPos should be at the center of the object
    private void AnimateTokens(Color color, GameObject gameObj, int numTokens)
    {
        // Viewport pos is in [0, 1] where (0, 0) is bottom right
        Vector2 viewportPos = Camera.main.WorldToViewportPoint(gameObj.transform.position);
        // GUI pos has (0, 0) is top left
        Vector2 pos = new Vector2(viewportPos.x * Screen.width, (1 - viewportPos.y) * Screen.height);

        int tokensEndNum = numCollected;
        tokensChange = numTokens;
        if (numTokens < 0) {
            numTokens = Mathf.Min(-numTokens, numCollected);
            tokensEndNum -= numTokens;
        } else {
            numTokens = Mathf.Min(numTokens, totalTokensNum - numCollected);
        }
        if (numTokens == 0)
        {
            audioFBController.Play("NegativeShow"); //fixes issue where they choose wrong but no tokens in bar so doesn't make it down to play neg FB. 
            return;
        }

        animatedTokensStartPos = pos;
        // No need for horizontal padding since it does nothing
        animatedTokensStartPos.x -= CalcTokensWidth(numTokens) / 2;
        animatedTokensStartPos.y -= tokenBoxPadding + tokenSize;
        animatedTokensPos = animatedTokensStartPos;

        animatedTokensEndPos = tokenBoxRect.position;
        animatedTokensEndPos.x += tokenBoxPadding + CalcTokensWidth(tokensEndNum);
        
        animatedTokensColor = color;

        // Start the animation phase state machine with the first state
        if (tokensChange < 0) {
            audioFBController.Play("NegativeShow");
        }
        else {
            audioFBController.Play("PositiveShow");
        }
        animationPhase = AnimationPhase.Show;
        animationStartTime = Time.unscaledTime;
        animationEndTime = animationStartTime + revealTime;
        animatedTokensNum = numTokens;
    }

    private Vector2 DrawTokens(Vector2 startPos, int numTokens)
    {
        startPos.x += tokenBoxPadding;
        startPos.y += tokenBoxPadding;
        for (int i = 0; i < numTokens; ++i)
        {
            GUI.DrawTexture(new Rect(startPos.x, startPos.y, tokenSize, tokenSize), tokenTexture);
            startPos.x += tokenSize + tokenSpacing;
        }
        return startPos;
    }

    private float CalcTokensWidth(int numTokens)
    {
        return tokenSize * numTokens + tokenSpacing * (numTokens - 1);
    }
}