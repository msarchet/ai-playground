using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using zombiegame;

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
    public int FontSize = 24;
    public int GUIWidth = 400;
    public int GUIHeight= 50;
    [Range(1, 5000)]
    public int RenderCount = 10;

    [Range(1, 100)]
    public int SlowDownCount = 1;

    public List<GameObject> gymContainers = new List<GameObject>();
    public List<Gyms> gyms = new List<Gyms>();
    private int seed;

    private Game gameToTest;
    private GameFitness[] LastResults;
    private int Generation = 1;
    private int IterationGenerations = 0;
    bool scored = false;
    bool scoring = false;
    List<GameFitness> cumaltiveResults = new List<GameFitness>();
    AshGenetics genetics = new AshGenetics(0, 0);
    void Start()
    {
    }

    private void Awake()
    {
        Physics.autoSimulation = false;

        this.genetics = new AshGenetics(this.NumberOfMoves, this.NumberOfGyms);

        this.genetics.Elitism = true;
        this.genetics.ElitismCount = this.ElitismCount;
        this.genetics.MutateChance = this.GeneMutateChance;
        //this.genetics.KillOffCount = this.KillCount;

        this.LastResults = new GameFitness[NumberOfGyms];

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


        this.genetics.SeedGeneration();

        for (int i = 0; i < NumberOfGyms; i++)
        {

            GameObject gymObject = new GameObject();
            gymObject.transform.name = $"Gym {i}";
            gymObject.transform.position = gymObject.transform.position + new Vector3((i % RowWidth) * 180.0f, 0, (i / RowWidth) * 100.0f);

            Gyms gym = gymObject.AddComponent<Gyms>();
            gym.Game = new Game(this.gameToTest, i);
            gym.Setup(gym.Game, this.genetics.Genomes[i]);
            gyms.Add(gym);

            gymContainers.Add(gymObject);
        }

        // make the game once and then copy it anytime we need to use it later
    }

    private void SetupGames(Game game)
    {
        for (int i = 0; i < gyms.Count; i++)
        {
            //newgame.SetupWorld(new Vector2Int(16000, 9000));
            //newgame.InitializeGame(this.NumberOfHumans, this.NumberOfZombies);
            gyms[i].Game.CopyFrom(game);
            gyms[i].Setup(gyms[i].Game, this.genetics.Genomes[i]);
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
        this.genetics.EvolveGeneration();

        this.SetupGames(this.gameToTest);
    }


    private int priorSlowdownCount =  1;
    private int priorRenderCount = -1;

    private void Update()
    {
        if (this.SlowDownCount < 1)
        {
            this.SlowDownCount = 1;
        }

        if (priorSlowdownCount != this.SlowDownCount || priorRenderCount != this.RenderCount)
        {
            foreach (var gym in this.gyms)
            {
                gym.SlowDown = this.SlowDownCount;
                if (gym.Game.id < this.RenderCount && !gym.Render)
                {
                    gym.ToggleRender();
                } else if (gym.Game.id >= this.RenderCount)
                {
                    if (gym.Render)
                    {
                        gym.ToggleRender();
                    }
                }
            }

            priorSlowdownCount = this.SlowDownCount;
            priorRenderCount = this.RenderCount;
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
                    foreach (var move in this.genetics.Genomes[result.Gym])
                    {
                        movesString.Append(",");
                        movesString.Append($"{move[0]};{move[1]}");
                    }

                    stringBuilder.Append(movesString.ToString());
                    stringBuilder.Append(System.Environment.NewLine);
                }

                System.IO.File.WriteAllText($"{this.genetics.Generation}-{this.NumberOfGenerationsToRun}-{System.DateTime.Now.ToFileTime()}.log", stringBuilder.ToString());

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

        var fitnesses = new double[this.NumberOfGyms][];
        GameFitness bestFitness = null;
        foreach (var gym in gyms)
        {
            var gameState = gym.Game.GameState;

            gym.Game.ScoreFitness(maxScore, this.NumberOfMoves);
            var fitness = gym.Game.Fitness.Fitness;

            if (fitness > maxFitness)
            {
                maxFitness = fitness;
                bestFitness = gym.Game.Fitness;
            }

            if (fitness < minFitness)
            {
                minFitness = fitness;
            }

            this.LastResults[i] = gym.Game.Fitness;
            fitnesses[i] = new double[2] { gym.Game.Fitness.Fitness, gym.Game.id };
            i++;
        }

        System.Array.Sort(fitnesses, (left, right) => right[0].CompareTo(left[0]));

        //Debug.Log(maxScore);
        this.genetics.Fitness = fitnesses;
        this.cumaltiveResults.Add(new GameFitness(this.LastResults[(int)fitnesses[0][1]]));
         
        scored = true;

        if (genetics.Generation % NumberOfGenerationsToRun == 0)
        {
            if (minFitness <= 0)
            {
                minFitness = 0.1;
            }
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
        } else
        {
            this.scored = false;
            this.EvolveGyms();
        }
    }

    bool unattended = false;
    private void OnGUI()
    {
        int fontSize = this.FontSize;
        int height = this.GUIHeight;
        int padding = 10;
        int distance = height + padding;
        int top = 10;
        int left = 50;
        int width = this.GUIWidth;

        GUI.color = Color.black;
        GUI.skin.label.fontSize = fontSize;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.button.fontSize = fontSize;
        GUI.skin.button.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(left, top, width, height), $"Number of Gyms {this.NumberOfGyms}");
        top += distance;

        GUI.Label(new Rect(left, top, width, height), $"Generation {this.genetics.Generation}");
        top += distance;

        if (this.cumaltiveResults.Any())
        {
            GUI.Label(new Rect(left, top, width, height), $"Max Fitness {this.cumaltiveResults.Last().Fitness}");
            top += distance;
            GUI.Label(new Rect(left, top, width, height), $"Max Score {this.cumaltiveResults.Last().Score}");
            top += distance;
            GUI.Label(new Rect(left, top, width, height), $"Used Moves {this.cumaltiveResults.Last().UsedMoves}");
            top += distance;
        }


        if (GUI.Button(new Rect(left, top, width, height), this.unattended ? "unattended" : "attended"))
        {
            this.unattended = !this.unattended;
        }

        top += distance;
        var slowdownSpeed = (int)GUI.HorizontalSlider(new Rect(left, top, width, height), this.SlowDownCount, 1.0f, 100.0f);
        top += distance;

        if (slowdownSpeed > 0 && slowdownSpeed < 100 && slowdownSpeed != this.SlowDownCount)
        {
            this.SlowDownCount = slowdownSpeed;
        }


    }
}
