namespace Baguettefy
{
    public static class MathUtils
    {
        public static float NormaliseRange(float val, float valmin, float valmax, float in_min = 0.0f, float in_max = 1.0f)
        {
            return (val - valmin) / (valmax - valmin) * (in_max - in_min) + in_min;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float Clamp01(float value)
        {
            if (value < 0F)
                return 0F;
            else if (value > 1F)
                return 1F;
            else
                return value;
        }
    }
}
