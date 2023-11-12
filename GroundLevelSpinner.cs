using UnityEngine;

namespace TerrainTools
{
    public static class GroundLevelSpinner
    {
        public const string MouseScrollWheel = "Mouse ScrollWheel";
        public const float ScrollPrecision = 0.05f;
        public const float SuperiorScrollPrecisionMultiplier = 0.2f;

        public const float MaxSpinner = 1.0f;
        public const float MinSpinner = 0.0f;

        public static float Value { get; private set; } = 1.0f;

        public static void Refresh()
        {
            var scrollDelta = ScrollDelta();
            if (scrollDelta > 0)
            {
                Up(scrollDelta);
            }
            if (scrollDelta < 0)
            {
                Down(scrollDelta);
            }
        }

        private static float ScrollDelta()
        {
            var scrollDelta = Input.GetAxis(MouseScrollWheel);
            if (scrollDelta != 0)
            {
                scrollDelta = scrollDelta > 0 ? ScrollPrecision : -ScrollPrecision;
            }
            return scrollDelta;
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