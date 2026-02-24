using Definition.Enum;
using System.Collections.Generic;
using CustomComponent;
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
            
            //InitializeProcedureState();
            GameEntry.Dialog.Init(1);

            //GameEntry.Dialog.StartDialog(1001);
            GameEntry.Dialog.StartDialog(1002);
        }

        /// <summary>
        /// 离开流程：反订阅事件、停止关卡并清理状态。
        /// </summary>
        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
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

            if (GameEntry.Dialog.IsInitialized)
            {
            }
            // if (TryUpdateRound(realElapseSeconds))
            // {
            //     return;
            // }
            //
            // TryRestartScene(procedureOwner, realElapseSeconds);
        }

        #endregion

        #region Other Methods

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

        #endregion
    }
}