using UnityEngine;


public class Floor : MonoBehaviour
{
    private readonly float MovementSpeed = 18f;

    //public GameObject floorTilePrefab;
    //public float tileLength = 20f; // Adjust this according to your needs
    //public int tilesOnScreen = 5;

    //private List<GameObject> activeTiles;
    //private Transform playerTransform;

    //void Start()
    //{
    //    activeTiles = new List<GameObject>();
    //    playerTransform = GameObject.Find("Player").GetComponent<Transform>();

    //    for (int i = 0; i < tilesOnScreen; i++)
    //    {
    //        SpawnTile();
    //    }
    //}

    void Update()
    {
        MoveFloor();

        //if (playerTransform.position.z > (activeTiles.Count - tilesOnScreen) * tileLength)
        //{
        //    SpawnTile();
        //    DeleteTile();
        //}
    }

    void MoveFloor()
    {
        transform.Translate(Vector3.back * MovementSpeed * Time.deltaTime);
    }

    //void SpawnTile()
    //{
    //    GameObject tile = Instantiate(floorTilePrefab, Vector3.forward * (activeTiles.Count * tileLength), Quaternion.identity);
    //    tile.name = "Floor";
    //    activeTiles.Add(tile);
    //}

    //void DeleteTile()
    //{
    //    Destroy(activeTiles[0]);
    //    activeTiles.RemoveAt(0);
    //}

}
