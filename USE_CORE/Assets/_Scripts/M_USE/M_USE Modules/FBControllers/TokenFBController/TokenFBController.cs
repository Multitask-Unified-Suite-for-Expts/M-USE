/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



using UnityEngine;
using USE_Data;


public class TokenFBController : MonoBehaviour
{
    public float tokenSize = 110;
    public float tokenSpacing = 0;
    public float tokenBoxPadding = 0;
    public float tokenBoxYOffset = 10;
    public Texture2D tokenTexture;

    public float scaledTokenSize;
    public float scaledTokenBoxPadding;
    public float scaledTokenBoxYOffset;

    public float scaleFactor;

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
    public enum AnimationPhase { None, Show, Update, Flashing };
    public AnimationPhase animationPhase = AnimationPhase.None;
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
    // Audio
    AudioFBController audioFBController;


    public void Init(DataController trialData, DataController frameData, AudioFBController audioFBController)
    {
        trialData.AddDatum("TokenBarValue", () => numCollected);
        trialData.AddDatum("TokenChange", () => tokensChange);
        trialData.AddDatum("TokenBarFull", ()=> tokenBarFull);
        frameData.AddDatum("TokenAnimationPhase", () => animationPhase.ToString());
        this.audioFBController = audioFBController;
        numCollected = 0;

        whiteStyle = new GUIStyle();
        whiteStyle.normal.background = Texture2D.whiteTexture;

        RecalculateTokenBox();

        SetPositiveShowAudioClip(audioFBController.GetClip("Positive"));
        SetNegativeShowAudioClip(audioFBController.GetClip("Negative"));

        //Subscribe to FullScreen Events:
        if(SessionValues.FullScreenController != null)
            SessionValues.FullScreenController.SubscribeToFullScreenChanged(OnFullScreenChanged);
    }


    public void RecalculateTokenBox()
    {
        Vector2 referenceResolution = new Vector2(1920, 1080);

        scaleFactor = Mathf.Min(Screen.width / referenceResolution.x, Screen.height / referenceResolution.y);

        scaledTokenSize = tokenSize * scaleFactor;
        scaledTokenBoxPadding = tokenBoxPadding * scaleFactor;
        scaledTokenBoxYOffset = tokenBoxYOffset * scaleFactor;

        float width = CalcTokensWidth(totalTokensNum) + 2 * scaledTokenBoxPadding;

        float xOffset = (Screen.width - width) / 2;

        tokenBoxRect = new Rect(
            xOffset,
            scaledTokenBoxYOffset,
            width,
            scaledTokenSize + 2 * scaledTokenBoxPadding
        );
    }

    public void AddTokens(GameObject gameObj, int numTokens, float? yAdj = null)
    {
        AnimateTokens(Color.green, gameObj, numTokens, yAdj);
    }

    public void RemoveTokens(GameObject gameObj, int numTokens, float? yAdj = null)
    {
        AnimateTokens(Color.grey, gameObj, -numTokens, yAdj);
    }

    public void SetTokenBarValue(int value)
    {
        numCollected = value;
    }
    public int GetTokenBarValue()
    {
        return numCollected;
    }
   public void ResetTokenBarFull()
    {
        tokenBarFull = false;

    }
    public bool IsTokenBarFull()
    {
        return tokenBarFull;
    }

    public void AdjustTokenBarSizing(int newTokenSize)
    {
        tokenSize = newTokenSize;
        RecalculateTokenBox();
    }

    public void OnGUI()
    {
        RenderTexture oldTexture = RenderTexture.active;
        if (Camera.main != null)
            if (Camera.main.targetTexture != null)
                RenderTexture.active = Camera.main.targetTexture;

        if (totalTokensNum < 0)
            return;

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
        Vector2 startPos = Vector2.one * scaledTokenBoxPadding;
        GUI.color = colorCollected;
        startPos = DrawTokens(startPos, numCollected);
        GUI.color = colorUncollected;
        if (numCollected < 0)
            numCollected = 0;
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
        RenderTexture.active = oldTexture;
    }

