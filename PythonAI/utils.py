import logging
from typing import List
from datetime import datetime, timedelta, time
from database import SessionLocal
from sqlalchemy.orm import Session as DBSession
from models import Session, TaskSchema, StudyRequest, TimeSlot, EnergyLevel, CachedAvailability, BlockedTime, Task as DBTask

def recalculate_cached_availability(user_id: int, db: DBSession):
    today = datetime.now().date()
    start_of_day = datetime.combine(today, time(8, 0))  # Assuming the day starts at 8 AM
    end_of_day = datetime.combine(today, time(22, 0))  # Assuming the day ends at 10 PM

    # get blocked times for today
    blocked = db.query(BlockedTime).filter(
        BlockedTime.user_id == user_id,
        BlockedTime.start_time >= start_of_day,
        BlockedTime.end_time <= end_of_day
    ).order_by(BlockedTime.start_time).all()

    # subtract blocked times from the available slots
    available = []
    current_start = start_of_day

    for b in blocked:
        if b.start_time > current_start:
            available.append((current_start, b.start_time))
        current_start = max(current_start, b.end_time)

        if current_start < end_of_day:
            available.append((current_start, end_of_day))
    
    # update the cached availability
    db.query(CachedAvailability).filter(CachedAvailability.user_id == user_id).delete()

    for start, end in available:
        db.add(CachedAvailability(user_id=user_id, start_time=start, end_time=end))
    
    db.commit()

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