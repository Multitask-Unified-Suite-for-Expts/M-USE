using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;

public class StimDefPointer: MonoBehaviour
{
    public StimDef StimDef;

    public StimDefPointer()
    {
        StimDef = new StimDef();
    }

    public StimDefPointer(StimDef sd)
    {
        StimDef = sd;
    }

    public T GetStimDef<T>() where T : StimDef
    {
        return (T) StimDef;
    }
}
