using Mafi;
using Mafi.Collections;
using Mafi.Core.Mods;

namespace MeteredGate {
	/// <summary>manifest.json 指定的主入口；负责绑定配置并注册原型。</summary>
	public sealed class MeteredGateMod : DataOnlyMod {
		public MeteredGateMod(ModManifest manifest) : base(manifest) {
			Log.Info("MeteredGate: mod constructed.");
		}

		/// <summary>游戏加载 mod 时调用。</summary>
		public override void RegisterPrototypes(ProtoRegistrator registrator) {
			MeteredGateSettings.Bind(JsonConfig);
			registrator.RegisterData(new MeteredGateData());
		}

		/// <summary>未来配置格式变化时在这里迁移旧值。</summary>
		public override void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) {
			// 0.1.0 使用第一版配置结构，不需要迁移旧配置。
		}

		public override void Dispose() {
			MeteredGateSettings.Unbind();
			base.Dispose();
		}
	}
}
