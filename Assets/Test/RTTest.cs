using System.Collections;
using System.Collections.Generic;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Demo;
using UnityEngine;

public class RTTest : MonoBehaviour
{
    public string str;
    // Start is called before the first frame update
    void Start()
    {
        Speaker.SpeakNative(str, Speaker.VoiceForCulture("cn"), 1, GUISpeech.Volume, GUISpeech.Pitch);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
