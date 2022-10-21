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
	
	internal class LavaBombMushroom : GlowshroomBase<LavaShroomTag> { //hollow's lantern
		
		public LavaBombMushroom() : base("LAVASHROOM") {
			
		}
		
		public override Color getLightColor() {
			return new Color(1.0F, 0.5F, 0.1F, 1F);
		}
		
		protected override string getTextureSubfolder() {
			return "LavaBombMushroom";
		}
		
	}
	
	internal class LavaShroomTag : GlowShroomTagBase {
		
		protected override void init() {
			
		}
		
		protected override void tick() {
			
		}
		
		protected override float getMinimumAllowableDepth() {
			return 1200;
		}
		
		protected override float getFireRate() {
			return 2;
		}
		
		protected override float getSize() {
			return 1.5F;
		}
		
		protected override GameObject createProjectile() {
			GameObject go = ObjectUtil.createWorldObject(EcoceanMod.lavaBomb.ClassID);
			go.GetComponent<LavaBombTag>().onFired();
			return go;
		}
		
	}
}
