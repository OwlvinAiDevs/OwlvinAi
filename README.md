<p align="center">
  <img src="docs/studybuddy_logo.png" alt="StudyBuddy Logo" width="200" />
</p>

<h1 align="center">OwlvinAI</h1>

<p align="center">
  Your intelligent study companion — powered by Unity and AI.
</p>

<p align="center">
  <a href="https://github.com/StuddyBuddyAi/StuddyBuddy/issues">
    <img alt="Issues" src="https://img.shields.io/github/issues/StuddyBuddyAi/StuddyBuddy?style=flat-square&logo=github">
  </a>
  <a href="https://github.com/StuddyBuddyAi/StuddyBuddy/blob/main/LICENSE">
    <img alt="License" src="https://img.shields.io/badge/license-pending-lightgrey?style=flat-square">
  </a>
</p>

---

## 📚 About the Project

**OwlvinAI** is a cross-platform productivity app designed to help students manage their time more effectively. By combining Unity with a Python-based AI backend, the app generates personalized study schedules and provides smart reminders, making it easier to stay organized and productive.

This project is a team-led capstone initiative and is currently in **Alpha development**.

## 🚀 Core Features (Alpha Milestone)

- **Homepage Dashboard** – See upcoming study sessions at a glance
- **Navigation UI** – Easy access to Calendar, Timer, and Settings
- **Calendar View** – Visual schedule of upcoming study tasks and sessions
- **Alarm Clock / Pomodoro Timer** – Study sessions with work/break intervals
- **AI-Generated Study Schedules** – Working JSON-based schedule via backend

## ⚙ Technologies Used

- **Frontend:** Unity 2022.3 (C#), Unity UI Toolkit
- **Backend:** Python 3.8+, FastAPI (deployed on [Render](https://studybuddy-api-w8g5.onrender.com))
- **Communication:** UnityWebRequest (HTTP)
- **Data:** Local storage via PlayerPrefs (planned Firebase)
- **Design:** Figma wireframes

## 🛠️ Installation (Users)

1. Download the latest build from GitHub
2. Run `StudyBuddy.exe` in the Build folder to launch the app

## Development Setup

### Clone the Repository

```bash
git clone https://github.com/StuddyBuddyAi/StuddyBuddy.git
cd StuddyBuddy
```

## 👨‍💻 Unity Setup for Developers

- Open the project in Unity Hub
- Load the `LLMAi` scene
- Press Play — Unity will contact the deployed backend via public endpoints:
  - `https://studybuddy-api-w8g5.onrender.com/ping` → should return `{"message":"pong"}`
  - `https://studybuddy-api-w8g5.onrender.com/generate_ai_schedule` → returns mock schedule JSON

The backend source code still lives in `/PythonAI/` if future updates or changes are needed.

## 🤝 Contribution

We welcome contributions once the core system is stabilized. For now, teammates should:

- Use Jira task IDs in your branch names (e.g. `SCRUM-185-ui-navigation`)
- Submit pull requests with clear descriptions
- Follow our style tile for UI consistency

A full contribution guide will be added soon.

## 📄 License

This project is currently shared within the scope of a team project and is not yet licensed for public distribution. License to be added in final release phase.

## 👥 Project Contributors 

- [Anastasia Altamirano](https://github.com/anapaltami)
- [Michael Nathan Belisaire](https://github.com/SoldierTaker)
- [Nathaniel McCleery](https://github.com/nate254347)

## 🧪 Current Project Status : Beta Build

| Component             | Status                          |
|-----------------------|----------------------------------|
| Ai Calendar Notes         | 🔧 Under integration            |
| Ai Chat Logs          | 🔧 Under integration            |
| Pomodoro Timer        | ✅ UI functional, backend Under integration    |
| Custom Ai Promting       | 🔧 Under integration          |
| Variable amount of daily notes | 🔧 Under integration          |
| Google Calendar sync  | 🔧 Under integration          |


## 🧪 Previous Project Status : Alpha Build

| Component             | Status                          |
|-----------------------|----------------------------------|
| Homepage UI           | ✅ Implemented                   |
| Navigation            | ✅ Implemented                   |
| Pomodoro Timer        | ✅ UI functional, backend TBD     |
| Calendar View         | 🔧 Under integration              |
| AI Scheduling (Stub)  | ✅ JSON mock working              |
| Unity ↔ Python Comm   | ✅ Using deployed Render server   |

## 🙌 Credits

- Logo based on an illustration by [Design.com](https://www.design.com) – Modified for the project
- Owl mascot & icon style inspired by [Study-focused UI trends]
- Fonts: ADLaM Display, Aharoni, Quicksand

### 🎨 Custom Color Palette
|  | Name | Hex | RGB | HSL |
| --- | --- | --- | --- | --- |
| ![](docs/000000.png) | Black | `#000000` | `rgb(0, 0, 0)` | `hsl(0, 0%, 0%)` |
| ![](docs/FFFFFF.png) | White | `#FFFFFF` | `rgb(255, 255, 255)` | `hsl(0, 0%, 100%)` |
| ![](docs/3DA0F1.png) | Azure | `#3DA0F1` | `rgb(61, 160, 241)` | `hsl(207, 87%, 59%)` |
| ![](docs/7DDA58.png) | Green | `#7DDA58` | `rgb(125, 218, 88)` | `hsl(99, 69%, 60%)` |
| ![](docs/98F5F9.png) | Aqua | `#98F5F9` | `rgb(152, 245, 249)` | `hsl(183, 88%, 79%)` |
| ![](docs/B06CE9.png) | Violet | `#B06CE9` | `rgb(176, 108, 233)` | `hsl(271, 73%, 67%)` |
| ![](docs/BBD9FF.png) | Sky Blue | `#BBD9FF` | `rgb(187, 217, 255)` | `hsl(215, 100%, 87%)` |
| ![](docs/EFC3CA.png) | Blush | `#EFC3CA` | `rgb(239, 195, 202)` | `hsl(349, 60%, 85%)` |
| ![](docs/FE9900.png) | Orange | `#FE9900` | `rgb(254, 153, 0)` | `hsl(35, 100%, 50%)` |

