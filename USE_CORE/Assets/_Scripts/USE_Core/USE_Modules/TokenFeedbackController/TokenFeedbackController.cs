using UnityEngine;

public class TokenFeedbackController : MonoBehaviour
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
    private int totalTokensNum = -1;
    private int numCollected = 0;
    // Rendering
    private Rect tokenBoxRect;
    private GUIStyle whiteStyle;
    // Animation
    enum AnimationPhase { None, Show, Update, Flashing };
    private AnimationPhase animationPhase = AnimationPhase.None;
    private Vector2 animatedTokensPos;
    private Vector2 animatedTokensStartPos;
    private Vector2 animatedTokensEndPos;
    private int animatedTokensNum;
    private Color tokenBoxColor;
    private float animationStartTime;
    private float animationEndTime;
    private float revealTime; // How long to show the tokens before animating
    private float updateTime; // How long each token update animation should take
    private float flashingTime; // How long the token bar should flash when it fills up

    public void Initialize(int numTokens, float revealTime, float updateTime, float flashingTime = 0.5f)
    {
        numCollected = 0;
        totalTokensNum = numTokens;
        this.revealTime = revealTime;
        this.updateTime = updateTime;
        this.flashingTime = flashingTime;

        whiteStyle = new GUIStyle();
        whiteStyle.normal.background = Texture2D.whiteTexture;

        float width = CalcTokensWidth(totalTokensNum) + 2 * tokenBoxPadding;
        tokenBoxRect = new Rect(
            (Screen.width - width) / 2,
            tokenBoxYOffset,
            width,
            tokenSize + 2 * tokenBoxPadding
        );
    }

    // gameObjPos should be at the center of the object
    public void AddTokens(Vector2 gameObjPos, int numTokens)
    {
        // Viewport pos is in [0, 1] where (0, 0) is bottom right
        Vector2 viewportPos = Camera.main.WorldToViewportPoint(gameObjPos);
        // GUI pos has (0, 0) is top left
        Vector2 pos = new Vector2(viewportPos.x * Screen.width, (1 - viewportPos.y) * Screen.height);
        int maxNewTokens = totalTokensNum - numCollected;
        numTokens = Mathf.Min(numTokens, maxNewTokens);

        animatedTokensStartPos = pos;
        // No need for horizontal padding since it does nothing
        animatedTokensStartPos.x -= CalcTokensWidth(numTokens) / 2;
        animatedTokensStartPos.y -= tokenBoxPadding + tokenSize;
        animatedTokensPos = animatedTokensStartPos;

        animatedTokensEndPos = tokenBoxRect.position;
        animatedTokensEndPos.x += tokenBoxPadding + CalcTokensWidth(numCollected);

        // Start the animation phase state machine with the first state
        animationPhase = AnimationPhase.Show;
        animationStartTime = Time.unscaledTime;
        animationEndTime = animationStartTime + revealTime;
        animatedTokensNum = numTokens;
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
        if (animationPhase == AnimationPhase.Update)
        {
            GUI.color = colorCollected;
            DrawTokens(animatedTokensPos, animatedTokensNum);
        }

        GUI.color = oldColor;
    }

    public bool IsAnimating()
    {
        return animationPhase != AnimationPhase.None;
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
                    numCollected += animatedTokensNum;
                    animationPhase = AnimationPhase.None;
                    if (numCollected >= totalTokensNum)
                    {
                        animationPhase = AnimationPhase.Flashing;
                        animationEndTime += flashingTime;
                    }
                    break;
                case AnimationPhase.Flashing:
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