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
                Session.Prolific_SessionID = data.sessionid;
                Session.Prolific_StudyID = data.studyid;
                Session.Prolific_PlayerID = data.playerid;
                Session.Prolific_ConfigFolderName = data.config;

                Debug.LogWarning($"✅ Received SessionID: {data.sessionid}, StudyID: {data.studyid}, PID: {data.playerid}, CONFIG: {data.config}");

                Session.StoringDataOnServer = true;
                Session.SubjectID = Session.Prolific_PlayerID;
                Session.SubjectAge = ""; //setting empty since dont know prolific subject age

                Session.UsingServerConfigs = true;
                ServerManager.SetSessionConfigFolderName(Session.Prolific_ConfigFolderName); //SET AS PROLIFIC NAME
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
        Debug.LogWarning("✅ Tasks complete — notifying parent page...");

#if UNITY_WEBGL && !UNITY_EDITOR
        NotifyParentTasksComplete();
#endif

    }


    private void SetEditorValuesManually()
    {
        Debug.LogWarning("Manually setting values for prolific testing");
        Session.Prolific_PlayerID = "123";
        Session.Prolific_ConfigFolderName = "Config_TestA";
        Session.Prolific_SessionID = "EditorSession";
        Session.Prolific_StudyID = "TestStudy";

        Session.StoringDataOnServer = true;
        Session.SubjectID = Session.Prolific_PlayerID;
        Session.SubjectAge = ""; //setting empty since dont know prolific subject age

        Session.UsingServerConfigs = true;
        ServerManager.SetSessionConfigFolderName(Session.Prolific_ConfigFolderName); //SET AS PROLIFIC NAME
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
