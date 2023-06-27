using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class SessionValues
{
    public static bool WebBuild; //Set immediately when initscreen starts

    public static bool UseDefaultConfigs; //Set in initScreen script after they select config in dropdown

    public static bool Using2DStim; //set when stim are loaded. Not in the best spot. gonna be set for every stim instead of just once. move it later. 

}
