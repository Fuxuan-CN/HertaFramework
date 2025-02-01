using System;
using System.Collections.Generic;

namespace Herta.Utils.ListSlicer
{
    public static class ListExtensions
    {
        public static List<T> Slice<T>(this List<T> source, int start, int end)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // 处理负索引
            start = start < 0 ? source.Count + start : start;
            end = end < 0 ? source.Count + end : end;

            // 确保索引在有效范围内
            start = Math.Max(0, Math.Min(start, source.Count));
            end = Math.Max(0, Math.Min(end, source.Count));

            // 确保 start 小于 end
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            // 创建切片列表
            var slicedList = new List<T>();
            for (int i = start; i < end; i++)
            {
                slicedList.Add(source[i]);
            }

            return slicedList;
        }
    }
}
