using UnityEngine;
using System.Collections;

public class TokenFeedbackController : MonoBehaviour {
    public int tokenSize = 100;
    public int tokenSpacing = 0;
    public int tokenBoxPadding = 0;
    public int tokenBoxYOffset = 10;
    public Texture2D tokenTexture;

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
    private float showTime; // How long to show the token update before animating
    private float updateTime; // How long each token update should take
    private float flashingTime; // How long the token bar should flash when it fills up

    public void Initialize(int numTokens, float showTime, float updateTime, float flashingTime) {
        totalTokensNum = numTokens;
        this.showTime = showTime;
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
    public void AddTokens(Vector2 gameObjPos, int numTokens) {
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

        animationPhase = AnimationPhase.Show;
        animationStartTime = Time.unscaledTime;
        animationEndTime = animationStartTime + showTime;
        animatedTokensNum = numTokens;
    }

    public bool IsAnimating() {
        return animationPhase != AnimationPhase.None;
    }

    public void OnGUI() {
        if (totalTokensNum < 0) {
            return;
        }

        GUI.BeginGroup(tokenBoxRect);
        Color oldBGColor = GUI.backgroundColor;
        Color oldColor = GUI.color;

        if (animationPhase == AnimationPhase.Flashing) {
            GUI.backgroundColor = tokenBoxColor;
            GUI.Box(new Rect(0, 0, tokenBoxRect.width, tokenBoxRect.height), GUIContent.none, whiteStyle);
        }

        Vector2 startPos = Vector2.one * tokenBoxPadding;
        GUI.color = new Color(0, 1.0f, 0);
        startPos = DrawTokens(startPos, numCollected);
        GUI.color = new Color(0.5f, 0.5f, 0.5f);
        DrawTokens(startPos, totalTokensNum - numCollected);

        GUI.backgroundColor = oldBGColor;
        GUI.EndGroup();

        if (animationPhase == AnimationPhase.Update) {
            GUI.color = new Color(0, 1.0f, 0);
            DrawTokens(animatedTokensPos, animatedTokensNum);
        }

        GUI.color = oldColor;
    }

    public void Update() {
        if (animationPhase == AnimationPhase.None) return;

        if (Time.unscaledTime >= animationEndTime) {
            animationStartTime = Time.unscaledTime;
            switch (animationPhase) {
                case AnimationPhase.Show:
                    animationPhase = AnimationPhase.Update;
                    animationEndTime = animationStartTime + updateTime;
                    break;
                case AnimationPhase.Update:
                    numCollected += animatedTokensNum;
                    Debug.Log(Time.unscaledTime + ": Adding " + animatedTokensNum);
                    if (numCollected >= totalTokensNum) {
                        animationPhase = AnimationPhase.Flashing;
                        animationEndTime = animationStartTime + flashingTime;
                    } else {
                        animationPhase = AnimationPhase.None;
                    }
                    break;
                case AnimationPhase.Flashing:
                    numCollected = 0;
                    animationPhase = AnimationPhase.None;
                    break;
            }
        }

        float dt = Time.unscaledTime - animationStartTime;
        switch (animationPhase) {
            case AnimationPhase.Show:
                break;
            case AnimationPhase.Update:
                animatedTokensPos = Vector2.Lerp(animatedTokensStartPos, animatedTokensEndPos, dt / updateTime);
                break;
            case AnimationPhase.Flashing:
                if (dt < flashingTime / 2) {
                    tokenBoxColor = new Color(0, 0, 1.0f);
                } else {
                    tokenBoxColor = new Color(1.0f, 0, 0);
                }
                break;
        }
    }

    private Vector2 DrawTokens(Vector2 startPos, int numTokens) {
        startPos.x += tokenBoxPadding;
        startPos.y += tokenBoxPadding;
        for (int i = 0; i < numTokens; ++i) {
            GUI.DrawTexture(new Rect(startPos.x, startPos.y, tokenSize, tokenSize), tokenTexture);
            startPos.x += tokenSize + tokenSpacing;
        }
        return startPos;
    }

    private float CalcTokensWidth(int numTokens) {
        return tokenSize * numTokens + tokenSpacing * (numTokens - 1);
    }
}