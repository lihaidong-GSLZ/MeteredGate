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
	public sealed class MeteredGateMod : DataOnlyMod, IMod {
		public MeteredGateMod(ModManifest manifest) : base(manifest) {
			Log.Info("MeteredGate: mod constructed.");
		}

		public override void RegisterPrototypes(ProtoRegistrator registrator) {
			MeteredGateSettings.Bind(JsonConfig);
			registrator.RegisterData(new MeteredGateData());
		}

		// DataOnlyMod 在 CoI 0.8.6 中以 final 方法实现 IMod.RegisterDependencies，
		// 因而不能 override。这里在派生类上重新声明 IMod，并显式重新实现
		// 该接口成员，确保模组加载器通过 IMod 调用时进入本实现。
		void IMod.RegisterDependencies(
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
