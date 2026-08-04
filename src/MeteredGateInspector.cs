using System;
using Mafi;
using Mafi.Core;
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
	/// Metered Gate 的建筑 Inspector。
	///
	/// 显示三个原生风格卡片：
	/// - 周期设置、周期进度和下次刷新倒计时；
	/// - 每周期放行数量、当前剩余配额和配额进度；
	/// - 当前运行状态。
	///
	/// Inspector 只读取实体状态并调用公开设置方法，不持有物流状态。
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

			m_cycleValue = new Label()
				.FontBold()
				.Width(84.px());
			m_countdownValue = new Label()
				.FontBold();
			m_cycleProgress = new ProgressBar()
				.AlignSelfStretch();

			m_quotaValue = new Label()
				.FontBold()
				.Width(84.px());
			m_quotaSummary = new Label()
				.FontBold();
			m_quotaProgress = new ProgressBar()
				.AlignSelfStretch();

			m_statusValue = new Label()
				.FontBold();

			var cycleControls = new Row(6.pt()) {
				new ButtonText("-10 s".AsLoc(), (Action)(() => Entity?.AdjustCycleSeconds(-10))).Compact(),
				m_cycleValue,
				new ButtonText("+10 s".AsLoc(), (Action)(() => Entity?.AdjustCycleSeconds(10))).Compact()
			}
				.MarginLeft(8.pt())
				.MarginRight(8.pt());

			var cycleCard = new PanelRow(6.pt())
				.AlignSelfStretch();
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
				new ButtonText("-1".AsLoc(), (Action)(() => Entity?.AdjustItemsPerCycle(-1))).Compact(),
				m_quotaValue,
				new ButtonText("+1".AsLoc(), (Action)(() => Entity?.AdjustItemsPerCycle(1))).Compact()
			}
				.MarginLeft(8.pt())
				.MarginRight(8.pt());

			var quotaCard = new PanelRow(6.pt())
				.AlignSelfStretch();
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

			var statusCard = new PanelRow(6.pt())
				.AlignSelfStretch();
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
				(Action)(() => Entity?.RestartCycle()))
				.Compact()
				.IconSize(14.px())
				.MarginLeft(4.pt())
				.Tooltip("Restart closed cycle".AsLoc());

			restartButton.OnClick((ClickEvent evt) => evt.StopPropagation());
			panel.Header.Add(restartButton);

			this.Observe(() => Entity == null ? "none" : $"{Entity.Id}_{Entity.UiUpdateTrigger}")
				.Do(_ => refresh());
		}

		private void refresh() {
			if (Entity == null) {
				return;
			}

			m_cycleValue.Value(new LocStrFormatted($"{Entity.CycleSeconds} s"));
			m_countdownValue.Value(new LocStrFormatted(
				$"Next quota refresh in {Entity.SecondsUntilNextCycle} s"));
			m_cycleProgress.ValueFromRatio(Entity.CycleElapsedTicks, Entity.CycleDurationTicks);

			m_quotaValue.Value(new LocStrFormatted(Entity.ItemsPerCycle.ToString()));
			m_quotaSummary.Value(new LocStrFormatted(
				$"{Entity.RemainingQuota} of {Entity.ItemsPerCycle} release permits available"));
			m_quotaProgress.ValueFromRatio(Entity.RemainingQuota, Entity.ItemsPerCycle);

			m_statusValue.Value(new LocStrFormatted(Entity.GateStatus));
		}
	}
}
