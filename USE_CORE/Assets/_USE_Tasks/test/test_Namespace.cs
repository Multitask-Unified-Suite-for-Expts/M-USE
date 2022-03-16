using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;

namespace test_Namespace
{
    public class test_TaskDef : TaskDef
    {
        private int myInt;
        private float myFloat;
        private string myString;
        private bool MyBool;
        private Vector2 myVector2;
        private Vector3 myVector3;
        private Vector4 myVector4;

        private int? myIntQ;
        private float? myFloatQ;
        private Vector2? myV2Q;
        private Vector3? myV3Q;
        private Vector4? myV4Q;

        private int[] myintArray;
        private float[] myFloatArray;
        private string[] myStringArray;
        private bool[] myBoolArray;
        private Vector2[] v2array;
        private Vector3[] v3array;
        private Vector4[] v4array;
        
        private int?[] myintqArray;
        private float?[] myFloatqArray;
        private bool?[] myBoolqArray;
        private Vector2?[] v2qarray;
        private Vector3?[] v3qarray;
        private Vector4?[] v4qarray;
        
        
        private List<int> myintList;
        private List<float> myFloatList;
        private List<string> myStringList;
        private List<bool> myBoolList;
        private List<Vector2> v2List;
        private List<Vector3> v3List;
        private List<Vector4> v4List;
        
        
        private List<int?> myintqList;
        private List<float?> myFloatqList;
        private List<bool?> myBoolqList;
        private List<Vector2?> v2qList;
        private List<Vector3?> v3qList;
        private List<Vector4?> v4qList;
        
        
        private List<int[]> myintArrayList;
        private List<float[]> myFloatArrayList;
        private List<string[]> myStringArrayList;
        private List<bool[]> myBoolArrayList;
        private List<Vector2[]> v2ArrayList;
        private List<Vector3[]> v3ArrayList;
        private List<Vector4[]> v4ArrayList;
        
        private List<int?[]> myintqArrayList;
        private List<float?[]> myFloatqArrayList;
        private List<bool?[]> myBoolqArrayList;
        private List<Vector2?[]> v2qArrayList;
        private List<Vector3?[]> v3qArrayList;
        private List<Vector4?[]> v4qArrayList;
        
        
        private List<int>[] myintListArray;
        private List<float>[] myFloatListArray;
        private List<string>[] myStringListArray;
        private List<bool>[] myBoolListArray;
        private List<Vector2>[] v2ListArray;
        private List<Vector3>[] v3ListArray;
        private List<Vector4>[] v4ListArray;
        
        private List<int?>[] myintqListArray;
        private List<float?>[] myFloatqListArray;
 //       private List<string?>[] myStringqListArray;
        private List<bool?>[] myBoolqListArray;
    }

    public class test_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
		//public int BlockCount;
		//public TrialDef[] TrialDefs;
    }

    public class test_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
		//public int BlockCount, TrialCountInBlock, TrialCountInTask;
		//public TrialStims TrialStims;
    }

    public class test_StimDef : StimDef
    {
        //Already-existing fields (inherited from Stim  Def)
        //public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
        //public string StimName;
        //public string StimPath;
        //public string PrefabPath;
        //public string ExternalFilePath;
        //public string StimFolderPath;
        //public string StimExtension;
        //public int StimCode; //optional, for analysis purposes
        //public string StimID;
        //public int[] StimDimVals; //only if this is parametrically-defined stim
        //[System.NonSerialized] //public GameObject StimGameObject; //not in config, generated at runtime
        //public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
        //public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
        //public Vector2 StimScreenLocation; //screen position calculated during trial
        //public float? StimScale;
        //public bool StimLocationSet;
        //public bool StimRotationSet;
        //public float StimTrialPositiveFbProb; //set to -1 if stim is irrelevant
        //public float StimTrialRewardMag; //set to -1 if stim is irrelevant
        //public TokenReward[] TokenRewards;
        //public int[] BaseTokenGain;
        //public int[] BaseTokenLoss;
        //public int TimesUsedInBlock;
        //public bool isRelevant;
        //public bool TriggersSonication;
        //public State SetActiveOnInitialization;
        //public State SetInactiveOnTermination;
    
    }
}