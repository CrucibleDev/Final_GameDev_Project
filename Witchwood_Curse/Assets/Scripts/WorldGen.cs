using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGen : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;

    public int gridWidth = 10; 
    public int gridHeight = 10; 

    private int[,] dungeonGrid;

    // Start is called before the first frame update
    void Start()
    {
        dungeonGrid = new int[gridWidth, gridHeight];
        //GameObject inst = Instantiate(prefab,new Vector3(40,0,0),Quaternion.identity);

        for (int i = 1; i <= 20; i++)
        {
            int x = Random.Range(1, gridWidth + 1);
            int z = Random.Range(1, gridHeight + 1);

            GameObject inst = Instantiate(prefab,new Vector3(x * 40, 0, z * 40),Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
