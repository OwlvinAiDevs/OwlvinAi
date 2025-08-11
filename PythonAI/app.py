import logging
from typing import List, Optional
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from ai_model import generate_schedule, format_schedule_prompt, format_chat_prompt, call_openai_api
from utils import parse_llm_response
from models import StudyRequest, ScheduleResponse

logging.basicConfig(level=logging.DEBUG)

# Initialize the FastAPI app
app = FastAPI()

@app.get("/ping")
def ping():
    return {"message": "pong"}

@app.post("/generate_schedule", response_model=ScheduleResponse)
def schedule(request: StudyRequest):
    logging.info(f"Received StudyRequest: user_id={request.user_id}, tasks={len(request.tasks)}, slots={len(request.available_slots)}")
    if not request.available_slots:
        raise HTTPException(status_code=400, detail="No available time slots provided.")
    if not request.tasks:
        raise HTTPException(status_code=400, detail="No tasks provided for scheduling.")
    if len(request.energy_level) < len(request.available_slots):
        raise HTTPException(status_code=400, detail="Not enough energy for available time slots.")
    return generate_schedule(request)

@app.post("/generate_ai_schedule", response_model=ScheduleResponse)
async def generate_ai_schedule(request: StudyRequest):
    try:
        logging.info(f"[START] /generate_ai_schedule for user_id={request.user_id}")
        logging.debug(f"Request JSON: {request.model_dump_json()}")
        prompt = format_schedule_prompt(request)
        logging.debug(f"Formatted prompt:\n{prompt}")
        logging.info("[CALLING] OpenAI API")
        gpt_response = await call_openai_api(prompt)
        logging.info("[SUCCESS] OpenAI API responded")
        logging.debug(f"Raw GPT response: {gpt_response}")
        sessions = parse_llm_response(gpt_response)
        logging.info(f"Parsed {len(sessions)} sessions from GPT response")

        if not isinstance(gpt_response, list) or not all(isinstance(item, dict) for item in gpt_response):
            raise ValueError("Invalid response format from OpenAI API. Expected a list of session dictionaries.")

        # Metrics and response formatting
        total_study_time = sum([s.task.duration_minutes for s in sessions])
        total_break_time = sum([s.break_after for s in sessions if s.break_after])
        scheduled_titles = {s.task.title for s in sessions}
        unscheduled = [t for t in request.tasks if t.title not in scheduled_titles]
        warnings = [f"ChatGPT did not include task '{t.title}' (due {t.due_date.strftime('%Y-%m-%d %H:%M')})" for t in unscheduled]

        logging.info(f"[DONE] Schedule generated with {len(warnings)} warnings")
        if sessions:
            logging.debug(f"First session: {sessions[0]!r}")
        return ScheduleResponse(
            user_id=request.user_id,
            sessions=sessions,
            total_study_time=total_study_time,
            total_break_time=total_break_time,
            success=len(unscheduled) == 0,
            message="Schedule generated successfully." if not unscheduled else "Some tasks could not be scheduled.",
            warnings=warnings
        )
    except ValueError as ve:
        logging.exception("[ERROR] ValueError from GPT response")
        raise HTTPException(status_code=400, detail=f"Invalid response format: {ve}")
    except Exception as e:
        logging.warning(f"[FALLBACK] AI scheduling failed: {e}. Using rule-based scheduling.")
        fallback = generate_schedule(request)
        fallback.warnings.append(f"AI scheduling failed: {e}. Fallback to rule-based scheduling used.")
        fallback.message = "AI scheduling failed. Rule-based scheduling used instead."
        fallback.success = False
        return fallback

class ChatPrompt(BaseModel):
    user_id: int
    message: str
    context: Optional[str] = ""

@app.post("/chat")
async def chat(prompt: ChatPrompt):
    try:
        final_prompt = format_chat_prompt(prompt.message, prompt.context)
        gpt_response = await call_openai_api(final_prompt)
        return {"response": gpt_response}
    
    except Exception as e:
        logging.error(f"[CHAT ERROR] {e}")
        raise HTTPException(status_code=500, detail="Error processing chat request")