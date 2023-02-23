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
	public sealed class ScatteredPiezoGenerator : WorldGenerator {
		
		private int numberToGen = 1;
		private Vector3 scatterRange = Vector3.zero;
		
		static ScatteredPiezoGenerator() {	
		
		}
		
		public ScatteredPiezoGenerator(int n, Vector3 pos, Vector3 range) : base(pos) {
			numberToGen = n;
			scatterRange = range;
		}
		
		public override void loadFromXML(XmlElement e) {
			numberToGen = e.getInt("number", 0, false);
			scatterRange = e.getVector("range").Value;
		}
		
		public override void saveToXML(XmlElement e) {
			e.addProperty("number", numberToGen);
			e.addProperty("range", scatterRange);
		}
		
		public override void generate(List<GameObject> generated) {
			for (int i = 0; i < numberToGen; i++) {
				Vector3 pos = MathUtil.getRandomVectorAround(position, scatterRange);
				GameObject go = ObjectUtil.createWorldObject(EcoceanMod.piezo.ClassID);
				go.transform.position = pos;
				go.transform.rotation = UnityEngine.Random.rotationUniform;
				generated.Add(go);
			}
		}
		
		public override string ToString() {
			return base.ToString()+" x"+numberToGen+" in R=["+scatterRange+"]";
		}
	}
}
