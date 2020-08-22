using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cube : MonoBehaviour
{
    public float MaxX = 100;
    public float MaxZ = 100;

    private Pelagia m_Pelagia;
    private Dictionary<string, GameObject> name_obj;

    public int columns = 100;                                        //Number of columns in our game board.
    public int rows = 100;                                           //Number of rows in our game board.
    // Start is called before the first frame update

    [System.Serializable]
    public class InitMsg
    {
        public string cmd;
		public string name;
		public int global_x;
        public int global_y;
		public float x;
		public float z;
	}

	[System.Serializable]
	public class RecMsg
	{
		public string cmd;
		public string name;
		public float d_x;
		public float d_z;
	}

	void Awake()
    {
        m_Pelagia = new Pelagia();
        m_Pelagia.Init("./3drpg2.json");

        InitMsg im = new InitMsg();
        im.cmd = "init";
        im.global_x = columns;
        im.global_y = rows;
        string json = JsonUtility.ToJson(im);
        m_Pelagia.Call("manager", json);

		name_obj = new Dictionary<string, GameObject>();
	}
    void Start()
    {
   
      
    }

	void NpcMove(string id, float x, float z)
	{

		if (name_obj.ContainsKey(id))
		{
			GameObject enemyGO = name_obj[id];
			Enemy enemy = enemyGO.GetComponent<Enemy>();
			enemy.MoveEnemy(x, z);
		}
	}

	//Update is called every frame.
	void Update()
	{
		long ms = m_Pelagia.MS();

		while (true)
		{
			string msg = m_Pelagia.GetRec();
			//Debug.Log(msg);
			if (msg == null || ((m_Pelagia.MS() - ms) >= 1000))
				return;
			else
			{
				RecMsg rm = JsonUtility.FromJson<RecMsg>(msg);

				if (rm.cmd == "move")
				{
					NpcMove(rm.name,rm.d_x, rm.d_z);
					print("cmd move" + rm.name + "  " + rm.d_x + "  " + rm.d_z);
				}else if (rm.cmd == "init")
				{
					uint count = 0;
					GameObject gameObject1 = GameObject.Find("Cube");
					for (int x = 0; x < MaxX; x++)
					{
						for (int z = 0; z < MaxZ; z++)
						{
							GameObject newobj = Instantiate(gameObject1, new Vector3(x, 0, z), Quaternion.identity);

							string key = "role" + (count++);
							name_obj[key] = newobj;

							InitMsg im = new InitMsg();
							im.cmd = "create";
							im.name = key;
							im.x = x;
							im.z = z;
							string json = JsonUtility.ToJson(im);
							m_Pelagia.Call("manager", json);
						}
					}
				}
			}
		}
	}
}
