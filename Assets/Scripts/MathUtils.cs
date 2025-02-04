using System;

public class MathUtils
{
    public static float GetOscillatingValue(int tickCount, int periodInSeconds)
    {
        int periodInTicks = periodInSeconds * 20;
        double phase = (2 * Math.PI * (tickCount % periodInTicks)) / periodInTicks;
        return (float)(0.5f * (1 + Math.Sin(phase)));
    }

    public static float GetOscillatingWithNegativeValue(int tickCount, int periodInSeconds)
    {
        int periodInTicks = periodInSeconds * 20;
        double phase = (2 * Math.PI * (tickCount % periodInTicks)) / periodInTicks;
        return (float)(0.5f * Math.Sin(phase));
    }
    public static float InverseLerp(float pDelta, float pStart, float pEnd)
    {
        return (pDelta - pStart) / (pEnd - pStart);
    }

    public static bool RandomBoolean(){
        return UnityEngine.Random.Range(0,2) == 0;
    }
}