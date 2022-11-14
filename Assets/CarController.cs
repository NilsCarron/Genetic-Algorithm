using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

[RequireComponent(typeof(NeuralNetwork))]
public class CarController : MonoBehaviour
{
    private Vector3 startPosition, startRotation;
    private NeuralNetwork network;

    [Header("Network Options")] 
    public int LAYERS = 1;
    public int NEURONS = 10;
    
    
    [Range(-1f, 1f)] public float a, t;
    public float timeSinceStart = 0f;

    [Header("Fitness")] public float overAllFitness;
    public float distanceMultiplier = 1.4f;//farer
    public float avgSpeedMultiplier = 0.2f;//faster
    public float sensorMultiplier;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;
    private float aSensor, bSensor, cSensor;
    private Vector3 input;

    private void Awake()
    {
        
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        network = GetComponent<NeuralNetwork>();

        
        
        network.Initialise(LAYERS, NEURONS);
    }


    private void FixedUpdate()
    {
        InputSensorts();
        lastPosition = transform.position;
        
        //neural network code
        (a, t) = network.RunNetwork(aSensor, bSensor, cSensor);
        
        
        
        MoveCar(a,t);
        timeSinceStart += Time.deltaTime;
        CalculateFitness();
        //a = 0;
        //t = 0;
    }

    public void Reset()
    {
        
        network.Initialise(LAYERS, NEURONS);
        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overAllFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;

    }

    public void ResetWithNetwork(NeuralNetwork net)
    {
        network = net;
        Reset();
    }

    private void Death()
    {
        GameObject.FindObjectOfType<GeneticManager>().Death(overAllFitness, network);
    }
    private void OnCollisionEnter(Collision collision)
    {
        Death();
    }


    private void CalculateFitness()
    {
        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled / timeSinceStart;

        overAllFitness = totalDistanceTravelled * distanceMultiplier + avgSpeed * avgSpeedMultiplier +(((aSensor+bSensor+cSensor)/3)*sensorMultiplier);


        if (timeSinceStart > 20 && overAllFitness < 40)
        {
            Death();

        }

        
        if (overAllFitness >= 1000)
        {
            //Save in json
            Death();

        }
        
    }
    
    private void InputSensorts()
    {
        Vector3 a = (transform.forward + transform.right);
        Vector3 b = (transform.forward);
        Vector3 c = (transform.forward - transform.right);

        Ray r = new Ray(transform.position, a);
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, Mathf.Infinity, 3))
        {
            aSensor = hit.distance / 30;
            Debug.DrawRay(r.origin, hit.point, Color.red);
            
        }
        r.direction = b;
        if (Physics.Raycast(r, out hit))
        {
            bSensor = hit.distance / 30;
            Debug.DrawRay(r.origin, hit.point, Color.blue);

        }
        r.direction = c;
        if (Physics.Raycast(r, out hit))
        {
            cSensor = hit.distance / 30;
            Debug.DrawRay(r.origin, hit.point, Color.green);

        }


    }
    
    
    public void MoveCar(float v, float h)
    {
        input = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, v * 11.4f), 0.02f);
        input = transform.TransformDirection(input);
        transform.position += input;
        transform.eulerAngles += new Vector3(0, h * 90 * 0.02f, 0f);
        
    }
    
}
