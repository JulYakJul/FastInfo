using UnityEngine;
using System.Collections;
using UnityEngine.Android; // Для запроса разрешений

public class AndroidTTS : MonoBehaviour
{
    private static AndroidJavaObject tts = null;
    
    void Start()
    {
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
        
        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", context, new TTSListener());
        #endif
    }
    
    public void Speak(string text)
    {
        #if UNITY_ANDROID
        if (tts != null)
        {
            tts.Call("setLanguage", GetLocale("ru")); // Для русского
            tts.Call("speak", text, 0, null, "utteranceId");
        }
        #endif
    }
    
    private AndroidJavaObject GetLocale(string languageCode)
    {
        AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale");
        return localeClass.CallStatic<AndroidJavaObject>("getDefault");
    }
    
    class TTSListener : AndroidJavaProxy
    {
        public TTSListener() : base("android.speech.tts.TextToSpeech$OnInitListener") {}
        void onInit(int status)
        {
            Debug.Log("TTS Initialized: " + (status == 0 ? "Success" : "Failed"));
        }
    }
}