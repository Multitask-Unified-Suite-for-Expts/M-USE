using System.Collections.Generic;
using FruitRunner_Namespace;
using UnityEngine;
using USE_StimulusManagement;
using System.Linq;


public class FR_ItemManager : MonoBehaviour
{
    public List<GameObject> BlockadePrefabs;

    public List<FruitRunner_StimDef> TrialQuaddles;


    public int[][] SpawnOrder_StimIndices;
    public string[][] QuaddleGeneralPositions;

    private int CurrentSpawnIndex = 0;

    public int BananaTokenGain;
    public int BlockadeTokenLoss;

    public bool StimFaceCamera;


    private void Start()
    {
        BlockadePrefabs = new List<GameObject>
        {
            Resources.Load<GameObject>("Prefabs/Blockade_Left"),
            Resources.Load<GameObject>("Prefabs/Blockade_Right")
        };
    }

    public void SetupQuaddleList(List<StimDef> stims)
    {
        TrialQuaddles = new List<FruitRunner_StimDef>();
        foreach (FruitRunner_StimDef stim in stims)
        {
            TrialQuaddles.Add(stim);
        }
    }

    public void SetSpawnOrder(int[][] spawnOrder)
    {
        SpawnOrder_StimIndices = spawnOrder;
    }

    public void SetQuaddleGeneralPositions(string[][] generalPositions)
    {
        QuaddleGeneralPositions = generalPositions;
    }


    public void SpawnItem(Transform parentTransform)
    {
        int[] currentIndicesArray = SpawnOrder_StimIndices[CurrentSpawnIndex];

        if (currentIndicesArray.Length == 1 && currentIndicesArray[0] == -1)
        {
            //Spawn a Blockade:
            GameObject blockade = Instantiate(BlockadePrefabs[Random.Range(0, BlockadePrefabs.Count)]);
            FR_Item_Blockade blockadeComponent = blockade.AddComponent<FR_Item_Blockade>();
            blockadeComponent.GeneralPosition = blockade.name.ToLower().Contains("left") ? "Left" : "Right";
            blockadeComponent.TokenLoss = BlockadeTokenLoss;
            blockadeComponent.SetItemPosition(parentTransform);
            blockade.name = "Blockade";
            blockade.tag = "Blockade";
            blockade.SetActive(true);
        }
        else if(currentIndicesArray.Length == 1 && currentIndicesArray[0] == -2)
        {
            //dont spawn a quaddle when its -2
        }
        else if (currentIndicesArray.Length == 1 && currentIndicesArray[0] == -3)
        {
            //Spawn a Banana:
            GameObject banana = Instantiate(Resources.Load<GameObject>("Prefabs/Banana"));
            banana.name = "Banana";
            FR_Item_Banana bananaComponent = banana.AddComponent<FR_Item_Banana>();
            bananaComponent.TokenGain = BananaTokenGain;
            bananaComponent.SetItemPosition(parentTransform);
            banana.SetActive(true);
        }
        else
        {
            //Spawn Quaddle(s)
            string[] currentPositionsArray = QuaddleGeneralPositions[CurrentSpawnIndex];
            int numToSpawn = currentIndicesArray.Length;

            for (int i = 0; i < numToSpawn; i++)
            {
                FruitRunner_StimDef quaddle = TrialQuaddles.FirstOrDefault(q => q.StimIndex == currentIndicesArray[i]);

                GameObject stim = Instantiate(quaddle.StimGameObject);
                stim.tag = "Quaddle";
                CapsuleCollider collider = stim.AddComponent<CapsuleCollider>();
                if(!Session.UsingDefaultConfigs) //TEMP SOLUTION
                    collider.center = new Vector3(0f, 0f, -1f);
                collider.isTrigger = true;
                if(StimFaceCamera)
                    stim.AddComponent<FaceCamera>();

                FR_Item_Quaddle quaddleComponent = stim.AddComponent<FR_Item_Quaddle>();
                quaddleComponent.QuaddleType = quaddle.QuaddleFeedbackType;
                quaddleComponent.QuaddleTokenRewardMag = quaddle.StimTokenRewardMag;
                
                quaddleComponent.GeneralPosition = currentPositionsArray[i];
               
                quaddleComponent.SetItemPosition(parentTransform);

                stim.SetActive(true);
            }
        }
        CurrentSpawnIndex++;

        if (CurrentSpawnIndex == SpawnOrder_StimIndices.Length)
            CurrentSpawnIndex = 0;
    }





}

