using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using Random = UnityEngine.Random;
using Range = UnityEngine.SocialPlatforms.Range;

public class GeneticManager : MonoBehaviour
{
    [Header("Refernces")] public CarController controller;


    [Header("Controls")] 
    public int initialPopulation = 90;
    [Range(0.0f, 1.0f)] 
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")] 
    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> genePool = new List<int>();
    private int naturallySelected;

    private NeuralNetwork[] population;

    [Header("Public View")] 
    public int currentGeneration;
    public int currentGenome;

    private void Start()
    {
        CreatePopulation();
    }

    private void CreatePopulation()
    {
        population = new NeuralNetwork[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
        
    }

    private void ResetToCurrentGenome()
    {
        controller.ResetWithNetwork(population[currentGenome]);
        
    }

    private void FillPopulationWithRandomValues(NeuralNetwork[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NeuralNetwork();
            newPopulation[startingIndex].Initialise(controller.LAYERS, controller.NEURONS);
            startingIndex++;
        }
        
    }

    public void Death(float fitness, NeuralNetwork network)
    {
        if (currentGenome < population.Length - 1)
        {
            population[currentGenome].fitness = fitness;
            currentGenome++;
            ResetToCurrentGenome();
        }
        else
        {
            Repopulate();

        }
    }


    private void Repopulate()
    {
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;
        SortPopulation();
        NeuralNetwork[] newPopulation = pickBestPopulation();
        Crossover(newPopulation);
        Mutate(newPopulation);
        
        FillPopulationWithRandomValues(newPopulation, naturallySelected);

        population = newPopulation;
        currentGenome = 0;
        ResetToCurrentGenome();
    }

    private void Mutate(NeuralNetwork[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++)
        {
            for (int j = 0; j < newPopulation[i].weights.Count; j++)
            {
                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[i].weights[j] = MutateMatrix(newPopulation[i].weights[j]);
                    
                }
            }
        }
    }

    private Matrix<float> MutateMatrix(Matrix<float> weight)
    {
        int RandomPoints = Random.Range(1, (weight.RowCount * weight.ColumnCount) / 7);
        Matrix<float> C = weight;
        for (int i = 0; i < RandomPoints; i++)
        {
            int randomCollumn = Random.Range(0, C.ColumnCount);
            int randomRow = Random.Range(0, C.ColumnCount);
            C[randomRow, randomCollumn] = Mathf.Clamp(C[randomRow, randomCollumn] + Random.Range(-1f, 1f), -1f, 1f);
            
        }

        return C;
    }


    private void Crossover(NeuralNetwork[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i ++)
        {
            int AIndex = i;
            int BIndex = i+1;

            if (genePool.Count >= 1)
            {
                for (int l = 0; l < 100; l++)
                {
                    AIndex = genePool[Random.Range(0, genePool.Count)];
                    BIndex = genePool[Random.Range(0, genePool.Count)];

                    if (AIndex != BIndex)
                        break;

                }
            }

            NeuralNetwork Child1 = new NeuralNetwork();
            NeuralNetwork Child2= new NeuralNetwork();
            Child1.Initialise(controller.LAYERS, controller.NEURONS);
            Child2.Initialise(controller.LAYERS, controller.NEURONS);
            Child1.fitness = 0;
            Child2.fitness = 0;

            for (int w = 0; w < Child1.weights.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.weights[w] = population[AIndex].weights[w];
                    Child2.weights[w] = population[BIndex].weights[w];

                }
                else
                {
                    Child2.weights[w] = population[AIndex].weights[w];
                    Child1.weights[w] = population[BIndex].weights[w];
                }
                
            }
            
            for (int w = 0; w < Child1.biases.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.biases[w] = population[AIndex].biases[w];
                    Child2.biases[w] = population[BIndex].biases[w];

                }
                else
                {
                    Child2.biases[w] = population[AIndex].biases[w];
                    Child1.biases[w] = population[BIndex].biases[w];
                }
                
            }

            newPopulation[naturallySelected] = Child1;
            
            newPopulation[naturallySelected] = Child2;
            naturallySelected += 2;

        }
    }


    private NeuralNetwork[] pickBestPopulation()
    {
        NeuralNetwork[] newPopulation = new NeuralNetwork[initialPopulation];
        for (int i = 0; i < bestAgentSelection; ++i)
        {
            newPopulation[naturallySelected] = population[i].InitialsieCopy(controller.LAYERS, controller.NEURONS);
            newPopulation[naturallySelected].fitness = 0;
            
            naturallySelected++;
            int f = Mathf.RoundToInt(population[i].fitness * 10);
            for (int j = 0; j < f++; j++)
            {
                genePool.Add(i);
                
            }
            
            
        }

        for (int i = 0; i < worstAgentSelection; i++)
        {
            int last = population.Length - 1;
            last -= i;
            int f = Mathf.RoundToInt(population[last].fitness * 10);
            for (int j = 0; j < f++; j++)
            {
                genePool.Add(last);
                
            }
        }

        return newPopulation;
        

    }

    /*private void SortPopulation()
    {
        for (int i = 0; i < population.Length; i++)
        {
            for (int j = i; j < population.Length; j++)
            {
                if (population[i].fitness < population[j].fitness)
                {
                    NeuralNetwork temp = population[i];
                    population[i] = population[j];
                    population[j] = temp;
                }
            }
        }

    }*/
    private static void Quick_Sort(NeuralNetwork[] arr, int left, int right) 
    {
        if (left < right)
        {
            int pivot = Partition(arr, left, right);

            if (pivot > 1) {
                Quick_Sort(arr, left, pivot - 1);
            }
            if (pivot + 1 < right) {
                Quick_Sort(arr, pivot + 1, right);
            }
        }
        
    }

    private static int Partition(NeuralNetwork[] arr, int left, int right)
    {
        float pivot = arr[left].fitness;
        while (true) 
        {

            while (arr[left].fitness < pivot) 
            {
                left++;
            }

            while (arr[right].fitness > pivot)
            {
                right--;
            }

            if (left < right)
            {
                if (arr[left] == arr[right]) return right;

                NeuralNetwork temp = arr[left];
                arr[left] = arr[right];
                arr[right] = temp;


            }
            else 
            {
                return right;
            }
        }
    }

    
    
    

    private void SortPopulation()
    {
        Quick_Sort(population, 0, population.Length);
    }
}
