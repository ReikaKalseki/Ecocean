using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting;
using UnityEngine.UI;
using System.Collections.Generic;
using ReikaKalseki.DIAlterra;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

namespace ReikaKalseki.Ecocean
{
	public class BaseCellEnviroHandler : MonoBehaviour {
		
		public Base seabase;
		public BaseCell cell;
		public int baseSize;
		
		private bool hasHatch;
		private bool hasMoonpool;
		private Bounds boundingBox;
		
		private float timeSinceEnviro;
		private BiomeBase currentBiome;
		private float temperature;
		private float depth;
		private float planktonSpawnRate;
		
		private float timeSincePlankton;
		private GameObject plankton;
		
		private float age;
		
		void Update() {
			if (!cell)
				cell = GetComponent<BaseCell>();
			if ((currentBiome == null || timeSinceEnviro > 120) && DIHooks.isWorldLoaded() && (transform.position-Player.main.transform.position).sqrMagnitude < 160000) {
				if (currentBiome == null) //do not recompute this just because 2 min since enviro
					computeBaseCell();
				computeEnvironment();
			}
			
			float dT = Time.deltaTime;
			
			age += dT;
			timeSinceEnviro += dT;
			
			if (plankton) {
				timeSincePlankton = 0;
				if (planktonSpawnRate <= 0)
					UnityEngine.Object.Destroy(plankton);
			}
			else if (age > 120 && planktonSpawnRate > 0 && depth > 5 && (hasHatch || hasMoonpool) && baseSize > 2 && (Player.main.transform.position-transform.position).sqrMagnitude < 40000) {
				timeSincePlankton += dT;
				float iVal = (float)MathUtil.linterpolate(baseSize, 5, 30, 60, 10, true);
				if (timeSincePlankton >= iVal) {
					if (UnityEngine.Random.Range(0F, 1F) <= planktonSpawnRate) {
						plankton = ObjectUtil.createWorldObject(EcoceanMod.plankton.ClassID);
						plankton.transform.position = MathUtil.getRandomVectorAround(boundingBox.RandomWithin(), new Vector3(5, 0, 5));
						plankton.transform.localScale = Vector3.one*0.5F;
						ObjectUtil.fullyEnable(plankton);
						plankton.GetComponent<PlanktonCloudTag>().isBaseBound = this;
					}
				}
			}
		}
		
		void OnDisable() {
			OnDestroy();
		}
		
		void OnDestroy() {
			if (plankton)
				UnityEngine.Object.DestroyImmediate(plankton);
		}
		
		public void computeBaseCell() {			
			UseableDiveHatch h = GetComponentInChildren<UseableDiveHatch>();
			hasHatch = h && !h.isForEscapePod && !h.isForWaterPark && !h.IsInside() && !h.GetOnLand();
			
			hasMoonpool = (bool)GetComponentInChildren<VehicleDockingBay>();
			
			boundingBox = new Bounds(cell.transform.position, Vector3.zero);
			foreach (Collider c in cell.GetComponentsInChildren<Collider>()) {
				if (c.gameObject.GetFullHierarchyPath().Contains("/AdjustableSupport/"))
					continue;
				boundingBox.Encapsulate(c.bounds);
			}
			
			if (!seabase)
				seabase = gameObject.FindAncestor<Base>();
			baseSize = seabase.GetComponentsInChildren<BaseCell>().Length;
		}
		
		public void computeEnvironment() {
			depth = -transform.position.y;
			currentBiome = BiomeBase.getBiome(transform.position);
			if (currentBiome == VanillaBiomes.VOID) { //void base very unlikely, probably loaded in with null
				currentBiome = null;
				return;
			}
			temperature = WaterTemperatureSimulation.main.GetTemperature(transform.position);
			
			planktonSpawnRate = transform.position.y < -500 ? 0 : MushroomVaseStrand.getSpawnRate(currentBiome); //there are no plankton spawns below -500
			SNUtil.log("Computed plankton spawn rate of "+planktonSpawnRate+" for base cell "+transform.position+" ("+currentBiome.displayName+")");
			
			timeSinceEnviro = 0;
		}
		
	}
}
