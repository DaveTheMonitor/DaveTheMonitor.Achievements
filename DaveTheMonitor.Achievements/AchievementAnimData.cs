namespace DaveTheMonitor.Achievements
{
    internal struct AchievementAnimData
    {
        public Achievement Achievement;
        public float Duration;
        public float CurrentTime;
        public float Transition;

        public AchievementAnimData(Achievement achievement, float duration, float transition)
        {
            Achievement = achievement;
            Duration = duration;
            Transition = transition;
            CurrentTime = 0;
        }
    }
}
