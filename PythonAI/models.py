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
    start_time: datetime
    end_time: datetime
    break_after: Optional[int] = 5 # Default break time after each session in minutes

    def __repr__(self):
        return f"Session(task={self.task.title}, start={self.start_time.strftime('%H:%M')}, end={self.end_time.strftime('%H:%M')}, break_after={self.break_after} min)"

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

# SQLAlchemy ORM models for database representation
Base = declarative_base()

class User(Base):
    __tablename__ = 'users'
    id = Column(Integer, primary_key=True)
    username = Column(String)
    email = Column(String, unique=True)
    auth_provider = Column(String)
    date_created = Column(DateTime, default=datetime.utcnow)

    tasks = relationship("Task", back_populates="user")
    notes = relationship("Note", back_populates="user")

class Task(Base):
    __tablename__ = 'tasks'
    id = Column(Integer, primary_key=True)
    title = Column(String)
    description = Column(Text)
    created_at = Column(DateTime, default=datetime.utcnow, index=True)
    due_date = Column(DateTime)
    duration_minutes = Column(Integer)
    completed = Column(Boolean, default=False)
    category = Column(String)
    user_id = Column(Integer, ForeignKey('users.id'))

    user = relationship("User", back_populates="tasks")

class Note(Base):
    __tablename__ = 'notes'
    id = Column(Integer, primary_key=True)
    content = Column(Text)
    created_at = Column(DateTime, default=datetime.utcnow, index=True)
    user_id = Column(Integer, ForeignKey('users.id'))

    user = relationship("User", back_populates="notes")

class SessionLog(Base):
    __tablename__ = 'session_logs'
    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('users.id'))
    task_id = Column(Integer, ForeignKey('tasks.id'), nullable=True)
    start_time = Column(DateTime, index=True)
    end_time = Column(DateTime, index=True)
    was_productive = Column(Boolean)

    user = relationship("User", backref="session_logs")
    task = relationship("Task", backref="session_logs")

class EnergyLevel(Base):
    __tablename__ = 'energy_levels'
    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('users.id'))
    timestamp = Column(DateTime, default=datetime.utcnow, index=True)
    level = Column(String)  # "low", "medium", "high"

    user = relationship("User", backref="energy_levels")

class CachedAvailability(Base):
    __tablename__ = 'cached_availability'

    __table_args__ = (
        UniqueConstraint('user_id', 'start_time', 'end_time', name='uq_user_availability_time'),
    )

    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('users.id'))
    start_time = Column(DateTime, index=True)
    end_time = Column(DateTime, index=True)
    source = Column(String, default="inferred")

    user = relationship("User", backref="cached_availability")

class BlockedTime(Base):
    __tablename__ = 'blocked_times'
    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('users.id'))
    start_time = Column(DateTime, index=True)
    end_time = Column(DateTime, index=True)
    reason = Column(String)

    user = relationship("User", backref="blocked_times")

class AIResponse(Base):
    __tablename__ = 'ai_responses'
    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('users.id'))
    timestamp = Column(DateTime, default=datetime.utcnow, index=True)
    response_json = Column(Text)

    user = relationship("User", backref="ai_responses")

class AIChatLog(Base):
    __tablename__ = 'ai_chat_logs'
    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('users.id'))
    timestamp = Column(DateTime, default=datetime.utcnow, index=True)
    role = Column(String)  # "user" or "assistant"
    message = Column(Text)

    user = relationship("User", backref="ai_chat_logs")