using System.Collections.Generic;
using FruitRunner_Namespace;
using UnityEngine;
using USE_StimulusManagement;
using System.Linq;


public class ItemSpawner : MonoBehaviour
{
    public List<GameObject> BlockadePrefabs;

    public List<FruitRunner_StimDef> TrialQuaddles;

    public int[][] SpawnOrder_StimIndices;
    public string[][] QuaddleGeneralPositions;

    private int CurrentSpawnIndex = 0;

    public bool RandomSpawnLocations;



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
            GameObject stim = Instantiate(BlockadePrefabs[Random.Range(0, BlockadePrefabs.Count)]);
            stim.name = "Blockade";
            stim.tag = "Blockade";
            stim.AddComponent<Item_Blockade>().SetItemPosition(RandomSpawnLocations, parentTransform);
            stim.SetActive(true);
        }
        else
        {
            //Spawn Quaddle(s)
            string[] currentPositionsArray = QuaddleGeneralPositions[CurrentSpawnIndex];
            int numToSpawn = currentIndicesArray.Length;

            for (int i = 0; i < currentIndicesArray.Length; i++)
            {
                FruitRunner_StimDef quaddle = TrialQuaddles.FirstOrDefault(q => q.StimIndex == currentIndicesArray[i]);

                GameObject stim = Instantiate(quaddle.StimGameObject);
                stim.tag = "Quaddle";
                stim.AddComponent<CapsuleCollider>().isTrigger = true;
                //stim.AddComponent<FaceCamera>();

                Item_Quaddle quaddleComponent = stim.AddComponent<Item_Quaddle>();
                quaddleComponent.QuaddleType = quaddle.QuaddleFeedbackType;
                quaddleComponent.QuaddleTokenRewardMag = quaddle.StimTokenRewardMag;
                Debug.LogWarning(currentPositionsArray[i]);
                quaddleComponent.QuaddleGeneralPosition = currentPositionsArray[i];
                quaddleComponent.SetItemPosition(RandomSpawnLocations, parentTransform);

                stim.SetActive(true);
            }
        }
        CurrentSpawnIndex++;

        if (CurrentSpawnIndex == SpawnOrder_StimIndices.Length)
            CurrentSpawnIndex = 0;
    }





}

