using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Messages
{
    public class ToggleRecordMessage : ValueChangedMessage<bool>
    {
        public ToggleRecordMessage(bool isRecording) : base(isRecording)
        {
        }
    }
}
