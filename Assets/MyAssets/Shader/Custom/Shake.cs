using UnityEngine;

public class Shake : MonoBehaviour
{
    [SerializeField] float posSpeed = 1f;
    [SerializeField] float rotSpeed = 1f;
    [SerializeField] float scaleSpeed = 1f;
    [SerializeField] float posAmount = 1f;
    [SerializeField] float rotAmount = 1f;
    [SerializeField, Range(0f, 1f)] float scaleAmount = 1f;

    Vector3 initialPos;
    Vector3 initialRot;
    Vector3 initialScale;
    float xPosDiff;
    float yPosDiff;
    float zRotDiff;
    float xScaleDiff;
    float yScaleDiff;

    void Start()
    {
        initialPos = transform.position;
        initialRot = transform.rotation.eulerAngles;
        initialScale = transform.localScale;
        xPosDiff = Random.Range(-1f, 1f) * Mathf.PI * 2f;
        yPosDiff = Random.Range(-1f, 1f) * Mathf.PI * 2f;
        zRotDiff = Random.Range(-1f, 1f) * Mathf.PI * 2f;
        xScaleDiff = Random.Range(-1f, 1f) * Mathf.PI * 2f;
        yScaleDiff = Random.Range(-1f, 1f) * Mathf.PI * 2f;
    }

    void Update()
    {
        float posTime = Time.time * posSpeed;
        float rotTime = Time.time * rotSpeed;
        float scaleTime = Time.time * scaleSpeed;


        float xPos = Mathf.Sin(posTime + xPosDiff);
        float yPos = Mathf.Sin(posTime + yPosDiff);
        Vector3 pos = new Vector3(xPos, yPos, 0f) * posAmount;
        transform.position = initialPos + pos;

        float zRot = Mathf.Sin(rotTime + zRotDiff);
        Vector3 rot = initialRot + (new Vector3(0f, 0f, zRot) * rotAmount);
        transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);

        float xScale = Mathf.Sin(scaleTime + xScaleDiff) * scaleAmount;
        float yScale = Mathf.Sin(scaleTime + yScaleDiff) * scaleAmount;
        Vector3 scale = new Vector3(initialScale.x + xScale, initialScale.y + yScale, 1f);
        transform.localScale = scale;
    }
}

