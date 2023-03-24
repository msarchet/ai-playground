using System.Collections.Generic;
using UnityEngine;

public class Game
{
    private Vector2Int size;
    Dictionary<int, double> distanceToKill = new Dictionary<int, double>();
    Dictionary<int, double> priorDistanceToKill = new Dictionary<int, double>();
    Dictionary<int, Vector2IntHasCoordinate> humanKdMaps = new Dictionary<int, Vector2IntHasCoordinate>();
    Dictionary<int, Vector2IntHasCoordinate> zombieKdMaps = new Dictionary<int, Vector2IntHasCoordinate>();
    public List<Vector2IntHasCoordinate> humanList = new List<Vector2IntHasCoordinate>();    
    public List<Vector2IntHasCoordinate> zombieList = new List<Vector2IntHasCoordinate>();    

    Vector2IntHasCoordinate ashKdMap = new Vector2IntHasCoordinate(0);

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
            this.humanKdMaps.Add(i, new Vector2IntHasCoordinate(i));
            this.zombieKdMaps.Add(i, new Vector2IntHasCoordinate(i));
        }
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
            var zombie = new Vector2Int(Random.Range(0, this.size.x), Random.Range(0, this.size.y));
            this.GameState.Zombies.Add(i, zombie); 

            var closestHuman = 0;
            var closestDistance = double.MaxValue;
            // TODO: Find a better lookup than the current

            foreach (var humanPair in this.GameState.Humans)
            {
                var human = humanPair.Value;
                var distanceToHuman = Helpers.MagnitudeSquared(zombie.x, human.x, zombie.y, human.y);

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
                var distanceToHuman = Helpers.MagnitudeSquared(zombiePair.Value.x, human.x, zombiePair.Value.y, human.y);

                if (distanceToHuman <= closestDistance)
                {
                    closestDistance = distanceToHuman;
                    closestHuman = humanPair.Key;
                }
            }

            this.GameState.HumanClosestToZombie[zombiePair.Key] = closestHuman;

        }
    }


    private KDTree<Vector2IntHasCoordinate> humanPositionTree = new KDTree<Vector2IntHasCoordinate>(2);
    private List<Vector2IntHasCoordinate> results = new List<Vector2IntHasCoordinate>();
    private double UnusedMovement = 0;
    public void TickNextState(Vector2Int ashPosition)
    {
        if (this.GameState.Phase != GamePhase.Playing)
        {
            return;
        }

        this.GameState.NumberOfUsedMoves++;

        var moveMagnitude = Helpers.MagnitudeSquared(this.GameState.Ash.x, ashPosition.x, this.GameState.Ash.y, ashPosition.y);
        var ashMaxMoveDistance = Helpers.MagnitudeSquared(0, 0, 1000, 100);
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

        if (humanList.Count == 0)
        {
            foreach (var humanPair in this.GameState.Humans)
            {
                this.humanKdMaps[humanPair.Key].Position = humanPair.Value;
                humanList.Add(this.humanKdMaps[humanPair.Key]);
            }
            humanPositionTree.ClearTree();
            humanPositionTree.Buildtree(humanList);
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

                    closestDistance = Helpers.MagnitudeSquared(zombie.x, this.GameState.Humans[humanId].x, zombie.y, this.GameState.Humans[humanId].y);
                } else
                {
                    this.GameState.HumanClosestToZombie.Remove(zombiePair.Key);
                }
            }

            bool wasTargetAsh = false;


            if (closestDistance == double.MaxValue)
            {
                wasTargetAsh = true;

                int increment = this.size.x / 20;
                int left = zombie.x - increment;
                int right = zombie.x + increment;
                int top = zombie.y + increment;
                int bottom = zombie.y - increment;

                results.Clear();
                while (left >= -increment || right <= size.x + increment || bottom >= -increment || top <= size.y + increment)
                {
                    humanPositionTree.SearchRange(new[] { left, bottom }, new[] { right, top }, results);

                    if (results.Count > 0)
                    {
                        break;
                    }

                    left -= increment;
                    right += increment;
                    top += increment;
                    bottom -= increment;
                }

                foreach (var result in results)
                {
                    var human = result.Position;
                    var distanceToHuman = Helpers.MagnitudeSquared(zombie.x, human.x, zombie.y, human.y);

                    if (distanceToHuman <= closestDistance)
                    {
                        closestDistance = distanceToHuman;
                        closestHuman = result.Id;
                    }
                }

                this.GameState.HumanClosestToZombie[zombiePair.Key] = closestHuman;

                results.Clear();

            }

            var targetPosition = this.GameState.Humans[closestHuman];
            var distanceToAsh = Helpers.MagnitudeSquared(this.GameState.Ash.x, zombie.x, this.GameState.Ash.y, zombie.y);

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
                    this.Fitness.Fitness += 50 * this.GameState.Humans.Count;
                }
            }

            var newPosition = Vector2.MoveTowards(zombie, targetPosition, 400.0f);
            var nextPosition = new Vector2Int((int)newPosition.x, (int)newPosition.y);

            var newDistanceToAsh = Helpers.MagnitudeSquared(nextPosition.x, ashPosition.x, nextPosition.y, ashPosition.y);
            if (newDistanceToAsh < distanceToAsh)
            {
                this.Fitness.Fitness += 50;
            }
            this.GameState.ZombieNextMoves.Add(zombiePair.Key, nextPosition);

            if (newDistanceToAsh <= AshKillRadiusSquared)
            {
                this.GameState.KilledZombies.Add(zombiePair.Key);
                this.GameState.NumberOfKilledZombies += 1;

                // Add Some fitness for killing a zombie
                this.Fitness.Fitness += 500;//100 * 1 + closestDistance / (400 * 400);

                if (wasTargetAsh)
                {
                    this.Fitness.Fitness += 50;
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
            this.UnusedMovement += leftMovement;
        }

        this.Fitness.Fitness += 5 * ((herdingZombies - 1) * (this.GameState.KilledZombies.Count / this.GameState.Zombies.Count)); ;

        int couldKill = 0;
        foreach (var distancePair in distanceToKill)
        {
            if ((distancePair.Value - AshKillRadiusSquared) < UnusedMovement)
            {
                couldKill++;
            } 

            if (distancePair.Value > priorDistanceToKill[distancePair.Key])
            {
                this.Fitness.Fitness -= 40;
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


        var killedHumanCount = this.GameState.KilledHumans.Count;
        var distanceToClosestHumanKilled = double.MaxValue;
        var distancesToHumans = new Dictionary<int, double>();

        foreach (var human in this.GameState.Humans)
        {
            var distanceToHuman = Helpers.MagnitudeSquared(this.GameState.Ash.x, human.Value.x, this.GameState.Ash.y, human.Value.y);
            distancesToHumans.Add(human.Key, distanceToHuman);
            var priorDistance = Helpers.MagnitudeSquared(priorPosition.x, human.Value.x, priorPosition.y, human.Value.y);

            if (distanceToHuman > priorDistance)
            {
                this.Fitness.Fitness -= 0;
            } else if (distanceToHuman < priorDistance)
            {
                this.Fitness.Fitness += 25;
            }

            if (this.GameState.KilledHumans.Contains(human.Key))
            {
                if (distanceToHuman < distanceToClosestHumanKilled)
                {
                    distanceToClosestHumanKilled = distanceToHuman;
                }
            }
        }

        this.GameState.DistanceToLastHuman = distanceToClosestHumanKilled;

        foreach (var killed in this.GameState.KilledHumans)
        {
            this.GameState.Humans.Remove(killed);
            this.Fitness.Fitness *= .5;
            if (distancesToHumans[killed] <= UnusedMovement)
            {
                this.Fitness.Fitness -= 100;// * (this.startingNumberOfHumans - this.GameState.Humans.Count);
            }
        }

        if (this.GameState.KilledHumans.Count > 0)
        {
            this.humanList.Clear();
        }

        if (distanceToClosestHumanKilled != double.MaxValue)
        {
            this.Fitness.Fitness -= 50;
        }

        // calculate the score
        var humansRemaining = this.GameState.Humans.Count;
        this.Fitness.Fitness += humansRemaining * 100;

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
                this.Fitness.Fitness += 50;
            }

        }

        distanceToKill.Clear();
        priorDistanceToKill.Clear();
    }

    public void PostTickMoveAndCleanup()
    {
        this.GameState.ZombieNextMoves.Clear();
        this.GameState.KilledZombies.Clear();
        if (this.GameState.KilledHumans.Count > 0)
        {
            humanList.Clear();
        }

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
