using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Core/Events/Float Event Channel")]
    public class FloatEventChannel : ScriptableObject
    {
        public event Action<float> OnRaised;
        public void Raise(float value) => OnRaised?.Invoke(value);
    }
}
