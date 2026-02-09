using System;
using Definition.Enum;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Runtime part data passed from external system.
    /// </summary>
    [Serializable]
    public sealed class CombinePartContext
    {
        public CombinePartType PartType = CombinePartType.Dou;

        public string PartDisplayName = "Dou";

        public string MechanicsExplanation = string.Empty;

        public bool LockAfterPlaced = true;
    }
}