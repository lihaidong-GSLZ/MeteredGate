using System;
using Mafi;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Input;
using Mafi.Serialization;

namespace MeteredGate {
	/// <summary>
	/// Inspector 修改实体状态时使用的可回放输入命令。
	/// UI 不直接写模拟状态，避免绕过游戏的输入调度、重放与联机同步路径。
	/// </summary>
	public enum MeteredGateCommandKind {
		AdjustCycleSeconds = 1,
		AdjustItemsPerCycle = 2,
		RestartCycle = 3
	}

	public sealed class MeteredGateConfigCmd : InputCommand {
		private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction =
			(obj, writer) => ((MeteredGateConfigCmd)obj).SerializeData(writer);

		private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction =
			(obj, reader) => ((MeteredGateConfigCmd)obj).DeserializeData(reader);

		public readonly EntityId EntityId;
		public readonly MeteredGateCommandKind Kind;
		public readonly int Value;

		public MeteredGateConfigCmd(
			EntityId entityId,
			MeteredGateCommandKind kind,
			int value = 0) {
			EntityId = entityId;
			Kind = kind;
			Value = value;
		}

		public static void Serialize(MeteredGateConfigCmd value, BlobWriter writer) {
			if (writer.TryStartClassSerialization(value)) {
				writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
			}
		}

		protected override void SerializeData(BlobWriter writer) {
			base.SerializeData(writer);
			Mafi.Core.EntityId.Serialize(EntityId, writer);
			writer.WriteInt((int)Kind);
			writer.WriteInt(Value);
		}

		public new static MeteredGateConfigCmd Deserialize(BlobReader reader) {
			MeteredGateConfigCmd value;
			if (reader.TryStartClassDeserialization(out value, null, null, false)) {
				reader.EnqueueDataDeserialization(value, s_deserializeDataDelayedAction, null);
			}
			return value;
		}

		protected override void DeserializeData(BlobReader reader) {
			base.DeserializeData(reader);
			reader.SetField(this, nameof(EntityId), Mafi.Core.EntityId.Deserialize(reader));
			reader.SetField(
				this,
				nameof(Kind),
				(MeteredGateCommandKind)reader.ReadInt());
			reader.SetField(this, nameof(Value), reader.ReadInt());
		}
	}

	[GlobalDependency(RegistrationMode.AsAllInterfaces, false, false)]
	public sealed class MeteredGateCommandsProcessor :
		ICommandProcessor<MeteredGateConfigCmd> {

		private readonly EntitiesManager m_entitiesManager;

		public MeteredGateCommandsProcessor(EntitiesManager entitiesManager) {
			m_entitiesManager = entitiesManager;
		}

		public void Invoke(MeteredGateConfigCmd command) {
			MeteredGateEntity entity;
			if (!m_entitiesManager.TryGetEntity(command.EntityId, out entity)) {
				command.SetResultError(
					$"Metered Gate entity '{command.EntityId}' was not found.");
				return;
			}

			switch (command.Kind) {
				case MeteredGateCommandKind.AdjustCycleSeconds:
					entity.AdjustCycleSeconds(command.Value);
					break;
				case MeteredGateCommandKind.AdjustItemsPerCycle:
					entity.AdjustItemsPerCycle(command.Value);
					break;
				case MeteredGateCommandKind.RestartCycle:
					entity.RestartCycle();
					break;
				default:
					command.SetResultError(
						$"Unsupported Metered Gate command: {command.Kind}.");
					return;
			}

			command.SetResultSuccess();
		}
	}
}
