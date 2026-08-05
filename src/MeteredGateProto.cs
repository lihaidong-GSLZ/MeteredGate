using System;
using System.Reflection;
using Mafi;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Zippers;
using Mafi.Core.Prototypes;
using StaticEntityId = Mafi.Core.Entities.Static.StaticEntityProto.ID;

namespace MeteredGate {
	/// <summary>
	/// 玩家可建造的连接器型原型。
	///
	/// 该类型不继承 ZipperProto，也不继承 MiniZipperProto：
	/// - ZipperProto 会带入平衡器/分流器专用的原型语义；
	/// - MiniZipperProto 会触发 MiniZipperValidator，要求切开现有运输带，并会被
	///   蓝图系统当作自动连接器忽略。
	///
	/// 因此这里直接继承 LayoutEntityProto，同时复用原版 Flat Connector 的
	/// EntityLayout 与 Gfx。放置高度范围由 sourceConnector.Layout 中的
	/// PlacementHeightRange 原生提供，不再使用 Harmony 修改放置器。
	/// </summary>
	public sealed class MeteredGateProto :
		LayoutEntityProto,
		IProtoWithPowerConsumption {

		private static readonly MethodInfo s_memberwiseClone = typeof(object).GetMethod(
			"MemberwiseClone",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(typeof(object).FullName, "MemberwiseClone");

		private static readonly FieldInfo s_graphicsOwner = typeof(LayoutEntityProto.Gfx).GetField(
			"m_proto",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(typeof(LayoutEntityProto.Gfx).FullName, "m_proto");

		private static readonly FieldInfo s_graphicsIconPath = typeof(LayoutEntityProto.Gfx).GetField(
			"<IconPath>k__BackingField",
			BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(
				typeof(LayoutEntityProto.Gfx).FullName,
				"<IconPath>k__BackingField");

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
			EntityCosts costs,
			Electricity electricityConsumed)
			: base(
				id,
				strings,
				sourceConnector.Layout,
				costs,
				cloneGraphics(sourceConnector),
				constructionDurationPerProduct: null,
				boostCost: null,
				cannotBeBuiltByPlayer: false,
				cannotBeDestroyedByFlood: false,
				isUnique: false,
				cannotBeReflected: sourceConnector.CannotBeReflected,
				autoBuildMiniZippers: false,
				doNotStartConstructionAutomatically: false,
				doNotCheckVehicleGoalHeightRange: false,
				canMoveUpDownWhenInvalidPlacement:
					sourceConnector.CanMoveUpDownWhenInvalidPlacement,
				collapseRubbleScale: null,
				customBuriedTolerance: null,
				tags: null) {

			ElectricityConsumed = electricityConsumed;

			// 这是核心结构不变量：高度范围必须直接来自连接器布局。
			if (Layout.PlacementHeightRange.From.Value !=
					sourceConnector.Layout.PlacementHeightRange.From.Value ||
				Layout.PlacementHeightRange.To.Value !=
					sourceConnector.Layout.PlacementHeightRange.To.Value) {
				throw new InvalidOperationException(
					"MeteredGate layout did not preserve the connector placement height range.");
			}
		}

		public override Type EntityType => typeof(MeteredGateEntity);

		public Electricity ElectricityConsumed { get; }

		/// <summary>
		/// 复制原版 Connector 图形，解除旧 Proto 所有权，并固定其工具栏图标路径。
		/// 图形对象在初始化时会写入所属 Proto，不能被两个 Proto 直接共享。
		/// </summary>
		private static LayoutEntityProto.Gfx cloneGraphics(MiniZipperProto sourceConnector) {
			LayoutEntityProto.Gfx source = sourceConnector.Graphics;
			var clone = (LayoutEntityProto.Gfx)s_memberwiseClone.Invoke(source, null);

			string connectorIconPath = sourceConnector.IconPath;
			if (string.IsNullOrEmpty(connectorIconPath)) {
				connectorIconPath =
					$"Assets/Unity/Generated/Icons/LayoutEntity/{sourceConnector.Id.Value}.png";
			}

			s_graphicsIconPath.SetValue(clone, connectorIconPath);
			s_graphicsIconIsCustom.SetValue(clone, true);
			s_graphicsOwner.SetValue(clone, null);

			Log.Info(
				$"MeteredGate: reusing Flat Connector graphics and toolbar icon " +
				$"'{connectorIconPath}'.");
			return clone;
		}
	}
}
