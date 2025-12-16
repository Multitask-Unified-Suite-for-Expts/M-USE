using UnityEngine;
using System;
using System.Runtime.InteropServices;



public class Prolific_Controller : MonoBehaviour
{
    #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NotifyParentTasksComplete();
    #endif


    private void Start()
    {
        Session.Prolific_Controller_Session = this;

#if UNITY_EDITOR && UNITY_WEBGL
        SetEditorValuesManually();
#endif
    }


    public void ReceiveURLParams(string json)
    {
        Debug.LogWarning("📩 Received params from page: " + json);
        try
        {
            var data = JsonUtility.FromJson<ParamData>(json);
            if (data != null)
            {
                Session.SessionID_Param = data.sessionid;
                Session.StudyID_Param = data.studyid;
                Session.PlayerID_Param = data.playerid;
                Session.ConfigFolderName_Param = data.config;

                Debug.LogWarning($"✅ Received SessionID: {data.sessionid}, StudyID: {data.studyid}, PID: {data.playerid}, CONFIG: {data.config}");

                Session.StoringDataOnServer = true;
                Session.SubjectID = Session.PlayerID_Param;
                Session.SubjectAge = ""; //setting empty since dont know prolific subject age

                Session.UsingServerConfigs = true;
                ServerManager.SetSessionConfigFolderName(Session.ConfigFolderName_Param); //SET AS PROLIFIC NAME
                Session.ConfigFolderPath = ServerManager.SessionConfigFolderPath;
            }
            else
            {
                Debug.LogWarning("⚠️ Failed to parse JSON data from page.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Error parsing params: " + ex.Message);
        }
    }

    public void TriggerRedirectoToCompletionURL()
    {
        Debug.LogWarning("✅ TASKS COMPLETE - NOTIFYING WORDPRESS PAGE");

#if UNITY_WEBGL && !UNITY_EDITOR
        NotifyParentTasksComplete();
#endif

    }


    private void SetEditorValuesManually()
    {
        Debug.LogWarning("Manually setting values for prolific testing");
        Session.PlayerID_Param = "123";
        Session.ConfigFolderName_Param = "SessionConfig_112_WEBGL_Test1";
        Session.SessionID_Param = "EditorSession";
        Session.StudyID_Param = "TestStudy";

        Session.StoringDataOnServer = true;
        Session.SubjectID = Session.PlayerID_Param;
        Session.SubjectAge = ""; //setting empty since dont know prolific subject age

        Session.UsingServerConfigs = true;
        ServerManager.SetSessionConfigFolderName(Session.ConfigFolderName_Param); //SET AS PROLIFIC NAME
        Session.ConfigFolderPath = ServerManager.SessionConfigFolderPath;
    }


}



[Serializable]
public class ParamData
{
    public string sessionid;
    public string studyid;
    public string playerid;
    public string config;

}
