using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
    public float AngleVariation = 45.0f;
    public float MagintudeVariation = 200.0f;
    public float GeneMutateChance = 0.01f;
    public int ElitismCount = 4;
    public int KillCount = 20;

    [Range(1, 100)]
    public int SlowDownCount = 1;

    public List<GameObject> gymContainers = new List<GameObject>();
    public List<Gyms> gyms = new List<Gyms>();
    private int seed;

    private Move[][] currentMoveGeneration;
    private Move[][] nextMoveGeneration;
    private Move[][] swapMoveGeneration;
    private Game gameToTest;
    private GameFitness[] LastResults;
    private int Generation = 1;
    private int IterationGenerations = 0;
    bool scored = false;
    bool scoring = false;
    List<GameFitness> cumaltiveResults = new List<GameFitness>();

    void Start()
    {
    }

    private Move[][] MakeMoveGenerations()
    {
        Move[][] moveGeneration = new Move[NumberOfGyms][];
        for (int i = 0; i < this.NumberOfGyms; i++)
        {
            moveGeneration[i] = new Move[NumberOfMoves];
        }

        return moveGeneration;
    }
    private void Awake()
    {
        Physics.autoSimulation = false;

        this.LastResults = new GameFitness[NumberOfGyms];
        this.currentMoveGeneration = this.MakeMoveGenerations();
        this.nextMoveGeneration = this.MakeMoveGenerations();
        this.swapMoveGeneration = this.MakeMoveGenerations();

        this.seed = Seed;
        this.gameToTest = new Game(seed, -1);
        this.gameToTest.SetupWorld(new Vector2Int(16000, 9000), this.NumberOfHumans);

        if (System.IO.File.Exists("game_inputs.dat"))
        {
            var inputs = System.IO.File.ReadAllLines("game_inputs.dat");
            var ashParts = inputs[0].Split(" ");
            var ash = new Vector2Int(int.Parse(ashParts[0]), int.Parse(ashParts[1]));
            this.gameToTest.GameState.Ash = ash;

            NumberOfHumans = int.Parse(inputs[1]);

            int i = 2;
            for (i = 2; i < 2 + NumberOfHumans; i++)
            {
                var humanParts = inputs[i].Split(" ");
                var human = new Vector2Int(int.Parse(humanParts[1]), int.Parse(humanParts[2]));
                this.gameToTest.GameState.Humans.Add(int.Parse(humanParts[0]), human);
            }

            NumberOfZombies = int.Parse(inputs[i]);
            for (i = 3 + NumberOfHumans; i < NumberOfZombies + NumberOfHumans + 3; i++)
            {
                var zombieParts = inputs[i].Split(" ");
                var zombie = new Vector2Int(int.Parse(zombieParts[1]), int.Parse(zombieParts[2]));
                this.gameToTest.GameState.Zombies.Add(int.Parse(zombieParts[0]), zombie);
            }

            this.gameToTest.PreBuildHumanToZombieMap();

        } else
        {
            this.gameToTest.InitializeRandomGame(NumberOfHumans, NumberOfZombies);
        }
        

        this.currentMoveGeneration = GenerateMoveSets();

        for (int i = 0; i < NumberOfGyms; i++)
        {

            GameObject gymObject = new GameObject();
            gymObject.transform.name = $"Gym {i}";
            gymObject.transform.position = gymObject.transform.position + new Vector3((i % RowWidth) * 180.0f, 0, (i / RowWidth) * 100.0f);

            Gyms gym = gymObject.AddComponent<Gyms>();
            gym.Game = new Game(this.gameToTest, i);
            gym.Setup(gym.Game, this.currentMoveGeneration[i]);
            gyms.Add(gym);

            gymContainers.Add(gymObject);
        }

        // make the game once and then copy it anytime we need to use it later
    }

    private void SetupGames(Game game,Move[][] moveSets)
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

    private void EvolveGyms()
    {
        this.ResetGyms();
        this.currentMoveGeneration.CopyTo(this.swapMoveGeneration, 0);
        EvolveMoveSets(this.swapMoveGeneration).CopyTo(this.currentMoveGeneration, 0);

        this.SetupGames(this.gameToTest, this.currentMoveGeneration);
    }

    private Move GenerateMove() => new Move(Mathf.Lerp(0.0f, 360.0f, 1.0f - Mathf.Pow(Random.value, 2.0f)), Mathf.Lerp(0.0f, 1000.0f, 1.0f - Mathf.Pow(Random.value, 2.0f)));

    private Move[][] GenerateMoveSets()
    {
        for (int i = 0; i < NumberOfGyms; i++)
        {
            var moves = this.currentMoveGeneration[i];
            for (int j = 0; j < moves.Length; j++)
            {
                moves[j] = GenerateMove(); 
            }
        }

        return this.currentMoveGeneration;
    }


    private int GetParent()
    {
        var choice = Random.value;

        if (choice <= 0.7)
        {
            return (int)Mathf.Lerp(0, 10,  Mathf.Pow(Random.value, 2f));
        } else
        {
            return Random.Range(10, NumberOfGyms - 1);
        }
    }

    private double[] MakeWheel(double[] fitnesses)
    {
        double [] result = new double[fitnesses.Length];
        double sum = fitnesses.Sum();

        double previousProbabiltiy = 0.0d;
        for (int i = 0; i < fitnesses.Length; i++)
        {
            previousProbabiltiy += (fitnesses[i] / sum); 
            result[i] = previousProbabiltiy;
        }

        return result;
    }

    private int GetParentRoullette(double[] wheel)
    {
        var choice = (double)Random.Range(0f, 1.0f);
        for (int i = 0; i < wheel.Length; i++)
        {
            if (choice < wheel[i])
            {
                return i;
            }
        }

        return wheel.Length - 1;
    }
    private Move[][] EvolveMoveSets(Move[][] moves)
    {
        Generation++;
        Random.InitState((int)Random.value + Generation);
        System.Array.Sort(this.LastResults, (left, right) =>
        {

            var first = right.Score.CompareTo(left.Score);

            if (first != 0)
            {
                return first;
            }

            var humans = right.HumansAlive.CompareTo(left.HumansAlive);

            if (humans != 0)
            {
                return humans;
            }

            return right.Fitness.CompareTo(left.Fitness);
        });

        var wheel = MakeWheel(this.LastResults.Select(fitness => fitness.Fitness).ToArray());

        var newMoves = this.nextMoveGeneration;

        int i = 0;
        int moveIndex = 0;

        this.cumaltiveResults.Add(new GameFitness(this.LastResults[0]));

        for (i = 0; i < this.ElitismCount; i++)
        {
            var result = this.LastResults[i];
            moves[result.Gym].CopyTo(newMoves[moveIndex], 0);
            moveIndex++;
        }

        for (; moveIndex <= (NumberOfGyms - this.KillCount); moveIndex += 2)
        {
            var parent1 = this.LastResults[GetParentRoullette(wheel)];
            var parent2 = this.LastResults[GetParentRoullette(wheel)];

            var parent1Moves = moves[parent1.Gym];
            var parent2Moves = moves[parent2.Gym];


            var choice = Random.Range(0, 2);

            var child1 = this.nextMoveGeneration[moveIndex];
            var child2 = this.nextMoveGeneration[moveIndex + 1];

            for (int j = 0; j < NumberOfMoves; j++)
            {
                if (j < this.NumberOfMoves / 5 || j > this.NumberOfMoves - this.NumberOfMoves / 3)
                {
                    child1[j] = parent1Moves[j];
                    child2[j] = parent2Moves[j];
                }
                else
                {
                    child1[j] = parent2Moves[j];
                    child2[j] = parent1Moves[j];
                }

                if (Random.value <= this.GeneMutateChance)
                {
                    child1[j] = GenerateMove();
                }

                if (Random.value <= this.GeneMutateChance)
                {
                    child2[j] = GenerateMove();
                }
            }
        }

        for (; moveIndex < NumberOfGyms; moveIndex +=2)
        {
            for (int j = 0; j < NumberOfMoves; j++)
            {
                this.nextMoveGeneration[moveIndex][j] = GenerateMove(); 
            }
        }

        return this.nextMoveGeneration;
    }

    private int priorSlowdownCount =  1;
    private void Update()
    {
        if (this.SlowDownCount < 1)
        {
            this.SlowDownCount = 1;
        }

        if (priorSlowdownCount != this.SlowDownCount)
        {
            foreach (var gym in this.gyms)
            {
                gym.SlowDown = this.SlowDownCount;
            }

            priorSlowdownCount = this.SlowDownCount;
        }

        if (scored)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || this.unattended)
            {
                var stringBuilder = new StringBuilder();
                foreach (var result in this.cumaltiveResults)
                {
                    stringBuilder.Append($"{result.Score},{result.Fitness}");
                    var movesString = new StringBuilder();
                    foreach (var move in this.currentMoveGeneration[result.Gym])
                    {
                        movesString.Append(",");
                        movesString.Append($"{move.Angle};{move.Magintude}");
                    }

                    stringBuilder.Append(movesString.ToString());
                    stringBuilder.Append(System.Environment.NewLine);
                }

                System.IO.File.WriteAllText($"{this.Generation}-{this.NumberOfGenerationsToRun}-{System.DateTime.Now.ToFileTime()}.log", stringBuilder.ToString());

                this.cumaltiveResults.Clear();
                this.scored = false;
                this.EvolveGyms();
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

        double maxFitness = 0;
        double minFitness = double.MaxValue;

        int i = 0;

        double maxScore = 0;
        foreach (var gym in gyms)
        {
            if (gym.Game.GameState.Score > maxScore)
            {
                maxScore = gym.Game.GameState.Score;
            }
        }

        foreach (var gym in gyms)
        {
            var gameState = gym.Game.GameState;

            gym.Game.ScoreFitness(maxScore, this.NumberOfMoves);
            var fitness = gym.Game.Fitness.Fitness;

            if (fitness > maxFitness)
            {
                maxFitness = fitness;
            }

            if (fitness < minFitness)
            {
                minFitness = fitness;
            }

            this.LastResults[i] = gym.Game.Fitness;
            i++;
        }

        scored = true;

        if (IterationGenerations < NumberOfGenerationsToRun)
        {
            for (i = 0; i < this.LastResults.Length; i++)
            {
                var result = this.LastResults[i];
                if (result.Won)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.blue;
                }
                else if (result.Lost)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.red;

                } else if (!result.Won && !result.Lost)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.yellow;

                }
                if (result.Fitness == maxFitness)
                {
                    gyms[i].Plane.GetComponent<MeshRenderer>().material.color = Color.green;
                }

                if ((maxFitness - minFitness) == 0.0f)
                {
                    gyms[i].transform.position += new Vector3(0.0f, 0.0f, 0.0f);

                } else
                {
                    gyms[i].transform.position += new Vector3(0.0f, (float)(result.Fitness - minFitness) / (float)(maxFitness - minFitness) * 10.0f, 0.0f);
                }

                //Debug.Log($"Gym {result.Gym} fitness : {result.Fitness} score: {result.Score} won : {result.Won} lost: {result.Lost}");
            }
            this.scored = false;
            this.IterationGenerations++;
            this.EvolveGyms();
        }
    }

    bool unattended = false;
    private void OnGUI()
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(10, 10, 100, 25), $"Generation {this.Generation}");
        if (this.cumaltiveResults.Any())
        {
            GUI.Label(new Rect(10, 40, 200, 25), $"Max Fitness {this.cumaltiveResults.Last().Fitness}");
            GUI.Label(new Rect(10, 80, 200, 25), $"Max Score {this.cumaltiveResults.Last().Score}");
        }

        if (GUI.Button(new Rect(10, 100, 200, 25), this.unattended ? "unattended" : "attended"))
        {
            this.unattended = !this.unattended;
        }
    }
}
