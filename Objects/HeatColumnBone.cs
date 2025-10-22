using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;
using ReikaKalseki.SeaToSea;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class HeatColumnBone : Spawnable {

		private string basis;

		internal static readonly Dictionary<string, string> basisTable = new Dictionary<string, string>();

		public static readonly Dictionary<string, Vector3> boneProps = new Dictionary<string, Vector3>{
			{ "08b4a416-2cdf-4c6b-8772-f58255e525d7", Vector3.one },
			{ "33c42808-c360-42b6-954d-5f10d0bffdeb", Vector3.one*0.5F },
			{ "42e1ac56-6fab-4a9f-95d9-eec5707fe62b", Vector3.one*0.75F },
			{ "4a4b434b-ec77-4217-b41d-bd9cf646d308", Vector3.one },
			{ "501c0536-7993-4ed6-be77-6287cedd8d02", Vector3.one },
			{ "6be26bed-91eb-42b9-be92-314d3bd028d6", Vector3.one },
			{ "6bf7e935-6e27-4b93-bc9c-25b7ec95c45e", Vector3.one },
			{ "70c0c560-1a47-46ea-9659-30c8072eb792", Vector3.one*0.5F },
			{ "7c5425d4-2339-436c-822a-d6b3922b489a", Vector3.one },
			{ "949d8657-1e5c-4418-8948-76b8b712fc57", Vector3.one*0.75F },
			{ "db44e245-1bf5-42b7-9da2-ab7c33e91241", Vector3.one*0.5F },
			{ "e64676d7-0648-4f1e-9ab0-8e37ec877ef9", Vector3.one },
		};

		internal HeatColumnBone(string s) : base("HeatColumnBone_" + s, "", "") {
			basis = s;
			OnFinishedPatching += () => {
				basisTable[ClassID] = basis;
			};
		}

		public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject(basis);
			world.EnsureComponent<HeatColumnBoneTag>().basis = basis;
			world.fullyEnable();
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			Renderer r = world.GetComponentInChildren<Renderer>();
			//r.transform.localScale = boneProps[basis];
			r.materials[0].SetColor("_Color", new Color(0.95F, 1.1F, 1.3F));
			return world;
		}
	}

	public class HeatColumnObject : MonoBehaviour {

		protected Rigidbody body;

		protected float speedFactor = UnityEngine.Random.Range(0.5F, 1.5F);
		protected float scaleFactor = UnityEngine.Random.Range(0.5F, 1F);

		internal void Update() {
			if (!body)
				body = gameObject.EnsureComponent<Rigidbody>();

			if (transform.position.y >= -5 && !gameObject.FindAncestor<StorageContainer>())
				gameObject.destroy();
			else if ((transform.position-Player.main.transform.position).sqrMagnitude >= 50000)
				gameObject.destroy();
		}

		void OnDisable() {
			if (!GetComponent<Pickupable>())
				gameObject.destroy(false);
		}
	}

	internal class HeatColumnBoneTag : HeatColumnObject {

		internal string basis;

		private Vector3 spin = MathUtil.getRandomVectorAround(Vector3.zero, 1)*0.6F;

		void Start() {
			
		}

		new void Update() {
			base.Update();
			if (string.IsNullOrEmpty(basis))
				basis = HeatColumnBone.basisTable[GetComponent<PrefabIdentifier>().ClassId];
			transform.localScale = HeatColumnBone.boneProps[basis]* scaleFactor;
			if (body) {
				body.mass = 50;
				body.isKinematic = false;
				//body.AddForce(Vector3.up * Time.deltaTime * 50, ForceMode.Acceleration);
				body.velocity = Vector3.up * 2.5F * speedFactor;
				body.angularVelocity = spin;
			}
		}

	}
}
