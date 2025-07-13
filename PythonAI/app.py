import logging
from datetime import datetime
from typing import List, Optional
from fastapi import FastAPI, HTTPException, Depends
from contextlib import asynccontextmanager
from ai_model import generate_schedule, format_schedule_prompt, call_openai_api
from utils import parse_llm_response, get_user_state
from database import init_db, SessionLocal
from pydantic import BaseModel
from sqlalchemy.orm import Session as DBSession
from models import StudyRequest, ScheduleResponse, User, Task as DBTask, SessionLog as DBSessionLog

logging.basicConfig(level=logging.DEBUG)

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup code
    logging.info("Starting up and initializing the database...")
    init_db()
    yield
    # Shutdown code
    logging.info("Shutting down...")

app = FastAPI(lifespan=lifespan)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

@app.get("/ping")
def ping():
    return {"message": "pong"}

@app.get("/user_state/", response_model=StudyRequest)
def fetch_user_state(user_id: int, db: DBSession = Depends(get_db)):
    """
    Fetches the user's study request state from the database.
    """
    try:
        user_state = get_user_state(user_id, db)
        if not user_state:
            raise HTTPException(status_code=404, detail="User state not found.")
        return user_state
    except Exception as e:
        logging.error(f"Error fetching user state: {e}")
        raise HTTPException(status_code=500, detail="Internal server error")

@app.post("/generate_schedule/", response_model=ScheduleResponse)
def schedule(request: StudyRequest):
    # Log the incoming request for debugging
    logging.info(f"Received StudyRequest: user_id={request.user_id}, tasks={len(request.tasks)}, slots={len(request.available_slots)}")

    # Basic validation
    if not request.available_slots:
        raise HTTPException(status_code=400, detail="No available time slots provided.")
    if not request.tasks:
        raise HTTPException(status_code=400, detail="No tasks provided for scheduling.")
    if len(request.energy_level) < len(request.available_slots):
        raise HTTPException(status_code=400, detail="Not enough energy for available time slots.")
    return generate_schedule(request)

@app.post("/generate_ai_schedule/", response_model=ScheduleResponse)
async def generate_ai_schedule(request: StudyRequest):
    """
    Calls OpenAI API with a formatted schedule prompt and returns raw response text.
    """
    try:
        logging.info(f"[START] /generate_ai_schedule for user_id={request.user_id}")
        logging.debug(f"Request JSON: {request.model_dump_json()}")

        prompt = format_schedule_prompt(request)
        logging.debug(f"Formatted prompt:\n{prompt}")

        logging.info("[CALLING] OpenAI API")
        gpt_response = await call_openai_api(prompt)
        logging.info("[SUCCESS] OpenAI API responded")

        logging.debug(f"Raw GPT response: {gpt_response}")

        if not isinstance(gpt_response, list) or not all(isinstance(item, dict) for item in gpt_response):
            raise ValueError("Invalid response format from OpenAI API. Expected a list of session dictionaries.")

        sessions = parse_llm_response(gpt_response)
        logging.info(f"Parsed {len(sessions)} sessions from GPT response")

        total_study_time = sum([s.task.duration_minutes for s in sessions])
        total_break_time = sum([s.break_after for s in sessions if s.break_after])

        # Compare scheduled vs original tasks
        scheduled_tasks = {s.task.title for s in sessions}
        original_tasks = request.tasks
        unscheduled_tasks = [t for t in original_tasks if t.title not in scheduled_tasks]
        warnings = [
            f"ChatGPT did not include task '{task.title}' (due {task.due_date.strftime('%Y-%m-%d %H:%M')}) in the generated schedule." for task in unscheduled_tasks
        ]

        logging.info(f"[DONE] Schedule generated with {len(warnings)} warnings")

        if sessions:
            logging.debug(f"First session: {sessions[0]!r}")
            logging.debug(f"All sessions: {sessions!r}")
        return ScheduleResponse(
            user_id=request.user_id,
            sessions=sessions,
            total_study_time=total_study_time,
            total_break_time=total_break_time,
            success=len(unscheduled_tasks) == 0,
            message="Schedule generated successfully." if not unscheduled_tasks else "Some tasks could not be scheduled due to time constraints.",
            warnings=warnings
        )
    except ValueError as ve:
        logging.exception("[ERROR] ValueError from GPT response")
        raise HTTPException(status_code=400, detail=f"Invalid response format: {ve}")
    
    except Exception as e:
        logging.warning(f"[FALLBACK] AI scheduling failed: {e}. Using rule-based scheduling.")
        fallback_response = generate_schedule(request)
        fallback_response.warnings.append(f"AI scheduling failed: {e}. Fallback to rule-based scheduling used.")
        fallback_response.message = "AI scheduling failed. Rule-based scheduling used instead."
        fallback_response.success = False # Fallback implies partial failure
        return fallback_response

class CreateTask(BaseModel):
    user_id: int
    title: str
    description: str = ""
    due_date: datetime
    duration_minutes: int
    category: str = ""
    completed: bool = False

class TaskOut(BaseModel):
    id: int
    user_id: int
    title: str
    description: str
    due_date: datetime
    duration_minutes: int
    category: str
    completed: bool

    class Config:
        orm_mode = True

@app.post("/tasks/", response_model=TaskOut)
def create_task(task: CreateTask, db: DBSession = Depends(get_db)):
    """
    Create a new task for the user.
    """
    db_task = DBTask(
        user_id=task.user_id,
        title=task.title,
        description=task.description,
        due_date=task.due_date,
        duration_minutes=task.duration_minutes,
        category=task.category,
        completed=task.completed
    )
    db.add(db_task)
    db.commit()
    db.refresh(db_task)
    logging.info(f"Task created: {db_task.title} for user {task.user_id}")
    return db_task

@app.get("/tasks/", response_model=List[TaskOut])
def get_tasks(user_id: int, db: DBSession = Depends(get_db)):
    tasks = db.query(DBTask).filter(DBTask.user_id == user_id).all()
    return tasks

class LogSession(BaseModel):
    user_id: int
    task_id: Optional[int] = None
    start_time: datetime
    end_time: datetime
    was_productive: bool = True

@app.post("/sessions/")
def log_session(session: LogSession, db: DBSession = Depends(get_db)):
    """
    Log a study session for the user.
    """
    db_session = DBSessionLog(**session.model_dump())
    db.add(db_session)
    db.commit()
    db.refresh(db_session)
    logging.info(f"Session logged: {db_session.id} for user {session.user_id}")
    return {"message": "Session logged successfully", "session_id": db_session.id}

@app.get("/sessions/", response_model=List[LogSession])
def get_sessions(user_id: int, db: DBSession = Depends(get_db)):
    return db.query(DBSessionLog).filter(DBSessionLog.user_id == user_id).all()