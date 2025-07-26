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
using System.Text.RegularExpressions;

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
        speed1_5xButton.onClick.AddListener(() => SetPlaybackSpeed(1.3f));
        speed2xButton.onClick.AddListener(() => SetPlaybackSpeed(1.5f));

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (ttsPlayer == null)
        {
            Debug.LogError("TTSPlayer не назначен!");
        }

        if (ttsPlayer == null)
        {
            Debug.LogError("TTSPlayer не назначен!");
            resultOutput.text = "Ошибка: TTSPlayer не настроен!";
        }
        else
        {
            Debug.Log("TTSPlayer инициализирован");
        }
    }

    void ProcessText()
    {
        if (isSpeaking)
        {
            StopSpeaking();
            return;
        }

        resultOutput.text = "Проверка соединения...\n";

        // Проверяем интернет-соединение
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            resultOutput.text += "Ошибка: Нет интернет-соединения\n";
            return;
        }

        string prompt = promptInput.text;

        if (string.IsNullOrEmpty(prompt))
        {
            resultOutput.text += "Ошибка: Введите промпт.\n";
            return;
        }

        StartCoroutine(SendRequestCoroutine(loadedFileContent, prompt));
    }

    IEnumerator SendRequestCoroutine(string fileContent, string prompt)
    {
        // Добавляем информацию о начале процесса
        resultOutput.text = "Подготовка запроса...\n";

        string textToSend = string.IsNullOrEmpty(fileContent) ? prompt : fileContent;
        string promptToSend = string.IsNullOrEmpty(fileContent) ? "" : prompt;

        // Логируем что отправляем
        resultOutput.text += $"Отправляемый текст: {textToSend.Substring(0, Mathf.Min(50, textToSend.Length))}...\n";
        if (!string.IsNullOrEmpty(promptToSend))
        {
            resultOutput.text += $"Промпт: {promptToSend}\n";
        }

        string cleanText = RemoveFormatting(textToSend);
        RequestData requestData = new RequestData
        {
            text = cleanText,
            prompt = promptToSend
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        resultOutput.text += "Формирование JSON...\n";

        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            // Добавляем обработчик SSL ошибок для Android
#if UNITY_ANDROID && !UNITY_EDITOR
        www.certificateHandler = new CustomCertificateHandler();
        resultOutput.text += "Инициализирован обработчик сертификатов для Android\n";
#endif

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 30;

            resultOutput.text += $"Отправка запроса на {serverUrl}...\n";
            yield return www.SendWebRequest();

            // Подробное логирование результата запроса
            resultOutput.text += $"Статус запроса: {www.result}\n";

            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorDetails = $"Ошибка: {www.error}\n";
                errorDetails += $"Код ответа: {www.responseCode}\n";
                if (!string.IsNullOrEmpty(www.downloadHandler?.text))
                {
                    errorDetails += $"Ответ сервера: {www.downloadHandler.text}\n";
                }

                Debug.LogError(errorDetails);
                resultOutput.text += errorDetails;
                yield break;
            }

            ResponseData response = null;

            try
            {
                resultOutput.text += "Получен ответ, обработка...\n";
                response = JsonUtility.FromJson<ResponseData>(www.downloadHandler.text);

                if (response == null)
                {
                    resultOutput.text += "Ошибка: ответ сервера пустой или в неверном формате\n";
                    yield break;
                }

                if (!string.IsNullOrEmpty(response.error))
                {
                    resultOutput.text += $"Ошибка от сервера: {response.error}\n";
                    yield break;
                }

                resultOutput.text = response.response; // Заменяем лог на финальный ответ
            }
            catch (Exception e)
            {
                string errorMsg = $"Ошибка парсинга ответа: {e.Message}\n";
                errorMsg += $"Сырой ответ: {www.downloadHandler.text}\n";

                Debug.LogError(errorMsg);
                resultOutput.text += errorMsg;
                yield break;
            }

            yield return StartCoroutine(SpeakResponse(response.response));
        }
    }

    IEnumerator SpeakResponse(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        string cleanText = RemoveFormatting(text);
        isSpeaking = true;

        var speakTask = ttsPlayer.Speak(cleanText);
        while (!speakTask.IsCompleted)
            yield return null;

        yield return new WaitWhile(() => audioSource.isPlaying);
        isSpeaking = false;
    }

    private string RemoveFormatting(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string result = Regex.Replace(input, @"\*{1,2}(.*?)\*{1,2}", "$1");
        result = Regex.Replace(result, @"_{1,2}(.*?)_{1,2}", "$1");
        result = Regex.Replace(result, @"~~(.*?)~~", "$1");
        result = Regex.Replace(result, @"`{1,3}(.*?)`{1,3}", "$1");
        result = Regex.Replace(result, @"\[(.*?)\]\(.*?\)", "$1");
        result = Regex.Replace(result, @"#+\s*", "");
        result = Regex.Replace(result, @"\s{2,}", " ");
        result = Regex.Replace(result, @"\r?\n\s*\r?\n", "\n");

        char[] specialChars = new char[] { '*', '_', '~', '`', '#', '^', '[', ']', '(', ')', '{', '}', '>', '|', '\\' };
        result = string.Join("", result.Split(specialChars, StringSplitOptions.RemoveEmptyEntries));

        return result.Trim();
    }

    public void SetPlaybackSpeed(float speed)
    {
        currentSpeed = speed;
        if (audioSource.isPlaying)
        {
            audioSource.pitch = speed;
        }
        UpdateSpeedButtonsUI();
    }

    private void UpdateSpeedButtonsUI()
    {
        speed1xButton.interactable = Math.Abs(currentSpeed - 1.0f) > 0.01f;
        speed1_5xButton.interactable = Math.Abs(currentSpeed - 1.5f) > 0.01f;
        speed2xButton.interactable = Math.Abs(currentSpeed - 2.0f) > 0.01f;
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
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
        
        intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_GET_CONTENT"));
        intentObject.Call<AndroidJavaObject>("setType", "*/*");
        intentObject.Call<AndroidJavaObject>("addCategory", intentClass.GetStatic<string>("CATEGORY_OPENABLE"));
        
        AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        
        currentActivity.Call("startActivityForResult", intentObject, 0);
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
            string rawContent = File.ReadAllText(filePath);
            loadedFileContent = RemoveFormatting(rawContent);
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

// Класс для обработки SSL сертификатов на Android
public class CustomCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Принимаем все сертификаты (только для тестирования!)
        return true;
    }
}