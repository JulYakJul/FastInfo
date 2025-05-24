using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using TMPro;
using System;
using System.Threading.Tasks;
using LeastSquares.Overtone;
using Assets.Overtone.Scripts;

[System.Serializable]
public class RequestData
{
    public string text;
    public string prompt;
}

[System.Serializable]
public class ResponseData
{
    public string response;
    public string error;
}

public class TextProcessor : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text fileNameDisplay;
    public TMP_InputField promptInput;
    public TMP_Text resultOutput;
    public Button processButton;
    public Button loadFileButton;

    [Header("TTS Settings")]
    public AudioSource audioSource;
    public TTSPlayer ttsPlayer;

    private string serverUrl = "https://fastinfo.cloudpub.ru/process";
    private string loadedFileContent;
    private bool isSpeaking = false;

    [Header("Speed Controls")]
    public Button speed1xButton;
    public Button speed1_5xButton;
    public Button speed2xButton;

    private float currentSpeed = 1.0f;

    void Start()
    {
        processButton.onClick.AddListener(ProcessText);
        loadFileButton.onClick.AddListener(LoadFile);
        fileNameDisplay.text = "Файл не выбран";
        resultOutput.text = "";

        speed1xButton.onClick.AddListener(() => SetPlaybackSpeed(1.0f));
        speed1_5xButton.onClick.AddListener(() => SetPlaybackSpeed(1.5f));
        speed2xButton.onClick.AddListener(() => SetPlaybackSpeed(2.0f));

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (ttsPlayer == null)
        {
            Debug.LogError("TTSPlayer не назначен!");
        }
    }

    void ProcessText()
    {
        if (isSpeaking)
        {
            StopSpeaking();
            return;
        }

        resultOutput.text = "";

        if (string.IsNullOrEmpty(loadedFileContent))
        {
            Debug.LogWarning("Файл не загружен!");
            return;
        }

        string prompt = promptInput.text;
        StartCoroutine(ProcessTextCoroutine(loadedFileContent, prompt));
    }

    public void SetPlaybackSpeed(float speed)
    {
        currentSpeed = speed;
        if (audioSource.isPlaying)
        {
            audioSource.pitch = speed;
        }

        // Визуальная обратная связь
        UpdateSpeedButtonsUI();
    }

    private void UpdateSpeedButtonsUI()
    {
        speed1xButton.interactable = Math.Abs(currentSpeed - 1.0f) > 0.01f;
        speed1_5xButton.interactable = Math.Abs(currentSpeed - 1.5f) > 0.01f;
        speed2xButton.interactable = Math.Abs(currentSpeed - 2.0f) > 0.01f;
    }

    IEnumerator ProcessTextCoroutine(string text, string prompt)
    {
        int chunkSize = 2000;
        int chunksCount = Mathf.CeilToInt(text.Length / (float)chunkSize);

        for (int i = 0; i < chunksCount; i++)
        {
            int start = i * chunkSize;
            int length = Mathf.Min(chunkSize, text.Length - start);
            string chunk = text.Substring(start, length);

            yield return StartCoroutine(SendAndSpeakChunk(chunk, prompt));
        }
    }

    IEnumerator SendAndSpeakChunk(string textChunk, string prompt)
    {
        RequestData requestData = new RequestData { text = textChunk, prompt = prompt };
        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 300;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Ошибка: {www.error}");
                resultOutput.text = $"Ошибка: {www.error}";
                yield break;
            }

            ResponseData response = null;

            try
            {
                response = JsonUtility.FromJson<ResponseData>(www.downloadHandler.text);
                resultOutput.text += response.response + "\n\n";
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка парсинга ответа: {e.Message}");
                yield break;
            }

            yield return StartCoroutine(SpeakResponse(response.response));
        }
    }

    IEnumerator SpeakResponse(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        isSpeaking = true;

        var speakTask = ttsPlayer.Speak(text);
        while (!speakTask.IsCompleted)
            yield return null;

        // Ждать, пока проиграется звук
        yield return new WaitWhile(() => audioSource.isPlaying);

        isSpeaking = false;
    }

    public void StopSpeaking()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        isSpeaking = false;
    }

    void LoadFile()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        NativeFilePicker.PickFile((path) =>
        {
            if (path == null) return;
            StartCoroutine(ReadFileContent(path));
        }, new[] { "text/plain", "application/pdf" });
#else
        string path = UnityEditor.EditorUtility.OpenFilePanel("Выберите файл", "", "txt,pdf");
        if (!string.IsNullOrEmpty(path))
        {
            StartCoroutine(ReadFileContent(path));
        }
#endif
    }

    IEnumerator ReadFileContent(string filePath)
    {
        fileNameDisplay.text = Path.GetFileName(filePath);

        try
        {
            loadedFileContent = File.ReadAllText(filePath);
            Debug.Log($"Файл загружен. Размер: {loadedFileContent.Length} символов");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка чтения: {e.Message}");
            loadedFileContent = null;
        }

        yield return null;
    }
}
