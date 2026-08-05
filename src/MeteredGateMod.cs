using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace MeteredGate {
	/// <summary>
	/// manifest.json 指定的主入口。高度数据来自 Flat Connector 的
	/// EntityLayout，并由公开的实体添加验证器强制执行。
	/// </summary>
	public sealed class MeteredGateMod : DataOnlyMod {
		public MeteredGateMod(ModManifest manifest) : base(manifest) {
			Log.Info("MeteredGate: mod constructed.");
		}

		public override void RegisterPrototypes(ProtoRegistrator registrator) {
			MeteredGateSettings.Bind(JsonConfig);
			registrator.RegisterData(new MeteredGateData());
		}

		public override void RegisterDependencies(
			DependencyResolverBuilder builder,
			ProtosDb protosDb,
			bool gameWasLoaded) {
			base.RegisterDependencies(builder, protosDb, gameWasLoaded);
			builder.RegisterDependency<MeteredGateHeightValidator>()
				.AsAllInterfaces();
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
