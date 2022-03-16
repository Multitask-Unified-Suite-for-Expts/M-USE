using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitScreenMaze : MonoBehaviour
{
    private GameObject initBG;
    private GameObject initButton;
    private Vector3 bgSize = new Vector3(10.0f, 10.0f, 0.5f);
    private Vector3 buttonSize = new Vector3(0.5f, 0.5f, 0.7f);

    // Start is called before the first frame update
    void Start()
    {
        // Get center pos of maze
        GameObject mazeCenter = GameObject.FindWithTag("Center");

        // Set BG
        initBG = GameObject.CreatePrimitive(PrimitiveType.Cube);
        initBG.name = "initBG";
        initBG.transform.position = mazeCenter.transform.position;
        initBG.transform.localScale = bgSize;
        var rendererBG = initBG.GetComponent<Renderer>();
        initBG.GetComponent<Renderer>().material.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f));

        // Set init button
        initButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
        initButton.name = "initButton";
        initButton.transform.position = mazeCenter.transform.position;
        initButton.transform.localScale = buttonSize;
        var rendererButton = initButton.GetComponent<Renderer>();
        rendererButton.material.SetColor("_BaseColor", Color.blue);
        initButton.AddComponent<ChangeSceneToMaze>();

        // TODO delete this
        Debug.Log("finish rendering initScreen!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