    private Vector2 DrawTokens(Vector2 startPos, int numTokens)
    {
        startPos.x += scaledTokenBoxPadding;
        startPos.y += scaledTokenBoxPadding;
        for (int i = 0; i < numTokens; ++i)
        {
            GUI.DrawTexture(new Rect(startPos.x, startPos.y, scaledTokenSize, scaledTokenSize), tokenTexture);
            startPos.x += scaledTokenSize + tokenSpacing;
        }
        return startPos;
    }

    public bool IsAnimating()
    {
        return animationPhase != AnimationPhase.None;
    }

    public string GetAnimationPhase()
    {
        return animationPhase.ToString();
    }

    public void Update()
    {
        if (animationPhase == AnimationPhase.None)
            return;

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
                    numCollected += tokensChange;
                    if (numCollected < 0)
                        numCollected = 0; //set number to 0 if you lose more than you have, avoids neg tokens
                    animationPhase = AnimationPhase.None;
                    if (numCollected >= totalTokensNum)
                    {
                        animationPhase = AnimationPhase.Flashing;
                        tokenBarFull = true;
                        audioFBController.Play("TripleCollected");
                        SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.EventCodeManager.SessionEventCodes["TokenFbController_FullTbAnimationStart"]);
                        animationEndTime += flashingTime;
                    }
                    break;
                case AnimationPhase.Flashing:
                    numCollected = 0;
                    animationPhase = AnimationPhase.None;
                    SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.EventCodeManager.SessionEventCodes["TokenFbController_FullTbAnimationEnd"]);
                    SessionValues.EventCodeManager.SendCodeNextFrame(SessionValues.EventCodeManager.SessionEventCodes["TokenFbController_TbReset"]);
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
                int flashingInterval = (int)(flashingTime * 10000 / 2);
                int elapsed = (int)((Time.unscaledTime - animationStartTime) * 10000 % (flashingTime * 10000));
                int colorIndex = elapsed / flashingInterval;
                if (colorIndex % 2 == 0)
                    tokenBoxColor = colorFlashing1;
                else
                    tokenBoxColor = colorFlashing2;
                break;
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
    private void AnimateTokens(Color color, GameObject gameObj, int numTokens, float? yAdj = null)
    {
        Vector3 yAdjusted = new Vector3(0, yAdj == null ? 0 : yAdj.Value, 0); //Ability to adjust how far above/below the GameObject the tokens appear

        // Viewport pos is in [0, 1] where (0, 0) is bottom right
        Vector2 viewportPos = Camera.main.WorldToViewportPoint(gameObj.transform.position + yAdjusted);
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
        animatedTokensStartPos.y -= scaledTokenBoxPadding + scaledTokenSize;
        animatedTokensPos = animatedTokensStartPos;

        animatedTokensEndPos = tokenBoxRect.position;
        animatedTokensEndPos.x += scaledTokenBoxPadding + CalcTokensWidth(tokensEndNum);
        
        animatedTokensColor = color;

        // Start the animation phase state machine with the first state
        if (tokensChange < 0)
            audioFBController.Play("NegativeShow");
        else
            audioFBController.Play("PositiveShow");
        
        animationPhase = AnimationPhase.Show;
        animationStartTime = Time.unscaledTime;
        animationEndTime = animationStartTime + revealTime;
        animatedTokensNum = numTokens;
    }

    private float CalcTokensWidth(int numTokens)
    {
        return scaledTokenSize * numTokens + tokenSpacing * (numTokens - 1);
    }


    private void OnFullScreenChanged(bool isFullScreen)
    {
        RecalculateTokenBox();
    }

    private void OnDestroy()
    {
        //Unsubscribe from FullScreenChanged Event:
        if (SessionValues.FullScreenController != null)
            SessionValues.FullScreenController.UnsubscribeToFullScreenChanged(OnFullScreenChanged);
    }

}
