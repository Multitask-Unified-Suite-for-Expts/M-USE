using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MovementCirclesController : MonoBehaviour
{
    public GameObject Instantiated;

    public GameObject LeftCircleGO;
    public GameObject MiddleCircleGO;
    public GameObject RightCircleGO;

    public Transform ParentCanvasTransform;

    public FR_PlayerManager PlayerManager;

    public Color OriginalCircleColor;
    public List<GameObject> Circles;
    public Vector3 position = new Vector3(0f, -(Screen.height * .41f), 0f);



    public void SetupMovementCircles(Transform parentCanvas, FR_PlayerManager playerManager)
    {
        ParentCanvasTransform = parentCanvas;
        PlayerManager = playerManager;

        Instantiated = Instantiate(Resources.Load<GameObject>("Prefabs/MovementCircles"));
        Instantiated.name = "MovementCirclesParent";
        Instantiated.transform.SetParent(ParentCanvasTransform.transform);
        Instantiated.transform.localScale = Vector3.one;
        Instantiated.transform.localPosition = position;
        Instantiated.transform.localRotation = Quaternion.identity;

        LeftCircleGO = Instantiated.transform.Find("LeftCircle").gameObject;
        LeftCircleGO.AddComponent<Button>().onClick.AddListener(() => HandleCircleClicked(LeftCircleGO));

        MiddleCircleGO = Instantiated.transform.Find("MiddleCircle").gameObject;
        MiddleCircleGO.AddComponent<Button>().onClick.AddListener(() => HandleCircleClicked(MiddleCircleGO));

        RightCircleGO = Instantiated.transform.Find("RightCircle").gameObject;
        RightCircleGO.AddComponent<Button>().onClick.AddListener(() => HandleCircleClicked(RightCircleGO));

        Circles = new List<GameObject>
        {
            LeftCircleGO,
            MiddleCircleGO,
            RightCircleGO
        };

        OriginalCircleColor = LeftCircleGO.GetComponent<Image>().color;

        MiddleCircleGO.GetComponent<Image>().color = Color.cyan; //Start middle one out as active
    }

    public void HandleCircleClicked(GameObject clickedGO)
    {
        HighlightActiveCircle(clickedGO);

        if (clickedGO == LeftCircleGO)
            PlayerManager.MoveToPosition(PlayerManager.LeftPos);
        else if (clickedGO == MiddleCircleGO)
            PlayerManager.MoveToPosition(PlayerManager.MiddlePos);
        else if (clickedGO == RightCircleGO)
            PlayerManager.MoveToPosition(PlayerManager.RightPos);
        else
            Debug.LogWarning("CLICKED GO DOESNT MATCH LEFT, MIDDLE, or RIGHT circle!");
    }

    public void HighlightActiveCircle(GameObject clickedGO)
    {
        foreach(GameObject go in Circles)
        {
            go.GetComponent<Image>().color = go == clickedGO ? Color.cyan : OriginalCircleColor;
        }
    }


    private void OnDestroy()
    {
        if (Instantiated != null)
            Destroy(Instantiated);
    }

}


