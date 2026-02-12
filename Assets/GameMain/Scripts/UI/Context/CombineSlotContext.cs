using System;
using Definition.Enum;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Runtime slot data passed from external system.
    /// </summary>
    [Serializable]
    public sealed class CombineSlotContext
    {
        public CombinePartType RequiredPartType = CombinePartType.Dou;

        public int BuildOrder = 0;

        public bool RequireStrictOrder = true;

        public Vector2 AnchoredPosition = Vector2.zero;

        public Vector2 SizeDelta = new Vector2(120f, 120f);

        public string MechanicsExplanation = string.Empty;

        public string MismatchHint = string.Empty;
    }

}