using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cube : MonoBehaviour
{
    public float MaxX = 100;
    public float MaxZ = 100;
    // Start is called before the first frame update
    void Start()
    {
        
        GameObject gameObject1 = GameObject.Find("Cube");
        for (int x = 0; x< MaxX; x++)
        {
            for(int z = 0; z < MaxZ; z++)
            {
                GameObject newobj = Instantiate(gameObject1, new Vector3(x, 0, z), Quaternion.identity);
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
