using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common;



public enum SoundChannel
{
    BGM,
    SOUND
}


public class SoundManager : MonoSingleton<SoundManager>
{

    public AudioSource[] Channel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
