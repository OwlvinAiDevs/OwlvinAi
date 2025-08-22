from datetime import datetime
from pydantic import BaseModel
from typing import List, Optional
from sqlalchemy import Column, Integer, String, Boolean, DateTime, ForeignKey, Text, UniqueConstraint
from sqlalchemy.orm import relationship, declarative_base

class TaskSchema(BaseModel):
    """A study task with required time, deadline, and optional category."""
    title: str
    due_date: datetime
    duration_minutes: int
    category: Optional[str] = None

    def __repr__(self):
        return f"Task(title={self.title}, duration={self.duration_minutes} min, due={self.due_date()}, category={self.category})"

class TimeSlot(BaseModel):
    """Represents an available block of time for scheduling study sessions."""
    start_time: datetime
    end_time: datetime

    def __repr__(self):
        return f"TimeSlot({self.start_time.strftime('%H:%M')} - {self.end_time.strftime('%H:%M')})"

class StudyRequest(BaseModel):
    """
    The payload for both rule-based and AI-based scheduling requests.

    Fields:
        user_id: Identifier for the student.
        energy_level: One value per available time slot, representing the user's energy level (1-3 scale).
        pomodoro_length: Preferred study block length in minutes (default: 25).
        available_slots: Time windows the user is available to study.
        tasks: List of tasks to schedule.
    """
    user_id: str
    energy_level: List[int]
    pomodoro_length: Optional[int] = 25
    available_slots: List[TimeSlot]
    tasks: List[TaskSchema]

class Session(BaseModel):
    """
    Represents a single scheduled study block.

    Fields:
        task: Task associated with this session.
        start_time: Start timestamp of the session.
        end_time: End timestamp of the session.
        break_after: Suggested break duration after the session in minutes.
    """
    task: TaskSchema
    task_id: Optional[int] = None  # For database compatibility
    start_time: datetime
    end_time: datetime
    break_after: Optional[int] = 5 # Default break time after each session in minutes

    def __repr__(self):
        return f"Session(task={self.task.title}, start={self.start_time.strftime('%H:%M')}, end={self.end_time.strftime('%H:%M')}, break_after={self.break_after} min)"

class CreateScheduledSession(BaseModel):
    user_id: int
    task_id: int
    start_time: datetime
    end_time: datetime
    break_after: Optional[int] = 5

class ScheduledSessionOut(BaseModel):
    id: int
    class Config:
        orm_mode = True

class ScheduleResponse(BaseModel):
    """
    Response returned to the frontend after scheduling generation.

    Fields:
        user_id: User receiving the schedule.
        sessions: List of scheduled study sessions.
        total_study_time: Sum of all study durations in minutes.
        total_break_time: Sum of all break times in minutes.
        success: True if all tasks were successfully scheduled.
        message: Additional info or error message.
    """
    user_id: str
    sessions: List[Session]
    total_study_time: int
    total_break_time: int
    success: bool = True
    message: Optional[str] = None
    warnings: Optional[List[str]] = []