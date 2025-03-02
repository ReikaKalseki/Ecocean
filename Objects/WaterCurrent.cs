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
using SMLHelper.V2.Assets;

namespace ReikaKalseki.Ecocean {
	
	public static class WaterCurrent {
		
		public static void register() {
			WaterCurrentBasic basic = new WaterCurrentBasic();
			basic.Patch();
			new WaterCurrentStrong().Patch();
			new WaterCurrentHot().Patch();
			new WaterCurrentHotStrong().Patch();
			new WaterCurrentImpassable().Patch();
			
			XMLLocale.LocaleEntry e = EcoceanMod.locale.getEntry("WaterCurrent");
        	EcoceanMod.waterCurrentCommon = TechTypeHandler.AddTechType(EcoceanMod.modDLL, e.key, e.name, e.desc);
			SNUtil.addPDAEntry(basic, 5, "PlanetaryGeology", e.pda, e.getField<string>("header"), d => d.key = EcoceanMod.waterCurrentCommon);
		}
		
	}
	
	public abstract class WaterCurrentBase<T> : Spawnable where T : WaterCurrentTag {
	        
		internal WaterCurrentBase() : base("WaterCurrent_"+typeof(T).Name.Replace("CurrentTag", ""), "Water Current", "") {

	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("42b38968-bd3a-4bfd-9d93-17078d161b29");
			world.EnsureComponent<TechTag>().type = EcoceanMod.waterCurrentCommon;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Medium;
			ObjectUtil.removeChildObject(world, "xCurrenBubbles");
			world.layer = LayerID.Useable;
			world.EnsureComponent<T>();
			world.name = ClassID+"[Clone]";
			return world;
	    }
			
	    protected override void ProcessPrefab(GameObject world) {
			base.ProcessPrefab(world);
			world.EnsureComponent<TechTag>().type = EcoceanMod.waterCurrentCommon;
	    }
			
	}
	
	public class WaterCurrentBasic : WaterCurrentBase<BasicCurrentTag> {
		
	}
	
	public class WaterCurrentStrong : WaterCurrentBase<StrongCurrentTag> {
		
	}
	
	public class WaterCurrentHot : WaterCurrentBase<HotCurrentTag> {
		
	}
	
	public class WaterCurrentHotStrong : WaterCurrentBase<HotStrongCurrentTag> {
		
	}
	
	public class WaterCurrentImpassable : WaterCurrentBase<ImpassableCurrentTag> {
		
	}
	
	public class BasicCurrentTag : WaterCurrentTag {
		
		internal BasicCurrentTag() : base(false, 10) {
			
		}
		
	}
	
	public class HotCurrentTag : WaterCurrentTag {
		
		internal HotCurrentTag() : base(true, 10) {
			
		}
		
	}
	
	public class StrongCurrentTag : WaterCurrentTag {
		
		internal StrongCurrentTag() : base(false, 18) {
			
		}
		
	}
	
	public class ImpassableCurrentTag : WaterCurrentTag {
		
		internal ImpassableCurrentTag() : base(false, 27.5F) {
			
		}
		
	}
	
	public class HotStrongCurrentTag : WaterCurrentTag {
		
		internal HotStrongCurrentTag() : base(true, 18) {
			
		}
		
	}
		
	public abstract class WaterCurrentTag : MonoBehaviour {
		
		internal readonly bool isHotWater;
		internal readonly float currentStrength;
		
		private float age;
		
		internal WaterCurrentTag(bool temp, float str) {
			isHotWater = temp;
			currentStrength = str;
		}
		
		private Current current;
		private Renderer render;
		
		void Update() {
			if (!current)
				current = GetComponent<Current>();
			if (!render)
				render = GetComponentInChildren<Renderer>();
			
			age += Time.deltaTime;
			
			if (age > 1 && age < 2 && Vector3.Distance(transform.position, Vector3.zero) <= 10) {
				UnityEngine.Object.Destroy(gameObject);
				return;
			}
			
			current.objectForce = currentStrength;
			current.activeAtDay = true;
			current.activeAtNight = true;
			if (isHotWater) {
				foreach (Rigidbody rb in current.rigidbodyList) {
					if (ObjectUtil.isPlayer(rb)) {
						rb.gameObject.FindAncestor<LiveMixin>().TakeDamage(4*Time.deltaTime, rb.transform.position, DamageType.Heat, gameObject);
					}
				}
				Color c = new Color(1.25F, 1, 1);
				render.materials[0].SetColor("_Color", c);
				render.materials[0].color = c;
			}
			else if (currentStrength >= 20) {
				Color c = new Color(1F, 1, 1.25F);
				render.materials[0].SetColor("_Color", c);
				render.materials[0].color = c;
			}
		}
		
		public float getCurrentStrength(Vector3 pos) {
			float len = transform.localScale.z*21/2F+2.5F;
			Vector3 pt1 = transform.position+transform.forward*len;
			Vector3 pt2 = transform.position-transform.forward*len;
			if (isInCylinder(pos, pt1, pt2))
				return 1;
			float dist = Mathf.Min(Vector3.Distance(pos, pt1), Vector3.Distance(pos, pt2));
			float f = dist/(64F*transform.localScale.z);
			if (currentStrength >= 20)
				f *= 0.75F;
			return f >= 1 ? 0 : Mathf.Sqrt(1-f);
		}
		
		internal bool isInCylinder(Vector3 pos, Vector3 pt1, Vector3 pt2) {
			Vector3 vec = pt2 - pt1;
			return Vector3.Dot(pos - pt1, vec) >= 0 && Vector3.Dot(pos - pt2, vec) <= 0;
		}
		
		void OnDestroy() {
			
		}
		
		void OnDisable() {
			
		}
		
	}
}
