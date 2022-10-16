using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SaveAPI
{
    /// <summary>
    /// A custom component that allows <see cref="PickupObject"/>s to set custom SaveAPI flags when acquired. Mostly used for custom breach shop blueprints
    /// </summary>
    public class SpecialPickupObject : MonoBehaviour
    {
        /// <summary>
        /// The custom SaveAPI flag that will be set when the object is acquired
        /// </summary>
        public CustomDungeonFlags CustomSaveFlagToSetOnAcquisition;
    }
}
