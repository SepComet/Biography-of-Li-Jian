using System.Collections.Generic;

namespace UI
{
    public class AIChatFormContext : UIContext
    {
        public AIChatFormController Controller;
        public string Title = "AI Chat";
        public bool ClearHistoryOnOpen = true;
        public int LanguageMode = 0;
        public List<AIChatMessageContext> Messages = new List<AIChatMessageContext>();
    }
}
