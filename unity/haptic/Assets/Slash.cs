using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
public class Slash : MonoBehaviour {
    public string portName = "COM4";
    public int baudRate = 9600;
    private SerialPort serialPort_;
    int count = 0;
    // Use this for initialization
    void Start () {
        serialPort_ = new SerialPort(portName, baudRate);
        serialPort_.Open();

    }
    void air()
    {
        try
        {
            serialPort_.Write("l");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
    // Update is called once per frame
    float timee = 0;
    bool go = false;
    void Update()
    {
        timee += Time.deltaTime;
        if (Input.GetMouseButtonDown(0))
        {
            AudioSource audioSource= this.GetComponent<AudioSource>();
            audioSource.Play();
            go = true;
            count = 10;
            timee = 0;
        }
        if (go && timee>3)
        {
            go = false;
                air();
        }
      
    }
}
