using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrisBlock : MonoBehaviour
{
    private float previousTime;
    private float fallTime = .8f;
    public Vector3 rotationPoint;

    //public static int height = 20;
    //public static int width = 10;

    public RectTransform rect;

    private bool Fall;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        Fall = true;
    }

    void Update()
    {

        if(Fall)
        {
            if (rect.transform.localPosition.x < 281 && rect.transform.localPosition.x > -280)
            {
                if(InputBroker.GetKeyDown(KeyCode.LeftArrow))
                    transform.position += new Vector3(-10, 0, 0);
            }
            if(rect.transform.localPosition.x > -281 && rect.transform.localPosition.x < 280)
            {
                if (InputBroker.GetKeyDown(KeyCode.RightArrow))
                    transform.position += new Vector3(10, 0, 0);
            }


            if(Time.time - previousTime > (InputBroker.GetKey(KeyCode.DownArrow) ? fallTime / 10 : fallTime))
            {
                transform.position += new Vector3(0, -10, 0);
                previousTime = Time.time;
            }

            if(InputBroker.GetKeyDown(KeyCode.UpArrow))
            {
                transform.RotateAround(transform.TransformPoint(rotationPoint), new Vector3(0, 0, 1), 90);

            }
        }


        if (rect.transform.localPosition.y <= -450)
        {
            Fall = false;
            rect.transform.localPosition = new Vector3(rect.transform.localPosition.x, rect.transform.localPosition.y, rect.transform.localPosition.z);
        }
    }

    //private bool ValidMove()
    //{
    //    foreach(Transform child in transform)
    //    {
    //        int roundedX = Mathf.RoundToInt(child.transform.position.x);
    //        int roundedY = Mathf.RoundToInt(child.transform.position.y);

    //        if(roundedX < 0 || roundedX >= width || roundedY < 0 || roundedY >= height)
    //            return false;
    //    }
    //    return true;
    //}
}
