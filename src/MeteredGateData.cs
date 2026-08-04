using Mafi;
using Mafi.Base;
using Mafi.Core;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Factory.Zippers;
using Mafi.Core.Maintenance;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using StaticEntityId = Mafi.Core.Entities.Static.StaticEntityProto.ID;

namespace MeteredGate {
	/// <summary>注册建筑原型；不负责运行时物流逻辑。</summary>
	internal sealed class MeteredGateData : IModData {
		public void RegisterData(ProtoRegistrator registrator) {
			// 先取得 Flat Conveyor，再根据端口形状查找匹配的原版 Balancer/Connector。
			// 这样比硬编码原型 ID 更能适应版本变化。
			TransportProto flatConveyor = registrator.PrototypesDb.GetOrThrow<TransportProto>(
				Ids.Transports.FlatConveyor);

			StaticEntityId sourceBalancerId = IdsCore.Transports.GetZipperIdFor(flatConveyor.PortsShape.Id);
			StaticEntityId sourceConnectorId = IdsCore.Transports.GetMiniZipperIdFor(flatConveyor.PortsShape.Id);

			ZipperProto sourceBalancer = registrator.PrototypesDb.GetOrThrow<ZipperProto>(sourceBalancerId);
			MiniZipperProto sourceConnector = registrator.PrototypesDb.GetOrThrow<MiniZipperProto>(sourceConnectorId);

			// 当前版本直接创建英文名称和说明，没有独立本地化表。
			Proto.Str strings = Proto.CreateStr(
				MeteredGateIds.Gate,
				"Metered gate",
				"Allows only a configured quantity of flat products to leave upstream transports during each cycle. " +
				"Unused quota does not accumulate.",
				"Name and description of the metered conveyor gate.");

			// 建造材料沿用原版 Flat Balancer；当前版本不需要工人、不消耗维护和电力。
			// 这些数值属于平衡性设定，不影响周期与物流语义。
			EntityCosts costs = new EntityCosts(
				sourceBalancer.Costs.BaseConstructionCost,
				0,
				sourceBalancer.Costs.DefaultPriority,
				MaintenanceCosts.Empty);

			// 组合 Connector 的 1×1 布局/图形与 Balancer 的可架高属性。
			var proto = new MeteredGateProto(
				MeteredGateIds.Gate,
				strings,
				sourceConnector,
				costs,
				sourceBalancer.CanBeElevated);

			// false 表示若 ID 已存在则不强制覆盖。
			registrator.PrototypesDb.Add(proto, false);
			Log.Info(
				$"MeteredGate: registered {MeteredGateIds.Gate.Value}; " +
				$"layout/graphics source={sourceConnectorId.Value}, elevation source={sourceBalancerId.Value}.");
		}
	}
}
