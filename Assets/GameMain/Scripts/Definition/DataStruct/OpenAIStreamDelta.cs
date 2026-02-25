using System;

namespace Definition.DataStruct
{
    [Serializable]
    public sealed class OpenAIStreamDelta
    {
        public string role;
        public string content;
    }
}
