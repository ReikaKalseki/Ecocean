using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class MountainCurrentSystem {

		public static readonly MountainCurrentSystem instance = new MountainCurrentSystem();

		private MountainCurrentSystem() {

		}

		public void registerFlowVector(int amt) {
			Transform t = MainCamera.camera.transform;
			if (Player.main.GetVehicle())
				t = Player.main.GetVehicle().transform;
			Vector3 vec = t.forward.setLength(amt);
			Vector2 flat = vec.XZ().normalized*amt;
			Vector2 posf = t.transform.position.XZ();
			string s = string.Format("{0},{1},{2},{3},{4},{5}", vec.x, vec.y, vec.z, t.position.x, t.position.y, t.position.z)+Environment.NewLine;
			string s2 = string.Format("{0},{1},{2},{3}", flat.x, flat.y, posf.x, posf.y)+Environment.NewLine;
			File.AppendAllText(Path.Combine(Path.GetDirectoryName(EcoceanMod.modDLL.Location), "mountain-flow-vectors.csv"), s);
			File.AppendAllText(Path.Combine(Path.GetDirectoryName(EcoceanMod.modDLL.Location), "mountain-flow-vectors-2D.csv"), s2);
		}

		public float getCurrentExposure(Vector3 position, Vector3 currentVec) {
			if (WorldUtil.isInCave(position) || WorldUtil.isInWreck(position))
				return 0;
			float d = 15;
			RaycastHit[] hits = Physics.RaycastAll(position, -currentVec.normalized, d);
			float minDist = 9999;
			foreach (RaycastHit hit in hits) {
				if (hit.transform) {
					if (hit.distance < minDist)
						minDist = hit.distance;
				}
			}
			float cutoff = 4;
			return minDist <= cutoff ? 0 : Mathf.Clamp01((minDist - cutoff) / (d - cutoff));
		}

		public Vector3 getCurrentVector(Vector3 position) {
			return Vector3.zero;
		}

		public Vector3 getNetCurrent(Vector3 position) {
			Vector3 current = this.getCurrentVector(position);
			return current * this.getCurrentExposure(position, current);
		}

	}

}
