using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Gyms : MonoBehaviour
{
    public Vector2Int Size = new Vector2Int(16000, 9000);
    public Game Game;
    public Move[] Moves;
    public GameObject Plane;
    public int Move = 0;
    public int SlowDown = 1;
    private Dictionary<int, GameObject> humans;
    private Dictionary<int, GameObject> zombies;
    private GameObject ash;
    private bool setup;


    // Update is called once per frame
    private void Awake()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.transform.parent = this.transform;
        plane.transform.localScale = new Vector3(160.0f, 1.0f, 90.0f);
        plane.transform.localPosition = new Vector3(80.0f, 0.0f, 45.0f);
        plane.GetComponent<MeshRenderer>().sharedMaterial.color = Color.white;
        this.Plane = plane;

        this.zombies = new Dictionary<int, GameObject>();   
        this.humans = new Dictionary<int, GameObject>();
        for (int i = 0; i < 100; i++ )
        {
            var zombieObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zombieObject.GetComponent<MeshRenderer>().material.color = Color.green;
            zombieObject.transform.parent = this.transform;
            zombieObject.transform.name = $"Zombie";
            zombieObject.SetActive(false);
            this.zombies.Add(i, zombieObject);
        }

        for (int i = 0; i < 100; i++ )
        {
            var humanObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            humanObject.GetComponent<MeshRenderer>().material.color = Color.blue;
            humanObject.transform.parent = this.transform;
            humanObject.transform.name = $"Human";
            humanObject.SetActive(false);
            this.humans.Add(i, humanObject);
        }

        this.ash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.ash.GetComponent<MeshRenderer>().material.color = Color.red;   
        this.ash.transform.parent = this.transform;
        this.ash.transform.name = "Ash";
        this.ash.SetActive(false);
    }


    public bool Render { get; private set; } = true;

    public void Setup(Game game, Move[] moves)
    {
        if (this.setup)
        {
            return;
        }

        this.Game = game;
        this.Moves = moves;

        foreach (var zombie in this.Game.GameState.Zombies)
        {
            var zombieObject = this.zombies[zombie.Key];
            zombieObject.SetActive(this.Render);
            zombieObject.transform.localPosition = new Vector3(zombie.Value.x / 100.0f, 1.0f, zombie.Value.y / 100.0f);
        }

        foreach (var human in this.Game.GameState.Humans)
        {
            var humanObject = this.humans[human.Key];
            humanObject.SetActive(this.Render);
            humanObject.transform.localPosition = new Vector3(human.Value.x / 100.0f, 1.0f, human.Value.y / 100.0f);
        }

        ash.SetActive(this.Render);
        this.setup = true;
    }

    public void Reset()
    {
        this.setup = false;
        this.Move = 0;
        foreach(var zombie in this.zombies)
        {
            zombie.Value.SetActive(false);
        }

        foreach (var human in this.humans)
        {
            human.Value.SetActive(false);
        }

        this.ash.SetActive(false);

        this.Plane.GetComponent<MeshRenderer>().material.color = Color.white;
        this.transform.localPosition = new Vector3(this.transform.localPosition.x, 0.0f, this.transform.localPosition.z);
    }

    private int slowdownCount = 0;
    public void ToggleRender()
    {
        var wasRendering = this.Render;
        this.Render = !this.Render;

        foreach (var zombie in this.zombies)
        {
            if (wasRendering)
            {
                zombie.Value.SetActive(this.Render);
            }
            else if (this.Game.GameState.Zombies.TryGetValue(zombie.Key, out var zombiePosition))
            {
                zombie.Value.SetActive(this.Render);
                zombie.Value.transform.localPosition = new Vector3(zombiePosition.x / 100.0f, 1.0f, zombiePosition.y / 100.0f);
            }
        }

        foreach (var human in this.humans)
        {
            if (wasRendering)
            {
                human.Value.SetActive(this.Render);
            }
            else if (this.Game.GameState.Humans.TryGetValue(human.Key, out var humanPosition))
            {
                human.Value.SetActive(this.Render);
                human.Value.transform.localPosition = new Vector3(humanPosition.x / 100.0f, 1.0f, humanPosition.y / 100.0f);
            }
        }

        if (wasRendering)
        {
            this.ash.SetActive(this.Render);
        } else
        {
            this.ash.SetActive(this.Render);
            this.ash.transform.localPosition = new Vector3(this.Game.GameState.Ash.x / 100.0f, 1.0f, this.Game.GameState.Ash.y / 100.0f);
        }

        this.Plane.SetActive(this.Render);
    }

    void FixedUpdate()
    {
        if (!this.setup)
        {
            return;
        }

        if (Move >= Moves.Length)
        {
            if (this.Game.GameState.Phase == GamePhase.Playing)
            {
                this.Game.GameState.Phase = GamePhase.Incomplete;
            }

            // maybe emit an event or something
            return; 
        }

        slowdownCount++;
        if (slowdownCount < this.SlowDown)
        {
            return;
        }

        slowdownCount = 0;
        var nextMoveX = this.Game.GameState.Ash.x + Mathf.Cos(Moves[Move].Angle) * Moves[Move].Magintude;
        var nextMoveY = this.Game.GameState.Ash.y + Mathf.Sin(Moves[Move].Angle) * Moves[Move].Magintude;
        nextMoveX = Mathf.Min(Mathf.Max(nextMoveX, 0), this.Size.x);
        nextMoveY = Mathf.Min(Mathf.Max(nextMoveY, 0), this.Size.y);

        var nextMove = new Vector2Int((int)nextMoveX, (int)nextMoveY);
        this.Game.TickNextState(nextMove);

        if (this.Render)
        {
            foreach (var zombie in this.Game.GameState.Zombies)
            {
                if (this.zombies.TryGetValue(zombie.Key, out GameObject zombieObject))
                {
                    zombieObject.transform.localPosition = new Vector3(zombie.Value.x / 100.0f, 1.0f, zombie.Value.y / 100.0f);
                }
            }

            this.ash.transform.localPosition = new Vector3(this.Game.GameState.Ash.x / 100.0f, 1.0f, this.Game.GameState.Ash.y / 100.0f);

            foreach (var zombie in this.Game.GameState.KilledZombies)
            {
                if (this.zombies.TryGetValue(zombie, out GameObject zombieObject))
                {
                    zombieObject.SetActive(false);
                }
            }

            foreach (var human in this.Game.GameState.KilledHumans)
            {
                if (this.humans.TryGetValue(human, out GameObject humanObject))
                {
                    humanObject.SetActive(false);
                }
            }

        }

        this.Game.PostTickMoveAndCleanup();
        Move++;
    }

}
