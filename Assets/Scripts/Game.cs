using System.Collections.Generic;
using UnityEngine;

public class Game
{
    private Vector2Int size;

    private int AshKillRadius = 2000;
    public int AshKillRadiusSquared;
    private int seed;
    private int id;
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
    }

    public void CopyFrom(Game game)
    {
        this.GameState.CopyFrom(game.GameState);
        this.Fitness.CopyFrom(game.Fitness);
        this.PlayedMoves.Clear();
        
        foreach(Move move in game.PlayedMoves)
        {
            this.PlayedMoves.Add(move);
        }

        this.size = game.size;
        this.startingNumberOfHumans = game.startingNumberOfHumans;
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
            var zombie = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
            this.GameState.Zombies.Add(i, zombie); 

            var closestHuman = 0;
            var closestDistance = double.MaxValue;
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

            this.GameState.HumanClosestToZombie[i] = closestHuman;

        }

        this.GameState.Ash = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
    }

    public void PreBuildHumanToZombieMap()
    {
        foreach (var zombiePair in this.GameState.Zombies)
        {
            var closestHuman = 0;
            var closestDistance = double.MaxValue;
            // TODO: Find a better lookup than the current

            foreach (var humanPair in this.GameState.Humans)
            {
                var human = humanPair.Value;
                var distanceToHuman = Helpers.MagnitudeSquared(zombiePair.Value, human);

                if (distanceToHuman <= closestDistance)
                {
                    closestDistance = distanceToHuman;
                    closestHuman = humanPair.Key;
                }
            }

            this.GameState.HumanClosestToZombie[zombiePair.Key] = closestHuman;

        }
    }

    public void TickNextState(Vector2Int ashPosition)
    {
        if (this.GameState.Phase != GamePhase.Playing)
        {
            return;
        }

        this.GameState.NumberOfUsedMoves++;

        var distanceToKill = new Dictionary<int, double>();
        var priorDistanceToKill = new Dictionary<int, double>();
        var moveMagnitude = Helpers.MagnitudeSquared(this.GameState.Ash, ashPosition);
        var ashMaxMoveDistance = 1000 * 1000;

        var leftMovement = ashMaxMoveDistance - moveMagnitude;
        var herdingZombies = 0;
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

            bool wasTargetAsh = false;
            if (closestDistance == double.MaxValue)
            {
                wasTargetAsh = true;
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
            priorDistanceToKill.Add(zombiePair.Key, distanceToAsh);

            bool isTargetAsh = false;

            if (distanceToAsh <= closestDistance)
            {
                targetPosition = ashPosition;
                isTargetAsh = true;
                this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);

                // Add some fitness for redirecting zombies
                this.Fitness.Fitness += 5;
                if (wasTargetAsh)
                {
                    this.Fitness.Fitness += 5;
                    herdingZombies++;
                }

                if (closestDistance <= (400 * 400))
                {
                    this.Fitness.Fitness += 20;
                }
            }

            var newPosition = Vector2.MoveTowards(zombie, targetPosition, 400.0f);
            var nextPosition = new Vector2Int((int)newPosition.x, (int)newPosition.y);

            var newDistanceToAsh = Helpers.MagnitudeSquared(nextPosition, ashPosition);

            this.GameState.ZombieNextMoves.Add(zombiePair.Key, nextPosition);

            if (newDistanceToAsh <= AshKillRadiusSquared)
            {
                this.GameState.KilledZombies.Add(zombiePair.Key);
                this.GameState.NumberOfKilledZombies += 1;

                // Add Some fitness for killing a zombie
                this.Fitness.Fitness += 25;

                if (wasTargetAsh)
                {
                    this.Fitness.Fitness += 5;
                }

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
                this.GameState.KilledHumans.Add(closestHuman);

                // Remove some fitness for a human dying

                this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);
                var movesToHuman = (int)closestDistance / (400 * 400);
                var ashMovesToHuman = (int)((Helpers.MagnitudeSquared(this.GameState.Humans[closestHuman], ashPosition) - AshKillRadiusSquared) / ashMaxMoveDistance);

                if (ashMovesToHuman < movesToHuman)
                {
                    this.Fitness.Fitness -= 15;
                }
            }
            
        }

        this.Fitness.Fitness += 5 * herdingZombies;

        foreach (var distancePair in distanceToKill)
        {
            if ((distancePair.Value - AshKillRadiusSquared) < leftMovement)
            {
                this.Fitness.Fitness -= 5;
            } 

            if (distancePair.Value > priorDistanceToKill[distancePair.Key])
            {
                this.Fitness.Fitness -= 20;
            }         
        }

        // set ashes new position
        this.GameState.Ash = ashPosition;


        var killedHumanCount = this.GameState.KilledHumans.Count;
        var distanceToClosestHumanKilled = double.MaxValue;
        foreach (var human in this.GameState.KilledHumans)
        {
            this.GameState.DistanceToLastHuman = Helpers.MagnitudeSquared(this.GameState.Ash, this.GameState.Humans[human]);

            if (this.GameState.DistanceToLastHuman < distanceToClosestHumanKilled)
            {
                distanceToClosestHumanKilled = this.GameState.DistanceToLastHuman;
            }

            this.GameState.Humans.Remove(human);
            this.Fitness.Fitness -= 10 * (this.startingNumberOfHumans - this.GameState.Humans.Count);
        }

        if (distanceToClosestHumanKilled != double.MaxValue && distanceToClosestHumanKilled - AshKillRadiusSquared <= leftMovement)
        {
            this.Fitness.Fitness -= 50;
        }

        // calculate the score
        var humansRemaining = this.GameState.Humans.Count;
        this.Fitness.Fitness += humansRemaining * 5;
        if (humansRemaining == 0)
        {
            this.GameState.Phase = GamePhase.Lost;
            this.GameState.Score = 0;
            return;
        } 

        int killedZombieCount = 0;
        int humanCountSquare = humansRemaining * humansRemaining;
        foreach (var zombie in this.GameState.KilledZombies)
        {
            this.GameState.Zombies.Remove(zombie);
            this.GameState.ZombieNextMoves.Remove(zombie);
            killedZombieCount++;
            this.GameState.Score += 10 * humanCountSquare * Helpers.GetFib(killedZombieCount);
        }
        this.Fitness.Fitness += killedZombieCount * 5;

        // move the zombies
        foreach (var movePair in this.GameState.ZombieNextMoves)
        {
            this.GameState.Zombies[movePair.Key] = movePair.Value;
        }

        if (this.GameState.Zombies.Count == 0)
        {
            this.GameState.Phase = GamePhase.Won;
            this.Fitness.Fitness += 50;
            return;
        }

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
        if (maxScore > 0)
        {
           this.Fitness.Fitness *= 1 + (2 * this.GameState.Score / maxScore);
        }
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
