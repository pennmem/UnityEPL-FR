using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FlyPath
{
    [SerializeField] public GameObject[] flyPoints;
}

public class FlyPaths : MonoBehaviour
{
    public GameObject flier;
    public float speed = 1f;
    public List<FlyPath> flyPaths;

    private bool stopFlight = false;

    void Start()
    {
        string participantCode = UnityEPL.GetParticipants()[0];
        System.Random reliable_random = new System.Random(participantCode.GetHashCode());
        List<FlyPath> nonpracticeFlyPaths = new List<FlyPath>(flyPaths.GetRange(1, flyPaths.Count - 1));
        nonpracticeFlyPaths.Shuffle(reliable_random);
        flyPaths.RemoveRange(1, flyPaths.Count - 1);
        flyPaths.AddRange(nonpracticeFlyPaths);
    }

    void OnEnable()
    {
        EditableExperiment.OnStateChange += OnStateChange;
    }

    void OnDisable()
    {
        EditableExperiment.OnStateChange -= OnStateChange;
    }

    void OnStateChange(string stateName, bool on, Dictionary<string, object> extraData)
    {
        if (stateName.Equals("ENCODING") && on)
        {
            int current_trial = (int)extraData["current_trial"];
            BeginFlight(current_trial);
        }
        if (stateName.Equals("ENCODING") && !on)
            StopFlight();
    }

    public void BeginFlight(int pathIndex)
    {
        Debug.Log("Beginning flight #" + pathIndex.ToString());
        stopFlight = false;
        StartCoroutine(DoFlight(pathIndex));
    }

    public void StopFlight()
    {
        stopFlight = true;
    }

    private IEnumerator DoFlight(int pathIndex)
    {
        if (pathIndex < 0 || pathIndex >= flyPaths.Count)
            throw new UnityException("That path index doesn't exist.");

        FlyPath flyPath = flyPaths[pathIndex];

        if (flyPath.flyPoints.Length < 2)
            throw new UnityException("That path has fewer than two points.");


        ///////////////Find the distance after each point on the path (not including the last point of course)
        float[] distancesAfterPoints = new float[flyPath.flyPoints.Length - 1];

        Vector3 originPoint = flyPath.flyPoints[0].transform.position;
        Vector3 destinationPoint;
        for (int i = 1; i < flyPath.flyPoints.Length; i++)
        {
            destinationPoint = flyPath.flyPoints[i].transform.position;
            float distanceAfterPoint = Vector3.Distance(originPoint, destinationPoint);
            distancesAfterPoints[i - 1] = distanceAfterPoint;
            originPoint = destinationPoint;
        }


        //////////////Fly along at a constant speed
        Vector3 firstPoint = flyPath.flyPoints[0].transform.position;
        Vector3 lastPoint = flyPath.flyPoints[flyPath.flyPoints.Length - 1].transform.position;
        flier.transform.position = firstPoint;
        flier.transform.LookAt(lastPoint);

        int lastPassedPointIndex = 0;
        float distanceTraveledSinceLastPoint = 0;

        while (!stopFlight)
        {
            Vector3 directionToNextPoint;
            if (lastPassedPointIndex < flyPath.flyPoints.Length - 1)
                directionToNextPoint = Vector3.Normalize(flyPath.flyPoints[lastPassedPointIndex + 1].transform.position - flyPath.flyPoints[lastPassedPointIndex].transform.position);
            else
                directionToNextPoint = flier.transform.forward;

            flier.transform.position = flier.transform.position + directionToNextPoint * speed * Time.deltaTime;

            distanceTraveledSinceLastPoint = Vector3.Distance(flyPath.flyPoints[lastPassedPointIndex].transform.position, flier.transform.position);
            if (lastPassedPointIndex < flyPath.flyPoints.Length - 1 && distanceTraveledSinceLastPoint > distancesAfterPoints[lastPassedPointIndex])
            {
                lastPassedPointIndex++;
                distanceTraveledSinceLastPoint = 0f;
            }

            yield return null;
        }
        Debug.Log("Flight over");
    }
}

static class MyExtensions
{
    public static void Shuffle<T>(this IList<T> list, System.Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}