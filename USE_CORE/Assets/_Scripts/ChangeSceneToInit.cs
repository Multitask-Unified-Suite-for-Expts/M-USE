using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeSceneToInit : MonoBehaviour
{
    private GameConfigs gameConfigs;

    // Start is called before the first frame update
    void Start()
    {
        gameConfigs = GameObject.FindWithTag("Game Configs").GetComponent<GameConfigs>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
