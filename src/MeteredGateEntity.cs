using System;
using Mafi;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.ElectricPower;
using Mafi.Core.Ports;
using Mafi.Core.Ports.Io;
using Mafi.Serialization;

namespace MeteredGate {
	/// <summary>
	/// Metered Gate 的运行时实体。它不是配方机器，而是一个自定义物流节点。
	///
	/// 数据流：上游调用 ReceiveAsMuchAsFromPort() -> 本实体最多接收 1 件 ->
	/// SimUpdate() 再尝试将该物品送往某个输出端口。
	///
	/// 周期使用游戏自己的 Mafi.Duration，因此随游戏模拟时间推进。
	/// 配额在物品被接收时扣除，因为此时物品已经离开上游。
	/// 输出堵塞时内部最多保留一件，不会继续预取。
	/// </summary>
	public sealed class MeteredGateEntity :
		LayoutEntity,
		IEntityWithPorts,
		IEntityWithSimUpdate,
		IEntityWithCloneableConfig,
		IElectricityConsumingEntity {

		// v1 是 0.1.0/0.2.0 的正式存档格式；v2 增加 ElectricityConsumer。
		// v1 载入时会在整个对象图完成恢复后创建并注册 consumer。
		private const int SaveVersion = 2;
		private const string ConfigCycleSeconds = "MeteredGate.CycleSeconds";
		private const string ConfigItemsPerCycle = "MeteredGate.ItemsPerCycle";

		private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction =
			(obj, writer) => ((MeteredGateEntity)obj).SerializeData(writer);

		private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction =
			(obj, reader) => ((MeteredGateEntity)obj).DeserializeData(reader);

		// ElectricityConsumer 是实体对象图的一部分，必须显式保存。
		// BlobReader 载入对象时不会执行构造函数，因此不能假设加载时会重新创建它。
		private IElectricityConsumer m_electricityConsumer;
		private bool m_hasPower;

		// 内部传输缓冲，逻辑容量严格为一件。
		private ProductQuantity m_buffer;
		// 本周期尚可从上游接收的件数。
		private Quantity m_remainingQuota;
		// 当前周期已推进的游戏模拟时间。
		private Duration m_cycleElapsed;
		// 每栋建筑独立保存的周期秒数。
		private int m_cycleSeconds;
		// 每周期刷新得到的总配额。
		private int m_itemsPerCycle;
		// 下一次优先尝试的输出端口下标，用于轮询。
		private int m_nextOutputIndex;

		/// <summary>
		/// 新建筑从 elapsed=0、quota=0 开始，必须等待一个完整周期才首次开放。
		/// </summary>
		public MeteredGateEntity(
			EntityId id,
			MeteredGateProto prototype,
			TileTransform transform,
			EntityContext context)
			: base(id, prototype, transform, context) {
			m_electricityConsumer = context.ElectricityConsumerFactory.CreateConsumer(this);
			m_hasPower = false;

			m_buffer = ProductQuantity.None;
			m_remainingQuota = Quantity.Zero;
			m_cycleElapsed = Duration.Zero;
			m_cycleSeconds = MeteredGateSettings.DefaultCycleSeconds;
			m_itemsPerCycle = MeteredGateSettings.DefaultItemsPerCycle;
			m_nextOutputIndex = 0;
		}

		public override bool CanBePaused => true;

		/// <summary>
		/// 暂停、禁用或重新启用后，旧的供电结果不能继续授权上游交货。
		/// Entity.UpdateIsEnabled() 会在本钩子返回后才通知观察者，因此这里先
		/// 清除瞬时供电状态，观察者不会同时看到新启用状态和旧授权。
		/// </summary>
		protected override void OnEnabledChanged() {
			setHasPower(false);
			// CoI 0.8.6 中基类实现为空；保留调用以兼容后续基类行为。
			base.OnEnabledChanged();
		}

		// 不覆盖 IsGeneralPriorityVisible。LayoutEntity 的默认实现会检测到本实体
		// 真实消耗 20 kW，从而显示游戏原生通用优先级。ElectricityConsumer 会
		// 观察 GeneralPriority 的变化，并据此更新供电不足时的分配顺序。
		public Electricity PowerRequired =>
			((MeteredGateProto)Prototype).ElectricityConsumed;

		public Option<IElectricityConsumerReadonly> ElectricityConsumer =>
			m_electricityConsumer.CreateOption<IElectricityConsumerReadonly>();

		// Inspector 只观察该计数器。它不进入存档，也不参与模拟结果。
		public int UiUpdateTrigger { get; private set; }
		public int CycleSeconds => m_cycleSeconds;
		public int ItemsPerCycle => m_itemsPerCycle;
		public int RemainingQuota => m_remainingQuota.Value;
		public int CycleElapsedTicks => m_cycleElapsed.Ticks;
		public int CycleDurationTicks => Duration.FromSec(m_cycleSeconds).Ticks;

		public int SecondsUntilNextCycle {
			get {
				int remainingTicks = Math.Max(0, CycleDurationTicks - m_cycleElapsed.Ticks);
				int ticksPerSecond = Duration.OneSecond.Ticks;
				return (remainingTicks + ticksPerSecond - 1) / ticksPerSecond;
			}
		}

		public string GateStatus {
			get {
				if (IsNotEnabled) {
					return "Paused or disabled";
				}
				if (!m_hasPower) {
					return "Not enough power";
				}
				if (m_buffer.IsNotEmpty) {
					return ConnectedOutputPorts.IsEmpty
						? "Holding 1 item; no output"
						: "Holding 1 item; output blocked";
				}
				if (m_remainingQuota.IsNotPositive) {
					return "Waiting for next cycle";
				}
				return "Ready";
			}
		}

		/// <summary>
		/// 上游尝试交货的入口。返回值是未被接收的数量，不是接收量。
		/// 所有输入端口共享同一个一件缓冲和同一份周期配额。
		/// </summary>
		public Quantity ReceiveAsMuchAsFromPort(ProductQuantity offered, IoPortToken sourcePort) {
			if (
				IsNotEnabled ||
				!m_hasPower ||
				offered.IsEmpty ||
				m_buffer.IsNotEmpty ||
				m_remainingQuota.IsNotPositive
			) {
				return offered.Quantity;
			}

			Quantity accepted = offered.Quantity.Min(Quantity.One).Min(m_remainingQuota);
			if (accepted.IsNotPositive) {
				return offered.Quantity;
			}

			m_buffer = offered.WithNewQuantity(accepted);
			m_remainingQuota -= accepted;
			touchUi();
			return offered.Quantity - accepted;
		}

		/// <summary>
		/// 每个有效模拟 tick 申请固定的 20 kW。供电失败时周期和物流冻结。
		/// 这是本模组有意采用的连续耗电语义；不依赖任何 Zipper 或
		/// Balancer 的运行时状态机。
		/// </summary>
		public void SimUpdate() {
			if (IsNotEnabled) {
				setHasPower(false);
				return;
			}

			setHasPower(m_electricityConsumer.TryConsume(false));
			if (!m_hasPower) {
				return;
			}

			int secondsBefore = SecondsUntilNextCycle;
			bool quotaRefreshed = advanceCycle();
			trySendBufferedItem();

			// 进度条按整秒刷新，避免每个模拟 tick 都触发 UI 字符串与布局更新。
			if (quotaRefreshed || SecondsUntilNextCycle != secondsBefore) {
				touchUi();
			}
		}

		private void setHasPower(bool hasPower) {
			if (m_hasPower == hasPower) {
				return;
			}

			m_hasPower = hasPower;
			touchUi();
		}

		public void SetCycleSeconds(int value) {
			int clamped = MeteredGateSettings.ClampCycleSeconds(value);
			if (clamped == m_cycleSeconds) {
				return;
			}

			m_cycleSeconds = clamped;
			m_cycleElapsed = Duration.Zero;
			m_remainingQuota = Quantity.Zero;
			touchUi();
		}

		public void AdjustCycleSeconds(int delta) {
			SetCycleSeconds(saturatingAdd(m_cycleSeconds, delta));
		}

		public void SetItemsPerCycle(int value) {
			int clamped = MeteredGateSettings.ClampItemsPerCycle(value);
			if (clamped == m_itemsPerCycle) {
				return;
			}

			m_itemsPerCycle = clamped;
			m_remainingQuota = m_remainingQuota.Min(new Quantity(clamped));
			touchUi();
		}

		public void AdjustItemsPerCycle(int delta) {
			SetItemsPerCycle(saturatingAdd(m_itemsPerCycle, delta));
		}

		public void RestartCycle() {
			m_cycleElapsed = Duration.Zero;
			m_remainingQuota = Quantity.Zero;
			touchUi();
		}

		public void AddToConfig(EntityConfigData config) {
			config.SetInt(ConfigCycleSeconds, m_cycleSeconds);
			config.SetInt(ConfigItemsPerCycle, m_itemsPerCycle);
		}

		public void ApplyConfig(EntityConfigData config) {
			int? cycleSeconds = config.GetInt(ConfigCycleSeconds);
			int? itemsPerCycle = config.GetInt(ConfigItemsPerCycle);

			if (cycleSeconds.HasValue) {
				m_cycleSeconds = MeteredGateSettings.ClampCycleSeconds(cycleSeconds.Value);
			}
			if (itemsPerCycle.HasValue) {
				m_itemsPerCycle = MeteredGateSettings.ClampItemsPerCycle(itemsPerCycle.Value);
			}

			m_cycleElapsed = Duration.Zero;
			m_remainingQuota = Quantity.Zero;
			m_nextOutputIndex = 0;
			touchUi();
		}

		/// <summary>
		/// 推进一个游戏模拟 tick。返回 true 表示刚刚跨过周期边界并刷新配额。
		/// </summary>
		private bool advanceCycle() {
			Duration cycleDuration = Duration.FromSec(m_cycleSeconds);
			m_cycleElapsed += Duration.OneTick;
			if (m_cycleElapsed < cycleDuration) {
				return false;
			}

			m_cycleElapsed = Duration.FromTicks(m_cycleElapsed.Ticks % cycleDuration.Ticks);
			m_remainingQuota = new Quantity(m_itemsPerCycle);
			return true;
		}

		private void trySendBufferedItem() {
			if (m_buffer.IsEmpty || ConnectedOutputPorts.IsEmpty) {
				return;
			}

			int outputsCount = ConnectedOutputPorts.Length;
			if (m_nextOutputIndex < 0 || m_nextOutputIndex >= outputsCount) {
				m_nextOutputIndex = 0;
			}

			for (int offset = 0; offset < outputsCount; offset++) {
				int index = (m_nextOutputIndex + offset) % outputsCount;
				ProductQuantity before = m_buffer;
				Quantity remaining = ConnectedOutputPorts[index].SendAsMuchAs(before);
				Quantity sent = before.Quantity - remaining;

				if (sent.IsNotPositive) {
					continue;
				}

				m_buffer = remaining.IsZero
					? ProductQuantity.None
					: before.WithNewQuantity(remaining);
				m_nextOutputIndex = (index + 1) % outputsCount;
				touchUi();
				return;
			}
		}

		/// <summary>
		/// 拆除时必须把自定义物流缓冲返还给
		/// AssetTransactionManager，不能让货物静默消失。
		/// </summary>
		protected override void OnDestroy() {
			if (m_buffer.IsNotEmpty) {
				Context.AssetTransactionManager.StoreClearedProduct(m_buffer);
				m_buffer = ProductQuantity.None;
			}

			base.OnDestroy();
		}

		public static void Serialize(MeteredGateEntity value, BlobWriter writer) {
			if (writer.TryStartClassSerialization(value)) {
				writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
			}
		}

		protected override void SerializeData(BlobWriter writer) {
			base.SerializeData(writer);
			writer.WriteInt(SaveVersion);
			writer.WriteGeneric(m_electricityConsumer);
			ProductQuantity.Serialize(m_buffer, writer);
			Quantity.Serialize(m_remainingQuota, writer);
			Duration.Serialize(m_cycleElapsed, writer);
			writer.WriteInt(m_cycleSeconds);
			writer.WriteInt(m_itemsPerCycle);
			writer.WriteInt(m_nextOutputIndex);
		}

		public static MeteredGateEntity Deserialize(BlobReader reader) {
			MeteredGateEntity value;
			if (reader.TryStartClassDeserialization(out value, null, null, false)) {
				reader.EnqueueDataDeserialization(value, s_deserializeDataDelayedAction, null);
			}
			return value;
		}

		protected override void DeserializeData(BlobReader reader) {
			base.DeserializeData(reader);

			int saveVersion = reader.ReadInt();
			switch (saveVersion) {
				case 1:
					// 0.1.0/0.2.0 没有保存 ElectricityConsumer。不能在这里立即
					// 创建，因为 ElectricityManager 的延迟数据可能尚未恢复，后续
					// 读取旧消费者列表会覆盖即时注册。整个对象图完成后再创建。
					reader.RegisterInitAfterLoad(
						this,
						nameof(initializeElectricityConsumerAfterV1Load),
						InitPriority.Lowest);
					break;

				case SaveVersion:
					m_electricityConsumer = reader.ReadGenericAs<IElectricityConsumer>();
					if (m_electricityConsumer == null) {
						throw new InvalidOperationException(
							"MeteredGate save contains no ElectricityConsumer.");
					}
					break;

				default:
					throw new InvalidOperationException(
						$"Unsupported MeteredGate save version: {saveVersion}.");
			}

			deserializeCommonState(reader);

			// m_hasPower 是可重新计算的瞬时状态；载入后等待第一个 SimUpdate。
			m_hasPower = false;
			touchUi();
		}

		private void deserializeCommonState(BlobReader reader) {
			m_buffer = ProductQuantity.Deserialize(reader);
			m_remainingQuota = Quantity.Deserialize(reader);
			m_cycleElapsed = Duration.Deserialize(reader);
			m_cycleSeconds = MeteredGateSettings.ClampCycleSeconds(reader.ReadInt());
			m_itemsPerCycle = MeteredGateSettings.ClampItemsPerCycle(reader.ReadInt());
			m_nextOutputIndex = Math.Max(0, reader.ReadInt());

			m_remainingQuota = m_remainingQuota.Clamp(
				Quantity.Zero,
				new Quantity(m_itemsPerCycle));

			Duration cycleDuration = Duration.FromSec(m_cycleSeconds);
			m_cycleElapsed = Duration.FromTicks(
				Math.Max(0, m_cycleElapsed.Ticks % cycleDuration.Ticks));
		}

		/// <summary>
		/// v1 迁移入口。InitPriority.Lowest 保证 ElectricityManager 的旧列表和
		/// 实体观察者图已经恢复，再通过官方 factory 注册新的 consumer。
		/// </summary>
		private void initializeElectricityConsumerAfterV1Load() {
			if (m_electricityConsumer != null) {
				throw new InvalidOperationException(
					"MeteredGate v1 migration attempted to create a duplicate ElectricityConsumer.");
			}

			m_electricityConsumer = Context.ElectricityConsumerFactory.CreateConsumer(this);
			m_hasPower = false;
			Log.Info($"MeteredGate: migrated entity {Id} from save format v1 to v2.");
		}

		private void touchUi() {
			unchecked {
				UiUpdateTrigger++;
			}
		}

		private static int saturatingAdd(int value, int delta) {
			long sum = (long)value + delta;
			if (sum > int.MaxValue) {
				return int.MaxValue;
			}
			if (sum < int.MinValue) {
				return int.MinValue;
			}
			return (int)sum;
		}
	}
}
