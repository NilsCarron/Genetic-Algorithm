using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;

public class GeneticManager : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;

    [Header("Controls")]
    //The amount of characters per generation
    public int initialPopulation = 85;
    [Range(0.0f, 1.0f)]
    //The higher, the less the character takes care of the last generation
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")]
    //This values can be tweaked
    //how much of the best genome we need to create the crossover
    public int bestAgentSelection = 8;
    //how much of the worst genome we need to create the crossover
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> _genePool = new List<int>();

    private int _naturallySelected;

    private NeuralNetwork[] _population;

    [Header("Public View")]
    //Only gives informations about the current character
    public int currentGeneration;
    public int currentGenome = 0;

    private void Start()
    {
        CreatePopulation();
    }

    
    /// <summary>This method creates the first population with random values.</summary>
    private void CreatePopulation()
    {
        _population = new NeuralNetwork[initialPopulation];
        FillPopulationWithRandomValues(_population, 0);
        ResetToCurrentGenome();
    }

    /// <summary>This will give the character its corresponding network. Called on creating the character</summary>
    private void ResetToCurrentGenome()
    {
        controller.ResetWithNetwork(_population[currentGenome]);
    }
    /// <summary>Will be called upon generating random values in the networks.</summary>
    /// <param name="newPopulation">Newly created neural network</param>
    /// <param name="startingIndex">Amount of character that won't be randomized (the naturally selected ones)</param>
    private void FillPopulationWithRandomValues (NeuralNetwork[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NeuralNetwork();
            newPopulation[startingIndex].Initialise(controller.LAYERS, controller.NEURONS);
            startingIndex++;
        }
    }
    /// <summary>called upon destruction to collect all the informations.</summary>
    /// <param name="fitness">The "score" the character made before its death </param>
    /// <param name="network">The neural network of the deceased character</param>
    public void Death (float fitness, NeuralNetwork network)
    {
        
        if (currentGenome < _population.Length -1)
        {

            _population[currentGenome].fitness = fitness;
            currentGenome++;
            ResetToCurrentGenome();

        }
        //no more genomes remaining in the generation, we create a new one
        else
        {
            RePopulate();
        }

    }

    
    
    /// <summary>Called upon the death of the last genome of the generation.</summary>
    private void RePopulate()
    {
        //remove all genomes of the current pool
        _genePool.Clear();
        currentGeneration++;
        _naturallySelected = 0;
        //sorting the deceased populations per their fitness
        SortPopulation();
        //We take the best population, identified thanks to the sorting previously done
        NeuralNetwork[] newPopulation = PickBestPopulation();
        
        //refill the pool
        Crossover(newPopulation);
        //mutate some characters in the generation, according to the mutation rate
        Mutate(newPopulation);
        
        //fill randomly, excepted for the naturally selected
        FillPopulationWithRandomValues(newPopulation, _naturallySelected);
        
        _population = newPopulation;

        currentGenome = 0;

        ResetToCurrentGenome();

    }

    /// <summary>Will fill the population with random values according to the mutation rate.</summary>
    /// <param name="newPopulation">Population in wich you want to pute mutations</param>
    private void Mutate (NeuralNetwork[] newPopulation)
    {

        for (int index = 0; index < _naturallySelected; index++)
        {

            for (int indexWeights = 0; indexWeights < newPopulation[index].weights.Count; indexWeights++)
            {

                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[index].weights[indexWeights] = MutateMatrix(newPopulation[index].weights[indexWeights]);
                }

            }

        }

    }
    
    /// <summary>This function randomize the weights of a matrix.</summary>
    /// <param name="matrixOfWeights">Forward left Raycast result</param>
    /// <returns name = "randomizedMatrix">The randomized matrix according to the mutation rate</returns>
    Matrix<float> MutateMatrix (Matrix<float> matrixOfWeights)
    {

        int randomPoints = Random.Range(1, (matrixOfWeights.RowCount * matrixOfWeights.ColumnCount) / 7);

        Matrix<float> randomizedMatrix = matrixOfWeights;

        for (int index = 0; index < randomPoints; index++)
        {
            int randomColumn = Random.Range(0, randomizedMatrix.ColumnCount);
            int randomRow = Random.Range(0, randomizedMatrix.RowCount);

            randomizedMatrix[randomRow, randomColumn] = Mathf.Clamp(randomizedMatrix[randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
        }

        return randomizedMatrix;

    }
    /// <summary>Called to refresh a generation with "childs" of the previous characters.</summary>
    /// <param name="newPopulation">the new population w ehave to create</param>
    private void Crossover (NeuralNetwork[] newPopulation)
    {
        //create random crossovers in the genepool
        for (int index = 0; index < numberToCrossover; index+=2)
        {
            int aIndex = index;
            int bIndex = index + 1;

            if (_genePool.Count >= 1)
            {
                for (int infiniteIndex = 0; infiniteIndex < 100; infiniteIndex++)
                    //we repeat it 100 times, to take 2 different parents (index A and B) 
                    //This is probably not the best way to do it, but it is quite simple to avoid most bugs
                {
                    aIndex = _genePool[Random.Range(0, _genePool.Count)];
                    bIndex = _genePool[Random.Range(0, _genePool.Count)];
                    
                    if (aIndex != bIndex)
                        break;
                }
            }
            //once we're here, we found 2 differents parents to realise the crossover
            //We generate two new characters called "Child1" and "Child2"
            NeuralNetwork child1 = new NeuralNetwork();
            NeuralNetwork child2 = new NeuralNetwork();
            child1.Initialise(controller.LAYERS, controller.NEURONS);
            child2.Initialise(controller.LAYERS, controller.NEURONS);
            child1.fitness = 0;
            child2.fitness = 0;

            //Iterating on the weights of the childs
            for (int indexOfChildWeights = 0; indexOfChildWeights < child1.weights.Count; indexOfChildWeights++)
            {
                //We add some more random with this coin flip to keep the child 1 or 2 in the position aIndex or bIndex
                //This is the crossover in itself for the  weights
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    child1.weights[indexOfChildWeights] = _population[aIndex].weights[indexOfChildWeights];
                    child2.weights[indexOfChildWeights] = _population[bIndex].weights[indexOfChildWeights];
                }
                else
                {
                    child2.weights[indexOfChildWeights] = _population[aIndex].weights[indexOfChildWeights];
                    child1.weights[indexOfChildWeights] = _population[bIndex].weights[indexOfChildWeights];
                }

            }

            //We do the same for the biases
            for (int indexOfChildBiases = 0; indexOfChildBiases < child1.biases.Count; indexOfChildBiases++)
            {

                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    child1.biases[indexOfChildBiases] = _population[aIndex].biases[indexOfChildBiases];
                    child2.biases[indexOfChildBiases] = _population[bIndex].biases[indexOfChildBiases];
                }
                else
                {
                    child2.biases[indexOfChildBiases] = _population[aIndex].biases[indexOfChildBiases];
                    child1.biases[indexOfChildBiases] = _population[bIndex].biases[indexOfChildBiases];
                }



            }
        
            newPopulation[_naturallySelected] = child1;
            _naturallySelected++;

            newPopulation[_naturallySelected] = child2;
            _naturallySelected++;
            //We add the two new childs
        }
    }

    
    
    /// <summary>This function will add the best and the worst characters in the future genepool.</summary>
    /// <returns name="newPopulation"> The new generation with the naturallyselected characters </returns>
    private NeuralNetwork[] PickBestPopulation()
    {
        //create new population
        NeuralNetwork[] newPopulation = new NeuralNetwork[initialPopulation];
        //Iterate on the number of best characters asked
        for (int indexBestToTake = 0; indexBestToTake < bestAgentSelection; indexBestToTake++)
        {
            
            newPopulation[_naturallySelected] = _population[indexBestToTake].InitialiseCopy(controller.LAYERS, controller.NEURONS);
            newPopulation[_naturallySelected].fitness = 0;
            _naturallySelected++;
            
            int fitnessToInerit = Mathf.RoundToInt(_population[indexBestToTake].fitness * 10);

            for (int indexOnFitness = 0; indexOnFitness < fitnessToInerit; indexOnFitness++)
            {
                _genePool.Add(indexBestToTake);
            }

        }
        //Iterate on the number of worst characters asked
        for (int indexWorstToTake = 0; indexWorstToTake < worstAgentSelection; indexWorstToTake++)
        {
            int last = _population.Length - 1;
            last -= indexWorstToTake;

            int fitnessToInerit = Mathf.RoundToInt(_population[last].fitness * 10);

            for (int indexOnFitness = 0; indexOnFitness < fitnessToInerit; indexOnFitness++)
            {
                _genePool.Add(last);
            }

        }

        return newPopulation;

    }

    /// <summary>.</summary>
    private void SortPopulation()
    {
        //Simple QuickSort
        SortArray(_population, 0, _population.Length - 1);
       

    }
    public NeuralNetwork[] SortArray(NeuralNetwork[] array, int leftIndex, int rightIndex)
    {
        var i = leftIndex;
        var j = rightIndex;
        var pivot = array[leftIndex].fitness;
        while (i <= j)
        {
            while (array[i].fitness < pivot)
            {
                i++;
            }
        
            while (array[j].fitness > pivot)
            {
                j--;
            }
            if (i <= j)
            {
                NeuralNetwork temp = array[i];
                array[i] = array[j];
                array[j] = temp;
                i++;
                j--;
            }
        }
    
        if (leftIndex < j)
            SortArray(array, leftIndex, j);
        if (i < rightIndex)
            SortArray(array, i, rightIndex);
        return array;
    }
    
}
