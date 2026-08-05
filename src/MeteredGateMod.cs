using HarmonyLib;
using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;

namespace MeteredGate {
	/// <summary>manifest.json 指定的主入口；负责配置、原型和局部高度补丁。</summary>
	public sealed class MeteredGateMod : DataOnlyMod {
		private const string HarmonyId =
			"lihaidong.MeteredGate.CustomHeightPolicy";

		private Harmony m_harmony;

		public MeteredGateMod(ModManifest manifest) : base(manifest) {
			Log.Info("MeteredGate: mod constructed.");
		}

		public override void RegisterPrototypes(ProtoRegistrator registrator) {
			MeteredGateSettings.Bind(JsonConfig);
			registrator.RegisterData(new MeteredGateData());
		}

		/// <summary>
		/// 在游戏早期初始化阶段安装高度补丁。
		/// DataOnlyMod.Initialize(...) 在 0.8.6c 中是 final，不能由模组覆盖；
		/// EarlyInit(...) 才是供派生模组扩展的生命周期入口。
		/// </summary>
		public override void EarlyInit(DependencyResolver resolver) {
			base.EarlyInit(resolver);

			// 防止生命周期异常重复调用时重复安装同一批 Harmony 补丁。
			if (m_harmony != null) {
				return;
			}

			m_harmony = new Harmony(HarmonyId);
			m_harmony.PatchAll(typeof(MeteredGateMod).Assembly);

			ThicknessIRange range = MeteredGateHeightPolicy.AllowedRange;
			Log.Info(
				$"MeteredGate: custom height policy installed during EarlyInit; " +
				$"range=[{range.From.Value}, {range.To.Value}].");
		}

		public override void MigrateJsonConfig(
			VersionSlim savedVersion,
			Dict<string, object> savedValues) {
			// 0.2.0 沿用 0.1.0 的配置结构，不需要迁移。
		}

		public override void Dispose() {
			if (m_harmony != null) {
				// Harmony 2.4.2 没有 UnpatchSelf()。
				// 按 Harmony ID 只卸载本模组安装的补丁。
				m_harmony.UnpatchAll(HarmonyId);
				m_harmony = null;
			}

			MeteredGateSettings.Unbind();
			base.Dispose();
		}
	}
}
