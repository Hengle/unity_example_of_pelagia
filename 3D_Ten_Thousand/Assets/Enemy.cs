using Completed;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MovingObject
{
    // Start is called before the first frame update

    public void MoveEnemy (float xDir, float zDir)
    {
        base.Move(xDir, zDir);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMove();
    }
}
