using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Completed
{
	using System.Collections.Generic;       //Allows us to use Lists. 
	using System.Data;
	using System.Linq;
	using System.Runtime.InteropServices;
    using UnityEngine.UI;                   //Allows us to use UI.

	public class GameManager : MonoBehaviour
	{
		[System.Serializable]
		public class NpcEntity
		{
			public int id;
			public int type;
			public int x;
			public int y;
		}
		[System.Serializable]
		public class RecMsg
		{
			public string msg;
			public int sx;
			public int sy;
			public int id;
			public int x;
			public int y;
			public int hp;
			public int target;
			public int del;
			public List<NpcEntity> npc;
		}
		[System.Serializable]
		public class InitMsg
		{
			public int global_x;
			public int global_y;
			public int view_x;
			public int view_y;
			public int x;
			public int y;
			public int npc_count;
			public int food_count;
			public int wall_count;
		}

		public GameObject exit;                                         //Prefab to spawn for exit.
		public GameObject[] floorTiles;                                 //Array of floor prefabs.
		public GameObject[] wallTiles;                                  //Array of wall prefabs.
		public GameObject[] foodTiles;                                  //Array of food prefabs.
		public GameObject[] enemyTiles;                                 //Array of enemy prefabs.
		public GameObject[] outerWallTiles;                             //Array of outer tile prefabs.
		public GameObject exitTitle;
		private Transform boardHolder;                                  //A variable to store a reference to the transform of our Board object.

		public int columns = 100;                                        //Number of columns in our game board.
		public int rows = 100;                                           //Number of rows in our game board.
		public int viewColumns = 100;                                        //Number of columns in our game board.
		public int viewRows = 100;                                           //Number of rows in our game board.
		public int enemyCount = 0;
		public int wallCount = 0;
		public int foodCount = 0;
		public float levelStartDelay = 2f;                      //Time to wait before starting level, in seconds.
		public float turnDelay = 0.1f;                          //Delay between each Player turn.
		public int playerFoodPoints = 100;                      //Starting value for Player food points.
		public static GameManager instance = null;              //Static instance of GameManager which allows it to be accessed by any other script.
		[HideInInspector] public bool playersTurn = true;       //Boolean to check if it's players turn, hidden in inspector but public.

		private Text levelText;                                 //Text to display current level number.
		private GameObject levelImage;                          //Image to block out level as levels are being set up, background for levelText.
		private int level = 1;                                  //Current level number, expressed in game as "Day 1".
		private Dictionary<string, GameObject> idEnemy = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> xyEnemy = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> idFood = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> xyFood = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> idWall = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> xyWall = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> xyFloor = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> xyOutWall = new Dictionary<string, GameObject>();

		private List<GameObject> recrecycleEnemy = new List<GameObject>();
		private List<GameObject> recrecycleFood = new List<GameObject>();
		private List<GameObject> recrecycleWall = new List<GameObject>();
		private List<GameObject> recrecycleFloor = new List<GameObject>();
		private List<GameObject> recrecycleOutWall = new List<GameObject>();

		private bool doingSetup = true;                         //Boolean to check if we're setting up board, prevent Player from moving during setup.
		private Pelagia m_Pelagia;
		//Awake is always called before any Start functions

		Player player;
	
		void Awake()
		{
			//Check if instance already exists
			if (instance == null)

				//if not, set instance to this
				instance = this;

			//If instance already exists and it's not this:
			else if (instance != this)

				//Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
				Destroy(gameObject);

			//Sets this to not be destroyed when reloading scene
			DontDestroyOnLoad(gameObject);

			GameObject go = GameObject.FindGameObjectWithTag("Player");
			player = go.GetComponent<Player>();

			m_Pelagia = new Pelagia();
			m_Pelagia.Init("./rpg.json");

			InitMsg im = new InitMsg();
			im.global_x = columns;
			im.global_y = rows;
			im.view_x = viewColumns;
			im.view_y = viewRows;
			im.npc_count = enemyCount;
			im.wall_count = wallCount;
			im.food_count = foodCount;
			string json = JsonUtility.ToJson(im);
			m_Pelagia.Call("init", json);

			//Call the InitGame function to initialize the first level 
			InitGame();
		}

		public void PlayerMove(int x, int y)
		{
			InitMsg im = new InitMsg();
			im.x = x;
			im.y = y;
			string json = JsonUtility.ToJson(im);
			m_Pelagia.Call("play_move", json);
		}

		//this is called only once, and the paramter tell it to be called only after the scene was loaded
		//(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static public void CallbackInitialization()
		{
			//register the callback to be called everytime the scene is loaded
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		//This is called each time a scene is loaded.
		static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			instance.level++;
			instance.InitGame();
		}

		//Sets up the outer walls and floor (background) of the game board.
		void BoardSetup()
		{
			//Instantiate Board and set boardHolder to its transform.
			boardHolder = new GameObject("Board").transform;

			int ex = (viewColumns / 2 >= columns) ? columns - 1 : viewColumns / 2;
			int ey = (viewRows / 2 >= rows) ? rows - 1 : viewRows / 2;

			//Loop along x axis, starting from -1 (to fill corner) with floor or outerwall edge tiles.
			for (int x = 0; x <= ex; x++)
			{
				//Loop along y axis, starting from -1 to place floor or outerwall tiles.
				for (int y = 0; y <= ey; y++)
				{
					//Check if we current position is at board edge, if so choose a random outer wall prefab from our array of outer wall tiles
					AddOutWall(x, y);

					GameObject toInstantiate;
					if (x == columns - 1 && y == rows - 1)
					{
						toInstantiate = exitTitle;
					} else
					{
						//Choose a random tile from our array of floor tile prefabs and prepare to instantiate it.
						toInstantiate = floorTiles[Random.Range(0, floorTiles.Length)];
					}

					//Instantiate the GameObject instance using the prefab chosen for toInstantiate at the Vector3 corresponding to current grid position in loop, cast it to GameObject.
					GameObject instance = Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;

					//Set the parent of our newly instantiated object instance to boardHolder, this is just organizational to avoid cluttering hierarchy.
					instance.transform.SetParent(boardHolder);

					xyFloor.Add(x.ToString() + "_" + y.ToString(), instance);
				}
			}
		}

		void InsideAddOutWall(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyOutWall.ContainsKey(key)) return;

			GameObject instance;
			if (recrecycleOutWall.Count > 0)
			{
				instance = recrecycleOutWall.Last<GameObject>();
				recrecycleOutWall.RemoveAt(recrecycleOutWall.Count - 1);
				instance.transform.position = new Vector3(x, y, 0f);
				instance.SetActive(true);
			} else
			{
				//Choose a random tile from our array of floor tile prefabs and prepare to instantiate it.
				GameObject toInstantiate = outerWallTiles[Random.Range(0, outerWallTiles.Length)];
				//Instantiate the GameObject instance using the prefab chosen for toInstantiate at the Vector3 corresponding to current grid position in loop, cast it to GameObject.
				instance = Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
			}

			//Set the parent of our newly instantiated object instance to boardHolder, this is just organizational to avoid cluttering hierarchy.
			instance.transform.SetParent(boardHolder);

			xyOutWall.Add(key, instance);
		}

		void AddOutWall(int x, int y)
		{
			bool alter = false;
			//Check if we current position is at board edge, if so choose a random outer wall prefab from our array of outer wall tiles.
			if (x == 0 || x == columns - 1 || y == 0 || y == rows - 1)
			{
				if (x == 0 && y == 0)
				{
					InsideAddOutWall(0, -1);
					InsideAddOutWall(-1, 0);
				}

				if (x == columns - 1 && y == rows - 1)
				{
					InsideAddOutWall(rows - 1, columns);
					InsideAddOutWall(rows, columns - 1);
				}

				if (x == 0 && y == rows - 1)
				{
					InsideAddOutWall(0, rows);
					InsideAddOutWall(-1, rows - 1);
				}

				if (x == columns - 1 && y == 0)
				{
					InsideAddOutWall(columns - 1, -1);
					InsideAddOutWall(columns, 0);
				}

				if (x == 0) { x = -1; alter = true; }
				if (y == 0) { y = -1; alter = true; }
				if (x == columns - 1) { x = columns; alter = true; }
				if (y == rows - 1) { y = rows; alter = true; }
			}

			if (alter)
				InsideAddOutWall(x, y);
		}

		void AddFloor(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyFloor.ContainsKey(key)) return;

			GameObject instance;
			if (recrecycleFloor.Count > 0)
			{
				instance = recrecycleFloor.Last<GameObject>();
				recrecycleFloor.RemoveAt(recrecycleFloor.Count - 1);
				instance.transform.position = new Vector3(x, y, 0f);
				instance.SetActive(true);
			}
			else
			{
				GameObject toInstantiate;
				if (x == columns - 1 && y == rows - 1)
				{
					toInstantiate = exitTitle;
				}
				else
				{
					//Choose a random tile from our array of floor tile prefabs and prepare to instantiate it.
					toInstantiate = floorTiles[Random.Range(0, floorTiles.Length)];
				}

				//Instantiate the GameObject instance using the prefab chosen for toInstantiate at the Vector3 corresponding to current grid position in loop, cast it to GameObject.
				instance = Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
			}

			//Set the parent of our newly instantiated object instance to boardHolder, this is just organizational to avoid cluttering hierarchy.
			instance.transform.SetParent(boardHolder);
			xyFloor.Add(key, instance);
		}

		void AddEnemy(string id, int x, int y)
		{
			if (idEnemy.ContainsKey(id)) return;

			GameObject r;
			if (recrecycleEnemy.Count > 0)
			{
				r = recrecycleEnemy.Last<GameObject>();
				recrecycleEnemy.RemoveAt(recrecycleEnemy.Count - 1);
				r.transform.position = new Vector3(x, y, 0f);
				r.SetActive(true);
			}
			else
			{
				r = LayoutObjectAtRandom(enemyTiles, x, y);
			}

			Enemy enemy = r.GetComponent<Enemy>();
			enemy.id = id;
			idEnemy.Add(id, r);
			xyEnemy.Add(x.ToString() + "_" + y.ToString(), r);
		}

		void REnemy(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyEnemy.ContainsKey(key))
			{
				GameObject go = xyEnemy[key];
				go.SetActive(false);
				recrecycleEnemy.Add(go);
				xyEnemy.Remove(key);
				Enemy enemy = go.GetComponent<Enemy>();
				idEnemy.Remove(enemy.id);
			}
		}

		void RFood(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyFood.ContainsKey(key))
			{
				GameObject go = xyFood[key];
				go.SetActive(false);
				recrecycleFood.Add(go);
				xyFood.Remove(key);
				Food food = go.GetComponent<Food>();
				idFood.Remove(food.id);
			}
		}

		void RFood(string id, int x, int y)
		{
			if (idFood.ContainsKey(id))
			{
				GameObject go = idFood[id];
				go.SetActive(false);
				recrecycleFood.Add(go);
				xyFood.Remove(x.ToString() + "_" + y.ToString());
				idFood.Remove(id);
			}
		}

		void RWall(string id, int x, int y)
		{
			if (idWall.ContainsKey(id))
			{
				GameObject go = idWall[id];
				go.SetActive(false);
				recrecycleWall.Add(go);
				xyWall.Remove(x.ToString() + "_" + y.ToString());
				idFood.Remove(id);
			}
		}

		void RWall(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyWall.ContainsKey(key))
			{
				GameObject go = xyWall[key];
				go.SetActive(false);
				recrecycleWall.Add(go);
				xyWall.Remove(key);
				Wall wall = go.GetComponent<Wall>();
				idWall.Remove(wall.id);
			}
		}

		void ROutWall(int x, int y)
		{
			bool alter = false;
			//Check if we current position is at board edge, if so choose a random outer wall prefab from our array of outer wall tiles.
			if (x == 0 || x == columns - 1 || y == 0 || y == rows - 1)
			{
				if (x == 0 && y == 0)
				{
					InsideROutWall(0, -1);
					InsideROutWall(-1, 0);
				}

				if (x == columns - 1 && y == rows - 1)
				{
					InsideROutWall(rows - 1, columns);
					InsideROutWall(rows, columns - 1);
				}

				if (x == 0 && y == rows - 1)
				{
					InsideROutWall(0, rows);
					InsideROutWall(-1, rows - 1);
				}

				if (x == columns - 1 && y == 0)
				{
					InsideROutWall(columns - 1, -1);
					InsideROutWall(columns, 0);
				}

				if (x == 0) { x = -1; alter = true; }
				if (y == 0) { y = -1; alter = true; }
				if (x == columns - 1) { x = columns; alter = true; }
				if (y == rows - 1) { y = rows; alter = true; }
			}

			if (alter)
				InsideROutWall(x, y);
		}

		void InsideROutWall(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyOutWall.ContainsKey(key))
			{
				GameObject go = xyOutWall[key];
				go.SetActive(false);
				recrecycleOutWall.Add(go);
				xyOutWall.Remove(key);
			}
		}

		void RFloor(int x, int y)
		{
			string key = x.ToString() + "_" + y.ToString();
			if (xyFloor.ContainsKey(key))
			{
				GameObject go = xyFloor[key];
				xyFloor.Remove(key);
				go.SetActive(false);
				recrecycleFloor.Add(go);
			}
		}

		void BoardSetup(int sX, int sY, int dX, int dY)
		{
			if(sX != dX)
			{
				int oY = dY - viewRows / 2;
				if (oY <= 0)
				{
					oY = 0;
				}
				int lY = dY + viewRows / 2;
				if (lY >= rows)
				{
					lY = rows - 1;
				}

				int rx;
				int x;
				if (sX < dX)
				{
					x = dX + viewColumns / 2;
					rx = sX - viewColumns / 2;
				} else
				{
					x = dX - viewColumns / 2;
					rx = sX + viewColumns / 2;
				}
				
				if(x >= 0 && x < columns)
				{
					//Loop along y axis, starting from -1 to place floor or outerwall tiles.
					for (int y = oY; y <= lY; y++)
					{

						AddFloor(x, y);
						//Check if we current position is at board edge, if so choose a random outer wall prefab from our array of outer wall tiles
						AddOutWall(x, y);
					}
				}

				if (rx >= 0 && rx < columns)
				{
					//Loop along y axis, starting from -1 to place floor or outerwall tiles.
					for (int y = oY; y <= lY; y++)
					{
						RFood(rx, y);
						REnemy(rx, y);
						RWall(rx, y);
						ROutWall(rx, y);
						RFloor(rx, y);
					}
				}
			}
			if (sY != dY)
			{
				int oX = dX - viewColumns / 2;
				if (oX <= 0)
				{
					oX = 0;
				}


				int lX = dX + viewColumns / 2;
				if (lX >= columns)
				{
					lX = columns - 1;
				}

				int ry;
				int y;
				if (sY < dY )
				{
					y = dY + viewRows / 2;
					ry = sY - viewRows / 2;
				} else
				{
					y = dY - viewRows / 2;
					ry = sY + viewRows / 2;
				}

				if (y >= 0 && y < rows)
				{
					//Loop along x axis, starting from -1 (to fill corner) with floor or outerwall edge tiles.
					for (int x = oX; x <= lX; x++)
					{
						AddFloor(x, y);
						//Check if we current position is at board edge, if so choose a random outer wall prefab from our array of outer wall tiles
						AddOutWall(x, y);
					}
				}

				if (ry >= 0 && ry < rows)
				{
					//Loop along x axis, starting from -1 (to fill corner) with floor or outerwall edge tiles.
					for (int x = oX; x <= lX; x++)
					{
						RFood(x, ry);
						REnemy(x, ry);
						RWall(x, ry);
						ROutWall(x, ry);
						RFloor(x, ry);
					}
				}
			}
		}

		void PlayMove(List<NpcEntity> npc, int sx, int sy, int x, int y)
		{
			BoardSetup(sx, sy, x, y);
			Entry(npc);
			player.PlayMove(sx, sy, x, y);
		}

		//Initializes the game for each level.
		void InitGame()
		{
			//While doingSetup is true the player can't move, prevent player from moving while title card is up.
			doingSetup = true;
			
			//Get a reference to our image LevelImage by finding it by name.
			levelImage = GameObject.Find("LevelImage");
			
			//Get a reference to our text LevelText's text component by finding it by name and calling GetComponent.
			levelText = GameObject.Find("LevelText").GetComponent<Text>();
			
			//Set the text of levelText to the string "Day" and append the current level number.
			levelText.text = "Day " + level;
			
			//Set levelImage to active blocking player's view of the game board during setup.
			levelImage.SetActive(true);
			
			//Call the HideLevelImage function with a delay in seconds of levelStartDelay.
			Invoke("HideLevelImage", levelStartDelay);

			BoardSetup();
		}
		
		
		//Hides black image used between levels
		void HideLevelImage()
		{
			//Disable the levelImage gameObject.
			levelImage.SetActive(false);
			
			//Set doingSetup to false allowing player to move again.
			doingSetup = false;
		}

		GameObject LayoutObjectAtRandom(GameObject[] tileArray, int x, int y)
		{
			//Choose a position for randomPosition by getting a random position from our list of available Vector3s stored in gridPosition
			Vector3 position = new Vector3(x, y, 0f);

			//Choose a random tile from tileArray and assign it to tileChoice
			GameObject tileChoice = tileArray[Random.Range(0, tileArray.Length)];

			//Instantiate tileChoice at the position returned by RandomPosition with no change in rotation
			return Instantiate(tileChoice, position, Quaternion.identity);
		}

		void Entry(List<NpcEntity> npc)
		{
			if (npc == null) return;

			IEnumerator rator = npc.GetEnumerator();
			while(rator.MoveNext())
			{
				//2 npc , 3 wall, 4 gift
				NpcEntity ne = (NpcEntity)rator.Current;
				if(ne.type == 2)
				{
					AddEnemy(ne.id.ToString(), ne.x, ne.y);
				} else if(ne.type == 3)
				{
					if (idWall.ContainsKey(ne.id.ToString())) return;

					GameObject r;
					if (recrecycleWall.Count > 0)
					{
						r = recrecycleWall.Last<GameObject>();
						recrecycleWall.RemoveAt(recrecycleWall.Count - 1);
						r.transform.position = new Vector3(ne.x, ne.y, 0f);
						r.SetActive(true);
					}
					else
					{
						r = LayoutObjectAtRandom(wallTiles, ne.x, ne.y);
					}

					Wall wall = r.GetComponent<Wall>();
					wall.id = ne.id.ToString();
					idWall.Add(ne.id.ToString(), r);
					xyWall.Add(ne.x.ToString() + "_" + ne.y.ToString(), r);
				}
				else if (ne.type == 4)
				{
					if (idFood.ContainsKey(ne.id.ToString())) return;

					GameObject r;
					if (recrecycleFood.Count > 0)
					{
						r = recrecycleFood.Last<GameObject>();
						recrecycleFood.RemoveAt(recrecycleFood.Count - 1);
						r.transform.position = new Vector3(ne.x, ne.y, 0f);
						r.SetActive(true);
					}
					else
					{
						r = LayoutObjectAtRandom(foodTiles, ne.x, ne.y);
					}

					Food food = r.GetComponent<Food>();
					food.id = ne.id.ToString();
					idFood.Add(ne.id.ToString(), r);
					xyFood.Add(ne.x.ToString() + "_" + ne.y.ToString(), r);
				}
			}
		}

		void PlayAttack(string id, int x, int y, int hp, string target, int del)
		{
			if (id == "1")
			{
				player.OnCantMove(hp);
				if (target != null)
				{				
					if(idFood.ContainsKey(target))
					{
						GameObject go = idFood[target];
						player.eatSound();
						if (del == 1) 
							RFood(target, x, y);
					} else
					{					
						if (idWall.ContainsKey(target))
						{
							GameObject go = idWall[target];
							Wall gWall = go.GetComponent<Wall>();
							gWall.DamageWall();
							if (del == 1)
								RWall(target, x, y);
						}
					}
				}
			}
		}

		void NpcAttack(string id, string target, int hp)
		{
			if(target == "1")
			{
				if (idEnemy.ContainsKey(id))
				{
					GameObject gameObject = idEnemy[id];
					Enemy enemy = gameObject.GetComponent<Enemy>();
					enemy.OnCantMove();
				}
				player.LoseFood(hp);
			}
		}

		void NpcMove(string id, int sx, int sy, int x, int y, int del)
		{
			if(del == 1)
			{
				REnemy(sx, sy);
			} else
			{
				if (idEnemy.ContainsKey(id))
				{
					GameObject enemyGO = idEnemy[id];
					Enemy enemy = enemyGO.GetComponent<Enemy>();
					xyEnemy.Remove(sx.ToString() + "_" + sy.ToString());

					if(!xyEnemy.ContainsKey(x.ToString() + "_" + y.ToString()))
						xyEnemy.Add(x.ToString() + "_" + y.ToString(), enemyGO);

					enemy.MoveEnemy(x, y);
				} else
				{
					AddEnemy(id, x, y);
				}
			}
		}

		//Update is called every frame.
		void Update()
		{
			//Check that playersTurn or enemiesMoving or doingSetup are not currently true.
			if (doingSetup)
				
				//If any of these are true, return and do not start MoveEnemies.
				return;

			long ms = m_Pelagia.MS();

			while(true)
			{
				string msg = m_Pelagia.GetRec();
				//Debug.Log(msg);
				if (msg == null || ((m_Pelagia.MS() - ms) >= 1000))
					return;
				else
				{
					RecMsg rm = JsonUtility.FromJson<RecMsg>(msg);
					if(rm.msg == "entry")
					{
						Entry(rm.npc);
					}
					else if(rm.msg == "play_move")
					{
						PlayMove(rm.npc, rm.sx, rm.sy, rm.x , rm.y);
					}
					else if(rm.msg == "play_attack")
					{
						PlayAttack(rm.id.ToString(), rm.x, rm.y, rm.hp, rm.target.ToString(), rm.del);
					}
					else if (rm.msg == "npc_attack")
					{
						NpcAttack(rm.id.ToString(), rm.target.ToString(), rm.hp);
					}
					else if (rm.msg == "npc_move")
					{
						NpcMove(rm.id.ToString(), rm.sx, rm.sy, rm.x, rm.y, rm.del);
					}
				}
			}
		}
			
		
		//GameOver is called when the player reaches 0 food points
		public void GameOver()
		{
			//Set levelText to display number of levels passed and game over message
			levelText.text = "After " + level + " days, you starved.";
			
			//Enable black background image gameObject.
			levelImage.SetActive(true);
			
			//Disable this GameManager.
			enabled = false;
		}
	}
}

