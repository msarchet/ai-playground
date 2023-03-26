using System;
using System.Collections.Generic;
using System.Text;

namespace zombiegame
{
    public class AshGenetics : GeneticsBase<double[]>
    {
        private double ashMovement = 1000;

        public double[][] Fitness { get; set; }

        public AshGenetics(int numberOfChromosomes, int population)
            : base(numberOfChromosomes, population, 2398743)
        {
            this.splitPointOne = numberOfChromosomes / 5;
            this.splitPointTwo = numberOfChromosomes - numberOfChromosomes / 5;
        }

        public override double[] GenerateChromosome()
        {
            return new double[]
            {
                this.random.NextDouble() * 360,
                (1.0 - Math.Pow(this.random.NextDouble(), 2)) * ashMovement,
            };
        }

        public override double[][] GetFitness()
        {
            return this.Fitness;
        }
    }
}
