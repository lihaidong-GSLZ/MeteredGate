using System;
using Mafi;
using Mafi.Core.Mods;

namespace MeteredGate {
	/// <summary>
	/// 读取 config.json 中“新建建筑”的默认值。已有建筑保留各自存档设置。
	/// </summary>
	internal static class MeteredGateSettings {
		private const string CycleSecondsKey = "default_cycle_seconds";
		private const string ItemsPerCycleKey = "default_items_per_cycle";

		public const int MinCycleSeconds = 1;
		public const int MaxCycleSeconds = 3600;
		public const int MinItemsPerCycle = 1;
		public const int MaxItemsPerCycle = 1000;

		private static ModJsonConfig s_config;

		public static int DefaultCycleSeconds { get; private set; } = 60;
		public static int DefaultItemsPerCycle { get; private set; } = 1;

		/// <summary>绑定官方 ModJsonConfig，并监听运行时配置变化。</summary>
		public static void Bind(ModJsonConfig config) {
			if (ReferenceEquals(s_config, config)) {
				Reload();
				return;
			}

			Unbind();
			s_config = config;
			s_config.OnValueChanged += onValueChanged;
			Reload();
		}

		/// <summary>取消事件订阅，避免静态引用残留。</summary>
		public static void Unbind() {
			if (s_config == null) {
				return;
			}

			s_config.OnValueChanged -= onValueChanged;
			s_config = null;
		}

		public static int ClampCycleSeconds(int value) {
			return Math.Max(MinCycleSeconds, Math.Min(MaxCycleSeconds, value));
		}

		public static int ClampItemsPerCycle(int value) {
			return Math.Max(MinItemsPerCycle, Math.Min(MaxItemsPerCycle, value));
		}

		private static void onValueChanged(string key) {
			if (key == CycleSecondsKey || key == ItemsPerCycleKey) {
				Reload();
			}
		}

		/// <summary>重新读取并夹紧默认值；不会修改已有实体。</summary>
		private static void Reload() {
			if (s_config == null) {
				return;
			}

			DefaultCycleSeconds = ClampCycleSeconds(s_config.GetInt(CycleSecondsKey, 60));
			DefaultItemsPerCycle = ClampItemsPerCycle(s_config.GetInt(ItemsPerCycleKey, 1));
			Log.Info(
				$"MeteredGate: defaults loaded: {DefaultItemsPerCycle} item(s) per {DefaultCycleSeconds} second(s). " +
				"Existing buildings retain their individual values.");
		}
	}
}
