using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameState
{
    public static GameState CachedState { get; set; }

    public Dictionary<int, Vector2Int> Humans { get; } = new Dictionary<int, Vector2Int>();

    public Dictionary<int, Vector2Int> Zombies { get; } = new Dictionary<int, Vector2Int>();

    public Dictionary<int, Vector2Int> ZombieNextMoves { get; set; } = new Dictionary<int, Vector2Int>();

    public Dictionary<int, int> HumanClosestToZombie { get; set; } = new Dictionary<int, int>();

    public Vector2Int Ash { get; set; } = new Vector2Int(0, 0);

    public HashSet<int> KilledHumans { get; set;  } = new HashSet<int>();
    public HashSet<int> KilledZombies { get; set;  } = new HashSet<int>();

    public int NumberOfKilledZombies { get; set; }
    public double DistanceToLastHuman { get; set; }

    public int NumberOfUsedMoves { get; set; }

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
            this.Humans[pair.Key] = pair.Value;
        }

        this.Zombies.Clear();
        foreach (var pair in gameState.Zombies)
        {
            this.Zombies[pair.Key] =  pair.Value;
        }

        this.ZombieNextMoves.Clear();
        foreach (var pair in gameState.ZombieNextMoves)
        {
            this.ZombieNextMoves[pair.Key] =  pair.Value;
        }

        this.HumanClosestToZombie.Clear();
        foreach (var pair in gameState.HumanClosestToZombie)
        {
            this.HumanClosestToZombie[pair.Key] = pair.Value;
        }

        this.Ash = gameState.Ash;

        this.KilledHumans.Clear();
        foreach (var human in gameState.KilledHumans)
        {
            this.KilledHumans.Add(human);
        }

        this.KilledZombies = new HashSet<int>(gameState.KilledZombies.ToList());

        this.KilledZombies.Clear();
        foreach (var zombie in gameState.KilledZombies)
        {
            this.KilledZombies.Add(zombie);
        }

        this.Score = gameState.Score;
        this.Phase = gameState.Phase;
        this.DistanceToLastHuman = gameState.DistanceToLastHuman;
        this.NumberOfUsedMoves = gameState.NumberOfUsedMoves;
        this.NumberOfKilledZombies = gameState.NumberOfKilledZombies;
    }
}
