using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{
    // Start is called before the first frame update

    public float speed = 5;
    public float MaxX = 100;
    public float MinX = 0;
    public float MaxZ = 100;
    public float MinZ = 0;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, new Vector3(Random.Range(MinX, MaxX), 0, Random.Range(MinZ, MaxZ)), speed * Time.deltaTime);
    }
}
