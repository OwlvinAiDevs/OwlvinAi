import logging
from datetime import datetime, timedelta
from typing import List, Optional
from fastapi import FastAPI, HTTPException, Depends
from contextlib import asynccontextmanager
from ai_model import generate_schedule, format_schedule_prompt, format_chat_prompt, call_openai_api
from utils import parse_llm_response, get_user_state, recalculate_cached_availability
from database import init_db, SessionLocal
from pydantic import BaseModel
from sqlalchemy.orm import Session as DBSession
from models import StudyRequest, ScheduleResponse, User, BlockedTime, EnergyLevel, CreateScheduledSession, ScheduledSessionOut
from models import Task as DBTask, SessionLog as DBSessionLog, ScheduledSession as DBScheduledSession
from models import StudyRequest, ScheduleResponse, User, BlockedTime, EnergyLevel, CreateScheduledSession, ScheduledSessionOut
from models import Task as DBTask, SessionLog as DBSessionLog, ScheduledSession as DBScheduledSession

logging.basicConfig(level=logging.DEBUG)

def seed_test_user_data(db: DBSession, user_id: int = 1):
    now = datetime.utcnow()

    user = db.query(User).filter(User.id == user_id).first()
    if not user:
        user = User(id=user_id, username="test_user", email="test_user@example.com", auth_provider="local")
        db.add(user)
        db.commit()

    if not db.query(DBTask).filter(DBTask.user_id == user_id).first():
        db.add_all([
            DBTask(
                user_id=user_id,
                title="Complete Python project",
                description="Finish the AI scheduling project for Owlvin.",
                due_date=now + timedelta(days=1),
                duration_minutes=60,
                category="AI"
            ),
            DBTask(
                user_id=user_id,
                title="Read AI research paper",
                description="Study the latest advancements in AI scheduling.",
                due_date=now + timedelta(days=2),
                duration_minutes=45,
                category="Research"
            )
        ])

    if not db.query(EnergyLevel).filter(EnergyLevel.user_id == user_id).first():
        db.add_all([
            EnergyLevel(user_id=user_id, timestamp=now, level="high"),
            EnergyLevel(user_id=user_id, timestamp=now + timedelta(hours=4), level="medium")
        ])

    if not db.query(BlockedTime).filter(BlockedTime.user_id == user_id).first():
        db.add_all([
            BlockedTime(user_id=user_id, start_time=now + timedelta(hours=1), end_time=now + timedelta(hours=2), reason="class"),
            BlockedTime(user_id=user_id, start_time=now + timedelta(hours=3), end_time=now + timedelta(hours=4), reason="gym"),
        ])

    db.commit()
    recalculate_cached_availability(user_id, db)

@asynccontextmanager
async def lifespan(app: FastAPI):
    logging.info("Starting up and initializing the database...")
    init_db()
    db = SessionLocal()
    try:
        seed_test_user_data(db, user_id=1)
    except Exception as e:
        logging.error(f"Error seeding test user: {e}")
    finally:
        db.close()
    yield
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

@app.get("/user_state", response_model=StudyRequest)
def fetch_user_state(user_id: int, db: DBSession = Depends(get_db)):
    try:
        user_state = get_user_state(user_id, db)
        if not user_state:
            raise HTTPException(status_code=404, detail="User state not found.")
        return user_state
    except Exception as e:
        logging.error(f"Error fetching user state: {e}")
        raise HTTPException(status_code=500, detail="Internal server error")

