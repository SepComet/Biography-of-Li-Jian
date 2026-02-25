using System;

namespace Definition.DataStruct
{
    [Serializable]
    public sealed class OpenAIStreamChunk
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public OpenAIStreamChoice[] choices;
    }
}
