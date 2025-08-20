import logging
from typing import List
from datetime import datetime
from models import Session, TaskSchema

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