using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class DynamicBonesFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_DynamicBones";
        public const string PluginName = "Dynamic Bones Fix";

        internal void Main() => Harmony.CreateAndPatchAll(typeof(Hooks));

        public static class Replacer
        {
            public static void AppendParticles(Transform bone, int parentIndex, float boneLength,
                DynamicBone dynamicBone)
            {
                DynamicBone.Particle particle = new DynamicBone.Particle
                {
                    m_Transform = bone,
                    m_ParentIndex = parentIndex
                };
                if (bone)
                {
                    particle.m_Position = particle.m_PrevPosition = bone.position;
                    particle.m_InitLocalPosition = bone.localPosition;
                    particle.m_InitLocalRotation = bone.localRotation;
                }
                else
                {
                    Transform parentTransform = dynamicBone.m_Particles[parentIndex].m_Transform;
                    if (dynamicBone.m_EndLength > 0f)
                    {
                        Transform parent = parentTransform.parent;
                        if (parent)
                        {
                            particle.m_EndOffset =
                                parentTransform.InverseTransformPoint(parentTransform.position * 2f - parent.position) *
                                dynamicBone.m_EndLength;
                        }
                        else
                        {
                            particle.m_EndOffset = new Vector3(dynamicBone.m_EndLength, 0f, 0f);
                        }
                    }
                    else
                    {
                        particle.m_EndOffset = parentTransform.InverseTransformPoint(
                            dynamicBone.transform.TransformDirection(dynamicBone.m_EndOffset) +
                            parentTransform.position);
                    }

                    particle.m_Position =
                        (particle.m_PrevPosition = parentTransform.TransformPoint(particle.m_EndOffset));
                }

                if (parentIndex >= 0)
                {
                    boneLength += (dynamicBone.m_Particles[parentIndex].m_Transform.position - particle.m_Position)
                        .magnitude;
                    particle.m_BoneLength = boneLength;
                    dynamicBone.m_BoneTotalLength = Mathf.Max(dynamicBone.m_BoneTotalLength, boneLength);
                }

                int count = dynamicBone.m_Particles.Count;
                dynamicBone.m_Particles.Add(particle);
                while (bone.childCount > 0)
                {
                    var isNotRoll = false;
                    var index = 0;
                    for (var i = 0; i < bone.childCount; i++)
                    {
                        Transform child = bone.GetChild(i);
                        var notValid = false;
                        if (dynamicBone.m_Exclusions != null)
                        {
                            notValid = dynamicBone.m_Exclusions.Contains(child);
                        }

                        if (!notValid && dynamicBone.m_notRolls != null)
                        {
                            isNotRoll = notValid = dynamicBone.m_notRolls.Contains(child);
                            index = i;
                        }

                        // if child is valid particle, append it as new particle
                        if (!notValid) AppendParticles(child, count, boneLength, dynamicBone);
                    }

                    // we only end up here if all children were excluded or notRolls
                    // if there is a notRoll, continue on that
                    if (isNotRoll) bone = bone.GetChild(index);
                    // else break the chain here
                    else break;
                }

                if (bone.childCount == 0 && (dynamicBone.m_EndLength > 0f || dynamicBone.m_EndOffset != Vector3.zero))
                {
                    AppendParticles(null, count, boneLength, dynamicBone);
                }
            }
        }

        internal static class Hooks
        {
            //Disable the SkipUpdateParticles method since it causes problems, namely causing jittering when the FPS is higher than 60
            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.SkipUpdateParticles))]
            internal static bool SkipUpdateParticles() => false;

            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.SkipUpdateParticles))]
            internal static bool SkipUpdateParticlesVer02() => false;

#if KK || EC || KKS
            // divert the call to AppendParticles with a call to the rewritten AppendParticles method above
            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.AppendParticles))]
            internal static bool DivertAppendParticles(DynamicBone __instance, Transform b, int parentIndex, float boneLength)
            {
                Replacer.AppendParticles(b, parentIndex, boneLength, __instance);
                return false;
            }
#endif
        }
    }
}