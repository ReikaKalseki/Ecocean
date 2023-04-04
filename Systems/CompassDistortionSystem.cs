using System;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using ReikaKalseki.DIAlterra;
using ReikaKalseki.Ecocean;

namespace ReikaKalseki.Ecocean {
	
	public class CompassDistortionSystem {
		
		public static readonly CompassDistortionSystem instance = new CompassDistortionSystem();
		
		private readonly List<LocalDistortion> localEffects = new List<LocalDistortion>();
		
		private Simplex1DGenerator globalDistortionCutoff;
		private Simplex1DGenerator globalDistortion;
		
		private float empDisplacement;
		private float lastEMPHitTime;
		private float empSpinTime;
		
		private CompassDistortionSystem() {
			addRegionalDistortion(new PointRadiusDistortion(new Vector3(445, -50, 1200), 400, 360, 0.03F)); //QEP
			addRegionalDistortion(new PointRadiusDistortion(new Vector3(-45, -1210, 115), 250, 360, 0.16F)); //ATP
			addRegionalDistortion(new BiomeDistortion(VanillaBiomes.UNDERISLANDS, 360, 0.35F));
		}
		
		public void addRegionalDistortion(LocalDistortion ld) {
			localEffects.Add(ld);
		}
		
		internal void tick(float time, float dT) {
			float dE = time-lastEMPHitTime;
			if (dE < empSpinTime) {
				float f = dE/empSpinTime;
				float speed = 0;
				if (f < 0.5F)
					speed = 1F;
				else if (f < 0.75F)
					speed = 1F-(f-0.5F);
				else
					speed = 0.75F-(f-0.75F)*3;
				if (speed > 0)
					empDisplacement += Mathf.Max(30, speed*speed*900)*dT; //deg/s
				//SNUtil.writeToChat("emp = "+dE.ToString("0.0000")+" > "+f.ToString("0.0000")+" > "+speed.ToString("0.0000")+" > "+empDisplacement.ToString("0.0000"));
			}
			else if (Math.Abs(empDisplacement) >= 1) {
				empDisplacement = Mathf.Min(empDisplacement%360F+30*dT, 360)%360F;
				//SNUtil.writeToChat(empDisplacement.ToString("000.0000"));
			}
			else {
				empDisplacement = 0;
			}
		}
		
		/** Returns degrees */
		public float getTotalDisplacement(Vector3 pos) {
			float ret = empDisplacement;
			getOrCreateGlobalNoise();
			float time = DayNightCycle.main.timePassedAsFloat;
			float noise = (float)globalDistortionCutoff.getValue(new Vector3(time, 0, 0));
			float globalThresh = 1F-2*Mathf.Clamp01(EcoceanMod.config.getFloat(ECConfig.ConfigEntries.GLOBALCOMPASS)); //config is 0-1 for "how often"
			//SNUtil.writeToChat("Global noise level = "+noise.ToString("0.0000")+"/"+globalThresh.ToString("0.0000"));
			if (noise >= globalThresh) {
				ret += (float)globalDistortion.getValue(new Vector3(time, 0, 0))*360F;
				//SNUtil.writeToChat("Disp = "+(globalDistortion.getValue(new Vector3(time, 0, 0))*360F).ToString("00.0000"));
			}
			foreach (LocalDistortion ld in localEffects) {
				if (ld.isInRange(pos)) {
					ret += ld.calculate(time);
				}
			}
			return ret%360F;
		}
		
		public void onHitByEMP(EMPBlast emp, float intensityFactor) {
			lastEMPHitTime = DayNightCycle.main.timePassedAsFloat;
			empSpinTime = intensityFactor+(emp ? emp.disableElectronicsTime : 0)*(2+intensityFactor);
		}
		
		private void getOrCreateGlobalNoise() {
			long use = SaveLoadManager.main.firstStart;
			if (globalDistortion == null || globalDistortion.seed != use) {
				globalDistortion = (Simplex1DGenerator)new Simplex1DGenerator(use).setFrequency(0.07F);
			}
			if (globalDistortionCutoff == null || globalDistortionCutoff.seed != use) {
				globalDistortionCutoff = (Simplex1DGenerator)new Simplex1DGenerator(use).setFrequency(0.011F);
			}
		}
		
		public abstract class LocalDistortion {
			
			internal readonly float magnitude; //degree deviation limit, up to 360
		
			internal readonly Simplex1DGenerator noiseFactor;
			
			protected LocalDistortion(float a, float f, long seed) {
				magnitude = a;
				noiseFactor = (Simplex1DGenerator)new Simplex1DGenerator(seed).setFrequency(f);
			}
			
			internal float calculate(float time) {
				return (float)noiseFactor.getValue(new Vector3(time, 0, 0))*magnitude;
			}
			
			internal abstract bool isInRange(Vector3 pos);
		}
	
		public class BiomeDistortion : LocalDistortion {
			
			internal readonly BiomeBase biome;
			
			public BiomeDistortion(BiomeBase b, float a, float f) : base(a, f, b.GetHashCode()) {
				biome = b;
			}
			
			internal override bool isInRange(Vector3 pos) {
				return biome.isInBiome(pos);
			}
			
		}
	
		public class PointRadiusDistortion : LocalDistortion {
			
			internal readonly Vector3 position;
			internal readonly float radius;
			
			public PointRadiusDistortion(Vector3 pos, float r, float a, float f) : base(a, f, (pos.x.GetHashCode() | (pos.z.GetHashCode() << 32)) ^ (pos.y*20F).GetHashCode()) {
				position = pos;
				radius = r;
			}
			
			internal override bool isInRange(Vector3 pos) {
				return (position-pos).sqrMagnitude <= radius*radius;
			}
			
		}
	}
	
}
