using UnityEngine;
using UnityEngine.UI;


//NT: USE THIS CLASS BY SETTING SessionValues.LoadingCanvas_GO Active or Inactive!

public class LoadingController : MonoBehaviour
{
    //For loading circle animation:
    public GameObject FillCircle_GO;
    [HideInInspector] public Image FillCircle_Image;
    private float Progress;

    //For star animation:
    public GameObject Star_GO;
    private float starRotationSpeed = 125f;


    void Start()
    {
        FillCircle_Image = FillCircle_GO.GetComponent<Image>();
        Progress = 0f;
        FillCircle_Image.fillAmount = Progress;
    }

    private void Update()
    {
        RotateStar();
        //LoadingCircleAnimation();
    }

    private void RotateStar()
    {
        Star_GO.transform.Rotate(Vector3.forward, starRotationSpeed * Time.deltaTime);
    }

    private void LoadingCircleAnimation()
    {
        if (FillCircle_GO.activeInHierarchy)
        {
            Progress += 1f * Time.deltaTime;
            if (Progress >= 1f)
                Progress = 0f;
            FillCircle_Image.fillAmount = Progress;
        }
    }


}
