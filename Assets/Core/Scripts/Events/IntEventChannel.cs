using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Core/Events/Int Event Channel")]
    public class IntEventChannel : ScriptableObject
    {
        public event Action<int> OnRaised;
        public void Raise(int value) => OnRaised?.Invoke(value);
    }
}
