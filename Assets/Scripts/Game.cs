using System.Collections.Generic;
using UnityEngine;

public class Game
{
    private Vector2Int size;
    Dictionary<int, double> distanceToKill = new Dictionary<int, double>();
    Dictionary<int, double> priorDistanceToKill = new Dictionary<int, double>();
    Dictionary<int, int[]> humanKdMaps = new Dictionary<int, int[]>();

    private int AshKillRadius = 2000;
    public int AshKillRadiusSquared;
    private int seed;
    public int id { get; private set; }

    private int startingNumberOfHumans;
    private Vector2Int lastKilledPosition;
    public GameState GameState { get; } = new GameState();

    public GameFitness Fitness { get; set; } = new GameFitness();

    private List<Move> PlayedMoves { get; set; } = new List<Move>();

    public Game(int seed, int id)
    {
        this.seed = seed;
        this.AshKillRadiusSquared = this.AshKillRadius * this.AshKillRadius;
        this.id = id;
    }

    public Game(Game game, int id) : this(game.seed, id)
    {
        this.GameState = new GameState(game.GameState);
        this.size = game.size;
        for (int i = 0; i < 100; i++)
        {
            this.humanKdMaps.Add(i, new int[2]);
        }
    }

    public void CopyFrom(Game game)
    {
        this.GameState.CopyFrom(game.GameState);
        this.Fitness.CopyFrom(game.Fitness);
        this.PlayedMoves.Clear();

        foreach (Move move in game.PlayedMoves)
        {
            this.PlayedMoves.Add(move);
        }

        this.size = game.size;
        this.startingNumberOfHumans = game.startingNumberOfHumans;
        this.UnusedMovement = game.UnusedMovement;
    }

    public void SetupWorld(Vector2Int size, int startingNumberOfHumans)
    {
        this.startingNumberOfHumans = startingNumberOfHumans;
        this.size = size;
    }

