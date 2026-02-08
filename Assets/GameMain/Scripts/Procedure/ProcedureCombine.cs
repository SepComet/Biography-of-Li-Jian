using Scene;
using System.Collections.Generic;
using CustomComponent;
using Definition.Enum;
using Event;
using GameFramework.Event;
using UI;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Procedure
{
    /// <summary>
    /// 斗拱拼装流程：
    /// 1. 进入流程后准备组件与事件；
    /// 2. 启动并监控拼装关卡（成功或超时）；
    /// 3. 结算后短暂等待并重开场景。
    /// </summary>
    public class ProcedureCombine : ProcedureBase
    {
        #region Property

        /// <summary>
        /// 单局超时时间（秒）。
        /// </summary>
        private const float TimeoutSeconds = 180f;

        /// <summary>
        /// 结算后重开场景的延迟（秒）。
        /// </summary>
        private const float RestartDelaySeconds = 2f;

        /// <summary>
        /// 统一日志前缀。
        /// </summary>
        private const string LogPrefix = "[GameplayATest]";

        /// <summary>
        /// 当前局是否已开始。
        /// </summary>
        private bool _isStarted = false;

        /// <summary>
        /// 当前局是否已结束。
        /// </summary>
        private bool _isFinished = false;

        /// <summary>
        /// 当前局是否通过。
        /// </summary>
        private bool _isPassed = false;

        /// <summary>
        /// 当前局累计耗时（秒）。
        /// </summary>
        private float _elapsed = 0f;

        /// <summary>
        /// 结算后的重开倒计时（秒）。
        /// </summary>
        private float _restartCountdown = 0f;

        /// <summary>
        /// 当前局开始时的实时时间戳。
        /// </summary>
        private float _startTimestamp = 0f;

        /// <summary>
        /// 拼装玩法组件入口。
        /// </summary>
        private CombineComponent _combineComponent = null;

        /// <summary>
        /// 当前流程不使用原生对话框。
        /// </summary>
        public override bool UseNativeDialog => false;

        #endregion

        #region FSM

        /// <summary>
        /// 进入流程：重置状态、订阅事件、拉取组件。
        /// </summary>
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            InitializeProcedureState();
        }

        /// <summary>
        /// 离开流程：反订阅事件、停止关卡并清理状态。
        /// </summary>
        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            ShutdownProcedureState();

            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 流程逐帧更新：
        /// 1. 未结算时推进关卡运行；
        /// 2. 已结算时推进重开倒计时。
        /// </summary>
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (TryUpdateRound(realElapseSeconds))
            {
                return;
            }

            TryRestartScene(procedureOwner, realElapseSeconds);
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// 初始化流程级状态并订阅事件。
        /// </summary>
        private void InitializeProcedureState()
        {
            ResetRoundState();
            GameEntry.Event.Subscribe(CombineCompletedEventArgs.EventId, OnPuzzleCompleted);

            _combineComponent = GameEntry.Combine;
            if (_combineComponent == null)
            {
                Log.Warning("{0} START failed. CombineComponent is missing.", LogPrefix);
            }

            Log.Info("{0} START timeout={1}s restartDelay={2}s.", LogPrefix, TimeoutSeconds.ToString("F0"),
                RestartDelaySeconds.ToString("F0"));
        }

        /// <summary>
        /// 清理流程级状态并反订阅事件。
        /// </summary>
        private void ShutdownProcedureState()
        {
            GameEntry.Event.Unsubscribe(CombineCompletedEventArgs.EventId, OnPuzzleCompleted);
            _combineComponent?.StopLevel();
            ResetRoundState();
        }

        /// <summary>
        /// 更新当前局状态。
        /// 返回 true 表示流程仍在关卡运行阶段，不进入重开逻辑。
        /// </summary>
        private bool TryUpdateRound(float realElapseSeconds)
        {
            if (_isFinished)
            {
                return false;
            }

            if (!_isStarted)
            {
                TryStartPuzzle();
                return true;
            }

            UpdateTimeout(realElapseSeconds);
            return true;
        }

        /// <summary>
        /// 启动拼装关卡并记录开始信息。
        /// </summary>
        private void TryStartPuzzle()
        {
            if (_combineComponent == null)
            {
                Log.Warning("{0} FAIL_NO_COMPONENT reason='CombineComponentMissing'.", LogPrefix);
                FinishAndScheduleRestart(false);
                return;
            }

            CombineFormContext context = BuildTestOpenData();
            int? serialId = _combineComponent.StartLevel(context);

            if (!serialId.HasValue)
            {
                Log.Warning("{0} FAIL_NO_CONTEXT reason='StartLevelFailed'.", LogPrefix);
                FinishAndScheduleRestart(false);
                return;
            }

            _isStarted = true;
            _elapsed = 0f;
            _startTimestamp = Time.realtimeSinceStartup;
            Log.Info("{0} START uiFormId={1} serialId={2}.", LogPrefix, ((int)UIFormId.CombineForm).ToString(),
                serialId.ToString());
        }

        /// <summary>
        /// 更新超时计时；超时后按失败结算。
        /// </summary>
        private void UpdateTimeout(float realElapseSeconds)
        {
            _elapsed += realElapseSeconds;
            if (_elapsed < TimeoutSeconds)
            {
                return;
            }

            _elapsed = TimeoutSeconds;
            Log.Warning("{0} FAIL_TIMEOUT elapsed={1:F2}s timeout={2:F2}s.", LogPrefix, _elapsed, TimeoutSeconds);
            FinishAndScheduleRestart(false);
        }

        /// <summary>
        /// 结算后推进重开倒计时，到时切换到换场景流程。
        /// </summary>
        private void TryRestartScene(ProcedureOwner procedureOwner, float realElapseSeconds)
        {
            if (!_isFinished)
            {
                return;
            }

            _restartCountdown -= realElapseSeconds;
            if (_restartCountdown > 0f)
            {
                return;
            }

            int nextSceneId = (int)SceneId.GameplayA;
            procedureOwner.SetData<VarInt32>("NextSceneId", nextSceneId);
            Log.Info("{0} RESTART result={1} nextSceneId={2}.", LogPrefix, _isPassed ? "PASS" : "FAIL",
                nextSceneId.ToString());
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        /// <summary>
        /// 构建测试用关卡上下文（槽位、部件与自动开始配置）。
        /// </summary>
        private static CombineFormContext BuildTestOpenData()
        {
            List<CombineSlotContext> slots = new List<CombineSlotContext>
            {
                new CombineSlotContext
                {
                    RequiredPartType = CombinePartType.Dou,
                    BuildOrder = 0,
                    RequireStrictOrder = true,
                    AnchoredPosition = new Vector2(-320f, -160f),
                    SizeDelta = new Vector2(120f, 120f),
                    MechanicsExplanation = "Dou transfers upper load and works as the base node."
                },
                new CombineSlotContext
                {
                    RequiredPartType = CombinePartType.Sheng,
                    BuildOrder = 1,
                    RequireStrictOrder = true,
                    AnchoredPosition = new Vector2(-320f, -20f),
                    SizeDelta = new Vector2(120f, 120f),
                    MechanicsExplanation = "Sheng raises layer height to form the bracket hierarchy."
                },
                new CombineSlotContext
                {
                    RequiredPartType = CombinePartType.Gong,
                    BuildOrder = 2,
                    RequireStrictOrder = true,
                    AnchoredPosition = new Vector2(-320f, 120f),
                    SizeDelta = new Vector2(120f, 120f),
                    MechanicsExplanation = "Gong spreads force laterally through overhang."
                },
                new CombineSlotContext
                {
                    RequiredPartType = CombinePartType.Qiao,
                    BuildOrder = 3,
                    RequireStrictOrder = true,
                    AnchoredPosition = new Vector2(-160f, 120f),
                    SizeDelta = new Vector2(120f, 120f),
                    MechanicsExplanation = "Qiao continues force transfer to the outer side."
                },
                new CombineSlotContext
                {
                    RequiredPartType = CombinePartType.Ang,
                    BuildOrder = 4,
                    RequireStrictOrder = true,
                    AnchoredPosition = new Vector2(0f, 120f),
                    SizeDelta = new Vector2(120f, 120f),
                    MechanicsExplanation = "Ang uses leverage to redirect eave load inward."
                }
            };

            List<CombinePartContext> parts = new List<CombinePartContext>
            {
                new CombinePartContext
                    { PartType = CombinePartType.Dou, PartDisplayName = "Dou", LockAfterPlaced = true },
                new CombinePartContext
                    { PartType = CombinePartType.Sheng, PartDisplayName = "Sheng", LockAfterPlaced = true },
                new CombinePartContext
                    { PartType = CombinePartType.Gong, PartDisplayName = "Gong", LockAfterPlaced = true },
                new CombinePartContext
                    { PartType = CombinePartType.Qiao, PartDisplayName = "Qiao", LockAfterPlaced = true },
                new CombinePartContext
                    { PartType = CombinePartType.Ang, PartDisplayName = "Ang", LockAfterPlaced = true }
            };

            return new CombineFormContext
            {
                Slots = slots,
                Parts = parts,
                AutoStart = true
            };
        }

        /// <summary>
        /// 拼装完成事件回调：记录耗时并按成功结算。
        /// </summary>
        private void OnPuzzleCompleted(object sender, GameEventArgs e)
        {
            if (!(e is CombineCompletedEventArgs))
            {
                return;
            }

            if (_isFinished)
            {
                return;
            }

            _elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _startTimestamp);
            Log.Info("{0} PASS elapsed={1:F2}s.", LogPrefix, _elapsed);
            FinishAndScheduleRestart(true);
        }

        /// <summary>
        /// 标记当前局结束，并开始重开倒计时。
        /// </summary>
        private void FinishAndScheduleRestart(bool isPassed)
        {
            _isPassed = isPassed;
            _isFinished = true;
            _restartCountdown = RestartDelaySeconds;
        }

        /// <summary>
        /// 重置局内运行态字段。
        /// </summary>
        private void ResetRoundState()
        {
            _isStarted = false;
            _isFinished = false;
            _isPassed = false;
            _elapsed = 0f;
            _restartCountdown = 0f;
            _startTimestamp = 0f;
            _combineComponent = null;
        }

        #endregion
    }
}
