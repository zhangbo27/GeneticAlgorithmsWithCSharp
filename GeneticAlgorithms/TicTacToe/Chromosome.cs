﻿namespace GeneticAlgorithms.TicTacToe
{
    public class Chromosome<TGene, TFitness>
    {
        public enum Strategies
        {
            None = -1,
            Create = 0,
            Mutate = 1,
            Crossover = 2
        }

        public TGene[] Genes { get; }

        public TFitness Fitness { get; }

        public Strategies Strategy { get; }

        public int Age { get; set; }

        public Chromosome(TGene[] genes, TFitness fitness, Strategies strategy)
        {
            Genes = genes;
            Fitness = fitness;
            Strategy = strategy;
        }
    }
}