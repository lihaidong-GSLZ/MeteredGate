using System;
using Mafi;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Ports;
using Mafi.Core.Products;
using Mafi.Core.Ports.Io;
using Mafi.Serialization;

namespace MeteredGate {
	/// <summary>
	/// Metered Gate 的运行时实体。它不是“配方机器”，而是一个自定义物流节点。
	///
	/// 数据流：上游调用 ReceiveAsMuchAsFromPort() -> 本实体最多接收 1 件 ->
	/// SimUpdate() 再尝试将该物品送往某个输出端口。
	///
	/// 周期使用游戏自己的 Mafi.Duration：Duration.FromSec() 表示游戏秒，
	/// Duration.OneTick 表示一个模拟 tick。因此它跟随游戏模拟时间，而不是现实时间。
	/// 这与官方配方 SetDuration(...) 使用同一种时间定义，但没有使用配方调度器。
	///
	/// 配额在物品被接收时扣除，因为此时物品已经离开上游储存/传送带。
	/// 输出堵塞时，内部最多保留一件，不会继续预取。
	/// </summary>
	public sealed class MeteredGateEntity :
		LayoutEntity,
		IEntityWithPorts,
		IEntityWithSimUpdate,
		IEntityWithCloneableConfig {

		// 自定义存档格式版本。字段顺序改变时必须升级并实现迁移。
		private const int SaveVersion = 1;
		// 下列两个键只用于复制/蓝图配置，不用于保存实时运行状态。
		private const string ConfigCycleSeconds = "MeteredGate.CycleSeconds";
		private const string ConfigItemsPerCycle = "MeteredGate.ItemsPerCycle";

		// CoI 的 Blob 序列化使用延迟回调；静态委托避免重复分配闭包。
		private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction =
			(obj, writer) => ((MeteredGateEntity)obj).SerializeData(writer);

		private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction =
			(obj, reader) => ((MeteredGateEntity)obj).DeserializeData(reader);

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
			m_buffer = ProductQuantity.None;
			m_remainingQuota = Quantity.Zero;
			m_cycleElapsed = Duration.Zero;
			m_cycleSeconds = MeteredGateSettings.DefaultCycleSeconds;
			m_itemsPerCycle = MeteredGateSettings.DefaultItemsPerCycle;
			m_nextOutputIndex = 0;
		}

		// 允许暂停单栋建筑；暂停后周期不推进，也不接收物品。
		public override bool CanBePaused => true;

		// Inspector 观察该计数器；状态变化时递增以触发 UI 刷新。
		public int UiUpdateTrigger { get; private set; }
		public int CycleSeconds => m_cycleSeconds;
		public int ItemsPerCycle => m_itemsPerCycle;
		public int RemainingQuota => m_remainingQuota.Value;
		public int CycleElapsedTicks => m_cycleElapsed.Ticks;
		public int CycleDurationTicks => Duration.FromSec(m_cycleSeconds).Ticks;

		/// <summary>
		/// 向上取整显示下次周期刷新还需多少秒。
		/// </summary>
		public int SecondsUntilNextCycle {
			get {
				int remainingTicks = Math.Max(0, CycleDurationTicks - m_cycleElapsed.Ticks);
				int ticksPerSecond = Duration.OneSecond.Ticks;
				return (remainingTicks + ticksPerSecond - 1) / ticksPerSecond;
			}
		}

		/// <summary>供 Inspector 显示的简短状态。</summary>
		public string GateStatus {
			get {
				if (IsNotEnabled) {
					return "Paused or disabled";
				}
				if (m_buffer.IsNotEmpty) {
					return ConnectedOutputPorts.IsEmpty ? "Holding 1 item; no output" : "Holding 1 item; output blocked";
				}
				if (m_remainingQuota.IsNotPositive) {
					return "Waiting for next cycle";
				}
				return "Ready";
			}
		}

