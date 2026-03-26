using SQLite;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Helpers
{
    public static class ExerciseMigrationHelper
    {
        public static async Task RunSubMuscleMigrationAsync(SQLiteAsyncConnection database)
        {
            // 1. Get all exercises that haven't been categorized yet
            var allExercises = await database.Table<Exercise>().ToListAsync();
            var exercisesToUpdate = allExercises.Where(e =>
                string.IsNullOrEmpty(e.SubMuscleGroup) || e.SubMuscleGroup == "General").ToList();

            if (exercisesToUpdate.Count == 0) return; // Already migrated!

            // 2. Loop through and assign the correct tag
            foreach (var exercise in exercisesToUpdate)
            {
                exercise.SubMuscleGroup = CategorizeExercise(exercise.Name, exercise.MuscleGroup);
                await database.UpdateAsync(exercise);
            }
        }

        private static string CategorizeExercise(string name, string mainMuscleGroup)
        {
            var lowerName = name.ToLower();

            return mainMuscleGroup switch
            {
                "Chest" => lowerName switch
                {
                    _ when lowerName.Contains("incline") => "Upper Chest",
                    _ when lowerName.Contains("decline") || lowerName.Contains("dip") => "Lower Chest",
                    _ => "Mid Chest" // Flat bench, pec deck, regular flys
                },

                "Back" => lowerName switch
                {
                    _ when lowerName.Contains("pullup") || lowerName.Contains("pulldown") || lowerName.Contains("chin") => "Lats",
                    _ when lowerName.Contains("shrug") || lowerName.Contains("face pull") => "Traps",
                    _ when lowerName.Contains("deadlift") || lowerName.Contains("hyperextension") || lowerName.Contains("good morning") => "Lower Back",
                    _ => "Mid Back" // Rows, T-Bar, etc.
                },

                "Legs" => lowerName switch
                {
                    _ when lowerName.Contains("curl") || lowerName.Contains("rdl") || lowerName.Contains("romanian") || lowerName.Contains("stiff") => "Hamstrings",
                    _ when lowerName.Contains("calf") || lowerName.Contains("calves") => "Calves",
                    _ when lowerName.Contains("thrust") || lowerName.Contains("bridge") || lowerName.Contains("kickback") => "Glutes",
                    _ => "Quads" // Squats, Leg Press, Extensions, Lunges
                },

                "Shoulders" => lowerName switch
                {
                    _ when lowerName.Contains("lateral") || lowerName.Contains("side") => "Side Delt",
                    _ when lowerName.Contains("rear") || lowerName.Contains("reverse") || lowerName.Contains("pec deck") => "Rear Delt",
                    _ => "Front Delt" // Overhead press, military press, front raises
                },

                "Arms" => lowerName switch
                {
                    _ when lowerName.Contains("tricep") || lowerName.Contains("extension") || lowerName.Contains("pushdown") || lowerName.Contains("skull") => "Triceps",
                    _ when lowerName.Contains("wrist") || lowerName.Contains("farmer") => "Forearms",
                    _ => "Biceps" // Curls, Preacher, Hammer
                },

                "Core" => lowerName switch
                {
                    _ when lowerName.Contains("oblique") || lowerName.Contains("twist") || lowerName.Contains("wood") => "Obliques",
                    _ => "Abs" // Crunches, planks, leg raises
                },

                _ => "General"
            };
        }
    }
}