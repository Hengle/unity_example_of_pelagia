using UnityEngine;
using System.Collections;

namespace Completed
{
	//The abstract keyword enables you to create classes and class members that are incomplete and must be implemented in a derived class.
	public abstract class MovingObject : MonoBehaviour
	{
		public float moveTime = 0.1f;			//Time it will take object to move, in seconds.
		public LayerMask blockingLayer;			//Layer on which collision will be checked.
		
		private float inverseMoveTime;			//Used to make movement more efficient.
		public bool isMoving;                  //Is the object currently moving.
		private Vector3 MoveEnd;

		//Protected, virtual functions can be overridden by inheriting classes.
		protected virtual void Start ()
		{						
			//By storing the reciprocal of the move time we can use it by multiplying instead of dividing, this is more efficient.
			inverseMoveTime = 1f / moveTime;
		}
		
		//Move returns true if it is able to move and false if not. 
		//Move takes parameters for x direction, y direction and a RaycastHit2D to check collision.
		protected bool Move (float xDir, float zDir)
		{
	
			//Store start position to move from, based on objects current transform position.
			Vector3 start = transform.position;

			// Calculate end position based on the direction parameters passed in when calling Move.
			Vector3 end = new Vector3 (xDir, 0, zDir);
			
			//Check if nothing was hit and that the object isn't already moving.
			if(!isMoving)
			{
				//Start SmoothMovement co-routine passing in the Vector2 end as destination
				isMoving = true;
				MoveEnd = end;
				//Return true to say that Move was successful
				return true;
			}
			
			//If something was hit, return false, Move was unsuccesful.
			return false;
		}

		protected void UpdateMove()
		{
			if (isMoving)
			{
				//Calculate the remaining distance to move based on the square magnitude of the difference between current position and end parameter. 
				//Square magnitude is used instead of magnitude because it's computationally cheaper.
				float sqrRemainingDistance = (transform.position - MoveEnd).sqrMagnitude;

				if (sqrRemainingDistance > float.Epsilon)
				{
					//Find a new position proportionally closer to the end, based on the moveTime
					Vector3 newPostion = Vector3.MoveTowards(transform.position, MoveEnd, inverseMoveTime * Time.deltaTime);
					this.transform.localPosition = newPostion;
				} else
				{
					//The object is no longer moving.
					isMoving = false;
				}
			}
		}
	}
}
