using System;
using System.Collections;
using System.Collections.Generic;

namespace Herta.Utils.RangeTest;

internal readonly struct RangeStartStop
{
    public readonly int Start;
    public readonly int Stop;

    public RangeStartStop(int start, int stop)
    {
        Start = start;
        Stop = stop;
    }
}

internal readonly struct RangeStep
{
    public readonly int Step;

    public RangeStep(int step)
    {
        Step = step;
    }
}

public readonly struct Range : IEnumerable<int>, IReadOnlyList<int>
{
    private readonly RangeStartStop _startStop;
    private readonly RangeStep _step;

    public int Start => _startStop.Start;
    public int Stop => _startStop.Stop;
    public int Step => _step.Step;
    public int Count
    {
        get
        {
            if (Start > 0)
                return Math.Max(0, (Stop - Start + Step - 1) / Step);
            else
                return Math.Max(0, (Start - Stop + (-Step) - 1) / (-Step));
        }
    }

    public Range(int start, int stop, int step = 1)
    {
        if (step == 0)
            throw new ArgumentException("Step cannot be zero. Please provide a non-zero step value.", nameof(step));

        _startStop = new RangeStartStop(start, stop);
        _step = new RangeStep(step);
    }

    public Range(int stop) : this(0, stop, stop > 0 ? 1 : -1) { }

    public Range(Range other) : this(other.Start, other.Stop, other.Step) { }

    public IEnumerator<int> GetEnumerator()
    {
        if (Start == Stop)
            yield break;

        int current = Start;
        if (Step > 0)
        {
            while (current < Stop)
            {
                yield return current;
                current += Step;
            }
        }
        else
        {
            while (current > Stop)
            {
                yield return current;
                current += Step;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Start + index * Step;
        }
    }

    public bool Contains(int value)
    {
        if (Step > 0)
            return value >= Start && value < Stop && (value - Start) % Step == 0;
        else
            return value <= Start && value > Stop && (value - Start) % Step == 0;
    }

    public Range Reverse()
    {
        if (Step > 0)
        {
            return new Range(Stop - 1, Start - 1, -Step);
        }
        else
        {
            return new Range(Stop + 1, Start + 1, -Step);
        }
    }

    // 判断是否包含在另一个范围中
    public bool IsContainedIn(Range other)
    {
        if (this.Step > 0 && other.Step > 0)
        {
            return this.Start >= other.Start && this.Stop <= other.Stop && (this.Step % other.Step == 0 || this.Step == other.Step);
        }
        else if (this.Step < 0 && other.Step < 0)
        {
            return this.Start <= other.Start && this.Stop >= other.Stop && (this.Step % other.Step == 0 || this.Step == other.Step);
        }
        else
        {
            return false;
        }
    }

    public override string ToString()
    {
        return $"Range(start: {Start}, stop: {Stop}, step: {Step})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is Range other)
        {
            return Start == other.Start && Stop == other.Stop && Step == other.Step;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Stop, Step);
    }
}