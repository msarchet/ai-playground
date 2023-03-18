using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor
{
    public int Id { get; }
    public Vector2Int Position { get; set; }

    public Actor(int id, Vector2Int position)
    {
        this.Id = id;
        this.Position = position;   
    }
}
public static class Helpers
{
    private static Dictionary<int, double> FibMap = new Dictionary<int, double>();
    public static Vector3 ConvertPositionToWorldPosition(Vector2Int position, Vector2 worldSize) => new Vector3(position.x / worldSize.x, 0.5f, position.y / worldSize.y);

    public static double MagnitudeSquared(Vector2Int position1, Vector2Int position2) => System.Math.Pow(position1.x - position2.x, 2) + System.Math.Pow(position1.y - position2.y, 2);

    public static double GetFib(int n)
    {
        if (FibMap.ContainsKey(n))
        {
            return FibMap[n];
        }

        if (n > 1)
        {
            double n1 = 0;
            double n2 = 0;

            if (FibMap.ContainsKey(n - 2))
            {
                n2 = FibMap[n - 2];
            }
            else
            {
                n2 = GetFib(n - 2);
                FibMap.Add(n - 2, n2);
            }

            if (FibMap.ContainsKey(n - 1))
            {
                n1 = FibMap[n - 1];
            }
            else
            {
                n1 = GetFib(n - 1);
                FibMap.Add(n - 1, n1);
            }

            return n1 + n2;
        }

        if (n == 1)
        {
            return 1;
        }

        return 0;
    }
}

public enum GamePhase
{
    Playing,
    Won,
    Lost,
    Incomplete
}

public class GameState
{
    public Dictionary<int, Actor> Humans { get; } = new Dictionary<int, Actor>();
    public Dictionary<int, Actor> Zombies { get; } = new Dictionary<int, Actor>();
    public Dictionary<int, Vector2Int> ZombieNextMoves { get; } = new Dictionary<int, Vector2Int>();
    public Dictionary<int, int> HumanClosestToZombie { get; } = new Dictionary<int, int>();
    public Actor Ash { get; } = new Actor(0, new Vector2Int(0, 0));

    public HashSet<int> KilledHumans { get;  } = new HashSet<int>();
    public HashSet<int> KilledZombie { get;  } = new HashSet<int>();

    public double Score { get; set; }

    public GamePhase Phase { get; set; } = GamePhase.Playing;
}

public class Game
{
    private Vector2Int size;

    private int AshKillRadius = 2000;
    private int AshKillRadiusSquared;
    private int seed;
    public Game(int seed)
    {
        this.seed = seed;
        this.AshKillRadiusSquared = this.AshKillRadius * this.AshKillRadius;
    }

    public GameState GameState { get; } = new GameState();
    public void SetupWorld(Vector2Int size)
    {
        this.size = size;
    }

