import logging
from typing import List
from datetime import datetime
from database import SessionLocal
from sqlalchemy.orm import Session as DBSession
from models import Session, TaskSchema, StudyRequest, TimeSlot, EnergyLevel, CachedAvailability, Task as DBTask

def parse_llm_response(structured_response: List[dict]) -> List[Session]:
    """
    Parses raw LLM response into a list of Session objects.
    
    Args:
        text (str): The raw response from the LLM.
        reference_date (str): The date to use as a reference for task dates, in 'YYYY-MM-DD' format.
        default_category (str): The category to assign to tasks if not specified.
    
    Returns:
        List[Session]: A list of Session objects created from the parsed tasks.
    """
    sessions = []

    for item in structured_response:
        try:
            task = TaskSchema(
                title=item["task"],
                due_date=datetime.fromisoformat(item["end"]), # Assuming end date is the due date for now
                duration_minutes=int((datetime.fromisoformat(item["end"]) - datetime.fromisoformat(item["start"])).total_seconds() / 60),
                category=item.get("category", "General")  # Default category if not specified
            )
            session = Session(
                task=task,
                start_time=datetime.fromisoformat(item["start"]),
                end_time=datetime.fromisoformat(item["end"]),
                break_after=item.get("break_after", 5)  # Default break after session in minutes
            )
            sessions.append(session)
        except Exception as e:
            logging.getLogger(__name__).error(f"Skipping item due to parse failure: {e}")
            continue
    return sessions

def get_user_state(user_id: int, db: DBSession = None) -> StudyRequest:
    """
    Retrieves the user's study request state from the database.
    
    Args:
        user_id (int): The ID of the user.
        db (DBSession, optional): The database session to use. If None, a new session will be created.
    
    Returns:
        StudyRequest: The user's study request state.
    """
    close_db = False
    if db is None:
        db = SessionLocal()
        close_db = True
    
    try:
        # tasks
        tasks = db.query(DBTask).filter(DBTask.user_id == user_id, DBTask.completed == False).all()
        task_schemas = [
            TaskSchema(
                title=t.title,
                due_date=t.due_date,
                duration_minutes=t.duration_minutes,
                category=t.category if t.category else "General"
            )
            for t in tasks
        ]

        # available time slots
        now = datetime.now()
        slots = db.query(CachedAvailability).filter(
            CachedAvailability.user_id == user_id,
            CachedAvailability.end_time > now
        ).order_by(CachedAvailability.start_time).all()

        time_slots = [
            TimeSlot(
                start_time=s.start_time,
                end_time=s.end_time,
            )
            for s in slots
        ]

        # energy levels (latest N for the slots)
        energy_raw = db.query(EnergyLevel).filter(EnergyLevel.user_id == user_id).order_by(EnergyLevel.timestamp.desc()).limit(len(time_slots)).all()

        def energy_str_to_int(e: str) -> int:
            return {
                "low": 1,
                "medium": 2,
                "high": 3
            }.get(e.lower(), 2) # Default to medium if not found
        
        energy_levels = [energy_str_to_int(e.level) for e in reversed(energy_raw)]

        # default pomodoro settings
        pomodoro_length = 25

        return StudyRequest(
            user_id=str(user_id),
            energy_level=energy_levels,
            pomodoro_length=pomodoro_length,
            available_slots=time_slots,
            tasks=task_schemas
            )
    
    finally:
        if close_db:
            db.close()