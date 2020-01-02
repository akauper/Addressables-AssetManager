using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skywatch
{
    public static class ExtensionMethods
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if(source == null)
                Debug.LogException(new NullReferenceException());
            if(action == null)
                Debug.LogException(new NullReferenceException());

            foreach(var element in source)
            {
                action(element);
            }
        }
    }
}
