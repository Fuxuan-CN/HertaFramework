using System;
using System.Collections;
using System.Collections.Generic;

namespace Herta.Utils.Range;

public readonly struct Range : IEnumerable<int>, IReadOnlyList<int>
{
    private readonly int _start;
    private readonly int _stop;
    private readonly int _step;

    public int Start => _start;
    public int Stop => _stop;
    public int Step => _step;
    public int Count
    {
        get
        {
            if (_step > 0)
                return Math.Max(0, (_stop - _start + _step - 1) / _step);
            else
                return Math.Max(0, (_start - _stop + (-_step) - 1) / (-_step));
        }
    }

    public Range(int start, int stop, int step = 1)
    {
        if (step == 0)
            throw new ArgumentException("Step cannot be zero.", nameof(step));

        _start = start;
        _stop = stop;
        _step = step;
    }

    public Range(int stop) : this(0, stop)
    {
        // empty range
    }

    public IEnumerator<int> GetEnumerator()
    {
        if (_start == _stop)
            yield break;

        int current = _start;
        if (_step > 0)
        {
            while (current < _stop)
            {
                yield return current;
                current += _step;
            }
        }
        else
        {
            while (current > _stop)
            {
                yield return current;
                current += _step;
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
            return _start + index * _step;
        }
    }

    public bool Contains(int value)
    {
        if (_step > 0)
            return value >= _start && value < _stop && (value - _start) % _step == 0;
        else
            return value <= _start && value > _stop && (value - _start) % _step == 0;
    }

    public Range Reverse()
    {
        if (_step > 0)
        {
            return new Range(_stop - 1, _start - 1, -_step);
        }
        else
        {
            return new Range(_stop + 1, _start + 1, -_step);
        }
    }

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
        return $"Range(start: {_start}, stop: {_stop}, step: {_step})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is Range other)
        {
            return _start == other._start && _stop == other._stop && _step == other._step;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_start, _stop, _step);
    }
}
