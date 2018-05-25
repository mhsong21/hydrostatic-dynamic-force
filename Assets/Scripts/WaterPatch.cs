using UnityEngine;
using System.Collections;

public class WaterPatch : MonoBehaviour 
{
    public static WaterPatch instance;

    public bool isMoving;

    //Wave height and speed
    public float scale = 0.1f;
    public float speed = 1.0f;
    //The width between the waves
    public float waveDistance = 1f;
    //Noise parameters
    public float noiseStrength = 1f;
    public float noiseWalk = 1f;

    void Awake()
    {
        instance = this;
    }

    public float GetWaveYPos(Vector3 position, float timeSinceStart)
    {
        //if (isMoving)
        //{
            //return WaveTypes.SinXWave(position, speed, scale, waveDistance, noiseStrength, noiseWalk, timeSinceStart);
        //}
        //else
        //{
            //return 0f;
        //}
		
		return 0f;
    }

    public float DistanceToWater(Vector3 position, float timeSinceStart)
    {
        float waterHeight = GetWaveYPos(position, timeSinceStart);

        float distanceToWater = position.y - waterHeight;

        return distanceToWater;
    }
}