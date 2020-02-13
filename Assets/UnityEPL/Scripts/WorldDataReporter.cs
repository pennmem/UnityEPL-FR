using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("UnityEPL/Reporters/World Data Reporter")]
public class WorldDataReporter : DataReporter
{

    public bool reportView = true;

    public bool isStatic = true;
    public bool doSpawnReport = true;

    public int framesPerReport = 60;

    private int offset;
    BoxCollider objectCollider;

    void Awake() {
        offset = (int)Random.Range(0, framesPerReport / 2);
    }

    void Update()
    {
        if (!isStatic) CheckTransformReport();
    }

    void BoxCheck()
    {
        if (reportView && GetComponent<BoxCollider>() == null)
        {
            reportView = false;
            throw new UnityException("You have selected enter/exit viewfield reporting for " + gameObject.name + " but there is no box collider on the object." +
                                      "  This feature uses collision detection to compare with camera bounds and other objects.  Please add a collider or " +
                                      "unselect viewfield enter/exit reporting.");
        }
        objectCollider = gameObject.GetComponent<BoxCollider>();
    }

    protected override void OnEnable() {
        base.OnEnable();
        BoxCheck();
        if(doSpawnReport)
            DoSpawnReport();
    }

    protected override void OnDisable() {
        if(doSpawnReport)
            DoDespawnReport();
    }

    // TODO: gather data in single function, use wrapper to set event type

    public void DoTransformReport(System.Collections.Generic.Dictionary<string, object> extraData = null)
    {
        if (extraData == null)
            extraData = new Dictionary<string, object>();
        System.Collections.Generic.Dictionary<string, object> transformDict = new System.Collections.Generic.Dictionary<string, object>(extraData);
        transformDict.Add("positionX", transform.position.x);
        transformDict.Add("positionY", transform.position.y);
        transformDict.Add("positionZ", transform.position.z);

        transformDict.Add("rotationX", transform.rotation.eulerAngles.x);
        transformDict.Add("rotationY", transform.rotation.eulerAngles.y);
        transformDict.Add("rotationZ", transform.rotation.eulerAngles.z);

        transformDict.Add("reportID", reportingID);
        transformDict.Add("objectName", gameObject.name);
        eventQueue.Enqueue(new DataPoint(gameObject.name + "Transform", TimeStamp(), transformDict));
    }

    private void CheckTransformReport()
    {
        if ((Time.frameCount + offset) % framesPerReport == 0)
        {
            DoTransformReport();
        }
    }

    private void DoSpawnReport() {
        System.Collections.Generic.Dictionary<string, object> transformDict = new System.Collections.Generic.Dictionary<string, object>();
        transformDict.Add("positionX", transform.position.x);
        transformDict.Add("positionY", transform.position.y);
        transformDict.Add("positionZ", transform.position.z);

        transformDict.Add("rotationX", transform.rotation.eulerAngles.x);
        transformDict.Add("rotationY", transform.rotation.eulerAngles.y);
        transformDict.Add("rotationZ", transform.rotation.eulerAngles.z);

        transformDict.Add("reportID", reportingID);
        transformDict.Add("objectName", gameObject.name);
        eventQueue.Enqueue(new DataPoint(gameObject.name + "Spawn", TimeStamp(), transformDict));
    }
    
    private void DoDespawnReport() {
        System.Collections.Generic.Dictionary<string, object> transformDict = new System.Collections.Generic.Dictionary<string, object>();
        transformDict.Add("reportID", reportingID);
        transformDict.Add("objectName", gameObject.name);
        eventQueue.Enqueue(new DataPoint(gameObject.name + "Despawn", TimeStamp(), transformDict));
    }

    private Vector3[] GetColliderVertexPositions(BoxCollider boxCollider) {
        Vector3[] vertices = new Vector3[9];

        Vector3 colliderCenter  = boxCollider.center;
        Vector3 colliderExtents = boxCollider.size/2.0f;
        Vector3 pointOffset = new Vector3(.02f, .02f, .02f);

        for (int i = 0; i < 8; i++)
        {
            Vector3 extents = colliderExtents;
            Vector3 offset = pointOffset;
            extents.Scale(new Vector3((i & 1) == 0 ? 1 : -1, (i & 2) == 0 ? 1 : -1, (i & 4) == 0 ? 1 : -1));
            offset.Scale(new Vector3((i & 1) == 0 ? 1 : -1, (i & 2) == 0 ? 1 : -1, (i & 4) == 0 ? 1 : -1));

            Vector3 vertexPosLocal = colliderCenter + extents - offset;

            Vector3 vertexPosGlobal = boxCollider.transform.TransformPoint(vertexPosLocal);

            // display vector3 to six decimal places
            vertices[i] = vertexPosGlobal;
        }
        vertices[8] = boxCollider.transform.TransformPoint(colliderCenter);
        
        return vertices;
    }
}