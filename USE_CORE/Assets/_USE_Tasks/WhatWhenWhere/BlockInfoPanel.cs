using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
            if(!string.IsNullOrEmpty(TaskLevel.BlockSummaryString))
            {
                blockInfoPanelText.GetComponent<Text>().supportRichText = true;
                blockInfoPanelText.GetComponent<Text>().text = "<size=17><color=#2d3436ff>" + TaskLevel.BlockSummaryString + "</color></size>";
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
