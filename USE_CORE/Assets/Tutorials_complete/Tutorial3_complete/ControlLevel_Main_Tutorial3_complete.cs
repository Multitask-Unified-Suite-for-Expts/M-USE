/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;

public class ControlLevel_Main_Tutorial3_complete : ControlLevel {

    public GameObject textObj,panelObj;

    public int numBlocks = 3, trialsPerBlock = 10;

    public override void DefineControlLevel(){
        State intro = new State("Intro");
        State mainTask = new State("MainTask");
        State goodbye = new State("Goodbye");
        AddActiveStates(new List<State> { intro, mainTask, goodbye });

        ControlLevel_TextSlides slideLevel = transform.GetComponent<ControlLevel_TextSlides>();

        slideLevel.slideText = new string[] {"Welcome to our study!\nThank you very much for participating.",
            "In this task you will be shown two objects on each trial. You will have to choose one of them by clicking on it with the mouse.",
            "Wait for the \"Go!\" signal before clicking.", "After clicking, you will get feedback. A green square means your choice was rewarded. A red square means it was not.",
            "Try to learn which object gives the most reward.", "Ask the experimenter if you have any questions, otherwise we will begin the experiment."};

        intro.AddChildLevel(slideLevel);
        intro.SpecifyTermination(() => slideLevel.Terminated, mainTask);

        ControlLevel_Block_Tutorial3_complete blockLevel = transform.GetComponent<ControlLevel_Block_Tutorial3_complete>();
        blockLevel.numBlocks = numBlocks;
        blockLevel.numTrials = trialsPerBlock;
        mainTask.AddChildLevel(blockLevel);
        mainTask.SpecifyTermination(() => blockLevel.Terminated, goodbye);

        goodbye.AddInitializationMethod(() =>
        {
            textObj.SetActive(true);
            panelObj.SetActive(true);
            textObj.GetComponent<Text>().text = "Thank you very much for your time!";
        });
        goodbye.AddTimer(2f, null);
    }
}
