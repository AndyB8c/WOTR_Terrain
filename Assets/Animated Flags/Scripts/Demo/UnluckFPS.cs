using UnityEngine;
using System;


public class UnluckFPS:MonoBehaviour
{
    public TextMesh _textMesh;
    public float updateInterval = 0.5f;
    float accum = 0.0f;
    int frames = 0;
    float timeleft;
    
    public void Start()
    {
        timeleft = updateInterval;  
        _textMesh = transform.GetComponent<TextMesh>();
      
    }
    
    public void Update()
    {
       timeleft -= Time.deltaTime;
        accum += Time.timeScale/Time.deltaTime;
        ++frames;
        if( timeleft <= 0.0f )
        {
          	_textMesh.text = "FPS " + (accum/frames).ToString("f2");
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
            
        }
    }
}