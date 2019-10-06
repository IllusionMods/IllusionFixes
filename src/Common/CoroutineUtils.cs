using System;
using System.Collections;
using UnityEngine;

namespace Common
{
    internal static class CoroutineUtils
    {
        /// <summary>
        /// Create a coroutine that calls the appendCoroutine after base coroutine finishes
        /// </summary>
        public static IEnumerator AppendCo(this IEnumerator baseCoroutine, IEnumerator appendCoroutine)
        {
            return new[] { baseCoroutine, appendCoroutine }.GetEnumerator();
        }

        /// <summary>
        /// Create a coroutine that calls the yieldInstruction after base coroutine finishes.
        /// Append further coroutines tu run after this.
        /// </summary>
        public static IEnumerator AppendCo(this IEnumerator baseCoroutine, YieldInstruction yieldInstruction)
        {
            return new object[] { baseCoroutine, yieldInstruction }.GetEnumerator();
        }

        /// <summary>
        /// Create a coroutine that calls each of the actions in order after base coroutine finishes
        /// </summary>
        public static IEnumerator AppendCo(this IEnumerator baseCoroutine, params Action[] actions)
        {
            return new object[] { baseCoroutine, CreateCoroutine(actions) }.GetEnumerator();
        }

        /// <summary>
        /// Create a coroutine that calls each of the action delegates on consecutive frames
        /// (yield return null is returned after each one of the actions)
        /// </summary>
        public static IEnumerator CreateCoroutine(params Action[] actions)
        {
            foreach (var action in actions)
            {
                action();
                yield return null;
            }
        }

        /// <summary>
        /// Create a coroutine that calls each of the supplied coroutines in order
        /// </summary>
        public static IEnumerator ComposeCoroutine(params IEnumerator[] coroutine)
        {
            return coroutine.GetEnumerator();
        }
    }
}