    public void InitializeGame(int numberOfHumans, int numberOfZombies)
    {
        Random.InitState(this.seed);

        for (int i = 0; i < numberOfHumans; i++)
        {
            this.GameState.Humans.Add(i, new Actor(i, new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y)))); 
        }    

        for (int i = 0; i < numberOfZombies; i++)
        {
            this.GameState.Zombies.Add(i, new Actor(i, new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y)))); 
        }

        this.GameState.Ash.Position = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
    }

    public void TickNextState(Vector2Int ashPosition)
    {
        if (this.GameState.Phase != GamePhase.Playing)
        {
            return;
        }
        foreach (var zombiePair in this.GameState.Zombies)
        {
            var zombie = zombiePair.Value;

            var closestDistance = double.MaxValue;
            var closestHuman = 0;

            // can we search outwards from the point of the zombie
            // to find the closest human instead
            if (this.GameState.HumanClosestToZombie.TryGetValue(zombiePair.Key, out int humanId))
            {
                if (this.GameState.Humans.ContainsKey(humanId))
                {
                    closestHuman = humanId;

                    closestDistance = Helpers.MagnitudeSquared(zombie.Position, this.GameState.Humans[humanId].Position);
                } else
                {
                    this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);
                }
            }

            if (closestDistance == double.MaxValue)
            {
                // TODO: Find a better lookup than the current
                foreach (var humanPair in this.GameState.Humans)
                {
                    var human = humanPair.Value;
                    var distanceToHuman = Helpers.MagnitudeSquared(zombie.Position, human.Position);

                    if (distanceToHuman <= closestDistance)
                    {
                        closestDistance = distanceToHuman;
                        closestHuman = humanPair.Key;
                    }
                }

                this.GameState.HumanClosestToZombie[zombiePair.Key] = closestHuman;
            }

            var targetPosition = this.GameState.Humans[closestHuman].Position;

            var distanceToAsh = Helpers.MagnitudeSquared(this.GameState.Ash.Position, zombie.Position);

            bool isTargetAsh = false;

            if (distanceToAsh <= closestDistance)
            {
                targetPosition = ashPosition;
                isTargetAsh = true;
                this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);
            }

            var newPosition = Vector2.MoveTowards(zombie.Position, targetPosition, 400.0f);
            var nextPosition = new Vector2Int((int)newPosition.x, (int)newPosition.y);

            var newDistanceToAsh = Helpers.MagnitudeSquared(nextPosition, ashPosition);
            this.GameState.ZombieNextMoves.Add(zombiePair.Key, nextPosition);

            if (newDistanceToAsh <= AshKillRadiusSquared)
            {
                this.GameState.KilledZombie.Add(zombiePair.Key);
                continue;
            }

            if (!isTargetAsh && closestDistance == 0)
            {
                this.GameState.KilledHumans.Add(closestHuman);
                this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);
            }
        }

        // set ashes new position
        this.GameState.Ash.Position = ashPosition;

        // move the zombies
        foreach (var movePair in this.GameState.ZombieNextMoves)
        {
            if (!this.GameState.KilledZombie.Contains(movePair.Key))
            {
                this.GameState.Zombies[movePair.Key].Position = movePair.Value;
            }
        }

        foreach (var human in this.GameState.KilledHumans)
        {
            this.GameState.Humans.Remove(human);
        }


        // calculate the score
        var humansRemaining = this.GameState.Humans.Count;

        if (humansRemaining == 0)
        {
            this.GameState.Phase = GamePhase.Lost;
            return;
        } 

        int killedZombieCount = 0;
        int humanCountSquare = humansRemaining * humansRemaining;

        foreach (var zombie in this.GameState.KilledZombie)
        {
            this.GameState.Zombies.Remove(zombie);
            killedZombieCount++;
            this.GameState.Score += 10 * humanCountSquare * Helpers.GetFib(killedZombieCount);
        }

        if (this.GameState.Zombies.Count == 0)
        {
            this.GameState.Phase = GamePhase.Won;
            return;
        }

    }

    public void PostTickMoveAndCleanup()
    {
        this.GameState.ZombieNextMoves.Clear();
        this.GameState.KilledZombie.Clear();
        this.GameState.KilledHumans.Clear();
    }
}
public struct Move
{
    public float Angle;
    public float Magintude;

    public Move(float angle, float magnitude)
    {
        this.Angle = angle;
        this.Magintude = magnitude;
    }
}

public static class GameManager
{
    public static Gyms CreateGameWithAgent(Move[] moves, Game game)
    {
        var newGameRoot = new Gyms();

        newGameRoot.Game = game;
        newGameRoot.Moves = moves;

        return newGameRoot;
    }
}

[RequireComponent(typeof(MeshRenderer))]
public class GameRoot : MonoBehaviour
{
    public int NumberOfGyms;
    public int NumberOfHumans;
    public int NumberOfZombies;
    public int RowWidth;
    public List<GameObject> gymContainers = new List<GameObject>();
    public List<Gyms> gyms = new List<Gyms>();
    private int seed;

