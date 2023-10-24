namespace DaveTheMonitor.Achievements
{
    internal struct AchievementAnimData
    {
        public Achievement Achievement;
        public float Duration;
        public float CurrentTime;
        public float Transition;
        public float YTransition;
        public float TargetY;
        public float CurrentY;

        public AchievementAnimData(Achievement achievement, float duration, float transition, float yTransition)
        {
            Achievement = achievement;
            Duration = duration;
            Transition = transition;
            CurrentTime = 0;
            YTransition = yTransition;
            TargetY = float.MaxValue;
            CurrentY = -1;
        }
    }
}
