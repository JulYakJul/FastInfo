using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TTSListener : AndroidJavaProxy
{
    public TTSListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
    void onInit(int status)
    {
        Debug.Log("TTS Initialized: " + (status == 0 ? "Success" : "Failed"));
    }
}

