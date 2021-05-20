using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitScreen : MonoBehaviour {

    public GameObject[] disableOnStart;
    public GameObject[] enableOnStart;
    public GameObject[] disableOnConfirm;
    public GameObject[] enableOnConfirm;
    public bool Confirmed;

    public event System.Action OnConfirm, OnLoadSettings;

    void Start(){
        foreach (GameObject g in disableOnStart)
            g.SetActive(false);
        foreach (GameObject g in enableOnStart)
            g.SetActive(true);
    }

    IEnumerator HandleConfirm(){
        if(OnLoadSettings != null){
            OnLoadSettings();
        }


        // this.gameObject.SetActive(false);

        foreach (GameObject g in disableOnConfirm)
            g.SetActive(false);
        foreach (GameObject g in enableOnConfirm)
            g.SetActive(true);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Confirmed = true;
        if(OnConfirm != null){
            OnConfirm();
        }
        this.gameObject.SetActive(false);
        yield return 0;
    }

    public void Confirm()
    {
        StartCoroutine(HandleConfirm());
    }

}
