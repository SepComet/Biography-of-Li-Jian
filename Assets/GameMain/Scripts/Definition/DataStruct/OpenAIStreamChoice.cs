using System;

namespace Definition.DataStruct
{
    [Serializable]
    public sealed class OpenAIStreamChoice
    {
        public int index;
        public OpenAIStreamDelta delta;
        public string finish_reason;
    }
}
