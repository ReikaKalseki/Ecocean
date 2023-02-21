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
			new WaterCurrentBasic().Patch();
			new WaterCurrentStrong().Patch();
			new WaterCurrentHot().Patch();
			new WaterCurrentHotStrong().Patch();
			new WaterCurrentImpassable().Patch();
		}
		
	}
	
	public abstract class WaterCurrentBase<T> : Spawnable where T : WaterCurrentTag {
	        
		internal WaterCurrentBase() : base("WaterCurrent_"+typeof(T).Name.Replace("CurrentTag", ""), "Water Current", "") {
			
	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("42b38968-bd3a-4bfd-9d93-17078d161b29");
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Medium;
			ObjectUtil.removeChildObject(world, "xCurrenBubbles");
			world.EnsureComponent<T>();
			return world;
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
		
		internal ImpassableCurrentTag() : base(false, 25) {
			
		}
		
	}
	
	public class HotStrongCurrentTag : WaterCurrentTag {
		
		internal HotStrongCurrentTag() : base(true, 18) {
			
		}
		
	}
		
	public abstract class WaterCurrentTag : MonoBehaviour {
		
		internal readonly bool isHotWater;
		internal readonly float currentStrength;
		
		internal WaterCurrentTag(bool temp, float str) {
			isHotWater = temp;
			currentStrength = str;
		}
		
		private Current current;
		
		void Update() {
			if (!current)
				current = GetComponent<Current>();
			current.objectForce = currentStrength;
			current.activeAtDay = true;
			current.activeAtNight = true;
			if (isHotWater) {
				foreach (Rigidbody rb in current.rigidbodyList) {
					if (rb.gameObject.FindAncestor<Player>()) {
						rb.gameObject.FindAncestor<LiveMixin>().TakeDamage(4*Time.deltaTime, rb.transform.position, DamageType.Heat, gameObject);
					}
				}
			}
		}
		
		void OnDestroy() {
			
		}
		
		void OnDisable() {
			
		}
		
	}
}
