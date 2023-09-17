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
			[ConfigEntry("Luma Oil Item Value", typeof(int), 4, 1, 6, 0)]GLOWCOUNT, //How many lumenshroom oil are collected per "grab"
			[ConfigEntry("Lumashroom Fertility Factor", typeof(float), 1, 0.2F, 2F, 1)]GLOWFIRERATE, //A multiplier for how often lumenshrooms fire
			[ConfigEntry("Bloodvine Attack Damage Factor", typeof(float), 1, 0.1F, 10F, 1)]BLOODDMG,
			[ConfigEntry("Anchor Pod Detonation Damage Factor", typeof(float), 1, 0.1F, 10F, 1)]ANCHORDMG,
			[ConfigEntry("Lava Bomb Damage Factor", typeof(float), 1, 0.1F, 10F, 1)]BOMBDMG, //How much damage a direct hit from a lava bomb does
			[ConfigEntry("Plankton Collection Multiplier", typeof(float), 1, 0.1F, 10F, 1)]PLANKTONRATE,
			[ConfigEntry("Magnetic Anomaly Frequency", typeof(float), 0.2F, 0F, 1F, 0)]GLOBALCOMPASS,
			[ConfigEntry("Leviathan Damage Immunity", typeof(float), 0.5F, 0F, 1F, 0)]LEVIIMMUNE, //To what degree to make leviathans immune to damage, fractionally (so 1 = 100% immune)
			[ConfigEntry("Electrical Defense Damage Cap", typeof(float), 20F, 1F, 1000F, 60F)]DEFENSECLAMP, //The maximum damage dealt by the seamoth electrical defense.
		}
	}
}
