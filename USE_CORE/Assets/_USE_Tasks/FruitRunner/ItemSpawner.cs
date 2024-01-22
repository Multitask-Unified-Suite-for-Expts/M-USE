using System.Collections.Generic;
using FruitRunner_Namespace;
using UnityEngine;
using USE_StimulusManagement;
using System.Linq;


public class ItemSpawner : MonoBehaviour
{
    public List<GameObject> BlockadePrefabs;
    public List<FruitRunner_StimDef> TrialQuaddles;
    public int[] SpawnOrder_StimIndices;
    private int SpawnIndex = 0;

    public bool RandomSpawnLocations;


    private void Update()
    {
        if(TrialQuaddles.Count > 0)
        {
            foreach(var quaddle in TrialQuaddles)
            {
                if(quaddle.StimGameObject == null)
                {
                    Debug.LogWarning("YUP STIM IS NULL!!!");
                }
            }
        }
    }

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

    public void SetSpawnOrder(int[] spawnOrder)
    {
        SpawnOrder_StimIndices = spawnOrder;
    }



    public void SpawnItem(Transform parentTransform)
    {
        GameObject stim;

        if (SpawnOrder_StimIndices[SpawnIndex] == -1)
        {
            //Spawn a Blockade:
            stim = Instantiate(BlockadePrefabs[Random.Range(0, BlockadePrefabs.Count)]);
            stim.name = "Blockade";
            stim.tag = "Blockade";
            stim.AddComponent<Item_Blockade>()
                .SetItemPosition(RandomSpawnLocations, parentTransform);
        }
        else
        {
            //Spawn A Quaddle
            FruitRunner_StimDef quaddle = TrialQuaddles.FirstOrDefault(q => q.StimIndex == SpawnOrder_StimIndices[SpawnIndex]);
            stim = Instantiate(quaddle.StimGameObject);
            stim.tag = "Quaddle";
            //stim.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            stim.AddComponent<CapsuleCollider>().isTrigger = true;
            stim.AddComponent<FaceCamera>();

            Item_Quaddle quaddleComponent = stim.AddComponent<Item_Quaddle>();
            quaddleComponent.QuaddleType = quaddle.QuaddleFeedbackType;
            quaddleComponent.QuaddleGeneralPosition = quaddle.QuaddleGeneralPosition;
            quaddleComponent.QuaddleTokenRewardMag = quaddle.StimTokenRewardMag;
            quaddleComponent.SetItemPosition(RandomSpawnLocations, parentTransform);
        }

        SpawnIndex++;

        if (SpawnIndex == SpawnOrder_StimIndices.Length)
            SpawnIndex = 0;

        stim.SetActive(true);
    }





}

