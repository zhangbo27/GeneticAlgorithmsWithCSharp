﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GeneticAlgorithms.LogicCircuits
{
    [TestClass]
    public class CircuitTests
    {
        private static readonly Random Random = new Random();

        public delegate Node FnCreateGene(int index);

        public delegate int FnFitnessDelegate(Node[] genes);

        public static int Fitness(Node[] genes, Tuple<bool[], bool>[] rules, Dictionary<char, bool?> inputs)
        {
            var circuit = NodesToCircuit(genes).Item1;
            var sourceLabels = "ABCD";
            var rulesPassed = 0;
            foreach (var rule in rules)
            {
                inputs.Clear();
                for (var i = 0; i < rule.Item1.Length; i++)
                    inputs.Add(sourceLabels[i], rule.Item1[i]);
                if (circuit.Output == rule.Item2)
                    rulesPassed++;
            }

            return rulesPassed;
        }

        [TestMethod]
        public void FitnessTest()
        {
            var inputs = new Dictionary<char, bool?>();

            var nodes = new[]
            {
                new Node((a, b) => new Source('A', inputs)),
                new Node((a, b) => new Source('B', inputs)),
                new Node((a, b) => new Or(a, b), 0, 1)
            };

            var rules = new[]
            {
                new Tuple<bool[], bool>(new[] {false, false}, false),
                new Tuple<bool[], bool>(new[] {false, true}, true),
                new Tuple<bool[], bool>(new[] {true, false}, true),
                new Tuple<bool[], bool>(new[] {true, true}, true)
            };

            var fitness = Fitness(nodes, rules, inputs);
            Assert.AreEqual(4, fitness);
            Assert.AreEqual(2, inputs.Count);
        }

        public static void Display(Chromosome<Node, int> candidate, Stopwatch watch)
        {
            var circuit = NodesToCircuit(candidate.Genes).Item1;
            Console.WriteLine("{0}\t{1}\t{2} ms", circuit, candidate.Fitness, watch.ElapsedMilliseconds);
        }

        public static Node CreateGene(int index, Tuple<Node.CrateGeneDelegate, ICircuit>[] gates,
            Tuple<Node.CrateGeneDelegate, ICircuit>[] sources)
        {
            var gateType = index < sources.Length
                ? sources[index]
                : gates[Random.Next(gates.Length)];

            int? indexA = null;
            int? indexB = null;
            if (gateType.Item2.InputCount > 0)
                indexA = Random.Next(index);
            if (gateType.Item2.InputCount > 1)
            {
                indexB = index > 1 && index >= sources.Length
                    ? Random.Next(index)
                    : 0;
                if (indexB == indexA)
                    indexB = Random.Next(index);
            }

            return new Node(gateType.Item1, indexA, indexB);
        }

        public static Node[] Mutate(Node[] input, FnCreateGene fnCreateGene, FnFitnessDelegate fnFitness,
            int sourceCount)
        {
            var childGenes = new Node[input.Length];
            Array.Copy(input, childGenes, input.Length);

            var count = Random.Next(1, 6);
            var initialFitness = fnFitness(childGenes);
            while (count-- > 0)
            {
                var indexesUsed = NodesToCircuit(childGenes).Item2.Skip(sourceCount).ToArray();
                if (indexesUsed.Length == 0)
                    return childGenes;
                var index = indexesUsed[Random.Next(indexesUsed.Length)];
                childGenes[index] = fnCreateGene(index);
                if (fnFitness(childGenes).CompareTo(initialFitness) > 0)
                    return childGenes;
            }

            return childGenes;
        }

        [TestMethod]
        public void CreateGeneTest()
        {
            var sourceContainer = new Dictionary<char, bool?> {{'A', false}, {'B', true}};

            var sources = new[]
            {
                new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('A', sourceContainer),
                    new Source('A', null)),
                new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('B', sourceContainer),
                    new Source('B', null))
            };

            var gates = new[]
            {
                new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new And(a, b), new And(null, null)),
                new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Or(a, b), new Or(null, null))
            };

            var nodes = new Node[6];
            for (var i = 0; i < nodes.Length; i++)
                nodes[i] = CreateGene(i, gates, sources);

            var circuit = NodesToCircuit(nodes).Item1;
            Console.Write("{0} = {1}", circuit, circuit.Output);
        }

        private Dictionary<char, bool?> _inputs;
        private List<Tuple<Node.CrateGeneDelegate, ICircuit>> _gates;
        private List<Tuple<Node.CrateGeneDelegate, ICircuit>> _sources;

        [TestInitialize]
        public void SetupClass()
        {
            _inputs = new Dictionary<char, bool?>();
            _gates = new List<Tuple<Node.CrateGeneDelegate, ICircuit>>
            {
                new Tuple<Node.CrateGeneDelegate, ICircuit>((i1, i2) => new And(i1, i2), new And(null, null)),
                new Tuple<Node.CrateGeneDelegate, ICircuit>((i1, i2) => new Not(i1), new Not(null))
            };
            _sources = new List<Tuple<Node.CrateGeneDelegate, ICircuit>>
            {
                new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('A', _inputs), new Source('A', null)),
                new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('B', _inputs), new Source('B', null))
            };
        }

        [TestMethod]
        public void GenerateOrTest()
        {
            var rules = new[]
            {
                new Tuple<bool[], bool>(new[] {false, false}, false),
                new Tuple<bool[], bool>(new[] {false, true}, true),
                new Tuple<bool[], bool>(new[] {true, false}, true),
                new Tuple<bool[], bool>(new[] {true, true}, true)
            };

            var optimalLength = 6;
            FindCircuit(rules, optimalLength);
        }

        [TestMethod]
        public void GenerateXorTest()
        {
            var rules = new[]
            {
                new Tuple<bool[], bool>(new[] {false, false}, false),
                new Tuple<bool[], bool>(new[] {false, true}, true),
                new Tuple<bool[], bool>(new[] {true, false}, true),
                new Tuple<bool[], bool>(new[] {true, true}, false)
            };

            FindCircuit(rules, 9);
        }

        [TestMethod]
        public void GenerateAxBxCTest()
        {
            var rules = new[]
            {
                new Tuple<bool[], bool>(new[] {false, false, false}, false),
                new Tuple<bool[], bool>(new[] {false, false, true}, true),
                new Tuple<bool[], bool>(new[] {false, true, false}, true),
                new Tuple<bool[], bool>(new[] {false, true, true}, false),
                new Tuple<bool[], bool>(new[] {true, false, false}, true),
                new Tuple<bool[], bool>(new[] {true, false, true}, false),
                new Tuple<bool[], bool>(new[] {true, true, false}, false),
                new Tuple<bool[], bool>(new[] {true, true, true}, true)
            };

            _sources.Add(new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('C', _inputs),
                new Source('C', null)));
            _gates.Add(new Tuple<Node.CrateGeneDelegate, ICircuit>((i1, i2) => new Or(i1, i2), new Or(null, null)));

            FindCircuit(rules, 12);
        }

        public Tuple<bool[], bool>[] Get2BitAdderRulesForBit(int bit)
        {
            var rules = new[]
            {
                new Tuple<int[], int[]>(new[] {0, 0, 0, 0}, new[] {0, 0, 0}),  // 0 + 0 = 0
                new Tuple<int[], int[]>(new[] {0, 0, 0, 1}, new[] {0, 0, 1}),  // 0 + 1 = 1
                new Tuple<int[], int[]>(new[] {0, 0, 1, 0}, new[] {0, 1, 0}),  // 0 + 2 = 2
                new Tuple<int[], int[]>(new[] {0, 0, 1, 1}, new[] {0, 1, 1}),  // 0 + 3 = 3
                new Tuple<int[], int[]>(new[] {0, 1, 0, 0}, new[] {0, 0, 1}),  // 1 + 0 = 1
                new Tuple<int[], int[]>(new[] {0, 1, 0, 1}, new[] {0, 1, 0}),  // 1 + 1 = 2
                new Tuple<int[], int[]>(new[] {0, 1, 1, 0}, new[] {0, 1, 1}),  // 1 + 2 = 3
                new Tuple<int[], int[]>(new[] {0, 1, 1, 1}, new[] {1, 0, 0}),  // 1 + 3 = 4
                new Tuple<int[], int[]>(new[] {1, 0, 0, 0}, new[] {0, 1, 0}),  // 2 + 0 = 2
                new Tuple<int[], int[]>(new[] {1, 0, 0, 1}, new[] {0, 1, 1}),  // 2 + 1 = 3
                new Tuple<int[], int[]>(new[] {1, 0, 1, 0}, new[] {1, 0, 0}),  // 2 + 2 = 4
                new Tuple<int[], int[]>(new[] {1, 0, 1, 1}, new[] {1, 0, 1}),  // 2 + 3 = 5
                new Tuple<int[], int[]>(new[] {1, 1, 0, 0}, new[] {0, 1, 1}),  // 3 + 0 = 3
                new Tuple<int[], int[]>(new[] {1, 1, 0, 1}, new[] {1, 0, 0}),  // 3 + 1 = 4
                new Tuple<int[], int[]>(new[] {1, 1, 1, 0}, new[] {1, 0, 1}),  // 3 + 2 = 5
                new Tuple<int[], int[]>(new[] {1, 1, 1, 1}, new[] {1, 1, 0}),  // 3 + 3 = 6
            };

            var bitNRules = rules.Select(rule =>
                new Tuple<bool[], bool>(rule.Item1.Select(b => b != 0).ToArray(), rule.Item2[2 - bit] != 0));
            _gates.Add(new Tuple<Node.CrateGeneDelegate, ICircuit>((i1, i2) => new Or(i1, i2), new Or(null, null)));
            _gates.Add(new Tuple<Node.CrateGeneDelegate, ICircuit>((i1, i2) => new Xor(i1, i2), new Xor(null, null)));
            _sources.Add(new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('C', _inputs),
                new Source('C', null)));
            _sources.Add(new Tuple<Node.CrateGeneDelegate, ICircuit>((a, b) => new Source('D', _inputs),
                new Source('D', null)));
            return bitNRules.ToArray();
        }

        [TestMethod]
        public void Test2BitAdder1SBit()
        {
            var rules = Get2BitAdderRulesForBit(0);
            FindCircuit(rules, 3);
        }

        [TestMethod]
        public void Test2BitAdder2SBit()
        {
            var rules = Get2BitAdderRulesForBit(1);
            FindCircuit(rules, 7);
        }

        [TestMethod]
        public void Test2BitAdder4SBit()
        {
            var rules = Get2BitAdderRulesForBit(2);
            FindCircuit(rules, 9);
        }

        public void FindCircuit(Tuple<bool[], bool>[] rules, int expectedLength)
        {
            var genetic = new Genetic<Node, int>();
            var watch = Stopwatch.StartNew();

            void FnDisplay(Chromosome<Node, int> candidate, int? length = null)
            {
                if (length != null)
                    Console.WriteLine("-- distinct nodes in circuit: {0}", NodesToCircuit(candidate.Genes).Item2.Count);
                else
                    Display(candidate, watch);
            }

            int FnGetFitness(Node[] genes) => Fitness(genes, rules, _inputs);

            Node FnCreateGene(int index) => CreateGene(index, _gates.ToArray(), _sources.ToArray());

            Node[] FnMutate(Node[] genes) => Mutate(genes, FnCreateGene, FnGetFitness, _sources.Count);

            var maxLength = 50;

            Node[] FnCreate() => Enumerable.Range(0, maxLength).Select(FnCreateGene).ToArray();

            Chromosome<Node, int> FnOptimizationFunction(int variableLength)
            {
                maxLength = variableLength;
                return genetic.BestFitness(FnGetFitness, 0, rules.Length, null, FnDisplay, FnMutate, FnCreate, 0, 3,
                    null, 30);
            }

            bool FnIsImprovement(Chromosome<Node, int> currentBest, Chromosome<Node, int> child) =>
                child.Fitness == rules.Length &&
                NodesToCircuit(child.Genes).Item2.Count < NodesToCircuit(currentBest.Genes).Item2.Count;

            bool FnIsOptimal(Chromosome<Node, int> child) =>
                child.Fitness == rules.Length &&
                NodesToCircuit(child.Genes).Item2.Count <= expectedLength;

            int FnGetNextFeatureValue(Chromosome<Node, int> currentBest) =>
                NodesToCircuit(currentBest.Genes).Item2.Count;

            var best = genetic.HillClimbing(FnOptimizationFunction, FnIsImprovement, FnIsOptimal, FnGetNextFeatureValue,
                FnDisplay, maxLength);
            Assert.AreEqual(rules.Length, best.Fitness);
            var circuit = NodesToCircuit(best.Genes).Item2;
            Assert.IsFalse(circuit.Count > expectedLength);
        }

        public static Tuple<ICircuit, ISet<int>> NodesToCircuit(Node[] genes)
        {
            var circuit = new List<ICircuit>();
            var usedIndexes = new List<ISet<int>>();
            for (var i = 0; i < genes.Length; i++)
            {
                var node = genes[i];
                var used = new HashSet<int> {i};
                ICircuit inputA = null;
                ICircuit inputB = null;
                if (node.IndexA != null && i > node.IndexA)
                {
                    inputA = circuit[(int) node.IndexA];
                    used.UnionWith(usedIndexes[(int) node.IndexA]);
                    if (node.IndexB != null && i > node.IndexB)
                    {
                        inputB = circuit[(int) node.IndexB];
                        used.UnionWith(usedIndexes[(int) node.IndexB]);
                    }
                }

                circuit.Add(node.CreateGate(inputA, inputB));
                usedIndexes.Add(used);
            }

            return new Tuple<ICircuit, ISet<int>>(circuit[circuit.Count - 1], usedIndexes[usedIndexes.Count - 1]);
        }
    }

    public class Node
    {
        public delegate ICircuit CrateGeneDelegate(ICircuit inputA, ICircuit inputB);

        public CrateGeneDelegate CreateGate { get; }
        public int? IndexA { get; }
        public int? IndexB { get; }

        public Node(CrateGeneDelegate createGate, int? indexA = null, int? indexB = null)
        {
            CreateGate = createGate;
            IndexA = indexA;
            IndexB = indexB;
        }
    }
}