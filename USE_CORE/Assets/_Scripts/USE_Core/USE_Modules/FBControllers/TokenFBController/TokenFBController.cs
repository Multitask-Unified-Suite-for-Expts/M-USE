using UnityEngine;

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
    // Audio
    AudioFBController audioFBController;

    public void Init(AudioFBController audioFBController)
    {
        this.audioFBController = audioFBController;
        numCollected = 0;

        whiteStyle = new GUIStyle();
        whiteStyle.normal.background = Texture2D.whiteTexture;

        float width = CalcTokensWidth(totalTokensNum) + 2 * tokenBoxPadding;
        tokenBoxRect = new Rect(
            (Screen.width - width) / 2,
            tokenBoxYOffset,
            width,
            tokenSize + 2 * tokenBoxPadding
        );

        SetPositiveShowAudioClip(audioFBController.GetClip("Positive"));
        SetNegativeShowAudioClip(audioFBController.GetClip("Negative"));
    }

    public void AddTokens(GameObject gameObj, int numTokens)
    {
        AnimateTokens(Color.green, gameObj, numTokens);
    }

    public void RemoveTokens(GameObject gameObj, int numTokens)
    {
        AnimateTokens(Color.red, gameObj, -numTokens);
    }

    public void OnGUI()
    {
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
    }

    public bool IsAnimating()
    {
        return animationPhase != AnimationPhase.None || audioFBController.IsPlaying();
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
                        audioFBController.Play("NegativeUpdate");
                    } else {
                        audioFBController.Play("PositiveUpdate");
                    }
                    numCollected += tokensChange;
                    animationPhase = AnimationPhase.None;
                    if (numCollected >= totalTokensNum)
                    {
                        animationPhase = AnimationPhase.Flashing;
                        animationEndTime += flashingTime;
                    }
                    break;
                case AnimationPhase.Flashing:
                    audioFBController.Play("Flashing");
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

    public TokenFBController SetTotalTokensNum(int numTokens)
    {
        totalTokensNum = numTokens;
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
        if (numTokens == 0) return;

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
        } else {
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