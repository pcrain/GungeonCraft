using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class LabelablePlayerItem : PlayerItem
    {
        public void SetLabel(string label)
        {
            currentLabel = label;
        }
        public string currentLabel;
    }
}