// Program.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

#nullable enable

class Program
{
    static string jsonPath = "demo-schedule.json";
    static string csvPath = "demo-schedule.csv";

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("=== Daily Demo Schedule Tool ===\n");
            Console.WriteLine("1. Generate new schedule");
            Console.WriteLine("2. Record completed demo");
            Console.WriteLine("3. View schedule preview");
            Console.WriteLine("4. Exit");

            Console.Write("Choose option: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    GenerateSchedule();
                    break;
                case "2":
                    RecordDemo();
                    break;
                case "3":
                    PreviewSchedule();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine(); // Add a blank line for readability before showing the menu again
        }
    }

    // ------------------------------
    // Generate a fresh schedule
    // ------------------------------
    static void GenerateSchedule()
    {
        var participants = PromptForParticipants();
        if (participants.Count == 0)
        {
            Console.WriteLine("No participants provided. Exiting.");
            return;
        }

        DateTime startDate = PromptForStartDate();
        int days = PromptForInt("How many days to schedule? (e.g. 10)", 1, 10000);
        bool skipWeekends = PromptForYesNo("Skip weekends? (y/n)", true);
        int startIndex = PromptForInt($"Which participant index should start first? (1..{participants.Count})", 1, participants.Count) - 1;

        var schedule = BuildSchedule(participants, startDate, days, skipWeekends, startIndex);

        SaveSchedule(schedule);
        Console.WriteLine($"✅ Schedule created and saved to '{jsonPath}' and '{csvPath}'.");
    }

    // ------------------------------
    // Record who actually did demo
    // ------------------------------
    static void RecordDemo()
    {
        var schedule = LoadSchedule();
        if (schedule.Count == 0)
        {
            Console.WriteLine("⚠️ No schedule found. Generate one first.");
            return;
        }

        Console.Write("Enter date to record (dd-MM-yyyy) [default: today]: ");
        string? input = Console.ReadLine();
        DateTime date = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(input))
        {
            if (!DateTime.TryParseExact(input.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Console.WriteLine("Invalid date format.");
                return;
            }
        }

        var entry = schedule.FirstOrDefault(e => e.Date.Date == date.Date);
        if (entry == null)
        {
            Console.WriteLine("No schedule entry found for that date.");
            return;
        }

        Console.WriteLine($"Scheduled: {entry.Presenter} | Backup1: {entry.Backup1} | Backup2: {entry.Backup2}");
        Console.Write("Enter who actually gave the demo (leave blank if cancelled): ");
        string? actual = Console.ReadLine();

        entry.ActualPresenter = string.IsNullOrWhiteSpace(actual) ? null : actual.Trim();

        SaveSchedule(schedule);
        Console.WriteLine("✅ Demo record updated.");
    }

    // ------------------------------
    // View schedule preview
    // ------------------------------
    static void PreviewSchedule()
    {
        var schedule = LoadSchedule();
        if (schedule.Count == 0)
        {
            Console.WriteLine("⚠️ No schedule found.");
            return;
        }

        Console.WriteLine("\n--- Schedule Preview ---");
        foreach (var e in schedule.Take(15))
        {
            string actual = string.IsNullOrEmpty(e.ActualPresenter) ? "(pending)" : e.ActualPresenter!;
            Console.WriteLine($"{e.Date:dd-MM-yyyy} | Planned: {e.Presenter} | Backups: {e.Backup1}, {e.Backup2} | ✅ Actual: {actual}");
        }
    }

    // ------------------------------
    // Helper functions
    // ------------------------------
    static List<string> PromptForParticipants()
    {
        Console.WriteLine("Enter participants (one per line).");
        Console.WriteLine("Or type 'file' to load from a CSV file.");
        Console.WriteLine("Finish with an empty line:");
        var list = new List<string>();

        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null) break;

            line = line.Trim();
            if (line.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                var fileList = LoadParticipantsFromFile();
                if (fileList.Count > 0) return fileList;
                Console.WriteLine("⚠️ No participants loaded from file. Continue entering manually...");
                continue;
            }

            if (string.IsNullOrEmpty(line)) break;
            list.Add(line);
        }

        return list;
    }

    static List<string> LoadParticipantsFromFile()
    {
        Console.Write("Enter path to CSV file: ");
        string? path = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Console.WriteLine("⚠️ File not found.");
            return new List<string>();
        }

        try
        {
            var lines = File.ReadAllLines(path)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            Console.WriteLine($"✅ Loaded {lines.Count} participants from file.");
            return lines;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error reading file: {ex.Message}");
            return new List<string>();
        }
    }


    static DateTime PromptForStartDate()
    {
        while (true)
        {
            Console.Write("Start date (dd-MM-yyyy) [default: today]: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return DateTime.Today;
            if (DateTime.TryParseExact(input.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return dt;
            Console.WriteLine("Invalid date format.");
        }
    }

    static int PromptForInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt} ");
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int val) && val >= min && val <= max) return val;
            Console.WriteLine($"Enter an integer between {min} and {max}.");
        }
    }

    static bool PromptForYesNo(string prompt, bool defaultYes = true)
    {
        while (true)
        {
            Console.Write($"{prompt} ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return defaultYes;
            input = input.ToLowerInvariant();
            if (input == "y" || input == "yes") return true;
            if (input == "n" || input == "no") return false;
            Console.WriteLine("Please answer y or n.");
        }
    }

    static List<ScheduleEntry> BuildSchedule(List<string> participants, DateTime start, int days, bool skipWeekends, int startIndex)
    {
        var schedule = new List<ScheduleEntry>();
        int idx = startIndex % participants.Count;
        DateTime current = start;
        int added = 0;

        while (added < days)
        {
            if (skipWeekends && (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday))
            {
                current = current.AddDays(1);
                continue;
            }

            string presenter = participants[idx % participants.Count];
            string backup1 = participants[(idx + 1) % participants.Count];
            string backup2 = participants[(idx + 2) % participants.Count];

            schedule.Add(new ScheduleEntry
            {
                Date = current,
                Presenter = presenter,
                Backup1 = backup1,
                Backup2 = backup2
            });

            idx = (idx + 1) % participants.Count;
            current = current.AddDays(1);
            added++;
        }

        return schedule;
    }

    static void SaveSchedule(List<ScheduleEntry> schedule)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(schedule, options);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);

            var sb = new StringBuilder();
            sb.AppendLine("Date,Presenter,Backup1,Backup2,ActualPresenter");
            foreach (var e in schedule)
            {
                string actual = e.ActualPresenter ?? "";
                sb.AppendLine($"{e.Date:dd-MM-yyyy},{EscapeCsv(e.Presenter)},{EscapeCsv(e.Backup1)},{EscapeCsv(e.Backup2)},{EscapeCsv(actual)}");
            }
            File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Save failed: {ex.Message}");
        }
    }

    static List<ScheduleEntry> LoadSchedule()
    {
        if (!File.Exists(jsonPath)) return new List<ScheduleEntry>();
        try
        {
            string json = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<List<ScheduleEntry>>(json) ?? new List<ScheduleEntry>();
        }
        catch
        {
            return new List<ScheduleEntry>();
        }
    }

    static string EscapeCsv(string value)
    {
        if (value.Contains(",") || value.Contains("\""))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    class ScheduleEntry
    {
        public DateTime Date { get; set; }
        public string Presenter { get; set; } = string.Empty;
        public string Backup1 { get; set; } = string.Empty;
        public string Backup2 { get; set; } = string.Empty;
        public string? ActualPresenter { get; set; }
    }
}
