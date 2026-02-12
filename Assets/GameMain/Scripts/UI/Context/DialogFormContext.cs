using Definition.Enum;

namespace UI
{
    public class DialogFormContext : UIContext
    {
        public float PlayingSpeed = 1f;

        public int ChapterId = 0;
        public int DialogId = 0;
        public string DialogTitle = string.Empty;
        public DialogFormMode DialogUIMode = DialogFormMode.None;

        public int CurrentLineId = 0;
        public string SpeakerId = string.Empty;
        public string SpeakerName = string.Empty;
        public ExpressionType Expression = ExpressionType.None;
        public int Direction = 0;
        public string Text = string.Empty;
        public EmphasisType Emphasis = EmphasisType.None;

        public int LineIndex = -1;
        public int TotalLines = 0;
        public bool IsLastLine = false;
        
        public DialogWindowAlpha DialogWindowAlpha = DialogWindowAlpha.Medium;
    }
}
