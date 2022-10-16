using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SaveAPI
{
    public class SpecialAIActor : MonoBehaviour
    {
        public bool SetsCustomFlagOnActivation;
        public CustomDungeonFlags CustomFlagToSetOnActivation;
        public bool SetsCustomFlagOnDeath;
        public CustomDungeonFlags CustomFlagToSetOnDeath;
        public bool SetsCustomCharacterSpecificFlagOnDeath;
        public CustomCharacterSpecificGungeonFlags CustomCharacterSpecificFlagToSetOnDeath;
    }
}
