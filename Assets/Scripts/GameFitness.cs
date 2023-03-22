public class GameFitness
{
    public double Score;
    public bool Won;
    public bool Lost;
    public double Fitness;
    public int Gym;
    public int HumansAlive;

    public GameFitness() { }

    public GameFitness(GameFitness fitness)
    {
        this.CopyFrom(fitness);
    }
    public void CopyFrom(GameFitness gameFitness)
    {
        this.Score = gameFitness.Score;
        this.Won = gameFitness.Won; 
        this.Lost = gameFitness.Lost;
        this.Fitness = gameFitness.Fitness;
        this.Gym = gameFitness.Gym;
        this.HumansAlive = gameFitness.HumansAlive; 
    }
}