		/// <summary>
		/// 上游尝试交货的入口。返回值是“未被接收的数量”，不是接收量。
		/// sourcePort 当前未使用，因为所有输入端口共享同一配额。
		/// </summary>
		public Quantity ReceiveAsMuchAsFromPort(ProductQuantity offered, IoPortToken sourcePort) {
			// 暂停、空交付、内部已有物品或配额耗尽时，整批拒收。
			if (IsNotEnabled || offered.IsEmpty || m_buffer.IsNotEmpty || m_remainingQuota.IsNotPositive) {
				return offered.Quantity;
			}

			// 接收量 = min(上游提供量, 1, 当前剩余配额)。
			// 因此上游即使一次提供多件，也只有一件能离开上游。
			Quantity accepted = offered.Quantity.Min(Quantity.One).Min(m_remainingQuota);
			if (accepted.IsNotPositive) {
				return offered.Quantity;
			}

			// ProductQuantity 保留产品类型，只把数量改为实际接收量。
			m_buffer = offered.WithNewQuantity(accepted);
			// 在“进入闸门”这一刻扣配额，输出堵塞也不会继续从上游吸料。
			m_remainingQuota -= accepted;
			UiUpdateTrigger++;
			return offered.Quantity - accepted;
		}

		/// <summary>
		/// 每次模拟更新推进一个 Duration.OneTick，然后尝试发送缓冲物品。
		/// 全局暂停时游戏不会推进模拟；单栋建筑暂停时这里主动返回。
		/// </summary>
		public void SimUpdate() {
			if (IsNotEnabled) {
				return;
			}

			advanceCycle();
			trySendBufferedItem();

			// Drives the visible progress bar. This value is intentionally not serialized.
			UiUpdateTrigger++;
		}

		/// <summary>
		/// 修改周期后清空当前配额并重新读条，防止修改参数获得免费额度。
		/// </summary>
		public void SetCycleSeconds(int value) {
			int clamped = MeteredGateSettings.ClampCycleSeconds(value);
			if (clamped == m_cycleSeconds) {
				return;
			}

			m_cycleSeconds = clamped;

			// Changing the period must not grant free quota. Start a fresh closed cycle.
			m_cycleElapsed = Duration.Zero;
			m_remainingQuota = Quantity.Zero;
			UiUpdateTrigger++;
		}

		public void AdjustCycleSeconds(int delta) {
			SetCycleSeconds(m_cycleSeconds + delta);
		}

		/// <summary>
		/// 降低配额会立即压低当前剩余额度；提高配额不会立即补发，等下一周期。
		/// </summary>
		public void SetItemsPerCycle(int value) {
			int clamped = MeteredGateSettings.ClampItemsPerCycle(value);
			if (clamped == m_itemsPerCycle) {
				return;
			}

			m_itemsPerCycle = clamped;
			m_remainingQuota = m_remainingQuota.Min(new Quantity(clamped));
			UiUpdateTrigger++;
		}

		public void AdjustItemsPerCycle(int delta) {
			SetItemsPerCycle(m_itemsPerCycle + delta);
		}

		/// <summary>手动清空配额并从零重新读条；不删除内部已缓冲物品。</summary>
		public void RestartCycle() {
			m_cycleElapsed = Duration.Zero;
			m_remainingQuota = Quantity.Zero;
			UiUpdateTrigger++;
		}

		/// <summary>复制/蓝图只导出周期和配额设置。</summary>
		public void AddToConfig(EntityConfigData config) {
			config.SetInt(ConfigCycleSeconds, m_cycleSeconds);
			config.SetInt(ConfigItemsPerCycle, m_itemsPerCycle);
		}

		/// <summary>应用复制配置，但不复制当前相位、剩余配额或输出轮询位置。</summary>
		public void ApplyConfig(EntityConfigData config) {
			int? cycleSeconds = config.GetInt(ConfigCycleSeconds);
			int? itemsPerCycle = config.GetInt(ConfigItemsPerCycle);

			if (cycleSeconds.HasValue) {
				m_cycleSeconds = MeteredGateSettings.ClampCycleSeconds(cycleSeconds.Value);
			}
			if (itemsPerCycle.HasValue) {
				m_itemsPerCycle = MeteredGateSettings.ClampItemsPerCycle(itemsPerCycle.Value);
			}

			// Copying/blueprinting transfers the settings, not live runtime allowance.
			m_cycleElapsed = Duration.Zero;
			m_remainingQuota = Quantity.Zero;
			m_nextOutputIndex = 0;
			UiUpdateTrigger++;
		}

