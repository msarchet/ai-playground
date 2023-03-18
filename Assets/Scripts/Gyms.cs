using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Gyms : MonoBehaviour
{
    public int NumberOfHumans = 2;
    public int NumberOfZombies = 1;
    public Vector2Int Size = new Vector2Int(16000, 9000);
    public Game Game;
    public Move[] Moves;
    private float timeSinceLastTick = 0;

    private Dictionary<int, GameObject> humans = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> zombies = new Dictionary<int , GameObject>();
    private GameObject ash;
    private bool setup;
    public GameObject Plane;

    public int Move = 0;
    // Update is called once per frame
    bool fixedHumans = false;


    void Start()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.parent = this.transform;
        plane.transform.localScale = new Vector3(16.0f, 0.0f, 9.0f);
        plane.transform.localPosition = new Vector3(80.0f, 0.0f, 45.0f);
        plane.GetComponent<MeshRenderer>().sharedMaterial.color = Color.white;   
        this.Plane = plane;

    }

    public void Setup(Game game, Move[] moves)
    {
        this.Game = game;
        this.Moves = moves;

        foreach (var zombie in this.Game.GameState.Zombies)
        {
            var zombieObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zombieObject.GetComponent<MeshRenderer>().material.color = Color.green;
            zombieObject.transform.localPosition = new Vector3(zombie.Value.Position.x / 100.0f, 1.0f, zombie.Value.Position.y / 100.0f);
            zombieObject.transform.parent = this.transform;
            zombieObject.transform.name = $"Zombie";
            this.zombies.Add(zombie.Key, zombieObject);
        }

        foreach (var human in this.Game.GameState.Humans)
        {
            var humanObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            humanObject.GetComponent<MeshRenderer>().material.color = Color.blue;
            humanObject.transform.localPosition = new Vector3(human.Value.Position.x / 100.0f, 1.0f, human.Value.Position.y / 100.0f);
            humanObject.transform.parent = this.transform;
            humanObject.transform.name = $"Human";
            this.humans.Add(human.Key, humanObject);
        }

        this.ash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.ash.GetComponent<MeshRenderer>().material.color = Color.red;   
        this.ash.transform.localPosition = new Vector3(this.Game.GameState.Ash.Position.x / 100.0f, 1.0f, this.Game.GameState.Ash.Position.y / 100.0f);
        this.ash.transform.parent = this.transform;
        this.ash.transform.name = "Ash";
        this.setup = true;
    }

    public void Reset()
    {
        this.setup = false;
        this.fixedHumans = false;
        this.Move = 0;

        foreach (var zombiePair in this.zombies)
        {
            var zombieObject = zombiePair.Value;
            GameObject.Destroy(zombieObject.gameObject);    
        }

        this.zombies.Clear();

        foreach (var humanPair in this.humans)
        {
            var humanObject = humanPair.Value;
            GameObject.Destroy(humanObject.gameObject);
        }

        this.humans.Clear();

        GameObject.Destroy(this.ash);
        this.ash = null;

        this.Plane.GetComponent<MeshRenderer>().material.color = Color.white;
        this.transform.localPosition = new Vector3(this.transform.localPosition.x, 0.0f, this.transform.localPosition.z);
    }

    void FixedUpdate()
    {

        if (!this.setup)
        {
            return;
        }

        if (!fixedHumans)
        {
            foreach(var humanPair in humans)
            {
                var humanObject = humanPair.Value;
                var human = this.Game.GameState.Humans[humanPair.Key];
                humanObject.transform.localPosition = new Vector3(human.Position.x / 100.0f, 1.0f, human.Position.y / 100.0f);
                humanObject.transform.parent = this.transform;
            }
        }
        if (Move >= Moves.Length)
        {
            if (this.Game.GameState.Phase == GamePhase.Playing)
            {
                this.Game.GameState.Phase = GamePhase.Lost;
            }

            // maybe emit an event or something
            return; 
        }

        timeSinceLastTick += Time.deltaTime;


        if (this.Game.GameState.Phase == GamePhase.Playing && timeSinceLastTick >= 0.05)
        {
            var nextMoveX = this.Game.GameState.Ash.Position.x + Mathf.Cos(Moves[Move].Angle) * Moves[Move].Magintude;
            var nextMoveY = this.Game.GameState.Ash.Position.y + Mathf.Sin(Moves[Move].Angle) * Moves[Move].Magintude;
            nextMoveX = Mathf.Min(Mathf.Max(nextMoveX, 0), this.Size.x);
            nextMoveY = Mathf.Min(Mathf.Max(nextMoveY, 0), this.Size.y);

            var nextMove = new Vector2Int((int)nextMoveX, (int)nextMoveY);
            this.Game.TickNextState(nextMove);

            foreach (var zombie in this.Game.GameState.Zombies)
            {
                if (this.zombies.TryGetValue(zombie.Key, out GameObject zombieObject))
                {
                    zombieObject.transform.localPosition = new Vector3(zombie.Value.Position.x / 100.0f, 1.0f, zombie.Value.Position.y / 100.0f);
                }
            }

            this.ash.transform.localPosition = new Vector3(this.Game.GameState.Ash.Position.x / 100.0f, 1.0f, this.Game.GameState.Ash.Position.y / 100.0f);

            foreach (var zombie in this.Game.GameState.KilledZombie)
            {
                if (this.zombies.TryGetValue(zombie, out GameObject zombieObject))
                {
                    GameObject.Destroy(zombieObject, 0.02f);
                    this.zombies.Remove(zombie);
                }
            }

            foreach (var human in this.Game.GameState.KilledHumans)
            {
                if (this.humans.TryGetValue(human, out GameObject humanObject))
                {
                    GameObject.Destroy(humanObject, 0.02f);
                    this.humans.Remove(human);
                }
            }

            this.Game.PostTickMoveAndCleanup();

            timeSinceLastTick -= 0.1f;
        }
    }
}
