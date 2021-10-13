using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private float runningTime;
    public float speed;
    public float length;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        runningTime += Time.deltaTime * speed;
        var yPos = Mathf.Sin(runningTime) * length;
        transform.position += new Vector3(0, yPos);
    }
}
