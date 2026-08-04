using StaticEntityId = Mafi.Core.Entities.Static.StaticEntityProto.ID;

namespace MeteredGate {
	/// <summary>
	/// 稳定原型 ID。它会进入存档，发布后不要随意改名。
	/// </summary>
	internal static class MeteredGateIds {
		public static readonly StaticEntityId Gate = new StaticEntityId("MeteredGate_Entity");
	}
}
