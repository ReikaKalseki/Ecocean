using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;

using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean {
	
	public class GlowOilMushroom : GlowshroomBase<GlowShroomTag> {
		
		public GlowOilMushroom() : base("GLOWSHROOM") {
			
		}
		
		public override Color getLightColor() {
			return new Color(0.4F, 0.7F, 1F, 1F);
		}
		
		protected override string getTextureSubfolder() {
			return "GlowOilMushroom";
		}
		
	}
	
	class GlowShroomTag : GlowShroomTagBase {
		
		protected override void init() {
			
		}
		
		protected override void tick() {
			
		}
		
		protected override float getMinimumAllowableDepth() {
			return 200;
		}
		
		protected override float getFireRate() {
			return EcoceanMod.config.getFloat(ECConfig.ConfigEntries.GLOWFIRERATE)
		}
		
		protected override GameObject createProjectile() {
			GameObject go = ObjectUtil.createWorldObject(EcoceanMod.glowOil.ClassID);
			go.GetComponent<GlowOilTag>().onLit();
			return go;
		}
		
	}
}
