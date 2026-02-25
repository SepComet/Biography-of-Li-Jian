using System;

namespace Definition.DataStruct
{
    [Serializable]
    public sealed class OpenAIMessage
    {
        public string role;
        public string content;
    }
}