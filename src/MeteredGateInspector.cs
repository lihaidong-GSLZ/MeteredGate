using System;
using Mafi;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.Ui.Library.Inspectors;
using Mafi.Unity.UiToolkit;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ClickEvent = UnityEngine.UIElements.ClickEvent;

namespace MeteredGate {
	/// <summary>
	/// Inspector 只读取实体状态。所有会改变模拟状态的操作都通过
	/// BaseInspector.ScheduleCommand() 进入游戏输入调度器。
	/// </summary>
	[GlobalDependency(RegistrationMode.AsAllInterfaces, false, false)]
	public sealed class MeteredGateInspector : BaseInspector<MeteredGateEntity> {
		private readonly Label m_cycleValue;
		private readonly Label m_countdownValue;
		private readonly ProgressBar m_cycleProgress;

		private readonly Label m_quotaValue;
		private readonly Label m_quotaSummary;
		private readonly ProgressBar m_quotaProgress;

		private readonly Label m_statusValue;

		public MeteredGateInspector(UiContext context) : base(context) {
			EmbedStatusToTheTop();

			m_cycleValue = new Label().FontBold().Width(84.px());
			m_countdownValue = new Label().FontBold();
			m_cycleProgress = new ProgressBar().AlignSelfStretch();

			m_quotaValue = new Label().FontBold().Width(84.px());
			m_quotaSummary = new Label().FontBold();
			m_quotaProgress = new ProgressBar().AlignSelfStretch();

			m_statusValue = new Label().FontBold();

			var cycleControls = new Column(4.pt()) {
				new Row(6.pt()) {
					new ButtonText(
						"-1 s".AsLoc(),
						(Action)(() => schedule(
							MeteredGateCommandKind.AdjustCycleSeconds,
							-1))).Compact(),
					m_cycleValue,
					new ButtonText(
						"+1 s".AsLoc(),
						(Action)(() => schedule(
							MeteredGateCommandKind.AdjustCycleSeconds,
							1))).Compact()
				},
				new Row(6.pt()) {
					new ButtonText(
						"-30 s".AsLoc(),
						(Action)(() => schedule(
							MeteredGateCommandKind.AdjustCycleSeconds,
							-30))).Compact(),
					new Label().Width(84.px()),
					new ButtonText(
						"+30 s".AsLoc(),
						(Action)(() => schedule(
							MeteredGateCommandKind.AdjustCycleSeconds,
							30))).Compact()
				}
			}
				.MarginLeft(8.pt())
				.MarginRight(8.pt());

			var cycleCard = new PanelRow(6.pt()).AlignSelfStretch();
			cycleCard.Add(
				new Column(4.pt()) {
					new Label("Cycle".AsLoc()).FontBold(),
					cycleControls,
					m_cycleProgress,
					m_countdownValue
				}
				.MarginLeft(8.pt())
				.MarginRight(8.pt())
				.MarginBottom(8.pt()));

			var quotaControls = new Row(6.pt()) {
				new ButtonText(
					"-1".AsLoc(),
					(Action)(() => schedule(
						MeteredGateCommandKind.AdjustItemsPerCycle,
						-1))).Compact(),
				m_quotaValue,
				new ButtonText(
					"+1".AsLoc(),
					(Action)(() => schedule(
						MeteredGateCommandKind.AdjustItemsPerCycle,
						1))).Compact()
			}
				.MarginLeft(8.pt())
				.MarginRight(8.pt());

			var quotaCard = new PanelRow(6.pt()).AlignSelfStretch();
			quotaCard.Add(
				new Column(4.pt()) {
					new Label("Release quota".AsLoc()).FontBold(),
					quotaControls,
					m_quotaProgress,
					m_quotaSummary
				}
				.MarginLeft(8.pt())
				.MarginRight(8.pt())
				.MarginBottom(8.pt()));

			var statusCard = new PanelRow(6.pt()).AlignSelfStretch();
			statusCard.Add(
				new Column(4.pt()) {
					new Label("Status".AsLoc()).FontBold(),
					m_statusValue
				}
				.MarginLeft(8.pt())
				.MarginRight(8.pt())
				.MarginBottom(8.pt()));

			var panelContent = new Column(8.pt()) {
				cycleCard,
				quotaCard,
				statusCard
			}
				.MarginBottom(8.pt());

			var panel = AddPanelWithHeader(panelContent);
			panel.Title("Metered gate".AsLoc());

			var restartButton = new ButtonIcon(
				Button.General,
				"Assets/Unity/UserInterface/General/Repeat.svg",
				(Action)(() => schedule(MeteredGateCommandKind.RestartCycle)))
				.Compact()
				.IconSize(14.px())
				.MarginLeft(4.pt())
				.Tooltip("Restart closed cycle".AsLoc());

			restartButton.OnClick((ClickEvent evt) => evt.StopPropagation());
			panel.Header.Add(restartButton);

			// 分别观察实体引用和整数触发器，避免每次刷新分配插值字符串。
			this.Observe(() => Entity)
				.Observe(() => Entity == null ? 0 : Entity.UiUpdateTrigger)
				.Do((_, __) => refresh());
		}

		private void schedule(MeteredGateCommandKind kind, int value = 0) {
			MeteredGateEntity entity = Entity;
			if (entity == null) {
				return;
			}

			ScheduleCommand(new MeteredGateConfigCmd(entity.Id, kind, value));
		}

		private void refresh() {
			// 对本次刷新使用同一个实体快照，避免选择切换时多次读取 Entity
			// 得到不同对象或在中途变为 null。
			MeteredGateEntity entity = Entity;
			if (entity == null) {
				return;
			}

			m_cycleValue.Value(new LocStrFormatted($"{entity.CycleSeconds} s"));
			m_countdownValue.Value(new LocStrFormatted(
				$"Next quota refresh in {entity.SecondsUntilNextCycle} s"));
			m_cycleProgress.ValueFromRatio(entity.CycleElapsedTicks, entity.CycleDurationTicks);

			m_quotaValue.Value(new LocStrFormatted(entity.ItemsPerCycle.ToString()));
			m_quotaSummary.Value(new LocStrFormatted(
				$"{entity.RemainingQuota} of {entity.ItemsPerCycle} release permits available"));
			m_quotaProgress.ValueFromRatio(entity.RemainingQuota, entity.ItemsPerCycle);

			m_statusValue.Value(new LocStrFormatted(entity.GateStatus));
		}
	}
}
