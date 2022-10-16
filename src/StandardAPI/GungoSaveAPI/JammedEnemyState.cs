using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SaveAPI
{
    /// <summary>
    /// An enum with 3 jammed enemy states mainly used to check if an enemy is valid
    /// </summary>
	public enum JammedEnemyState
    {
        /// <summary>
        /// Enemy is always valid
        /// </summary>
        NoCheck,
        /// <summary>
        /// Requires jammed enemy
        /// </summary>
        Jammed,
        /// <summary>
        /// Requires unjammed enemy
        /// </summary>
        Unjammed
    }
}
