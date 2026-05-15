namespace Miko.Animation;

public static class EasingFunctions
{
    public static float Evaluate(TimingFunction function, float t, CubicBezierParams? bezierParams = null)
    {
        return function switch
        {
            TimingFunction.Linear => t,
            TimingFunction.Ease => CubicBezierEval(0.25f, 0.1f, 0.25f, 1.0f, t),
            TimingFunction.EaseIn => CubicBezierEval(0.42f, 0f, 1f, 1f, t),
            TimingFunction.EaseOut => CubicBezierEval(0f, 0f, 0.58f, 1f, t),
            TimingFunction.EaseInOut => CubicBezierEval(0.42f, 0f, 0.58f, 1f, t),
            TimingFunction.StepStart => t >= 0 ? 1f : 0f,
            TimingFunction.StepEnd => t >= 1f ? 1f : 0f,
            TimingFunction.CubicBezier when bezierParams.HasValue =>
                CubicBezierEval(bezierParams.Value.X1, bezierParams.Value.Y1, bezierParams.Value.X2, bezierParams.Value.Y2, t),
            _ => t
        };
    }

    private static float CubicBezierEval(float x1, float y1, float x2, float y2, float t)
    {
        float solvedT = SolveCubicBezierX(x1, x2, t);
        return SampleCurveY(y1, y2, solvedT);
    }

    private static float SampleCurveY(float y1, float y2, float t)
    {
        return ((1f - 3f * y2 + 3f * y1) * t + (3f * y2 - 6f * y1)) * t * t + 3f * y1 * t;
    }

    private static float SolveCubicBezierX(float x1, float x2, float x)
    {
        // Newton-Raphson iteration to solve for t given x
        float t = x;
        for (int i = 0; i < 8; i++)
        {
            float currentX = SampleCurveX(x1, x2, t) - x;
            if (MathF.Abs(currentX) < 1e-6f) return t;

            float derivative = (3f * (1f - 3f * x2 + 3f * x1) * t + 2f * (3f * x2 - 6f * x1)) * t + 3f * x1;
            if (MathF.Abs(derivative) < 1e-6f) break;

            t -= currentX / derivative;
        }

        // Fallback: bisection
        float lo = 0f, hi = 1f;
        t = x;
        while (lo < hi)
        {
            float midX = SampleCurveX(x1, x2, t);
            if (MathF.Abs(midX - x) < 1e-6f) return t;
            if (x > midX) lo = t;
            else hi = t;
            t = (lo + hi) / 2f;
        }
        return t;
    }

    private static float SampleCurveX(float x1, float x2, float t)
    {
        return ((1f - 3f * x2 + 3f * x1) * t + (3f * x2 - 6f * x1)) * t * t + 3f * x1 * t;
    }
}
