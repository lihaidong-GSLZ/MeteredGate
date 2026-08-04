using System;
using System.Reflection;
using Mafi;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Zippers;
using Mafi.Core.Prototypes;
using StaticEntityId = Mafi.Core.Entities.Static.StaticEntityProto.ID;

namespace MeteredGate {
	/// <summary>
	/// 建筑原型：复用原版 Flat Connector 的 1×1 四向动态端口布局和图形，
	/// 同时使用 ZipperProto 的可架高放置契约。运行逻辑由 MeteredGateEntity 提供。
	/// </summary>
	public sealed class MeteredGateProto : ZipperProto {
		// 反射仅用于复制图形配置，不参与物料传输。
		private static readonly MethodInfo s_memberwiseClone = typeof(object).GetMethod(
			"MemberwiseClone",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(typeof(object).FullName, "MemberwiseClone");

		// Gfx 内部记录所属原型；复制后必须清空旧 owner，避免两个 Proto 共用状态。
		// 该私有字段名是当前版本中最脆弱的依赖之一。
		private static readonly FieldInfo s_graphicsOwner = typeof(LayoutEntityProto.Gfx).GetField(
			"m_proto",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(typeof(LayoutEntityProto.Gfx).FullName, "m_proto");

		public MeteredGateProto(
			StaticEntityId id,
			Proto.Str strings,
			MiniZipperProto sourceConnector,
			EntityCosts costs,
			bool canBeElevated)
			: base(
				id,
				strings,
				sourceConnector.Layout,
				costs,
				Electricity.Zero,
				canBeElevated,
				cloneGraphics(sourceConnector.Graphics)) {
		}

		// 建造本 Proto 时，游戏实例化我们的自定义实体，而不是原版 Zipper。
		public override Type EntityType => typeof(MeteredGateEntity);

		/// <summary>浅复制原版图形配置，并解除与原 Proto 的所有权绑定。</summary>
		private static LayoutEntityProto.Gfx cloneGraphics(LayoutEntityProto.Gfx source) {
			var clone = (LayoutEntityProto.Gfx)s_memberwiseClone.Invoke(source, null);

			// Gfx keeps the prototype that initialized it. Sharing the original instance would
			// bind both prototypes to the same owner, so detach the shallow clone before adding it.
			s_graphicsOwner.SetValue(clone, null);
			return clone;
		}
	}
}
