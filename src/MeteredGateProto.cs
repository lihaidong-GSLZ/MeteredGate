using System;
using System.Reflection;
using Mafi;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Zippers;
using Mafi.Core.Prototypes;
using StaticEntityId = Mafi.Core.Entities.Static.StaticEntityProto.ID;

namespace MeteredGate {
	/// <summary>
	/// 建筑原型：继续使用 ZipperProto 的正常玩家建筑语义。
	/// Flat Connector 只提供 1×1 四向动态端口布局和图形，不提供升降策略。
	/// 高度限制由 MeteredGateHeightPolicy 独立实现。
	/// </summary>
	public sealed class MeteredGateProto : ZipperProto {
		private static readonly MethodInfo s_memberwiseClone = typeof(object).GetMethod(
			"MemberwiseClone",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(typeof(object).FullName, "MemberwiseClone");

		private static readonly FieldInfo s_graphicsOwner = typeof(LayoutEntityProto.Gfx).GetField(
			"m_proto",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(typeof(LayoutEntityProto.Gfx).FullName, "m_proto");

		// IconPath 的 setter 是私有的。复制 Connector 的 Gfx 后，需要写入
		// backing field，并把 IconIsCustom 设为 true，阻止新 Proto 初始化时
		// 按 MeteredGate 的 ID 重新生成不存在的图标路径。
		private static readonly FieldInfo s_graphicsIconPath = typeof(LayoutEntityProto.Gfx).GetField(
			"<IconPath>k__BackingField",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(
				typeof(LayoutEntityProto.Gfx).FullName,
				"<IconPath>k__BackingField");

		// IconIsCustom 是当前游戏 API 中的 public readonly 字段，不能直接赋值。
		// 通过反射写入复制后的 Gfx，避免初始化阶段重写 Connector 图标路径。
		private static readonly FieldInfo s_graphicsIconIsCustom = typeof(LayoutEntityProto.Gfx).GetField(
			"IconIsCustom",
			BindingFlags.Instance | BindingFlags.Public)
			?? throw new MissingFieldException(
				typeof(LayoutEntityProto.Gfx).FullName,
				"IconIsCustom");

		public MeteredGateProto(
			StaticEntityId id,
			Proto.Str strings,
			MiniZipperProto sourceConnector,
			EntityCosts costs)
			: base(
				id,
				strings,

				// Connector 在这里仅作为布局/端口模板。
				sourceConnector.Layout,

				costs,
				Electricity.Zero,

				// 不再读取 sourceBalancer.CanBeElevated，也不模拟 MiniZipperProto。
				// Metered Gate 本身是允许架高的玩家建筑。
				true,

				cloneGraphics(sourceConnector)) {
		}

		public override Type EntityType => typeof(MeteredGateEntity);

		/// <summary>
		/// 浅复制原版 Connector 图形，解除旧 Proto 所有权，并固定其菜单图标路径。
		/// 该逻辑只影响图形配置，不参与高度限制。
		/// </summary>
		private static LayoutEntityProto.Gfx cloneGraphics(MiniZipperProto sourceConnector) {
			LayoutEntityProto.Gfx source = sourceConnector.Graphics;
			var clone = (LayoutEntityProto.Gfx)s_memberwiseClone.Invoke(source, null);

			// 若原版 Connector 已完成初始化，直接复用当前路径；否则按照
			// Core LayoutEntity 的原生生成规则构造其资源路径。
			string connectorIconPath = sourceConnector.IconPath;
			if (string.IsNullOrEmpty(connectorIconPath)) {
				connectorIconPath =
					$"Assets/Unity/Generated/Icons/LayoutEntity/{sourceConnector.Id.Value}.png";
			}

			s_graphicsIconPath.SetValue(clone, connectorIconPath);
			s_graphicsIconIsCustom.SetValue(clone, true);

			// 复制对象必须由 MeteredGateProto 在自身初始化阶段重新绑定。
			s_graphicsOwner.SetValue(clone, null);

			Log.Info(
				$"MeteredGate: reusing Flat Connector toolbar icon " +
				$"'{connectorIconPath}'.");
			return clone;
		}
	}
}