@app.post("/seed_test_user")
def seed_test_user(db: DBSession = Depends(get_db)):
    seed_test_user_data(db, user_id=1)
    return {"status": "Test user seeded successfully", "user_id": 1}

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
async def generate_ai_schedule(request: StudyRequest, db: DBSession = Depends(get_db)):
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

        # Clear old scheduled sessions for this user
        deleted = db.query(DBScheduledSession).filter(DBScheduledSession.user_id == int(request.user_id)).delete()
        logging.info(f"[CLEANUP] Deleted {deleted} previous scheduled sessions for user {request.user_id}")

        # Persist AI-generated sessions into scheduled_sessions table
        for s in sessions:
            matched_task = db.query(DBTask).filter(
                DBTask.title == s.task.title,
                DBTask.user_id == int(request.user_id)
            ).first()
            if matched_task:
                s.task_id = matched_task.id
                logging.debug(f"[MATCH] Found task '{matched_task.title}' with ID {matched_task.id} for user {request.user_id}")
                db.add(DBScheduledSession(
                    user_id=int(request.user_id),
                    task_id=matched_task.id,
                    start_time=s.start_time,
                    end_time=s.end_time,
                    break_after=s.break_after or 5
                ))
            else:
                logging.warning(f"[MISS] Task '{s.task.title}' not found in DB for user {request.user_id}")
        db.commit()
        logging.info("[COMMIT] Scheduled sessions saved to database")

        # Metrics and response formatting
        # Clear old scheduled sessions for this user
        deleted = db.query(DBScheduledSession).filter(DBScheduledSession.user_id == int(request.user_id)).delete()
        logging.info(f"[CLEANUP] Deleted {deleted} previous scheduled sessions for user {request.user_id}")

        # Persist AI-generated sessions into scheduled_sessions table
        for s in sessions:
            matched_task = db.query(DBTask).filter(
                DBTask.title == s.task.title,
                DBTask.user_id == int(request.user_id)
            ).first()
            if matched_task:
                s.task_id = matched_task.id
                logging.debug(f"[MATCH] Found task '{matched_task.title}' with ID {matched_task.id} for user {request.user_id}")
                db.add(DBScheduledSession(
                    user_id=int(request.user_id),
                    task_id=matched_task.id,
                    start_time=s.start_time,
                    end_time=s.end_time,
                    break_after=s.break_after or 5
                ))
            else:
                logging.warning(f"[MISS] Task '{s.task.title}' not found in DB for user {request.user_id}")
        db.commit()
        logging.info("[COMMIT] Scheduled sessions saved to database")

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

@app.post("/schedule_session", response_model=ScheduledSessionOut)
def create_scheduled_session(session: CreateScheduledSession, db: DBSession = Depends(get_db)):
    db_session = DBScheduledSession(**session.model_dump())
    db.add(db_session)
    db.commit()
    db.refresh(db_session)
    logging.info(f"Scheduled session created: {db_session.id} for user {session.user_id}")
    return db_session

@app.get("/scheduled_sessions", response_model=List[ScheduledSessionOut])
def get_scheduled_sessions(user_id: int, db: DBSession = Depends(get_db)):
    return db.query(DBScheduledSession).filter(DBScheduledSession.user_id == user_id).all()

class ChatPrompt(BaseModel):
    user_id: int
    message: str
    include_context: bool = False # Toggle to include user context in the prompt

@app.post("/chat")
async def chat(prompt: ChatPrompt, db: DBSession = Depends(get_db)):
    try:
        # Pull context if requested
        context = ""
        if prompt.include_context:
            user_state: StudyRequest = get_user_state(prompt.user_id, db)

            context += "📅 User's current schedule:\n"
            for i, s in enumerate(user_state.available_slots):
                context += f"- Slot {i+1}: {s.start_time.strftime('%a %I:%M %p')} to {s.end_time.strftime('%I:%M %p')}\n"
            
            context += "\n📚 User's current tasks:\n"
            for t in user_state.tasks:
                context += f"- {t.title} ({t.duration_minutes} mins, due {t.due_date.strftime('%Y-%m-%d %H:%M')}, category: {t.category})\n"
        
        final_prompt = format_chat_prompt(prompt.message, context)
        gpt_response = await call_openai_api(final_prompt)
        return {"response": gpt_response}
    
    except Exception as e:
        logging.error(f"[CHAT ERROR] {e}")
        raise HTTPException(status_code=500, detail="Error processing chat request")

# --- Task Management Endpoints ---

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

@app.post("/tasks", response_model=TaskOut)
def create_task(task: CreateTask, db: DBSession = Depends(get_db)):
    db_task = DBTask(**task.model_dump())
    db.add(db_task)
    db.commit()
    db.refresh(db_task)
    logging.info(f"Task created: {db_task.title} for user {task.user_id}")
    return db_task

@app.get("/tasks", response_model=List[TaskOut])
def get_tasks(user_id: int, db: DBSession = Depends(get_db)):
    return db.query(DBTask).filter(DBTask.user_id == user_id).all()

# --- Session Logging Endpoints ---

class LogSession(BaseModel):
    user_id: int
    task_id: Optional[int] = None
    start_time: datetime
    end_time: datetime
    was_productive: bool = True

@app.post("/sessions")
def log_session(session: LogSession, db: DBSession = Depends(get_db)):
    db_session = DBSessionLog(**session.model_dump())
    db.add(db_session)
    db.commit()
    db.refresh(db_session)
    logging.info(f"Session logged: {db_session.id} for user {session.user_id}")
    return {"message": "Session logged successfully", "session_id": db_session.id}

@app.get("/sessions", response_model=List[LogSession])
def get_sessions(user_id: int, db: DBSession = Depends(get_db)):
    return db.query(DBSessionLog).filter(DBSessionLog.user_id == user_id).all()