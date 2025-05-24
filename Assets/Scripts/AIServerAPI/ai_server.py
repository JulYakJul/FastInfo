from fastapi import FastAPI, Request
import ollama
import json

app = FastAPI()

@app.post("/process")
async def process_text(request: Request):
    try:
        # Получаем raw JSON данные
        data = await request.json()
        text = data.get("text")
        prompt = data.get("prompt")
        
        response = ollama.generate(
            model="gemma2:2b",
            prompt=f"{prompt}\n\nТекст: {text}"
        )
        return {"response": response["response"]}
    except Exception as e:
        return {"error": str(e)}