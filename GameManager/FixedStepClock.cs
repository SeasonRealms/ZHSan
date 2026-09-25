using System;

namespace Zhsan.GameManager;

/// <summary>Preserves the old default 60 Hz logic clock without owning a frame loop.</summary>
internal sealed class FixedStepClock
{
    public const double StepSeconds = 1d / 60d;
    public const double MaximumElapsedSeconds = 0.5d;
    private double _remainder;
    private bool _advancing;

    public int Advance(double elapsedSeconds, Action<float> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (_advancing) throw new InvalidOperationException("The logic clock cannot advance reentrantly.");
        _advancing = true;
        try
        {
            _remainder = Math.Min(MaximumElapsedSeconds, _remainder + Math.Min(elapsedSeconds, MaximumElapsedSeconds));
            int steps = 0;
            // A tiny tolerance avoids losing an exact tick to repeated double subtraction.
            while (_remainder + 1e-12 >= StepSeconds)
            {
                _remainder = Math.Max(0, _remainder - StepSeconds);
                steps++;
                update((float)StepSeconds);
            }
            return steps;
        }
        finally { _advancing = false; }
    }
}
