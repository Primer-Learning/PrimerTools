using System;
using System.Collections.Generic;

namespace RockPaperScissors;

/// <summary>
/// A memory pool that can reclaim all borrowed memory at once.
/// </summary>
public static class MemoryPool<T>
{
    private static readonly Stack<List<T>> _listPool = new();
    private static readonly Dictionary<int, Stack<T[]>> _arrayPool = new();

    private static readonly List<List<T>> _borrowedLists = new();
    private static readonly List<T[]> _borrowedArrays = new();

    public static List<T> BorrowList()
    {
        if (!_listPool.TryPop(out var list))
            list = new List<T>();
        _borrowedLists.Add(list);
        return list;
    }

    public static T[] BorrowArray(int length)
    {
        if (!_arrayPool.TryGetValue(length, out var stack))
        {
            stack = new Stack<T[]>();
            _arrayPool.Add(length, stack);
        }

        if (!stack.TryPop(out var array))
            array = new T[length];

        _borrowedArrays.Add(array);
        return array;
    }

    public static void ReclaimAll()
    {
        foreach (var list in _borrowedLists)
        {
            list.Clear();
            _listPool.Push(list);
        }

        foreach (var array in _borrowedArrays)
        {
            if (!_arrayPool.TryGetValue(array.Length, out var stack))
            {
                stack = new Stack<T[]>();
                _arrayPool[array.Length] = stack;
            }

            Array.Clear(array);

            stack.Push(array);
        }
    }
}