using UnityEngine;
using System.Collections;

namespace Completed
{
	//Enemy inherits from MovingObject, our base class for objects that can move, Player also inherits from this.
	public class Enemy : MovingObject
	{
		public string id;
		public AudioClip attackSound1;						//First of two audio clips to play when attacking the player.
		public AudioClip attackSound2;						//Second of two audio clips to play when attacking the player.
		
		private Animator animator;							//Variable of type Animator to store a reference to the enemy's Animator component.
	
		//Start overrides the virtual Start function of the base class.
		protected override void Start ()
		{
			//Get and store a reference to the attached Animator component.
			animator = GetComponent<Animator> ();
			
			//Call the start function of our base class MovingObject.
			base.Start ();
		}
		
		
		//Override the AttemptMove function of MovingObject to include functionality needed for Enemy to skip turns.
		//See comments in MovingObject for more on how base AttemptMove function works.
		bool EnemyMove (int xDir, int yDir)
		{
			base.Move (xDir, yDir);
			return true;
		}
		
		
		//MoveEnemy is called by the GameManger each turn to tell each Enemy to try to move towards the player.
		public Vector2 MoveEnemy (int xDir, int yDir)
		{
			//Call the AttemptMove function and pass in the generic parameter Player, because Enemy is moving and expecting to potentially encounter a Player
			EnemyMove (xDir, yDir);

			return new Vector2(xDir, yDir);
		}

		//OnCantMove is called if Enemy attempts to move into a space occupied by a Player, it overrides the OnCantMove function of MovingObject 
		//and takes a generic parameter T which we use to pass in the component we expect to encounter, in this case Player
		public void OnCantMove ()
		{
			//Set the attack trigger of animator to trigger Enemy attack animation.
			animator.SetTrigger("enemyAttack");

			//Call the RandomizeSfx function of SoundManager passing in the two audio clips to choose randomly between.
			SoundManager.instance.RandomizeSfx(attackSound1, attackSound2);
		}

		private void Update()
		{
			UpdateMove();
		}

		protected override void OnMoveComplete()
		{

		}
	}
}
