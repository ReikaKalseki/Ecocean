﻿using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;

using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean {
	
	internal class GlowOilMushroom : GlowshroomBase<GlowShroomTag> {
		
		public GlowOilMushroom() : base("GLOWSHROOM") {
			
		}
		
		public override Color getLightColor() {
			return new Color(0.4F, 0.7F, 1F, 1F);
		}
		
		protected override string getTextureSubfolder() {
			return "GlowOilMushroom";
		}
		
	}
	
	internal class GlowShroomTag : GlowShroomTagBase {
		
		private static readonly Vector3 reaperlessTripleVent = new Vector3(-1150, -243, -258);
		
		protected override void init() {
			
		}
		
		protected override void tick() {
			if (Vector3.Distance(transform.position, reaperlessTripleVent) <= 200)
    			UnityEngine.Object.Destroy(gameObject);
		}
		
		protected override float getMinimumAllowableDepth() {
			return 240;
		}
		
		protected override float getFireRate() {
			return EcoceanMod.config.getFloat(ECConfig.ConfigEntries.GLOWFIRERATE);
		}
		
		protected override GameObject createProjectile() {
			GameObject go = ObjectUtil.createWorldObject(EcoceanMod.glowOil.ClassID);
			go.GetComponent<GlowOilTag>().onLit();
			return go;
		}
		
	}
}