    public void InitializeRandomGame(int numberOfHumans, int numberOfZombies)
    {
        Random.InitState(this.seed);
        this.startingNumberOfHumans = numberOfHumans;
        for (int i = 0; i < numberOfHumans; i++)
        {
            this.GameState.Humans.Add(i, new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y)));
        }

        for (int i = 0; i < numberOfZombies; i++)
        {
            this.GameState.Zombies.Add(i, new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y)));
        }

        this.GameState.Ash = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
    }

    public void PreBuildHumanToZombieMap()
    {
    }


    private double UnusedMovement = 0;
    private int priorHumanCount = -1;
    private int[][] points;
    private KDTree humanPositionTree = new KDTree(2, new int[0][] {});
    public void TickNextState(Vector2Int ashPosition)
    {
        if (this.GameState.Phase != GamePhase.Playing)
        {
            return;
        }

        this.GameState.NumberOfUsedMoves++;

        var moveMagnitude = Helpers.MagnitudeSquared(this.GameState.Ash.x, ashPosition.x, this.GameState.Ash.y, ashPosition.y);
        var ashMaxMoveDistance = Helpers.MagnitudeSquared(0, 1000, 0, 1000);
        var leftMovement = ashMaxMoveDistance - moveMagnitude;
        var herdingZombies = 0;
        //KDTree<Vector2IntHasCoordinate> zombieTree = new KDTree<Vector2IntHasCoordinate>(2);
        //zombieList.Clear();
        //foreach (var zombiePair in this.GameState.Zombies)
        //{
        //    this.zombieKdMaps[zombiePair.Key].Position = zombiePair.Value;
        //    zombieList.Add(this.zombieKdMaps[zombiePair.Key]);
        //}
        //zombieTree.Buildtree(zombieList);


        if (priorHumanCount != this.GameState.Humans.Count)
        {
            var points = new int[GameState.Humans.Count][];
            int i = 0;
            foreach (var humanPair in this.GameState.Humans)
            {
                points[i] = new int[3];
                points[i][0] = humanPair.Value.x;
                points[i][1] = humanPair.Value.y;
                points[i][2] = humanPair.Key;
                i++;
            }

            humanPositionTree.ReleaseTree();
            humanPositionTree.BuildTree(points);
            priorHumanCount = this.GameState.Humans.Count;
        }

        foreach (var zombiePair in this.GameState.Zombies)
        {
            var zombie = zombiePair.Value;

            var nearest = humanPositionTree.Nearest(new[] { zombie.x, zombie.y });
            var closestDistance = Helpers.MagnitudeSquared(zombie.x, nearest.Item[0], zombie.y, nearest.Item[1]);

            var targetPosition = new Vector2Int(nearest.Item[0], nearest.Item[1]);
            var distanceToAsh = Helpers.MagnitudeSquared(this.GameState.Ash.x, zombie.x, this.GameState.Ash.y, zombie.y);

            priorDistanceToKill.Add(zombiePair.Key, distanceToAsh);

            bool isTargetAsh = false;

            if (distanceToAsh <= closestDistance)
            {
                targetPosition = ashPosition;
                isTargetAsh = true;

                // Add some fitness for redirecting zombies
                this.Fitness.Fitness += 50;
                herdingZombies++;

                //if (closestDistance <= (400 * 400))
                //{
                //    this.Fitness.Fitness += 50 * this.GameState.Humans.Count;
                //}
            }

            var newPosition = Vector2.MoveTowards(zombie, targetPosition, 400.0f);
            var nextPosition = new Vector2Int((int)newPosition.x, (int)newPosition.y);

            var newDistanceToAsh = Helpers.MagnitudeSquared(nextPosition.x, ashPosition.x, nextPosition.y, ashPosition.y);
            if (newDistanceToAsh < distanceToAsh)
            {
                this.Fitness.Fitness += 200;
            }
            this.GameState.ZombieNextMoves.Add(zombiePair.Key, nextPosition);

            if (newDistanceToAsh <= AshKillRadiusSquared)
            {
                this.GameState.KilledZombies.Add(zombiePair.Key);
                this.GameState.NumberOfKilledZombies += 1;

                // Add Some fitness for killing a zombie
                this.Fitness.Fitness += 500;//100 * 1 + closestDistance / (400 * 400);

                if (closestDistance > 400 * 400)
                {
                    this.Fitness.Fitness += 5;
                }

                continue;
            }
            else
            {
                distanceToKill.Add(zombiePair.Key, newDistanceToAsh);
            }

            if (!isTargetAsh && closestDistance == 0)
            {
                this.GameState.KilledHumans.Add(nearest.Item[2]);

                // Remove some fitness for a human dying
                
                if (newDistanceToAsh > distanceToAsh)
                {
                    if (newDistanceToAsh - AshKillRadiusSquared < ashMaxMoveDistance)
                    {
                        this.Fitness.Fitness -= 50;
                    }
                    //if (newDistanceToAsh <= UnusedMovement)
                    //{
                    //    this.Fitness.Fitness -= 10 * (distanceToAsh / UnusedMovement);
                    //}

                }
            }

            
        }

        if (herdingZombies == 0 || this.GameState.KilledZombies.Count == 0)
        {
            this.Fitness.Fitness -= 10 * leftMovement / ashMaxMoveDistance;
            this.UnusedMovement += ashMaxMoveDistance;
        }

        this.Fitness.Fitness += 25 * ((herdingZombies - 1) * (this.GameState.KilledZombies.Count / this.GameState.Zombies.Count)); ;

        int couldKill = 0;
        foreach (var distancePair in distanceToKill)
        {
            if ((distancePair.Value - AshKillRadiusSquared) < UnusedMovement)
            {
                couldKill++;
            } 

            if (distancePair.Value > priorDistanceToKill[distancePair.Key])
            {
                this.Fitness.Fitness -= 50;
            }         
        }

        this.Fitness.Fitness -= 10 * couldKill;
        if (couldKill == this.GameState.Zombies.Count)
        {
            this.Fitness.Fitness -= 200;
        }

        // set ashes new position
        var priorPosition = this.GameState.Ash;
        this.GameState.Ash = ashPosition;


        var distanceToClosestHumanKilled = double.MaxValue;

        foreach (var humanPair in this.GameState.Humans)
        {
            var human = this.GameState.Humans[humanPair.Key];

            if (this.GameState.KilledHumans.Contains(humanPair.Key))
            {
                var distanceToHuman = Helpers.MagnitudeSquared(this.GameState.Ash.x, human.x, this.GameState.Ash.y, human.y);
                var priorDistance = Helpers.MagnitudeSquared(priorPosition.x, human.x, priorPosition.y, human.y);
                if (distanceToHuman - ashMaxMoveDistance < priorDistance)
                {
                    this.Fitness.Fitness -= 500;
                }
            }
        }

        this.GameState.DistanceToLastHuman = distanceToClosestHumanKilled;

        foreach (var killed in this.GameState.KilledHumans)
        {
            this.GameState.Humans.Remove(killed);
        }

        //if (distanceToClosestHumanKilled != double.MaxValue)
        //{
        //    this.Fitness.Fitness -= 50;
        //}

        // calculate the score
        var humansRemaining = this.GameState.Humans.Count;
        //this.Fitness.Fitness += humansRemaining * 100;

        if (humansRemaining == 0)
        {
            this.GameState.Phase = GamePhase.Lost;
            this.GameState.Score = 0;
        }
        else
        {
            int killedZombieCount = 0;
            int humanCountSquare = humansRemaining * humansRemaining;
            foreach (var zombie in this.GameState.KilledZombies)
            {
                this.GameState.Zombies.Remove(zombie);
                this.GameState.ZombieNextMoves.Remove(zombie);
                killedZombieCount++;
                this.GameState.Score += 10 * humanCountSquare * Helpers.GetFib(killedZombieCount);
            }
            this.Fitness.Fitness += killedZombieCount * 10;

            // move the zombies
            foreach (var movePair in this.GameState.ZombieNextMoves)
            {
                this.GameState.Zombies[movePair.Key] = movePair.Value;
            }

            if (this.GameState.Zombies.Count == 0)
            {
                this.GameState.Phase = GamePhase.Won;
                this.Fitness.Fitness += 500 * humansRemaining;
            }

        }

        distanceToKill.Clear();
        priorDistanceToKill.Clear();
    }

    public void PostTickMoveAndCleanup()
    {
        this.GameState.ZombieNextMoves.Clear();
        this.GameState.KilledZombies.Clear();

        this.GameState.KilledHumans.Clear();
    }

    public void ScoreFitness(double maxScore, int numberOfMoves)
    {
        if (this.GameState.Phase == GamePhase.Won)
        {
            this.Fitness.Won = true;
        }
        else if (this.GameState.Phase == GamePhase.Lost)
        {
            this.Fitness.Lost = true;
        }  else if (this.GameState.Phase == GamePhase.Incomplete)
        {
            this.Fitness.Fitness *= .5;
        }
        //this.Fitness.PlayedMoves = this.PlayedMoves.ToArray();
        this.Fitness.Gym = this.id;

        this.Fitness.Score = this.GameState.Score;
        this.Fitness.HumansAlive = this.GameState.Humans.Count;
        this.Fitness.UsedMoves = this.GameState.NumberOfUsedMoves;
        if (maxScore > 0)
        {
           this.Fitness.Fitness *= 1 + (2 * this.GameState.Score / maxScore);
        }

        //this.Fitness.Fitness *= 5.0 * (1.0 - (this.GameState.NumberOfUsedMoves / numberOfMoves));
        //if (this.Fitness.Won)
        //{
        //    this.Fitness.Fitness += 1;
        //} 

        //if (this.GameState.Phase == GamePhase.Incomplete)
        //{
        //    this.Fitness.Fitness += 0.5;
        //}

    }
}
