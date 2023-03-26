using System;
using System.Linq;

namespace zombiegame
{
    public abstract class GeneticsBase<T>
    {
        protected readonly Random random;

        protected int numberOfChromosomes;
        protected int population;
        protected int seed;
        public T[][] Genomes;
        protected int splitPointOne;
        protected int splitPointTwo;

        public int Generation { get; protected set; }

        public bool Elitism { get; set; }

        public int ElitismCount { get; set; }

        public int KillOffCount { get; set; }

        public double MutateChance { get; set; }

        public GeneticsBase(int numberOfChromosomes, int population, int seed)
        {
            this.numberOfChromosomes = numberOfChromosomes;
            this.seed = seed;
            if (this.population % 2 != 0)
            {
                throw new ArgumentException("population must be an even number");
            }
            this.population = population;
            this.random = new Random(seed); 
        }

        public void SeedGeneration()
        {
            this.Genomes = new T[population][];
            for (int i = 0; i < population; i++)
            {
                this.Genomes[i] = new T[numberOfChromosomes];
                for (int j = 0; j < this.numberOfChromosomes; j++)
                {
                    this.Genomes[i][j] = GenerateChromosome();
                }

            }
        }

        public abstract T GenerateChromosome();

        public abstract double[][] GetFitness();

        public void EvolveGeneration()
        {
            this.Generation++;

            var newGeneration = new T[population][];
            var fitnesses = GetFitness();
            Array.Sort(fitnesses, (left, right) => right[0].CompareTo(left[0]));
            var wheel = MakeWheel(fitnesses);
            int currentGenome = 0;
            if (this.Elitism)
            {
                for (; currentGenome < this.ElitismCount; currentGenome++)
                {
                    newGeneration[currentGenome] = this.Genomes[(int)fitnesses[currentGenome][1]];
                }
            }

            for (; currentGenome < this.population - this.KillOffCount; currentGenome += 2)
            {
                var parent1 = this.Genomes[GetParentRoullette(wheel)];
                var parent2 = this.Genomes[GetParentRoullette(wheel)];

                var child1 = new T[numberOfChromosomes];
                var child2 = new T[numberOfChromosomes];

                newGeneration[currentGenome] = child1;
                newGeneration[currentGenome + 1] = child2;

                for (int j = 0; j < this.numberOfChromosomes; j++)
                {
                    if (j < this.splitPointOne || j > this.splitPointTwo)
                    {
                        child1[j] = parent1[j];
                        child2[j] = parent2[j];
                    } else
                    {
                        child1[j] = parent2[j];
                        child2[j] = parent1[j];
                    }

                    if (this.random.NextDouble() < this.MutateChance)
                    {
                        child1[j] = this.GenerateChromosome();
                    }

                    if (this.random.NextDouble() < this.MutateChance)
                    {
                        child2[j] = this.GenerateChromosome();
                    }
                }
            }

            for (; currentGenome < this.population; currentGenome++)
            {
                newGeneration[currentGenome] = new T[this.numberOfChromosomes];

                for (int i = 0; i < this.numberOfChromosomes; i++)
                {
                    newGeneration[currentGenome][i] = GenerateChromosome(); 
                }
            }

            this.Genomes = newGeneration;
        }

        private double[] MakeWheel(double[][] fitnesses)
        {
            double[] result = new double[fitnesses.Length];
            double sum = 0;

            for (int i = 0; i < fitnesses.Length; i++)
            {
                sum += fitnesses[i][0];
            }

            double previousProbabiltiy = 0.0d;
            for (int i = 0; i < fitnesses.Length; i++)
            {
                if (sum < 0.0)
                {
                    previousProbabiltiy += 1.0 - (fitnesses[i][0] / sum);

                }
                else
                {
                    previousProbabiltiy += (fitnesses[i][0] / sum);
                }
                result[i] = previousProbabiltiy;
            }

            return result;
        }

        private int GetParentRoullette(double[] wheel)
        {
            var choice = random.NextDouble();
            for (int i = 0; i < wheel.Length; i++)
            {
                if (choice < wheel[i])
                {
                    return i;
                }
            }

            return wheel.Length - 1;
        }
    }
}
