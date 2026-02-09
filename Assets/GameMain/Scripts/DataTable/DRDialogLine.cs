using CustomUtility;
using Definition.Enum;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRDialogLine : DataRowBase
    {
        private int m_Id;

        /// <summary>
        /// 获取对话行编号
        /// </summary>
        public override int Id => m_Id;

        /// <summary>
        /// 获取说话人 Id。
        /// </summary>
        public string SpeakerId { get; private set; }

        /// <summary>
        /// 获取说话人表情。
        /// </summary>
        public ExpressionType Expression { get; private set; }

        /// <summary>
        /// 获取说话人显示名。
        /// </summary>
        public string SpeakerName { get; private set; }

        /// <summary>
        /// 获取说话人朝向。
        /// </summary>
        public int Direction { get; private set; }

        /// <summary>
        /// 获取对话内容。
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// 获取对话效果。
        /// </summary>
        public EmphasisType Emphasis { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] fields = dataRowString.Split('\t');

            int index = 0;
            index++;
            m_Id = int.Parse(fields[index++]);
            index++;
            SpeakerId = fields[index++];
            Expression = EnumUtility<ExpressionType>.Get(fields[index++]);
            SpeakerName = fields[index++];
            Direction = int.Parse(fields[index++]);
            Text = fields[index++];
            Emphasis = EnumUtility<EmphasisType>.Get(fields[index++]);

            return true;
        }
    }
}