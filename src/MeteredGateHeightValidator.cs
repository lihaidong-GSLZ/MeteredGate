using Mafi;
using Mafi.Core;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Entities.Validators;
using Mafi.Core.Terrain;
using Mafi.Localization;

namespace MeteredGate {
	/// <summary>
	/// 对 Metered Gate 强制执行 Flat Connector 布局声明的相对高度范围。
	/// PlacementHeightRange 本身只影响放置器的升降输入，不是最终建造验证；
	/// 因此必须通过公开的实体添加验证器保证范围外位置不能提交建造。
	/// </summary>
	public sealed class MeteredGateHeightValidator :
		IEntityAdditionValidator<LayoutEntityAddRequest> {
		private readonly TerrainManager m_terrainManager;

		public EntityValidatorPriority Priority => EntityValidatorPriority.Default;

		public MeteredGateHeightValidator(TerrainManager terrainManager) {
			m_terrainManager = terrainManager;
		}

		public EntityValidationResult CanAdd(LayoutEntityAddRequest request) {
			if (!(request.Proto is MeteredGateProto proto)) {
				return EntityValidationResult.Success;
			}

			Tile3i placementPosition = request.Transform.Position;
			Tile2i originTile = placementPosition.Xy;

			// 地图边界由游戏的原生 Terrain validator 报告。验证器集合也支持
			// 继续调用其他 validator 的模式，所以这里仍需避免越界索引。
			if (!m_terrainManager.TerrainArea.ContainsTile(originTile)) {
				return EntityValidationResult.Success;
			}

			HeightTilesI terrainHeight =
				m_terrainManager[originTile].Height.TilesHeightRounded;
			ThicknessTilesI relativeHeight = placementPosition.Height - terrainHeight;
			ThicknessIRange allowedRange = proto.Layout.PlacementHeightRange;

			if (relativeHeight >= allowedRange.From &&
				relativeHeight <= allowedRange.To) {
				return EntityValidationResult.Success;
			}

			var playerMessage = new LocStrFormatted(
				$"Metered Gate must be placed between {allowedRange.From.Value} and " +
				$"{allowedRange.To.Value} levels above the terrain.");
			string diagnosticMessage =
				$"MeteredGate relative placement height {relativeHeight.Value} is outside " +
				$"the allowed range [{allowedRange.From.Value}, {allowedRange.To.Value}].";

			return EntityValidationResult.CreateError(
				playerMessage,
				diagnosticMessage);
		}
	}
}
