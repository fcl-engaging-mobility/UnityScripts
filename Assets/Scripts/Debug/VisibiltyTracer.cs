using UnityEngine;

public class VisibiltyTracer : MonoBehaviour
{
    public float xDegrees = 90;
    public float yDegrees = 50;

    public int xSamples = 40;
    public int ySamples = 10;

    public float maxDistance = 300;

    void Update()
    {
        //RaycastHit hit;
        //if (Physics.Raycast(transform.position, transform.forward, out hit, 200))
        //    Debug.DrawLine(transform.position, hit.point, Color.green);
    }

    void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawLine(Vector3.zero, new Vector3(1000, 0, 0));
        //Debug.DrawLine(transform.position, transform.position + transform.forward * 300, Color.red);
        RaycastHit hit;
        float xOffset = xSamples == 1? 0 : xDegrees / (xSamples-1);
        float yOffset = ySamples == 1? 0 : yDegrees / (ySamples-1);
        float xReset = xSamples == 1 ? 0 : -xDegrees * 0.5f;
        float yReset = ySamples == 1 ? 0 : -yDegrees * 0.5f;
        float x = xReset;
        Quaternion q = transform.rotation;
        for (int i = 0; i < xSamples; i++)
        {
            float y = yReset;
            for (int j = 0; j < ySamples; j++)
            {
                Vector3 forward = (q * Quaternion.Euler(y, x, 0)) * Vector3.forward;
                if (Physics.Raycast(transform.position, forward, out hit, maxDistance))
                    Debug.DrawLine(transform.position, hit.point, Color.green);
                else
                    Debug.DrawLine(transform.position, transform.position + forward * maxDistance, Color.green);
                y += yOffset;
            }
            x += xOffset;
        }

    }
}
