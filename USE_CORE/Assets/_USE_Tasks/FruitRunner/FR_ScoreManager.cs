using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class FR_ScoreManager : MonoBehaviour
{
    public GameObject ScoreGO;
    public TextMeshProUGUI ScoreText;
    [HideInInspector] public int Score;


    private void Start()
    {
        SubscribeToEventManager();
    }


    void Update()
    {
        if (ScoreText != null)
        {
            ScoreText.text = $"{Score}";
        }
    }

    public void ActivateScoreText()
    {
        ScoreGO.SetActive(true);
    }

    public void DeactivateScoreText()
    {
        ScoreGO.SetActive(false);
    }

    public void SubscribeToEventManager()
    {
        FR_EventManager.OnScoreChanged += ScoreChanged;
    }

    public void ScoreChanged(int scoreChange)
    {
        Score += scoreChange;
        if (scoreChange != 0)
            StartCoroutine(ScoreCoroutine(scoreChange));
    }

    private void OnDestroy()
    {
        FR_EventManager.OnScoreChanged -= ScoreChanged;
    }

    private IEnumerator ScoreCoroutine(int scoreChange)
    {
        Color startingColor = ScoreText.color;
        ScoreText.color = scoreChange > 0 ? Color.green : Color.red;
        yield return new WaitForSeconds(.25f);
        ScoreText.color = startingColor;
    }
}
