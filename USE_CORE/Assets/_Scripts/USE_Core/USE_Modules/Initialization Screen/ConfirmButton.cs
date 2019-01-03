using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfirmButton : MonoBehaviour {
    [HideInInspector]
    public bool confirmed;

    public void Confirm()
    {
        Debug.Log("asdgkh");
        confirmed = true;
    }
}
