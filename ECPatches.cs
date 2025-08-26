using System;
using System.Collections;   //Working with Lists and Collections
using System.Collections.Generic;   //Working with Lists and Collections
using System.IO;    //For data read/write methods
using System.Linq;   //More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using ReikaKalseki.DIAlterra;

using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.

namespace ReikaKalseki.Ecocean {

	static class ECPatches {

		[HarmonyPatch(typeof(CyclopsHornButton))]
		[HarmonyPatch("OnPress")]
		public static class CyclopsHornHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Callvirt, "FMOD_CustomEmitter", "Play", true, new Type[0]);
					codes.Insert(idx, InstructionHandlers.createMethodCall("ReikaKalseki.Ecocean.ECHooks", "honkCyclopsHorn", false, typeof(CyclopsHornButton)));
					codes.Insert(idx, new CodeInstruction(OpCodes.Ldarg_0));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(Geyser))]
		[HarmonyPatch("OnTriggerStay")]
		public static class GeyserDamageTick {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					codes.patchInitialHook(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1), InstructionHandlers.createMethodCall("ReikaKalseki.Ecocean.ECHooks", "tickObjectInGeyser", false, typeof(Geyser), typeof(Collider)));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(Current))]
		[HarmonyPatch("Update")]
		public static class CurrentTick {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Callvirt, "UnityEngine.Rigidbody", "AddForce", true, new Type[]{typeof(Vector3), typeof(ForceMode)});
					codes[idx].operand = InstructionHandlers.convertMethodOperand("ReikaKalseki.Ecocean.ECHooks", "applyCurrentForce", false, typeof(Rigidbody), typeof(Vector3), typeof(ForceMode), typeof(Current));
					codes.Insert(idx, new CodeInstruction(OpCodes.Ldarg_0));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(uGUI_DepthCompass))]
		[HarmonyPatch("UpdateCompass")]
		public static class OverrideHUDCompass {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Callvirt, "uGUI_Compass", "set_direction", true, new Type[]{typeof(float)});
					codes[idx].operand = InstructionHandlers.convertMethodOperand("ReikaKalseki.Ecocean.ECHooks", "setHUDCompassDirection", false, typeof(uGUI_Compass), typeof(float));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(CyclopsCompassHUD))]
		[HarmonyPatch("Update")]
		public static class OverrideCyclopsCompass {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Callvirt, "UnityEngine.Transform", "set_rotation", true, new Type[]{typeof(Quaternion)});
					codes[idx].operand = InstructionHandlers.convertMethodOperand("ReikaKalseki.Ecocean.ECHooks", "setCyclopsCompassDirection", false, typeof(Transform), typeof(Quaternion));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}
		/*
        [HarmonyPatch(typeof(SeamothTorpedoWhirlpool))]
        [HarmonyPatch("Update")]
        public static class TickVortexTorpedo {

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                InsnList codes = new InsnList(instructions);
                try {
                    int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Ldnull);
                    codes.InsertRange(idx, new InsnList{new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Ecocean.ECHooks", "tickVortexTorpedo", false, typeof(SeamothTorpedoWhirlpool))});
                    FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
                }
                catch (Exception e) {
                    FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
                    FileLog.Log(e.Message);
                    FileLog.Log(e.StackTrace);
                    FileLog.Log(e.ToString());
                }
                return codes.AsEnumerable();
            }
        }*/

		[HarmonyPatch(typeof(Geyser))]
		[HarmonyPatch("Start")]
		public static class GeyserSpawn {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					codes.patchInitialHook(new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Ecocean.ECHooks", "onGeyserSpawn", false, typeof(Geyser)));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(MeleeAttack))]
		[HarmonyPatch("OnTouch")]
		public static class MeleeBiteabilityHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Callvirt, "MeleeAttack", "CanBite", true, new Type[]{typeof(GameObject)});
					codes[idx].operand = InstructionHandlers.convertMethodOperand("ReikaKalseki.Ecocean.ECHooks", "canMeleeBite", false, typeof(MeleeAttack), typeof(GameObject));
					FileLog.Log("Done patch " + MethodBase.GetCurrentMethod().DeclaringType);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(MeleeAttack))]
		[HarmonyPatch("GetTarget")]
		public static class MeleeBaseTargetHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList();
				try {
					codes.add(OpCodes.Ldarg_0);
					codes.add(OpCodes.Ldarg_1);
					codes.invoke("ReikaKalseki.Ecocean.ECHooks", "getMeleeTarget", false, typeof(MeleeAttack), typeof(Collider));
					codes.add(OpCodes.Ret);
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}
		}

		[HarmonyPatch(typeof(FiltrationMachine))]
		[HarmonyPatch("UpdateFiltering")]
		public static class WaterFilterSaltRateHook {

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				InsnList codes = new InsnList(instructions);
				try {
					int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Stfld, "FiltrationMachine", "timeRemainingSalt");
					idx = InstructionHandlers.getLastOpcodeBefore(codes, idx, OpCodes.Ldloc_S);
					codes.InsertRange(idx + 1, new InsnList { new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall("ReikaKalseki.Ecocean.ECHooks", "getWaterFilterSaltTickTime", false, typeof(float), typeof(FiltrationMachine)) });
				}
				catch (Exception e) {
					FileLog.Log("Caught exception when running patch " + MethodBase.GetCurrentMethod().DeclaringType + "!");
					FileLog.Log(e.Message);
					FileLog.Log(e.StackTrace);
					FileLog.Log(e.ToString());
				}
				return codes.AsEnumerable();
			}

		}
	}
}
