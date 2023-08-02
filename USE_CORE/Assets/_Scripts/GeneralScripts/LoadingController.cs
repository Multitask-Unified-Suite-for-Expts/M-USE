using UnityEngine;
using UnityEngine.UI;


public class LoadingController : MonoBehaviour
{
    public GameObject FillCircle_GO;
    [HideInInspector] public Image FillCircle_Image;
    public bool Loading;
    private float Progress = 0;


    void Start()
    {
        FillCircle_Image = FillCircle_GO.GetComponent<Image>();

        Progress = 0;
        FillCircle_Image.fillAmount = Progress;
    }

    private void Update()
    {
        if(Loading)
        {
            Progress += .005f;

            if (Progress >.995f)
                Progress = 0f;
            
            FillCircle_Image.fillAmount = Progress;
        }
    }


}
