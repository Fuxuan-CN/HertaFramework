
using System;
using System.Collections.Generic;

namespace Herta.Utils.Range
{
    public struct Range
    // 类似python的 range() , 可以用 foreach 遍历
    {
        private readonly int _start;
        private readonly int _stop;
        private readonly int _step;

        public Range(int start, int stop, int step = 1)
        {
            _start = start;
            _stop = stop;
            _step = step;
        }

        public IEnumerable<int> GetEnumerator()
        {
            int current = _start;
            while (_step > 0 ? current < _stop : current > _stop)
            {
                yield return current;
                current += _step;
            }
        }
    }
}