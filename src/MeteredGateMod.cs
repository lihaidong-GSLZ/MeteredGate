using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;

namespace MeteredGate {
	/// <summary>
	/// manifest.json 指定的主入口。0.3.0 不再安装 Harmony 补丁；
	/// 放置高度范围直接来自原版 Flat Connector 的 EntityLayout。
	/// </summary>
	public sealed class MeteredGateMod : DataOnlyMod {
		public MeteredGateMod(ModManifest manifest) : base(manifest) {
			Log.Info("MeteredGate: mod constructed.");
		}

		public override void RegisterPrototypes(ProtoRegistrator registrator) {
			MeteredGateSettings.Bind(JsonConfig);
			registrator.RegisterData(new MeteredGateData());
		}

		public override void MigrateJsonConfig(
			VersionSlim savedVersion,
			Dict<string, object> savedValues) {
			// 当前 JSON 配置键自 0.2.x 起未改变。
		}

		public override void Dispose() {
			MeteredGateSettings.Unbind();
			base.Dispose();
		}
	}
}
