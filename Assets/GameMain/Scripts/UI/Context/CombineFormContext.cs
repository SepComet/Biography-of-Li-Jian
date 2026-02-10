using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Open data for MVC-based GameplayA form.
    /// </summary>
    public sealed class CombineFormContext : UIContext
    {
        /// <summary>
        /// Slot definitions. Position and order are provided externally.
        /// </summary>
        public List<CombineSlotContext> Slots;

        /// <summary>
        /// Optional part definitions. When null/empty, parts are derived from slots.
        /// </summary>
        public List<CombinePartContext> Parts;

        /// <summary>
        /// Auto start puzzle after runtime nodes are generated.
        /// </summary>
        public bool AutoStart = true;
    }
}