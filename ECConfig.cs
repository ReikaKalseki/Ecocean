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
			[ConfigEntry("Lumashroom Fertility Factor", typeof(float), 1, 0.2F, 2F, 1)]GLOWFIRERATE,
		}
	}
}
