using System.Collections.Generic;
using AIChara;
using HS2;
using System.Linq;
using HarmonyLib;
using Manager;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using UnityEngine;


namespace Wizzy
{
	[BepInPlugin("com.test.calcstate", "CalcStateOverrider", "1.0.0")]
	public class CalcStateOverrider : BaseUnityPlugin
	{
		internal void Awake()
		{
			var harmony = new Harmony("com.test.calcstate");
			var original = typeof(HS2.GlobalHS2Calc).GetMethod("CalcState");
			var replacement = typeof(CalcStateOverrides).GetMethod("CalcStateReplacement");
			harmony.Patch(original, new HarmonyMethod(replacement));
		}
	}

	public class CalcStateOverrides
	{
		public static ManualLogSource MyLogSource = new ManualLogSource("HS2_Personality_Patcher");
		[HarmonyPatch(typeof(HS2.GlobalHS2Calc))]
		[HarmonyPatch(nameof(HS2.GlobalHS2Calc.CalcState))]
		[HarmonyPriority(0)]
		public static bool CalcStateReplacement(ChaFileGameInfo2 _param, int _personality)
		{
			MyLogSource = BepInEx.Logging.Logger.CreateLogSource("HS2_Personality_Patcher");
			MyLogSource.LogDebug("CalcState Replacement Call Injection");
			if (_param == null)
			{
				return false;
			}
			if (_param.nowDrawState == ChaFileDefine.State.Broken)
			{
				if (_param.Broken > 0)
				{
					return false;
				}
			}
			else if (_param.nowDrawState == ChaFileDefine.State.Dependence && _param.Dependence > 0)
			{
				return false;
			}
			List<(ChaFileDefine.State, int)> list = new List<(ChaFileDefine.State, int)>
					{
						(ChaFileDefine.State.Favor, _param.Favor),
						(ChaFileDefine.State.Enjoyment, _param.Enjoyment),
						(ChaFileDefine.State.Slavery, _param.Slavery),
						(ChaFileDefine.State.Aversion, _param.Aversion)
					};
			if (list.Any<(ChaFileDefine.State, int)>(((ChaFileDefine.State id, int state) l) => l.state >= 20))
			{
				CalcState(list, ref _param.nowState);
			}
			else
			{
				_param.nowState = ChaFileDefine.State.Blank;
			}
			if (list.Any<(ChaFileDefine.State, int)>(((ChaFileDefine.State id, int state) l) => l.state >= 50))
			{
				CalcState(list, ref _param.nowDrawState);
			}
			else
			{
				_param.nowDrawState = ChaFileDefine.State.Blank;
			}
			if (_param.Broken >= 100)
			{
				_param.nowState = ChaFileDefine.State.Broken;
				_param.nowDrawState = ChaFileDefine.State.Broken;
			}
			else if (_param.Dependence >= 100)
			{
				_param.nowState = ChaFileDefine.State.Dependence;
				_param.nowDrawState = ChaFileDefine.State.Dependence;
			}
			if (_param.nowDrawState == ChaFileDefine.State.Favor && _param.Favor >= 100)
			{
				SaveData.SetAchievementAchieve(12);
			}
			if (_param.nowDrawState == ChaFileDefine.State.Enjoyment && _param.Enjoyment >= 100)
			{
				SaveData.SetAchievementAchieve(13);
			}
			if (_param.nowDrawState == ChaFileDefine.State.Slavery && _param.Slavery >= 100)
			{
				SaveData.SetAchievementAchieve(14);
			}
			if (_param.nowDrawState == ChaFileDefine.State.Aversion && _param.Aversion >= 100)
			{
				SaveData.SetAchievementAchieve(15);
			}
			if (_param.nowDrawState == ChaFileDefine.State.Broken)
			{
				SaveData.SetAchievementAchieve(16);
			}
			if (_param.nowDrawState == ChaFileDefine.State.Dependence)
			{
				SaveData.SetAchievementAchieve(17);
			}
			bool CalcState(List<(ChaFileDefine.State id, int state)> _list, ref ChaFileDefine.State _state)
			{
				if (Game.infoPersonalParameterTable.Count == 0)
				{
					MyLogSource.LogWarning("Game.infoPersonalParameterTable is empty.");
					return false;
				}

				int minKey = Game.infoPersonalParameterTable.Keys.Min();
				if (!_list.Any() || !Game.infoPersonalParameterTable.ContainsKey(_personality))
				{
					MyLogSource.LogWarning("'_personality' val doesn't exist in Game.infoPersonalParameterTable.");
					MyLogSource.LogWarning("HS2_Personality_Patcher will avert this crash for you, but you might want to check your abdata files.");
					MyLogSource.LogWarning($"Setting _personality to {minKey} instead.");
					_personality = minKey;
				}
				try
				{
					List<(ChaFileDefine.State, int)> source = _list.OrderByDescending(l => l.state).ToList();
					foreach (int s in Game.infoPersonalParameterTable[_personality].statusPrioritys)
					{
						if (source.Any<(ChaFileDefine.State, int)>(((ChaFileDefine.State id, int state) m) => m.id == (ChaFileDefine.State)s))
						{
							_state = (ChaFileDefine.State)s;
							break;
						}
					}
				}
				catch (KeyNotFoundException)
				{
					MyLogSource.LogWarning("Crash averted by HS2_Personality_Patcher, you need to check your installation to prevent future issues.");
				}
				return false;
			}
			return false;
		}
	}
}
