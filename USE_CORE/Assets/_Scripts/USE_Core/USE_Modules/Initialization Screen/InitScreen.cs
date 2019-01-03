using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitScreen : MonoBehaviour {

    public GameObject[] disableOnStart;
    public GameObject[] enableOnStart;
    public GameObject[] disableOnConfirm;
    public GameObject[] enableOnConfirm;
    [HideInInspector] 
    public bool Confirmed;


    public void Confirm()
    {
        Confirmed = true;
    }

}
