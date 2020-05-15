using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RPGM.Gameplay
{
    /// <summary>
    /// A simple camera follower class. It saves the offset from the
    ///  focus position when started, and preserves that offset when following the focus.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public Transform focus;
        public float smoothTime = 2;
        GameObject play;

        Vector3 offset;

        void LateUpdate()
        {
            if (play != null)
            {
                transform.position = new Vector3(play.transform.position.x + (float)3.5, play.transform.position.y + (float)3.5, transform.position.z);

            }
        }
        void Awake()
        {
            play = GameObject.FindGameObjectWithTag("Player");
        }
    }
}