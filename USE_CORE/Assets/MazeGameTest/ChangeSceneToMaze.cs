using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ChangeSceneToMaze : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Handle screen touches.
        
        /*
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            // TODO change this to actually change scene
            Debug.Log("TOUCH ON BUTTON FOUND");
        }*/
    }
/*
    IEnumerator OnMouseDown()
    {
        // TODO I think the seconds thing isn't working
        
        Debug.Log("Changing scene to maze...");
        SceneManager.LoadSceneAsync("MazeScene");
        yield return new WaitForSeconds(1);
        SceneManager.UnloadSceneAsync("InitScene");
        yield return new WaitForSeconds(1);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("MazeScene"));  
        
            
    }*/
}
