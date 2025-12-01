# FastInfo

**FastInfo** — это Unity-приложение для быстрого получения и обработки информации с использованием искусственного интеллекта и технологии синтеза речи (TTS). Приложение позволяет пользователям загружать большие текстовые файлы, задавать вопросы и получать ответы в текстовом и аудиоформате.

## Основные возможности

- **Обработка текста с ИИ** - Загрузка и анализ текстовых файлов с помощью языковых моделей
- Возможность задавать вопросы по содержимому больших файлов
- **Text-to-Speech** - Озвучивание ответов с поддержкой русского и английского языков
- **Контроль скорости воспроизведения** - Настройка скорости речи (1x, 1.3x, 1.5x)
- Поддержка Windows и Android

## Технологии

| Категория        | Технологии и описание |
|-----------------|---------------------|
| **Backend (API Server)** | **Unity Engine**: 2022.3.49f1<br>**C#**<br>**Python**: 3.11+<br>**FastAPI**<br>**Ollama**<br>**Модель ИИ**: Gemma2:2b |
| **TTS (Text-to-Speech)** | **Overtone TTS Engine**<br>**Поддерживаемые голоса**:<br>🇺🇸 Английский: Amy <br>🇷🇺 Русский: Denis, Dmitri, Irina, Ruslan |
| **Дополнительные технологии** | **UnityWebRequest**: HTTP-клиент для связи с сервером<br>**JSON**<br>**Git** |

## Использование

1. **Запустите Unity проект** и откройте сцену `SampleScene`
2. **Загрузите текстовый файл** нажав кнопку "Загрузить файл"
3. **Введите промпт** в текстовое поле (например: Расскажи про "тема из такста")
4. **Нажмите на кнопку On** для отправки запроса
5. **Прослушайте ответ** - текст автоматически озвучится
6. **Управляйте скоростью** воспроизведения кнопками скорости

<div align="center">
  <img src="https://github.com/JulYakJul/FastInfo/blob/main/GitImages/interface.jpg?raw=true" width="250"/>
</div>

## Требования

### Unity Editor
- Unity 2022.3.49f1
- .NET Framework 4.x

### Платформы
- Windows (x86, x64)
- Android (API Level 22+)

## Установка и настройка

### 1. Клонирование репозитория
```bash
git clone https://github.com/your-username/FastInfo.git
cd FastInfo
```

### 2. Настройка Unity проекта
Запустите проект на версии Unity 2022.3.49f1

### 3. Настройка сервера ИИ
```bash
# Установите зависимости
pip install fastapi ollama uvicorn

# Установите модель Ollama
ollama pull gemma2:2b

# Запустите сервер
python Assets/Scripts/AIServerAPI/ai_server.py
```

### 4. Конфигурация сервера
По умолчанию приложение подключается к серверу по адресу: `https://fastinfo.cloudpub.ru/process`

Для использования локального сервера измените URL в `TextProcessor.cs`:
```csharp
private string serverUrl = "http://localhost:8000/process";
```

## Структура проекта

```
FastInfo/
├── Assets/
│   ├── Scripts/
│   │   ├── AIServerAPI/         # Python сервер ИИ
│   │   │   ├── ai_server.py     # FastAPI сервер
│   │   │   └── AIServerAPI.py   # Вспомогательные функции
│   │   ├── TextProcessor.cs     # Основная логика приложения
│   │   ├── AndroidTTS.cs        # TTS для Android
│   │   └── TTSListener.cs       # Обработчик событий TTS
│   ├── Overtone/               # TTS движок
│   │   ├── Scripts/            # C# скрипты TTS
│   │   ├── Resources/          # Голосовые модели
│   │   └── Plugins/            # Нативные библиотеки
│   ├── Scenes/
│   │   └── SampleScene.unity   # Основная сцена
│   ├── Sprites/                # UI элементы
│   └── TestResources/          # Тестовые файлы
├── ProjectSettings/            # Настройки Unity проекта
└── README.md
```

## API Endpoints

### POST `/process`
Обрабатывает текст с помощью ИИ модели.

**Тело запроса:**
```json
{
    "text": "Текст для обработки",
    "prompt": "Инструкция для ИИ"
}
```

**Ответ:**
```json
{
    "response": "Ответ от ИИ модели"
}
```

**Ошибка:**
```json
{
    "error": "Описание ошибки"
}
```

---

*FastInfo - быстрый доступ к информации через ИИ и голосовые технологии*