		/// <summary>推进游戏模拟周期，并在跨过边界时刷新配额。</summary>
		private void advanceCycle() {
			// 与配方时长一样，周期最终转换为 Mafi.Duration。
			Duration cycleDuration = Duration.FromSec(m_cycleSeconds);
			// 不是 DateTime/Stopwatch；只增加一个游戏模拟 tick。
			m_cycleElapsed += Duration.OneTick;
			if (m_cycleElapsed < cycleDuration) {
				return;
			}

			// 用取模保留当前周期相位。下面使用“赋值”而不是“加法”，
			// 所以未使用配额不会跨周期累计成爆发。
			m_cycleElapsed = Duration.FromTicks(m_cycleElapsed.Ticks % cycleDuration.Ticks);
			m_remainingQuota = new Quantity(m_itemsPerCycle);
		}

		/// <summary>
		/// 尝试把缓冲物品送往输出。采用简单 round-robin，
		/// 不包含原版 Balancer 的优先级和均匀分配控制。
		/// </summary>
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
				// SendAsMuchAs 返回“未发送的剩余数量”。
				Quantity remaining = ConnectedOutputPorts[index].SendAsMuchAs(before);
				Quantity sent = before.Quantity - remaining;

				if (sent.IsNotPositive) {
					continue;
				}

				m_buffer = remaining.IsZero
					? ProductQuantity.None
					: before.WithNewQuantity(remaining);
				m_nextOutputIndex = (index + 1) % outputsCount;
				UiUpdateTrigger++;
				return;
			}
		}

		/// <summary>游戏保存系统调用的序列化入口。</summary>
		public static void Serialize(MeteredGateEntity value, BlobWriter writer) {
			if (writer.TryStartClassSerialization(value)) {
				writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
			}
		}

		/// <summary>按固定顺序写入内部物品、配额、周期相位和设置。</summary>
		protected override void SerializeData(BlobWriter writer) {
			base.SerializeData(writer);
			writer.WriteInt(SaveVersion);
			ProductQuantity.Serialize(m_buffer, writer);
			Quantity.Serialize(m_remainingQuota, writer);
			Duration.Serialize(m_cycleElapsed, writer);
			writer.WriteInt(m_cycleSeconds);
			writer.WriteInt(m_itemsPerCycle);
			writer.WriteInt(m_nextOutputIndex);
		}

		/// <summary>游戏载入系统调用的反序列化入口。</summary>
		public static MeteredGateEntity Deserialize(BlobReader reader) {
			MeteredGateEntity value;
			if (reader.TryStartClassDeserialization(out value, null, null, false)) {
				reader.EnqueueDataDeserialization(value, s_deserializeDataDelayedAction, null);
			}
			return value;
		}

		/// <summary>按与 SerializeData 完全相同的顺序恢复运行状态。</summary>
		protected override void DeserializeData(BlobReader reader) {
			base.DeserializeData(reader);
			int saveVersion = reader.ReadInt();
			if (saveVersion != SaveVersion) {
				throw new InvalidOperationException($"Unsupported MeteredGate save version: {saveVersion}");
			}

			m_buffer = ProductQuantity.Deserialize(reader);
			m_remainingQuota = Quantity.Deserialize(reader);
			m_cycleElapsed = Duration.Deserialize(reader);
			m_cycleSeconds = MeteredGateSettings.ClampCycleSeconds(reader.ReadInt());
			m_itemsPerCycle = MeteredGateSettings.ClampItemsPerCycle(reader.ReadInt());
			m_nextOutputIndex = Math.Max(0, reader.ReadInt());

			// Defensive clamps for manually edited/corrupt saves.
			m_remainingQuota = m_remainingQuota.Clamp(Quantity.Zero, new Quantity(m_itemsPerCycle));
			Duration cycleDuration = Duration.FromSec(m_cycleSeconds);
			m_cycleElapsed = Duration.FromTicks(Math.Max(0, m_cycleElapsed.Ticks % cycleDuration.Ticks));
			UiUpdateTrigger++;
		}
	}
}
