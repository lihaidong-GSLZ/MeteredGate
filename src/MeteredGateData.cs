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
	/// <summary>注册建筑原型；运行时物流由 MeteredGateEntity 负责。</summary>
	internal sealed class MeteredGateData : IModData {
		public void RegisterData(ProtoRegistrator registrator) {
			TransportProto flatConveyor = registrator.PrototypesDb.GetOrThrow<TransportProto>(
				Ids.Transports.FlatConveyor);

			StaticEntityId sourceConnectorId =
				IdsCore.Transports.GetMiniZipperIdFor(flatConveyor.PortsShape.Id);

			// Flat Connector 只提供不可变的布局与图形模板：1×1 四向端口、
			// 放置高度范围和模型资源。自定义 Proto 不继承其特殊类型身份；
			// 范围由 MeteredGateHeightValidator 在最终添加请求中强制执行。
			MiniZipperProto sourceConnector =
				registrator.PrototypesDb.GetOrThrow<MiniZipperProto>(sourceConnectorId);

			Proto.Str strings = Proto.CreateStr(
				MeteredGateIds.Gate,
				"Metered gate",
				"Allows only a configured quantity of flat products to leave upstream transports during each cycle. " +
				"Unused quota does not accumulate.",
				"Name and description of the metered conveyor gate.");

			// Flat Connector 是自动生成实体，原型成本为 None，不能直接作为玩家建筑成本。
			// 玩家建筑成本取自一段 Flat Conveyor；自动 Connector 自身没有建造成本。
			EntityCosts costs = new EntityCosts(
				baseConstructionCost: flatConveyor.Costs.BaseConstructionCost,
				defaultPriority: flatConveyor.Costs.DefaultPriority,
				workers: 0,
				maintenance: MaintenanceCosts.Empty);

			var proto = new MeteredGateProto(
				MeteredGateIds.Gate,
				strings,
				sourceConnector,
				costs,
				Electricity.FromKw(20));

			registrator.PrototypesDb.Add(proto, false);
			Log.Info(
				$"MeteredGate: registered {MeteredGateIds.Gate.Value}; " +
				$"connector template={sourceConnectorId.Value}, " +
				$"placement range=[{proto.Layout.PlacementHeightRange.From.Value}, " +
				$"{proto.Layout.PlacementHeightRange.To.Value}], " +
				$"workers={costs.Workers}, power={proto.ElectricityConsumed.Value} kW.");
		}
	}
}
