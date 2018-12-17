using UnityEngine;

public class ShowHideCursor : MonoBehaviour {
	public bool hideCursorAtStart = false;
	void Start () {
		if(hideCursorAtStart)
			Cursor.visible = false;
	}
	void Update () {
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))&& Input.GetKeyDown(KeyCode.C))
        {
            Cursor.visible = true;
        }
        else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))&& Input.GetKeyDown(KeyCode.X))
        {
            Cursor.visible = false;
        }		
	}
}
