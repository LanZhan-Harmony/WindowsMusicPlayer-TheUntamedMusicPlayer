using System.Numerics;
using Microsoft.UI.Composition;

namespace UntamedMusicPlayer.Helpers.Animations;

/// <summary>
/// Extension of <see cref="Composition"/>, required for generic extension
/// methods as NaturalMotionAnimation and CompositionAnimation have different
/// base classes and generic methods can't handle it
/// </summary>
public static class CompositionNaturalMotion
{
    extension<T>(T animation)
        where T : NaturalMotionAnimation
    {
        /// <summary>
        /// Sets the delay time in seconds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="animation"></param>
        /// <param name="delayTime">Delay Time in seconds</param>
        /// <returns></returns>
        public T SetDelayTime(double delayTime)
        {
            animation.DelayTime = TimeSpan.FromSeconds(delayTime);
            return animation;
        }

        public T SetDelayTime(TimeSpan delayTime)
        {
            animation.DelayTime = delayTime;
            return animation;
        }

        public T SetDelayBehavior(AnimationDelayBehavior behavior)
        {
            animation.DelayBehavior = behavior;
            return animation;
        }

        #region SetDelay

        /// <summary>
        /// Sets the delay time in seconds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="animation"></param>
        /// <param name="delayTime">Delay Time in seconds</param>
        /// <returns></returns>
        public T SetDelay(double delayTime, AnimationDelayBehavior behavior)
        {
            animation.DelayTime = TimeSpan.FromSeconds(delayTime);
            animation.DelayBehavior = behavior;
            return animation;
        }

        public T SetDelay(TimeSpan delayTime, AnimationDelayBehavior behavior)
        {
            animation.DelayBehavior = behavior;
            animation.DelayTime = delayTime;
            return animation;
        }

        #endregion
    }

    extension(SpringVector3NaturalMotionAnimation animation)
    {
        #region SetDampingRatio

        public SpringVector3NaturalMotionAnimation SetDampingRatio(float dampingRatio)
        {
            animation.DampingRatio = dampingRatio;
            return animation;
        }

        #endregion


        #region SetPeriod

        public SpringVector3NaturalMotionAnimation SetPeriod(double duration)
        {
            return duration >= 0 ? SetPeriod(animation, TimeSpan.FromSeconds(duration)) : animation;
        }

        public SpringVector3NaturalMotionAnimation SetPeriod(TimeSpan duration)
        {
            animation.Period = duration;
            return animation;
        }

        #endregion
    }

    extension<T>(T animation)
        where T : Vector3NaturalMotionAnimation
    {
        #region SetInitialValue

        public T SetInitialValue(float x, float y, float z)
        {
            animation.InitialValue = new(x, y, z);
            return animation;
        }

        public T SetInitialValue(Vector3? value)
        {
            animation.InitialValue = value;
            return animation;
        }

        #endregion


        #region SetFinalValue

        public T SetFinalValue(float x, float y, float z)
        {
            animation.FinalValue = new(x, y, z);
            return animation;
        }

        public T SetFinalValue(Vector3? value)
        {
            animation.FinalValue = value;
            return animation;
        }

        #endregion
    }
}
