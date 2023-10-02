/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

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
using System;
using System.Threading.Tasks;
using UnityEngine.UI;
using USE_ExperimenterDisplay;

public class BlockInfoPanel : ExperimenterDisplayPanel
{
    public BlockInfoList biList;
    public GameObject blockInfoPanel;
    public GameObject blockInfoPanelText;

    // Start is called before the first frame update
    public override void CustomPanelInitialization()
    {
        biList = new BlockInfoList();
        biList.Initialize();
        blockInfoPanel = GameObject.Find("BlockInfoPanel");
        blockInfoPanelText = GameObject.Find("BlockInfoSummaryText");
    }

    public override void CustomPanelUpdate()
    {
        if (TrialLevel != null)
        {
            blockInfoPanelText.GetComponent<Text>().supportRichText = true;

            if (TaskLevel.CurrentBlockSummaryString?.Length > 0)
            {
                blockInfoPanelText.GetComponent<Text>().text = "<size=23><color=#2d3436ff>" + 
                                                               "<b>\nBlock Count:</b> " + (TaskLevel.BlockCount + 1) +
                                                               "<b>\nTrial Count in Block:</b> " + (TrialLevel.TrialCount_InBlock + 1) + 
                                                               "\n" + TaskLevel.CurrentBlockSummaryString + 
                                                               "\n" + TaskLevel.PreviousBlockSummaryString +
                                                               "</color></size>";
                                                               
            }
            else
            {
                blockInfoPanelText.GetComponent<Text>().text = "<size=23><color=#2d3436ff>" +
                                                               "<b>\nBlock Count:</b> " + (TaskLevel.BlockCount + 1) +
                                                               "<b>\nTrial Count in Block:</b> " + (TrialLevel.TrialCount_InBlock + 1) +
                                                               "</color></size>";
            }
        }
            
    }

    public class BlockInfo
    {
        public string dataDescription;
        //public string dataValue;

        public string GenerateTextDescription()
        {
            return dataDescription + " -> "; // add + dataValue eventually
        }
    }
    public class BlockInfoList
    {
        List<BlockInfo> BlockInfos = new List<BlockInfo>();
        public string GenerateBlockInfo()
        {
            string completeString = "";
            foreach (BlockInfo bi in BlockInfos)
            {
                completeString = completeString + bi.GenerateTextDescription() + "\n";
            }

            Debug.Log("BlockInfo: " + completeString);

            return completeString;
        }
        public void Initialize(Func<List<BlockInfo>> CustomBlockInfoList = null)
        {
            if (CustomBlockInfoList == null)
                BlockInfos = DefaultBlockInfoList(); //this is your default function
            else
                BlockInfos = CustomBlockInfoList(); //allows users to specify task-specific lists - this will end up looking something like the various task-specific classes like WWW_TaskDef or whatever

            //GenerateTextForPanel(); //method that loops through each hotkey and creates the string to show the hotkey options, using the GenerateTextDescription function of each on
        }
        public List<BlockInfo> DefaultBlockInfoList()
        {
            List<BlockInfo> BlockInfoList = new List<BlockInfo>();

            BlockInfo blockNumber = new BlockInfo
            {
                dataDescription = "Block: "
            };
            BlockInfoList.Add(blockNumber);

            BlockInfo blockPerformance = new BlockInfo
            {
                dataDescription = "Block Performance: "
            };
            BlockInfoList.Add(blockPerformance);

            return BlockInfoList;
        }
    }
}
