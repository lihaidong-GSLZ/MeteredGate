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
			TransportProto flatConveyor = registrator.PrototypesDb.GetOrThrow<TransportProto>(
				Ids.Transports.FlatConveyor);

			StaticEntityId sourceBalancerId =
				IdsCore.Transports.GetZipperIdFor(flatConveyor.PortsShape.Id);
			StaticEntityId sourceConnectorId =
				IdsCore.Transports.GetMiniZipperIdFor(flatConveyor.PortsShape.Id);

			// Balancer 只提供建造成本；不再提供 CanBeElevated。
			ZipperProto sourceBalancer =
				registrator.PrototypesDb.GetOrThrow<ZipperProto>(sourceBalancerId);

			// Connector 只提供 1×1 布局、动态端口和图形模板。
			MiniZipperProto sourceConnector =
				registrator.PrototypesDb.GetOrThrow<MiniZipperProto>(sourceConnectorId);

			Proto.Str strings = Proto.CreateStr(
				MeteredGateIds.Gate,
				"Metered gate",
				"Allows only a configured quantity of flat products to leave upstream transports during each cycle. " +
				"Unused quota does not accumulate.",
				"Name and description of the metered conveyor gate.");

			EntityCosts costs = new EntityCosts(
				sourceBalancer.Costs.BaseConstructionCost,
				0,
				sourceBalancer.Costs.DefaultPriority,
				MaintenanceCosts.Empty);

			var proto = new MeteredGateProto(
				MeteredGateIds.Gate,
				strings,
				sourceConnector,
				costs);

			registrator.PrototypesDb.Add(proto, false);
			Log.Info(
				$"MeteredGate: registered {MeteredGateIds.Gate.Value}; " +
				$"layout/graphics template={sourceConnectorId.Value}, " +
				$"cost template={sourceBalancerId.Value}, elevation policy=custom.");
		}
	}
}
