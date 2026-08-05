using System.Runtime.CompilerServices;
using HarmonyLib;
using Mafi;
using Mafi.Core;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Transports;
using Mafi.Unity.InputControl;
using Mafi.Unity.InputControl.Factory;
using Mafi.Unity.Ui.Controllers.LayoutEntityPlacing;

namespace MeteredGate {
	/// <summary>
	/// Metered Gate 的独立高度策略。
	///
	/// 不继承 MiniZipperProto，也不读取 Connector 的 PlacementHeightRange。
	/// 允许范围直接由运输支柱的全局高度能力定义：
	/// [0, MAX_PILLAR_HEIGHT - 1]。
	/// </summary>
	internal static class MeteredGateHeightPolicy {
		private sealed class PlacerState {
			public PlacerState() { }

			internal bool IsMeteredGatePlacement;
		}

		private static readonly ConditionalWeakTable<StaticEntityMassPlacer, PlacerState>
			s_placerStates = new ConditionalWeakTable<StaticEntityMassPlacer, PlacerState>();

		internal static ThicknessIRange AllowedRange => new ThicknessIRange(
			0,
			TransportPillarProto.MAX_PILLAR_HEIGHT.Value - 1);

		internal static void SetActive(StaticEntityMassPlacer placer, bool isActive) {
			s_placerStates.GetOrCreateValue(placer).IsMeteredGatePlacement = isActive;
		}

		internal static bool IsActive(StaticEntityMassPlacer placer) {
			PlacerState state;
			return s_placerStates.TryGetValue(placer, out state)
				&& state.IsMeteredGatePlacement;
		}

		internal static void ClampCursor(StaticEntityMassPlacer placer, TerrainCursor cursor) {
			if (!IsActive(placer)) {
				return;
			}

			ThicknessIRange range = AllowedRange;
			ThicknessTilesI current = cursor.RelativeHeight;
			ThicknessTilesI clamped = range.ClampToRange(current);

			if (clamped.Value != current.Value) {
				cursor.RelativeHeight = clamped;
			}
		}
	}

	/// <summary>
	/// 每次开始放置建筑时，显式替换放置器的允许高度范围。
	/// 这里不依赖 sourceConnector.Layout.PlacementHeightRange。
	/// </summary>
	[HarmonyPatch(
		typeof(StaticEntityMassPlacer),
		nameof(StaticEntityMassPlacer.SetLayoutEntityToPlace))]
	internal static class MeteredGateSetPlacementRangePatch {
		private static void Postfix(
			StaticEntityMassPlacer __instance,
			ILayoutEntityProto __0,
			ref ThicknessIRange ___m_allowedHeightRange,
			TerrainCursor ___m_terrainCursor) {

			bool isMeteredGate = __0 is MeteredGateProto;
			MeteredGateHeightPolicy.SetActive(__instance, isMeteredGate);

			if (!isMeteredGate) {
				return;
			}

			___m_allowedHeightRange = MeteredGateHeightPolicy.AllowedRange;
			MeteredGateHeightPolicy.ClampCursor(__instance, ___m_terrainCursor);

			Log.Info(
				$"MeteredGate: custom placement height range=" +
				$"[{___m_allowedHeightRange.From.Value}, " +
				$"{___m_allowedHeightRange.To.Value}].");
		}
	}

	/// <summary>
	/// 原版普通 LayoutEntityPreview 在到达范围边界后，仍允许“继续移动”。
	/// 对 Metered Gate 单独关闭这条后备分支；其他建筑不受影响。
	/// </summary>
	[HarmonyPatch(
		typeof(LayoutEntityPreview),
		nameof(LayoutEntityPreview.CanMoveUpDownIfValid))]
	internal static class MeteredGatePreviewBoundaryPatch {
		private static void Postfix(
			LayoutEntityPreview __instance,
			ref bool __result) {

			if (__instance.LayoutEntityProto is MeteredGateProto) {
				__result = false;
			}
		}
	}

	/// <summary>
	/// 游戏的 Shift 快速升高一次增加 5 格，可能从范围内部直接跨过上限。
	/// 原方法执行后再做一次硬夹值，确保最终高度绝不会超过范围。
	/// </summary>
	[HarmonyPatch(typeof(StaticEntityMassPlacer), "raiseEntity")]
	internal static class MeteredGateRaiseClampPatch {
		private static void Postfix(
			StaticEntityMassPlacer __instance,
			TerrainCursor ___m_terrainCursor) {

			MeteredGateHeightPolicy.ClampCursor(__instance, ___m_terrainCursor);
		}
	}

	/// <summary>与快速升高对应，防止一次快速降低越过最低高度。</summary>
	[HarmonyPatch(typeof(StaticEntityMassPlacer), "lowerEntity")]
	internal static class MeteredGateLowerClampPatch {
		private static void Postfix(
			StaticEntityMassPlacer __instance,
			TerrainCursor ___m_terrainCursor) {

			MeteredGateHeightPolicy.ClampCursor(__instance, ___m_terrainCursor);
		}
	}
}
