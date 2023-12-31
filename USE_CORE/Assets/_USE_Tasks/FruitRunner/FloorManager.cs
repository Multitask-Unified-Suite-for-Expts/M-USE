using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FloorManager : MonoBehaviour
{
    private readonly float MovementSpeed = 15;
    private GameObject floorTilePrefab;
    private int tilesOnScreen = 8;
    private List<GameObject> activeTiles;
    private ItemSpawner itemSpawner;
    private int NumTilesSpawned;

    private bool Move;


    public void ActivateMovement()
    {
        Move = true;
    }

    public void DeactivateMovement()
    {
        Move = false;
    }



    void Start()
    {
        floorTilePrefab = Resources.Load<GameObject>("Prefabs/Tile");

        activeTiles = new List<GameObject>();
        itemSpawner = GameObject.Find("ItemSpawner").GetComponent<ItemSpawner>();

        for (int i = 0; i < tilesOnScreen; i++)
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

        if (activeTiles.Count > 0)
        {
            BoxCollider collider = activeTiles[0].GetComponent<BoxCollider>();

            if(collider.bounds.max.z < transform.position.z - collider.bounds.size.z)
            {
                DeleteTile();
                SpawnTile();
            }
        }
    }

    void MoveTiles()
    {
        foreach(var tile in activeTiles)
        {
            tile.transform.Translate(Vector3.back * MovementSpeed * Time.deltaTime);
        }
    }

    void SpawnTile()
    {
        Vector3 spawnPos = new Vector3(0f, -.5f, 0f); //for first one

        if(activeTiles.Count > 0)
        {
            GameObject lastTile = activeTiles[activeTiles.Count - 1];
            BoxCollider lastTileCollider = lastTile.GetComponent<BoxCollider>();
            spawnPos.z = lastTile.transform.position.z + lastTileCollider.bounds.size.z; //removed .25f
        }

        GameObject tile = Instantiate(floorTilePrefab, spawnPos, Quaternion.identity);
        tile.name = "Tile " + (NumTilesSpawned + 1);

        tile.gameObject.transform.parent = gameObject.transform;

        if (NumTilesSpawned > 0) //Dont spawn items on the first 1
            itemSpawner.SpawnItem(tile.transform);

        activeTiles.Add(tile);

        NumTilesSpawned++;
    }

    void DeleteTile()
    {
        Destroy(activeTiles[0]);
        activeTiles.RemoveAt(0);
    }

}
