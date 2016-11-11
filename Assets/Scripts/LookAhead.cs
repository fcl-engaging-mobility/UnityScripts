using UnityEngine;

public class LookAhead : MonoBehaviour
{
    public float lookAheadTime = 2f;
    public SimulationElement simulationElement;

    int id = 0;
    Vector3 lookAtPosition;
    TrafficManager trafficManager;
    Vector3 offset = Vector3.zero;
    Vector3 newPos = Vector3.zero;
    Quaternion rotation;

    void Start()
    {
        id = simulationElement.ID;
        trafficManager = simulationElement.GetComponentInParent<TrafficManager>();
        offset = trafficManager.transform.position;
        trafficManager.GetPosition(id, (trafficManager.timeController.time + lookAheadTime) % trafficManager.AnimationLength, ref lookAtPosition);
        lookAtPosition += offset;
        rotation = transform.rotation;
    }
	
	void Update()
    {
        if (trafficManager.GetPosition(id, (trafficManager.timeController.time + lookAheadTime) % trafficManager.AnimationLength, ref newPos))
        {
            //lookAtPosition = newPos + offset;
            lookAtPosition = Vector3.Lerp(lookAtPosition, newPos + offset, 0.04f);
            Vector3 dir = lookAtPosition - transform.position;
            if (dir.sqrMagnitude > 0.01f)
            {
                rotation = Quaternion.Lerp(rotation, Quaternion.LookRotation(dir, Vector3.up), 0.04f);
                transform.rotation = rotation;
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, 0.02f);
            }
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, 0.02f);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(lookAtPosition, 0.4f);
    }

}
