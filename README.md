# DemoScheduleApp
AI-Assisted Demo Schedule Generator
# Overview
This is a simple C# console application that helps teams organize daily AI-assisted coding demos.
* Each participant is assigned a demo day.
* Two backup presenters are automatically scheduled in case the primary person skips.
* After a demo is completed, you can mark it as done, so the schedule stays up to date.
* The schedule is saved and reloaded automatically using JSON files.
* Participants can be entered manually or loaded from a CSV file.
# Features
📅 Automatic schedule generation from a start date

👥 Fair rotation: everyone presents once, with 2 backups each day

💾 Persistent storage: schedule saved to schedule.json

✅ Mark as completed when someone finishes their demo

📂 Load participants:

Manually enter names

Or load from a CSV file

🔄 Wrap-around logic ensures backups rotate evenly
# How It Works
1. Enter participants (or load from file).
2. Provide a start date.
3. The program builds a schedule assigning:

   . Primary presenter
 
   . Backup1
 
   . Backup2
 
5. After each demo, mark it as completed.
6. Exit and restart — the schedule persists across runs.
