using CustomUtility;
using Definition.Enum;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRDialog : DataRowBase
    {
        private int m_Id;

        /// <summary>
        /// 获取对话编号。
        /// </summary>
        public override int Id => m_Id;

        /// <summary>
        /// 获取对话标识。
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// 获取对话形式。
        /// </summary>
        public DialogUIMode UIMode { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] fields = dataRowString.Split('\t');

            int index = 1;
            m_Id = int.Parse(fields[index++]);
            index++;
            Title = fields[index++];
            UIMode = EnumUtility<DialogUIMode>.Get(fields[index++]);

            return true;
        }
    }
}