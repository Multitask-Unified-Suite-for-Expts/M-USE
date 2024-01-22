using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FloorManager : MonoBehaviour
{
    public float FloorMovementSpeed = 20f; //20 is great for humans
    private GameObject FloorTilePrefab;
    public int NumTilesSpawned;
    public int TotalTiles;
    public List<GameObject> ActiveTiles;

    private ItemSpawner itemSpawner;

    private bool Move;

    public float TileScale_Z;


    public void SetTotalTiles(int numPerGroup, int numGroups)
    {
        TotalTiles = 1 + (numPerGroup * numGroups); //Add 1 for initial empty tile
    }

    void Start()
    {
        FloorTilePrefab = Resources.Load<GameObject>("Prefabs/Tile_Double");

        ActiveTiles = new List<GameObject>();
        itemSpawner = GameObject.Find("ItemSpawner").GetComponent<ItemSpawner>();

        for (int i = 0; i <= TotalTiles; i++)
        {
            SpawnTile();
        }

        Move = false;
    }



    void Update()
    {
        if (!Move)
            return;

        MoveTiles();

        if (ActiveTiles.Count > 0)
        {
            BoxCollider collider = ActiveTiles[0].GetComponent<BoxCollider>();

            if(collider.bounds.max.z < transform.position.z - collider.bounds.size.z)
            {
                DeleteTile();
                //SpawnTile();
            }
        }
    }

    void MoveTiles()
    {
        foreach(var tile in ActiveTiles)
        {
            tile.transform.Translate(Vector3.back * FloorMovementSpeed * Time.deltaTime);
        }
    }

    void SpawnTile()
    {
        Vector3 spawnPos = new Vector3(0f, -.5f, -.5f); //for first one

        if(ActiveTiles.Count > 0)
        {
            GameObject lastTile = ActiveTiles[ActiveTiles.Count - 1];
            BoxCollider lastTileCollider = lastTile.GetComponent<BoxCollider>();
            spawnPos.z = lastTile.transform.position.z + lastTileCollider.bounds.size.z;
        }

        GameObject tile = Instantiate(FloorTilePrefab, spawnPos, Quaternion.identity);
        tile.name = "Tile " + (NumTilesSpawned + 1);
        tile.transform.localScale = new Vector3(1f, 1f, TileScale_Z);
        tile.gameObject.transform.parent = gameObject.transform;
        tile.AddComponent<Item_Floor>();

        //if (NumTilesSpawned > 1 && NumTilesSpawned % 2 != 0) //No item on first floor, and then have an empty floor in between each floor that has an item. 
        if (NumTilesSpawned > 1) //No item on first floor, and then have an empty floor in between each floor that has an item. 
            itemSpawner.SpawnItem(tile.transform);

        //if(NumTilesSpawned + 1 == TotalTiles)
        //{
        //    Debug.LogWarning("GONNA SPAWN AN ARCH");
        //    GameObject arch = Instantiate(Resources.Load<GameObject>("Prefabs/Arch"));
        //    arch.transform.SetParent(tile.transform);

        //}

        ActiveTiles.Add(tile);

        NumTilesSpawned++;
    }



    public void ActivateMovement()
    {
        Move = true;
    }

    public void DeactivateMovement()
    {
        Move = false;
    }



    void DeleteTile()
    {
        Destroy(ActiveTiles[0]);
        ActiveTiles.RemoveAt(0);
    }


}
