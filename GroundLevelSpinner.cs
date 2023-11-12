using UnityEngine;

namespace TerrainTools
{
    public static class GroundLevelSpinner
    {
        public const float MaxSpinner = 1.0f;
        public const float MinSpinner = 0.0f;

        public static float Value { get; private set; } = 1.0f;

        public static void Refresh()
        {
            var scrollDelta = Keybindings.ScrollDelta();
            if (scrollDelta > 0)
            {
                Up(scrollDelta);
            }
            if (scrollDelta < 0)
            {
                Down(scrollDelta);
            }
        }

        private static void Up(float scrollDelta)
        {
            if (Value + scrollDelta > MaxSpinner)
            {
                Value = MaxSpinner;
            }
            else
            {
                Value = Mathf.Round((Value + scrollDelta) * 100) / 100;
            }
        }

        private static void Down(float scrollDelta)
        {
            if (Value + scrollDelta < MinSpinner)
            {
                Value = MinSpinner;
            }
            else
            {
                Value = Mathf.Round((Value + scrollDelta) * 100) / 100;
            }
        }
    }
}