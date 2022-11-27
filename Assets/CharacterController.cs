using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NeuralNetwork))]

public class CharacterController : MonoBehaviour
{
    private Vector3 _startPosition, _startRotation;
    private NeuralNetwork _network;

    [Range(-1f,1f)]
    //Two results of the algorithm
    public float acceleration,rotation;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
     //The fitness is determined with the speed, distance and the deviation to the center of the road
    public float overallFitness;
    public float distanceMultipler = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;

    [Header("Network Options")]
    //This must be ajusted, too many layers or neurones, can make the program lag
    public int LAYERS = 1;
    public int NEURONS = 10;
    //this position is used to calculate the distance the character has done
    private Vector3 _lastPosition;
    private float _totalDistanceTravelled;
    private float _avgSpeed;
    private Vector3 _inp;

    //This 3 sensors are A : right, B : forward, C : left
    private float _aSensor,_bSensor,_cSensor;

    private void Awake() {
        _startPosition = transform.position;
        _startRotation = transform.eulerAngles;
        _network = GetComponent<NeuralNetwork>();
    }

    public void ResetWithNetwork (NeuralNetwork net)
    {
        _network = net;
        Reset();
    }

    
    /// <summary>This function creates a new genome</summary>
    public void Reset() {

        timeSinceStart = 0f;
        _totalDistanceTravelled = 0f;
        _avgSpeed = 0f;
        _lastPosition = _startPosition;
        overallFitness = 0f;
        transform.position = _startPosition;
        transform.eulerAngles = _startRotation;
    }
    /// <summary>This function is called on collision with the limits of the roads, it'll kill and reset the character.</summary>
  
    private void OnCollisionEnter (Collision collision) {
        Death();
    }

    private void FixedUpdate() {
        //this update Will make move the character and save all the datas we need to update the neural network
        InputSensors();
        _lastPosition = transform.position;
        (acceleration, rotation) = _network.RunNetwork(_aSensor, _bSensor, _cSensor);
        MoveCharacter(acceleration,rotation);
        timeSinceStart += Time.deltaTime;
        CalculateFitness();

    }
    /// <summary>Called on death, will save the datas of the deceased character in the Genetic Manager.</summary>
    private void Death ()
    {
        GameObject.FindObjectOfType<GeneticManager>().Death(overallFitness, _network);
    }
    
    /// <summary> Called to update the fitness.</summary>
    private void CalculateFitness() {
        
        _totalDistanceTravelled += Vector3.Distance(transform.position,_lastPosition);
        _avgSpeed = _totalDistanceTravelled/timeSinceStart;
        //the fitness is determined with the speed, the distance and the deviation to the road
       overallFitness = (_totalDistanceTravelled*distanceMultipler)+(_avgSpeed*avgSpeedMultiplier)+(((_aSensor+_bSensor+_cSensor)/3)*sensorMultiplier);
        //Will the car if it does'nt move enough
        if (timeSinceStart > 20 && overallFitness < 40) {
            Death();
        }
        //Will kill the car if it made some turns around the circuit
        if (overallFitness >= 1000) {
        //reaching this place mean having the best Neural Network possible
            Death();
        //Could save the datas in a Json for future load
        }

    }
    /// <summary>Will update the position of the 3 sensors.</summary>
    private void InputSensors() {
    

        Vector3 rightSensor = (transform.forward+transform.right);
        Vector3 centerSensor = (transform.forward);
        Vector3 leftSensor = (transform.forward-transform.right);

        Ray r = new Ray(transform.position,rightSensor);
        RaycastHit hit;
        //Cast a ray that will meet the circuit's borders and return the distance
        if (Physics.Raycast(r, out hit)) {
            _aSensor = hit.distance/20;
            //Displays the sensor on debug mode
            Debug.DrawLine(r.origin, hit.point, Color.blue);
        }

        r.direction = centerSensor;

        if (Physics.Raycast(r, out hit)) {
            _bSensor = hit.distance/20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = leftSensor;

        if (Physics.Raycast(r, out hit)) {
            _cSensor = hit.distance/20;
            Debug.DrawLine(r.origin, hit.point, Color.yellow);
        }

    }
    
    /// <summary>Update the position of the character.</summary>
    /// <param name="v">Speed of the character</param>
    /// <param name="h">Direction of the character</param>
    public void MoveCharacter (float v, float h) {
        //Watch out for the values, they have to be low
        _inp = Vector3.Lerp(Vector3.zero,new Vector3(0,0,v*11.4f),0.02f);
        _inp = transform.TransformDirection(_inp);
        transform.position += _inp;

        transform.eulerAngles += new Vector3(0, (h*90)*0.02f,0);
    }

}
