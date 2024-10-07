using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class DialogueController : MonoBehaviour
{
    private GameObject DialogueGO;
    private TextMeshProUGUI Dialogue_Text;
    private Animator dialogueAnimator;
    [HideInInspector] public Canvas Canvas;

    public void CreateDialogueBox(string textString, float displayTime)
    {
        if (DialogueGO != null)
        {
            Debug.LogWarning("EXISTS ALREADY SO DESTROYING DIALOGUE BOX");
            Destroy(DialogueGO);
        }

        DialogueGO = Instantiate(Resources.Load<GameObject>("Dialogue"));
        DialogueGO.name = "Dialogue";
        if (DialogueGO == null)
        {
            Debug.LogWarning("FAILED TO LOAD FROM RESOURCES");
            return;
        }

        Dialogue_Text = DialogueGO.GetComponentInChildren<TextMeshProUGUI>();
        Dialogue_Text.text = textString;

        SpriteRenderer renderer = DialogueGO.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 999; // Ensures it's on top

        DialogueGO.transform.SetParent(Canvas.transform, false);

        // Get the Animator component for triggering scale in and out animations
        dialogueAnimator = DialogueGO.GetComponent<Animator>();

        // Start the coroutine to wait for time, then scale out and destroy
        StartCoroutine(ScaleOutAndDestroy(displayTime));
    }

    private IEnumerator ScaleOutAndDestroy(float displayTime)
    {
        yield return new WaitForSeconds(displayTime);

        if (DialogueGO != null)
        {
            dialogueAnimator.SetBool("IsScalingOut", true);

            AnimatorClipInfo[] clipInfo = dialogueAnimator.GetCurrentAnimatorClipInfo(0);
            float scaleOutDuration = clipInfo[0].clip.length * 3f; //wiggle room

            yield return new WaitForSeconds(scaleOutDuration);

            Destroy(DialogueGO);
        }
        else
        {
            Debug.LogWarning("CANT DESTROY DIALOGUE BECAUSE IT'S NULL");
        }
    }
}


