from fastapi import FastAPI
import ollama

app = FastAPI()

@app.post("/process")
async def process_text(text: str, prompt: str):
    response = ollama.generate(
        model="gemma2:2b",
        prompt=f"{prompt}\n\nТекст: {text}"
    )
    return {"summary": response["response"]}