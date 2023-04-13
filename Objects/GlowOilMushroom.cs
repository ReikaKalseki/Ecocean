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
	
	internal class GlowOilMushroom : GlowshroomBase<GlowShroomTag> {
		
		public GlowOilMushroom() : base("GLOWSHROOM") {
			//collectionMethod = HarvestType.None;
		}
		
		public override Color getLightColor() {
			return new Color(0.4F, 0.7F, 1F, 1F);
		}
		
		protected override string getTextureSubfolder() {
			return "GlowOilMushroom";
		}

		protected override bool isExploitable() {
			return true;
		}
		
	}
	
	internal class GlowShroomTag : GlowShroomTagBase {
		
		protected override void init() {
			
		}
		
		protected override void tick() {/*
			if (!isGrown) {
				if (Mathf.Min(Vector3.Distance(transform.position, northDuneBit), Vector3.Distance(transform.position, reaperlessTripleVent)) <= 200)
    				UnityEngine.Object.Destroy(gameObject);
			}*/
		}
		
		protected override float getMinimumAllowableDepth() {
			return 240;
		}
		
		protected override float getNextFireInterval() {
			return UnityEngine.Random.Range(30, 120F)*EcoceanMod.config.getFloat(ECConfig.ConfigEntries.GLOWFIRERATE);
		}
		
		protected override GameObject createProjectile() {
			GameObject go = ObjectUtil.createWorldObject(EcoceanMod.naturalOil.ClassID);
			return go;
		}
		
		internal override void onFire(GameObject go) {
			go.GetComponent<GlowOilTag>().onFired();
		}
		
	}
}
