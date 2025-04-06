using HarmonyLib;
using System.Reflection.Emit;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace RatopiaTwitchIntegration.Patches
{
    [HarmonyPatch(typeof(CCMake_Info))]
    public class CCMake_InfoPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new[] { typeof(int) })]
        public static void ForceChatterName_Int(CCMake_Info __instance, int _grade_max)
        {
            ForceChatterName(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        public static void ForceChatterName(CCMake_Info __instance)
        {
            (string, Gender) NG = ModBase.Instance.tUsers.NewAwaiting();
            __instance.Name = NG.Item1;
            __instance.m_Gender = NG.Item2;
        }
    }

    [HarmonyPatch(typeof(T_Citizen))]
    public class T_CitizenPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("MakeCtizen_ByCC")]
        public static void ClearReservedUsers(T_Citizen __instance)
        {
            ModBase.Instance.tUsers.CreatedCitizens(__instance);
        }
    }

    [HarmonyPatch(typeof(CitizenCaveUI))]
    public class CitizenCaveUIPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("RecruitBtn")]
        public static void ClearReservedUsers(CitizenCaveUI __instance)
        {
            ModBase.Instance.tUsers.ClearAwaiting();
        }
    }
}