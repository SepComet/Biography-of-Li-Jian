namespace UI
{
    public interface IFormController<T> where T : UIContext
    {
        int? OpenUI(T context);
        void CloseUI();
    }
}