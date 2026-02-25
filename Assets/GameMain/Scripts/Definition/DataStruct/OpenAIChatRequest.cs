using System;
using System.Collections.Generic;

namespace Definition.DataStruct
{
    [Serializable]
    public sealed class OpenAIChatRequest
    {
        public string model;
        public bool stream;
        public double temperature;
        public List<OpenAIMessage> messages;
    }
}
