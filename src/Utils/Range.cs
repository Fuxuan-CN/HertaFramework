using System;
using System.Collections;
using System.Collections.Generic;

namespace Herta.Utils.Range;

public struct Range : IEnumerable<int>
{
    private readonly int _start;
    private readonly int _stop;
    private readonly int _step;

    public int Start => _start;
    public int Stop => _stop;
    public int Step => _step;

    public Range(int start, int stop, int step = 1)
    {
        if (step == 0)
            throw new ArgumentException("Step cannot be zero.", nameof(step));

        _start = start;
        _stop = stop;
        _step = step;
    }

    public IEnumerator<int> GetEnumerator()
    {
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

    public override string ToString()
    {
        return $"Range(start: {_start}, stop: {_stop}, step: {_step})";
    }
}
