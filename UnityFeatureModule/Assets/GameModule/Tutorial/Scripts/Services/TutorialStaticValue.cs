namespace GameModule.Tutorial.Scripts.Services
{
    public static class TutorialStaticValue
    {
        public static string NextTask => "next_task";

        public class TaskFlow
        {
            public static string TapAnywhere        => "tap_any_where";
            public static string TapToGameObject    => "tap_game_object";
            public static string WaitTime           => "wait_time";
            public static string WaitEnterScreen => "wait_on_enter_screen";
        }
    }
}