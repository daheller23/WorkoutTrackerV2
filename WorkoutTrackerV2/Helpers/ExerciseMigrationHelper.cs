using SQLite;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Helpers
{
    public static class ExerciseMigrationHelper
    {
        // 1. THE DYNAMIC RULESET
        // Order matters! Place specific keywords (like "incline") above generic ones (like "bench").
        private static readonly List<SubMuscleRule> MappingRules =
        [
            // CHEST
            new() { MainGroup = "Chest", SubGroup = "Upper Chest", Keywords = ["incline", "upper"] },
            new() { MainGroup = "Chest", SubGroup = "Lower Chest", Keywords = ["decline", "dip", "lower"] },
            new() { MainGroup = "Chest", SubGroup = "Mid Chest",   Keywords = ["flat", "bench", "fly", "pec deck", "cable cross"] },
            
            // BACK
            new() { MainGroup = "Back", SubGroup = "Lats",       Keywords = ["pullup", "pulldown", "chin", "lat"] },
            new() { MainGroup = "Back", SubGroup = "Traps",      Keywords = ["shrug", "face pull", "upright"] },
            new() { MainGroup = "Back", SubGroup = "Lower Back", Keywords = ["deadlift", "hyper", "good morning", "extension"] },
            new() { MainGroup = "Back", SubGroup = "Mid Back",   Keywords = ["row", "t-bar", "seated cable"] },

            // LEGS
            new() { MainGroup = "Legs", SubGroup = "Hamstrings", Keywords = ["curl", "rdl", "romanian", "stiff", "glute-ham"] },
            new() { MainGroup = "Legs", SubGroup = "Calves",     Keywords = ["calf", "calves", "raise"] },
            new() { MainGroup = "Legs", SubGroup = "Glutes",     Keywords = ["thrust", "bridge", "kickback", "abductor"] },
            new() { MainGroup = "Legs", SubGroup = "Quads",      Keywords = ["squat", "press", "extension", "lunge", "hack"] },

            // SHOULDERS
            new() { MainGroup = "Shoulders", SubGroup = "Side Delt",  Keywords = ["lateral", "side"] },
            new() { MainGroup = "Shoulders", SubGroup = "Rear Delt",  Keywords = ["rear", "reverse", "face"] },
            new() { MainGroup = "Shoulders", SubGroup = "Front Delt", Keywords = ["front", "press", "military", "arnold"] },

            // ARMS
            new() { MainGroup = "Arms", SubGroup = "Triceps",  Keywords = ["tricep", "extension", "pushdown", "skull", "kickback"] },
            new() { MainGroup = "Arms", SubGroup = "Forearms", Keywords = ["wrist", "farmer", "hold"] },
            new() { MainGroup = "Arms", SubGroup = "Biceps",   Keywords = ["curl", "preacher", "hammer", "spider"] },

            // CORE
            new() { MainGroup = "Core", SubGroup = "Obliques", Keywords = ["oblique", "twist", "wood", "side"] },
            new() { MainGroup = "Core", SubGroup = "Abs",      Keywords = ["crunch", "plank", "raise", "sit", "ab"] }
        ];

        public static async Task RunSubMuscleMigrationAsync(SQLiteAsyncConnection database)
        {
            var allExercises = await database.Table<Exercise>().ToListAsync();

            // Find exercises that need categorization
            var exercisesToUpdate = allExercises.Where(e =>
                string.IsNullOrEmpty(e.SubMuscleGroup) || e.SubMuscleGroup == "General").ToList();

            if (exercisesToUpdate.Count == 0) return;

            foreach (var exercise in exercisesToUpdate)
            {
                exercise.SubMuscleGroup = CategorizeExercise(exercise.Name, exercise.MuscleGroup);
                await database.UpdateAsync(exercise);
            }
        }

        private static string CategorizeExercise(string name, string mainMuscleGroup)
        {
            var lowerName = name.ToLower();

            // 2. THE DYNAMIC MATCHER
            // Get all rules that apply to this specific Muscle Group
            var applicableRules = MappingRules.Where(r => r.MainGroup == mainMuscleGroup);

            foreach (var rule in applicableRules)
            {
                // If any keyword in the rule matches the exercise name, return that sub-group!
                if (rule.Keywords.Any(keyword => lowerName.Contains(keyword)))
                {
                    return rule.SubGroup;
                }
            }

            // 3. FALLBACKS
            // If an exercise has a weird name (e.g., "The Widowmaker"), default it cleanly
            return mainMuscleGroup switch
            {
                "Chest" => "Mid Chest",
                "Back" => "Mid Back",
                "Legs" => "Quads",
                "Shoulders" => "Front Delt",
                "Arms" => "Biceps",
                "Core" => "Abs",
                _ => "General"
            };
        }
    }

    // A clean data model to define our mapping rules
    public class SubMuscleRule
    {
        public string MainGroup { get; set; } = string.Empty;
        public string SubGroup { get; set; } = string.Empty;
        public string[] Keywords { get; set; } = [];
    }
}