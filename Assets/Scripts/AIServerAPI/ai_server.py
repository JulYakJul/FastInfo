from fastapi import FastAPI, Request
import ollama
import json

app = FastAPI()

@app.post("/process")
async def process_text(request: Request):
    try:
        data = await request.json()
        text = data.get("text", "").strip()
        prompt = data.get("prompt", "").strip()

        if not text and not prompt:
            return {"error": "Не указан ни текст, ни промпт."}

        # Формируем финальный prompt
        final_prompt = ""
        if prompt and not text:
            final_prompt = prompt
        elif text and not prompt:
            final_prompt = f"Текст: {text}"
        else:
            final_prompt = f"{prompt}\n\nТекст: {text}"

        response = ollama.generate(
            model="gemma2:2b",
            prompt=final_prompt
        )

        return {"response": response["response"]}
    except Exception as e:
        return {"error": str(e)}