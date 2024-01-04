using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elipsoid : MonoBehaviour
{
    private float radiusX = 2.0f;
    private float radiusY = 1.5f;
    private float radiusZ = 2.0f;
    private int segments = 128;

    public Transform middleSpawnPoint;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    //void Start()
    //{
    //    CreateEllipsoid();
    //}

    public void CreateEllipsoid()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Material material = Resources.Load<Material>("Materials/TileMaterial");
        meshRenderer.material = material;

        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(radiusX * 2f, radiusY * 2f, radiusZ * 2f);

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[(segments + 1) * (segments + 1)];
        for (int i = 0; i <= segments; i++)
        {
            float phi = Mathf.PI * i / segments;
            for (int j = 0; j <= segments; j++)
            {
                float theta = 2.0f * Mathf.PI * j / segments;

                float x = radiusX * Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = radiusY * Mathf.Sin(phi) * Mathf.Sin(theta);
                float z = radiusZ * Mathf.Cos(phi);

                vertices[i * (segments + 1) + j] = new Vector3(x, y, z);
            }
        }

        int[] triangles = new int[segments * segments * 6];
        int index = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int next = (i + 1) % (segments + 1);
                int nextJ = (j + 1) % (segments + 1);

                triangles[index++] = i * (segments + 1) + j;
                triangles[index++] = i * (segments + 1) + next;
                triangles[index++] = next * (segments + 1) + j;

                triangles[index++] = i * (segments + 1) + next;
                triangles[index++] = next * (segments + 1) + nextJ;
                triangles[index++] = next * (segments + 1) + j;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;

        transform.position = new Vector3(0f, -1f, 0f);
        transform.localScale = new Vector3(4, 1, 4);


        // Create spawn points
        middleSpawnPoint = new GameObject("MiddleSpawnPoint").transform;
        leftSpawnPoint = new GameObject("LeftSpawnPoint").transform;
        rightSpawnPoint = new GameObject("RightSpawnPoint").transform;

        // Position spawn points
        middleSpawnPoint.position = transform.position;
        leftSpawnPoint.position = transform.position - transform.right * radiusX;
        rightSpawnPoint.position = transform.position + transform.right * radiusX;

        // Optionally, you can parent the spawn points to the ellipsoid
        middleSpawnPoint.parent = transform;
        leftSpawnPoint.parent = transform;
        rightSpawnPoint.parent = transform;

    }
}
