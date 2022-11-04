using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean
{
	public class ECConfig
	{		
		public enum ConfigEntries {
			[ConfigEntry("Luma Oil Lifespan Factor", typeof(float), 1, 0.2F, 5F, 1)]GLOWLIFE,
			[ConfigEntry("Lava Oil Item Value", typeof(int), 1, 1, 6, 0)]GLOWCOUNT,
			[ConfigEntry("Lumashroom Fertility Factor", typeof(float), 1, 0.2F, 2F, 1)]GLOWFIRERATE,
			[ConfigEntry("Bloodvine Attack Damage Factor", typeof(float), 1, 0.1F, 10F, 1)]BLOODDMG,
			[ConfigEntry("Anchor Pod Detonation Damage Factor", typeof(float), 1, 0.1F, 10F, 1)]ANCHORDMG,
			[ConfigEntry("Lava Bomb Damage Factor", typeof(float), 1, 0.1F, 10F, 1)]BOMBDMG,
		}
	}
}
