using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public static GameState CachedState { get; set; }
    public Dictionary<int, Vector2Int> Humans { get; } = new Dictionary<int, Vector2Int>();
    public Dictionary<int, Vector2Int> Zombies { get; } = new Dictionary<int, Vector2Int>();
    public Dictionary<int, Vector2Int> ZombieNextMoves { get; set; } = new Dictionary<int, Vector2Int>();
    public Dictionary<int, int> HumanClosestToZombie { get; set; } = new Dictionary<int, int>();
    public Vector2Int Ash { get; set; } = new Vector2Int(0, 0);

    public HashSet<int> KilledHumans { get; set;  } = new HashSet<int>();
    public HashSet<int> KilledZombie { get; set;  } = new HashSet<int>();

    public double Score { get; set; }

    public GamePhase Phase { get; set; } = GamePhase.Playing;

    public GameState() {}

    public GameState(GameState gameState)
    {
        this.CopyFrom(gameState);
    }

    public void CopyFrom(GameState gameState)
    {
        this.Humans.Clear();
        foreach (var pair in gameState.Humans)
        {
            this.Humans.Add(pair.Key, pair.Value);
        }

        this.Zombies.Clear();
        foreach (var pair in gameState.Zombies)
        {
            this.Zombies.Add(pair.Key, pair.Value);
        }

        this.ZombieNextMoves.Clear();
        foreach (var pair in gameState.ZombieNextMoves)
        {
            this.ZombieNextMoves.Add(pair.Key, pair.Value);
        }

        this.HumanClosestToZombie.Clear();
        foreach (var pair in gameState.HumanClosestToZombie)
        {
            this.HumanClosestToZombie.Add(pair.Key, pair.Value);    
        }

        this.Ash = gameState.Ash;

        this.KilledHumans = new HashSet<int>(gameState.KilledHumans.ToList());
        this.KilledZombie = new HashSet<int>(gameState.KilledZombie.ToList());

        this.Score = gameState.Score;
        this.Phase = gameState.Phase;

    }
}

public class Game
{
    private Vector2Int size;

    private int AshKillRadius = 2000;
    private int AshKillRadiusSquared;
    private int seed;

    public GameState GameState { get; } = new GameState();

    public Game(int seed)
    {
        this.seed = seed;
        this.AshKillRadiusSquared = this.AshKillRadius * this.AshKillRadius;
    }

    public Game(Game game) : this(game.seed)
    {
        this.GameState = new GameState(game.GameState);
        this.size = game.size;
    }

    public void CopyFrom(Game game)
    {
        this.GameState.CopyFrom(game.GameState);
        this.size = game.size;
    }

    public void SetupWorld(Vector2Int size)
    {
        this.size = size;
    }

