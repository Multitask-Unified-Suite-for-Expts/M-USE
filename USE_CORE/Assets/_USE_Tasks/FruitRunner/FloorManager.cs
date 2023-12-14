using System.Collections.Generic;
using UnityEngine;


public class FloorManager : MonoBehaviour
{
    private readonly float MovementSpeed = 18f;
    public GameObject floorTilePrefab;
    private int tilesOnScreen = 15;
    private List<GameObject> activeTiles;
    private ItemSpawner itemSpawner;
    private int NumTilesSpawned;

    void Start()
    {
        activeTiles = new List<GameObject>();
        itemSpawner = GameObject.Find("ItemSpawner").GetComponent<ItemSpawner>();

        for (int i = 0; i < tilesOnScreen; i++)
        {
            SpawnTile();
        }
    }

    void Update()
    {
        MoveTiles();

        if (activeTiles.Count > 0 && activeTiles[0].transform.position.z <= -10f)
        {
            SpawnTile();
            DeleteTile();
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
            spawnPos = new Vector3(0, -.5f, lastTile.transform.position.z + (lastTileCollider.bounds.size.z + .2f));
        }
        
        GameObject tile = Instantiate(floorTilePrefab, spawnPos, Quaternion.identity);
        tile.name = "Tile";
        tile.gameObject.transform.parent = gameObject.transform;

        if(NumTilesSpawned > 3) //Dont spawn items on the first 3
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
