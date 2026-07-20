using Microsoft.Xna.Framework;
using System;

namespace tsorcRevamp.Utilities
{
    /// <summary>
    /// Piecewise-lerp keyframe curve. Ported from the BroadswordRework player-weapon system
    /// (Items/Weapons/Melee/Broadswords/BroadswordRework/Utilities/_DataStructures/Gradient.cs)
    /// so puppet swing code can use the same non-linear easing without depending on that
    /// experimental item-component folder.
    /// </summary>
    public abstract class Gradient
    {
        static Gradient()
        {
            Gradient<float>.LerpFunc = MathHelper.Lerp;
            Gradient<Vector2>.LerpFunc = Vector2.Lerp;
            Gradient<Color>.LerpFunc = Color.Lerp;
        }
    }

    public class Gradient<T> : Gradient
    {
        public struct GradientKey
        {
            public float Time;
            public T Value;

            public GradientKey(float time, T value)
            {
                Time = time;
                Value = value;
            }

            public static implicit operator GradientKey((float time, T value) tuple)
                => new(tuple.time, tuple.value);
        }

        public static Func<T, T, float, T> LerpFunc { protected get; set; }

        private readonly GradientKey[] keys;

        public Gradient(params GradientKey[] values)
        {
            if (LerpFunc == null)
                throw new NotSupportedException($"Gradient<{typeof(T).Name}>.{nameof(LerpFunc)} is not defined.");
            if (values == null || values.Length == 0)
                throw new ArgumentException("Array length must not be zero.");
            keys = values;
        }

        public T GetValue(float time)
        {
            GradientKey left = keys[0];
            GradientKey right = keys[keys.Length - 1];
            bool leftDefined = false;
            bool rightDefined = false;

            for (int i = 0; i < keys.Length; i++)
            {
                if (!leftDefined || (keys[i].Time > left.Time && keys[i].Time <= time))
                {
                    left = keys[i];
                    leftDefined = true;
                }
            }

            for (int i = keys.Length - 1; i >= 0; i--)
            {
                if (!rightDefined || (keys[i].Time < right.Time && keys[i].Time >= time))
                {
                    right = keys[i];
                    rightDefined = true;
                }
            }

            return left.Time == right.Time ? left.Value : LerpFunc(left.Value, right.Value, (time - left.Time) / (right.Time - left.Time));
        }
    }
}