    public void InitializeGame(int numberOfHumans, int numberOfZombies)
    {
        Random.InitState(this.seed);

        for (int i = 0; i < numberOfHumans; i++)
        {
            this.GameState.Humans.Add(i, new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y))); 
        }    

        for (int i = 0; i < numberOfZombies; i++)
        {
            var zombie = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
            this.GameState.Zombies.Add(i, zombie); 

            var closestHuman = 0;
            // TODO: Find a better lookup than the current
            foreach (var humanPair in this.GameState.Humans)
            {
                var human = humanPair.Value;
                var closestDistance = double.MaxValue;
                var distanceToHuman = Helpers.MagnitudeSquared(zombie, human);

                if (distanceToHuman <= closestDistance)
                {
                    closestDistance = distanceToHuman;
                    closestHuman = humanPair.Key;
                }
            }

            this.GameState.HumanClosestToZombie[i] = closestHuman;

        }

        this.GameState.Ash = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
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

                    closestDistance = Helpers.MagnitudeSquared(zombie, this.GameState.Humans[humanId]);
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
                    var distanceToHuman = Helpers.MagnitudeSquared(zombie, human);

                    if (distanceToHuman <= closestDistance)
                    {
                        closestDistance = distanceToHuman;
                        closestHuman = humanPair.Key;
                    }
                }

                this.GameState.HumanClosestToZombie[zombiePair.Key] = closestHuman;
            }

            var targetPosition = this.GameState.Humans[closestHuman];

            var distanceToAsh = Helpers.MagnitudeSquared(this.GameState.Ash, zombie);

            bool isTargetAsh = false;

            if (distanceToAsh <= closestDistance)
            {
                targetPosition = ashPosition;
                isTargetAsh = true;
                this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);
            }

            var newPosition = Vector2.MoveTowards(zombie, targetPosition, 400.0f);
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
        this.GameState.Ash = ashPosition;

        // move the zombies
        foreach (var movePair in this.GameState.ZombieNextMoves)
        {
            if (!this.GameState.KilledZombie.Contains(movePair.Key))
            {
                this.GameState.Zombies[movePair.Key] = movePair.Value;
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

[RequireComponent(typeof(MeshRenderer))]
public class GameRoot : MonoBehaviour
{
    public int NumberOfGyms;
    public int NumberOfHumans;
    public int NumberOfZombies;
    public int NumberOfMoves;
    public int RowWidth;
    public int Seed;
    public int NumberOfGenerationsToRun = 100;

    public List<GameObject> gymContainers = new List<GameObject>();
    public List<Gyms> gyms = new List<Gyms>();
    private int seed;

    private List<Move[]> currentMoveGeneration;
    private Game gameToTest;
    private List<GameScoreResult> LastResults;
    private int Generation = 1;
    private int IterationGenerations = 0;
    bool scored = false;
    bool scoring = false;

    void Start()
    {
    }

    private void Awake()
    {
        Physics.autoSimulation = false;
        this.seed = Seed;
        this.gameToTest = new Game(seed);
        this.gameToTest.SetupWorld(new Vector2Int(16000, 9000));
        this.gameToTest.InitializeGame(this.NumberOfHumans, this.NumberOfZombies);
        this.currentMoveGeneration = GenerateMoveSets();
        GameState.CachedState = this.gameToTest.GameState;

        SetupGames(this.gameToTest, this.currentMoveGeneration);

        for (int i = 0; i < NumberOfGyms; i++)
        {

            GameObject gymObject = new GameObject();
            gymObject.transform.name = $"Gym {i}";
            gymObject.transform.position = gymObject.transform.position + new Vector3((i % RowWidth) * 180.0f, 0, (i / RowWidth) * 100.0f);

            Gyms gym = gymObject.AddComponent<Gyms>();
            gym.Setup(new Game(this.gameToTest), this.currentMoveGeneration[i]);
            gyms.Add(gym);

            gymContainers.Add(gymObject);
        }

        // make the game once and then copy it anytime we need to use it later
    }

    private void SetupGames(Game game, List<Move[]> moveSets)
    {
        for (int i = 0; i < gyms.Count; i++)
        {
            //newgame.SetupWorld(new Vector2Int(16000, 9000));
            //newgame.InitializeGame(this.NumberOfHumans, this.NumberOfZombies);
            gyms[i].Game.CopyFrom(game);
            gyms[i].Setup(gyms[i].Game, moveSets[i]);
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

        this.SetupGames(this.gameToTest, this.currentMoveGeneration);
    }

    private Move GenerateMove() => new Move(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 1000.0f));
    private List<Move[]> GenerateMoveSets()
    {
        List<Move[]> moveSets = new List<Move[]>();
        for (int i = 0; i < NumberOfGyms; i++)
        {
            var moves = new Move[NumberOfMoves];
            for (int j = 0; j < moves.Length; j++)
            {
                moves[j] = GenerateMove(); 
            }

            moveSets.Add(moves);
        }

        return moveSets;
    }


    private int GetParent()
    {
        var choice = Random.value;

        if (choice <= 0.7)
        {
            return Random.Range(0, this.NumberOfMoves / 3);
        } else
        {
            return Random.Range(this.NumberOfMoves / 3, NumberOfGyms);
        }
    }
    private List<Move[]> EvolveMoveSets(List<Move[]> moves)
    {
        Generation++;
        Random.InitState((int)Random.value + Generation);
        this.LastResults.Sort((left, right) => right.Score.CompareTo(left.Score));
        var newMoves = new List<Move[]>();

        int i = 0;

        for (i = 0; i < 10; i++)
        {
            newMoves.Add(this.LastResults[i].Moves);
        }

        for (; newMoves.Count <= NumberOfGyms; i += 2)
        {
            var parent1 = this.LastResults[GetParent()].Moves;
            var parent2 = this.LastResults[GetParent()].Moves;


            var choice = Random.Range(0, 2);

            var child1 = new Move[moves[0].Length];
            var child2 = new Move[moves[0].Length];

            for (int j = 0; j < moves[0].Length; j++)
            {
                if (j < moves.Count / 3)
                {
                child1[j] = parent1[j];
                child2[j] = parent2[j];


                }

                child1[j] = parent2[j];
                child2[j] = parent1[j];
            }

            if (Random.value <= 0.2)
            {
                var leftMutate = Random.Range(0, moves[0].Length / 3);
                var rightMutate = Random.Range(moves[0].Length / 3 + 1, moves[0].Length);

                child1[leftMutate] = GenerateMove();
                child1[rightMutate] = GenerateMove();

                child2[leftMutate] = GenerateMove();
                child2[rightMutate] = GenerateMove();
            }

            newMoves.Add(child1);
            newMoves.Add(child2);

        }

        return newMoves;
    }

    private void Update()
    {
        if (scored)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                this.scored = false;
                this.EvolveGyms(this.currentMoveGeneration);
                this.IterationGenerations = 0;
            }
            return;
        }

        foreach (var gameContainer in gyms)
        {
            if (gameContainer.Game == null)
            {
                return;
            }

            if (gameContainer.Game.GameState.Phase == GamePhase.Playing)
            {
                return;
            }
        }

        List<GameScoreResult> results = new List<GameScoreResult>(NumberOfGyms);
        double maxFitness = 0;
        double minFitness = double.MaxValue;

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

            fitness *= .5 + (gameContainer.Game.GameState.Humans.Count / NumberOfHumans) * 3;
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

        Debug.Log($"Max Fitness: {maxFitness}");
        // make new genes here
        for (i = 0; i < results.Count; i++)
        {
            var result = results[i];
            if (result.Won)
            {
                gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.blue;
            }
            else if (result.Lost)
            {
                gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.red;

            }
            if (result.Fitness == maxFitness)
            {
                gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.green;
            }

            gyms[i].transform.position += new Vector3(0.0f, (float)(result.Fitness - minFitness) / (float)(maxFitness - minFitness) * 10.0f, 0.0f);

            //Debug.Log($"Gym {result.Gym} fitness : {result.Fitness} score: {result.Score} won : {result.Won} lost: {result.Lost}");
        }

        this.LastResults = results;

        if (IterationGenerations < NumberOfGenerationsToRun)
        {
            this.scored = false;
            this.IterationGenerations++;
            this.EvolveGyms(this.currentMoveGeneration);
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 25), $"Generation {this.Generation}");
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
