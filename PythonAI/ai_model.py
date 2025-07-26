import json
import re
import time
import openai
import asyncio
import logging
from openai import AsyncOpenAI
from dotenv import load_dotenv
from datetime import datetime, timedelta
from typing import List
from models import StudyRequest, Session, ScheduleResponse

load_dotenv()  # Load environment variables from .env file

def generate_schedule(request: StudyRequest) -> ScheduleResponse:
    sessions: List[Session] = []
    remaining_tasks = sorted(request.tasks, key=lambda t: t.due_date) # Sort tasks by due date
    unscheduled_tasks = []
    break_after = 5 # Default break after the session in minutes
    total_study_time = 0
    total_break_time = 0

    slot_index = 0
    while remaining_tasks and slot_index < len(request.available_slots):
        slot = request.available_slots[slot_index]
        slot_start = slot.start_time
        slot_end = slot.end_time

        while remaining_tasks and slot_start < slot_end:
            task = remaining_tasks[0]
            energy = request.energy_level[slot_index] # 1 = low, 2 = medium, 3 = high
            multiplier = {1: 1.25, 2: 1.0, 3: 0.75}.get(energy, 1.0) # task duration increases with lower energy and increases with higher energy
            adjusted_duration_minutes = round(task.duration_minutes * multiplier)
            task_duration = timedelta(minutes=adjusted_duration_minutes)

            # Check if the task fits in the current time slot
            if slot_start + task_duration <= slot_end:
                session = Session(
                    task=task,
                    start_time=slot_start,
                    end_time=slot_start + task_duration,
                    break_after=break_after
                )
                sessions.append(session)
                total_study_time += adjusted_duration_minutes
                total_break_time += break_after
                slot_start += task_duration + timedelta(minutes=break_after)

                # Move to the next time slot after the session and break
                remaining_tasks.pop(0)
            else:
                # If the task doesn't fit, move to the next time slot
                break

        slot_index += 1
    
    # If there are still remaining tasks that couldn't be scheduled
    unscheduled_tasks.extend(remaining_tasks)
    warning_messages = [
        f"Unable to schedule task '{task.title}' before its duedate of {task.due_date.strftime("%Y-%m-%d %H:%M")}." for task in unscheduled_tasks
    ]
    
    return ScheduleResponse(
        user_id=request.user_id,
        sessions=sessions,
        total_study_time=total_study_time,
        total_break_time=total_break_time,
        success=len(unscheduled_tasks) == 0,
        message="All tasks scheduled successfully." if not unscheduled_tasks else "Some tasks could not be scheduled due to time constraints.",
        warnings=warning_messages
    )

def format_schedule_prompt(request: StudyRequest) -> str:
    lines = []
    lines.append(f"The user prefers a study session length of {request.pomodoro_length} minutes.")

    # Time slot formatting
    lines.append("Available time slots with energy levels are:")
    for i, slot in enumerate(request.available_slots):
        energy = request.energy_level[i] if i < len(request.energy_level) else "unknown"
        start = slot.start_time.strftime("%A, %B %d at %I:%M %p")
        end = slot.end_time.strftime("%I:%M %p")
        lines.append(f"- {start} to {end} (Energy Level: {energy})")

    # Task formatting
    lines.append("Tasks to be scheduled (with exact categories):")
    for task in request.tasks:
        due = task.due_date.strftime("%A, %B %d")
        category = task.category or "General"
        lines.append(f"- {task.title}, {task.duration_minutes} min, due {due}, category: {category}")
    lines.append("\nImportant: Do not infer or rename task categories. Use the exact category string provided for each task.")


    # Instruction for GPT format compliance
    lines.append("\nPlease generate an optimized study schedule using the given constraints.")
    lines.append("Respond ONLY with a plain JSON array of session dictionaries using this format (no markdown, no commentary):")
    lines.append("""
[
    {
        "task": "Complete Python project",
        "start": "2025-07-13T09:00:00",
        "end": "2025-07-13T09:25:00",
        "category": "AI"
    }
]
""")
    return "\n".join(lines)

def format_chat_prompt(message: str, context: str = "") -> str:
    """
    Formats the chat prompt for OpenAI API.
    """
    formatted_prompt = (
        "You are an AI assistant helping a user with their study schedule.\n"
        "Respond helpfully to the user's question. If the user asks about scheduling, reference their study sessions.\n"
        "If the user provides a freeform request like \"I need to study for 1 hour tonight\", break the task into 25-minute Pomodoro blocks with 5-minute breaks in between, and return each block as a separate session in JSON format.\n"
        f"Today's date is {datetime.utcnow().date()}.\n"
        f"{context}\n"
        f"User says:\n\"{message}\"\n\n"
        "Respond with:\n"
        "- A helpful reply in natural language.\n"
        "- If you inferred new tasks, also return with a plain JSON array of session dictionaries using this format (no markdown, no commentary):\n"
        """Example format:
[
  {
    "task": "Study physics",
    "start": "2025-07-21T18:00:00",
    "end": "2025-07-21T18:25:00",
    "category": "Science"
  },
  {
    "task": "Study physics",
    "start": "2025-07-21T18:30:00",
    "end": "2025-07-21T18:55:00",
    "category": "Science"
  }
]
"""
        "Only include JSON if you inferred new tasks from the message."
    )
    return formatted_prompt


async def call_openai_api(prompt: str, max_retries: int = 3, delay: float = 2.0) -> List[dict]:
    """
    Calls OpenAI with retry logic on timeout and parses JSON from the response.
    """
    client = AsyncOpenAI()  # Initialize OpenAI client

    for attempt in range(max_retries):
        try:
            logging.info(f"[GPT] Attempt {attempt + 1} to call OpenAI")
            response = await asyncio.wait_for(
                client.chat.completions.create(
                    model = "gpt-4",
                    messages = [
                        {"role": "system", "content": "You are a helpful assistant that generates study schedules."},
                        {"role": "user", "content": prompt}
                    ],
                    temperature = 0.7, # Adjust temperature for creativity vs. precision
                    max_tokens = 1000, # Limit response length
                    n = 1 # Number of responses to generate
                ),
                timeout=30.0 # Timeout for the API call in seconds
            )
            text = response.choices[0].message.content.strip()
            logging.debug(f"[GPT RAW TEXT] {text}")
            json_str = re.search(r"\[.*\]", text, re.DOTALL).group(0)
            parsed_json = json.loads(json_str)
            logging.info("[GPT] Successfully parsed JSON response")
            return parsed_json
        
        except openai.APITimeoutError as te:
            logging.warning(f"[GPT TIMEOUT] Timeout on attempt {attempt + 1}: {te}")
            time.sleep(delay)

        except json.JSONDecodeError as je:
            logging.error(f"[GPT ERROR] JSON parsing failed: {je}")
            raise ValueError("Malformed JSON response from OpenAI API.")
        
        except Exception as e:
            logging.error(f"[GPT ERROR] Unexpected exception: {e}")
            if attempt == max_retries - 1:
                raise e
            time.sleep(delay)
    raise RuntimeError("Failed to get a valid response from OpenAI after multiple attempts.")