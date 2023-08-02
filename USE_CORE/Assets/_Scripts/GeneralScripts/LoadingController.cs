using UnityEngine;
using UnityEngine.UI;


//NT: USE THIS CLASS BY SETTING SessionValues.LoadingCanvas_GO Active or Inactive!

public class LoadingController : MonoBehaviour
{
    public GameObject FillCircle_GO;
    [HideInInspector] public Image FillCircle_Image;

    private float Progress;
    private float incrementValue = 1f;


    void Start()
    {
        FillCircle_Image = FillCircle_GO.GetComponent<Image>();
        Progress = 0f;
        FillCircle_Image.fillAmount = Progress;
    }

    private void Update()
    {
        if(FillCircle_GO.activeInHierarchy)
        {
            Progress += incrementValue * Time.deltaTime;

            if (Progress >= 1f)
                Progress = 0f;
            
            FillCircle_Image.fillAmount = Progress;
        }
    }


}
