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
	
	public class LavaBombMushroom : GlowshroomBase<LavaShroomTag> { //hollow's lantern
		
		public LavaBombMushroom() : base("LAVASHROOM") {
			
		}
		
		public override Color getLightColor() {
			return new Color(1.0F, 0.5F, 0.1F, 1F);
		}
		
		protected override string getTextureSubfolder() {
			return "LavaBombMushroom";
		}

		protected override bool isExploitable() {
			return false;
		}
		
	}
	
	public class LavaShroomTag : GlowShroomTagBase {
		
		protected override void init() {
			
		}
		
		protected override void tick() {
			if (isGrown)		
				gameObject.transform.localScale = new Vector3(1.5F, 1.8F, 1.5F);
		}
		
		protected override float getMinimumAllowableDepth() {
			return 1200;
		}
		
		protected override float getNextFireInterval() {
			return UnityEngine.Random.Range(20F, 40F);
		}
		
		protected override float getSize() {
			return 1.5F;
		}
		
		protected override float getFireDistance() {
			return 200;
		}
		
		protected override GameObject createProjectile() {
			GameObject go = ObjectUtil.createWorldObject(EcoceanMod.lavaBomb.ClassID);
			return go;
		}
		
		protected override void updateBrightness(float f) {
			f = Mathf.Min(0.5F, f)*2; //0.5 is the max it reaches before the quick burst before firing
			//if (isGrown)
			//	f = 0;
			foreach (Renderer r in renderers) {
				RenderUtil.setEmissivity(r.materials[0], 0.5F+f*1.5F);
				Color c = Color.Lerp(Color.white, new Color(0.8F, 0, 0, 1), 1-f);
				//if (isGrown)
				//	c = Color.black;
				r.materials[0].SetColor("_GlowColor", c);				
			}
		}
		
		internal override void onFire(GameObject go) {
			go.GetComponent<LavaBombTag>().onFired();
		}
		
		protected override float getFireVelocity() {
			return UnityEngine.Random.Range(1F, 2.5F);
		}
		
		protected override DIPrefab<FloraPrefabFetch> getPrefab() {
			return EcoceanMod.lavaShroom;
		}
		
	}
}