    private List<Move[]> currentMoveGeneration;
    void Start()
    {
        this.seed = Random.Range(0, 2309485);
        for (int i = 0; i < NumberOfGyms; i++)
        {
            GameObject gym = new GameObject();
            gym.transform.name = $"Gym {i}";

            gym.transform.position = gym.transform.position + new Vector3((i % RowWidth) * 180.0f, 0, (i / RowWidth) * 100.0f);
            var newGame = gym.AddComponent<Gyms>();
            gyms.Add(newGame);
            gymContainers.Add(gym);
        }

        this.currentMoveGeneration = GenerateMoveSets();
        SetupGames(this.currentMoveGeneration);
    }

    private void Awake()
    {
    }

    private void SetupGames(List<Move[]> moveSets)
    {
        for (int i = 0; i < gyms.Count; i++)
        {
            var game = new Game(seed);
            game.SetupWorld(new Vector2Int(16000, 9000));
            game.InitializeGame(this.NumberOfHumans, this.NumberOfZombies);

            gyms[i].Setup(game, moveSets[i]);
        }

    }

    private void ResetGyms()
    {
        foreach (var gym in gyms)
        {
            gym.Reset();
        }
    }

    private void EvolveGyms(List<Move[]> moveSets)
    {
        this.ResetGyms();
        this.currentMoveGeneration = EvolveMoveSets(moveSets);

        this.SetupGames(this.currentMoveGeneration);
    }

    private List<Move[]> GenerateMoveSets()
    {
        List<Move[]> moveSets = new List<Move[]>();
        for (int i = 0; i < NumberOfGyms; i++)
        {
            var moves = new Move[25];
            for (int j = 0; j < moves.Length; j++)
            {
                moves[j] = new Move(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 400.0f));
            }

            moveSets.Add(moves);
        }

        return moveSets;
    }

    private List<Move[]> EvolveMoveSets(List<Move[]> moves)
    {
        return moves;
    }

    float timeSinceLastCheck = 0.0f;
    bool scored = false;
    private void Update()
    {
        if (scored)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                this.scored = false;
                this.EvolveGyms(this.currentMoveGeneration);
            }
            return;
        }

        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck > 5.0f)
        {
            Debug.Log("Scoring");
            timeSinceLastCheck -= 5.0f;

            List<GameScoreResult> results = new List<GameScoreResult>();
            double maxFitness = 0;
            double minFitness = double.MaxValue;
            foreach (var gameContainer in gyms)
            {
                if (gameContainer.Game.GameState.Phase == GamePhase.Playing)
                {
                    return;
                }
            }

            int i = 0;
            foreach (var gameContainer in gyms)
            {
                var result = new GameScoreResult();
                result.Score = gameContainer.Game.GameState.Score;
                result.Won = gameContainer.Game.GameState.Phase == GamePhase.Won;
                result.Lost = gameContainer.Game.GameState.Phase == GamePhase.Lost;
                result.Moves = gameContainer.Moves;
                result.Gym = i;
                i++;
                double fitness = result.Score;


                if (result.Won)
                {
                    fitness *= 1.2;
                }

                if (result.Lost)
                {
                    fitness *= 0.5;
                }

                result.Fitness = fitness;

                if (fitness > maxFitness)
                {
                    maxFitness = fitness;
                }

                if (fitness < minFitness)
                {
                    minFitness = fitness;
                }

                results.Add(result);
            }

            scored = true;

            // make new genes here
            for (i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result.Won)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.blue;
                } else if (result.Lost)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.red;

                }
                if (result.Fitness == maxFitness)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.green;
                }

                gyms[i].transform.position += new Vector3(0.0f, (float)(result.Fitness - minFitness) / (float)(maxFitness - minFitness) * 10.0f, 0.0f);

                Debug.Log($"Gym {result.Gym} fitness : {result.Fitness} score: {result.Score} won : {result.Won} lost: {result.Lost}");
            }

            // reload the sim from the start here
        }

    }
}

public struct GameScoreResult
{
    public double Score;
    public bool Won;
    public bool Lost;
    public double Fitness;
    public int Gym;
    public Move[] Moves;
}
