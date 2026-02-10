using GameFramework.Fsm;
using GameFramework.Procedure;
using UI;


namespace Procedure
{
    public class ProcedureMenu : ProcedureBase
    {
        public override bool UseNativeDialog => false;

        private MenuFormController _menuFormController;

        #region FSM

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            _menuFormController = new MenuFormController();
            _menuFormController.OpenUI(new MenuFormContext());
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        }

        #endregion
    }
}