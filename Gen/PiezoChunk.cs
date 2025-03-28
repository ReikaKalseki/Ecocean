using System;
using System.Collections.Generic;
using System.Xml;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Crafting;

using UnityEngine;
using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean
{
	public sealed class PiezoChunk : WorldGenerator {
		
		static PiezoChunk() {	
		
		}
		
		public PiezoChunk(Vector3 pos) : base(pos) {
			
		}
		
		public override void loadFromXML(XmlElement e) {
			
		}
		
		public override void saveToXML(XmlElement e) {
			
		}
		
		public override bool generate(List<GameObject> generated) {
			int n = UnityEngine.Random.Range(4, 7); //4-6 horizontal
			Vector3[] angs = new Vector3[n+2];
			for (int i = 0; i < n; i++) {
				float ang = (360F/n)*i;
				ang += UnityEngine.Random.Range(-15F, 15F);
				angs[i] = new Vector3(UnityEngine.Random.Range(0, 360F), ang, UnityEngine.Random.Range(60F, 120F));
			}
			angs[angs.Length-1] = new Vector3(UnityEngine.Random.Range(-30F, 30F), UnityEngine.Random.Range(0, 360F), 0);
			angs[angs.Length-2] = new Vector3(UnityEngine.Random.Range(150F, 210F), UnityEngine.Random.Range(0, 360F), 0);
			foreach (Vector3 ang in angs) {
				GameObject go = ObjectUtil.createWorldObject(EcoceanMod.piezo.ClassID);
				go.transform.position = position;
				go.transform.rotation = Quaternion.Euler(ang.x, ang.y, ang.z);//UnityEngine.Random.rotationUniform;
				go.transform.position = go.transform.position+go.transform.up*UnityEngine.Random.Range(0.25F, 0.5F);
				generated.Add(go);
			}
			return true;
		}
		
		public override LargeWorldEntity.CellLevel getCellLevel() {
			return LargeWorldEntity.CellLevel.VeryFar;
		}
	}
}
