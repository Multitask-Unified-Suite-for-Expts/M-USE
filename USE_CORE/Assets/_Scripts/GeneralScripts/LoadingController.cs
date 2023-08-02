using UnityEngine;
using UnityEngine.UI;


public class LoadingController : MonoBehaviour
{
    public GameObject Circle_GO;
    public GameObject FillCircle_GO;

    [HideInInspector] public Image Circle_Image;
    [HideInInspector] public Image FillCircle_Image;

    public bool Loading;

    private float Progress = 0;

    void Start()
    {
        Debug.Log("LOADING START!");
        SessionValues.LoadingController = this;
        Circle_Image = Circle_GO.GetComponent<Image>();
        FillCircle_Image = FillCircle_GO.GetComponent<Image>();
    }

    private void Update()
    {
        if(Loading)
        {
            Progress += .01f;
            if (Progress == 1)
                Progress = 0;

            Circle_Image.fillAmount = Progress;
        }
    }


}
