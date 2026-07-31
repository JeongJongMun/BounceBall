using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Core/Events/String Event Channel")]
    public class StringEventChannel : ScriptableObject
    {
        public event Action<string> OnRaised;
        public void Raise(string value) => OnRaised?.Invoke(value);
    }
}
