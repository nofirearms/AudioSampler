using AudioSampler.Model;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Messages
{
    public class ToggleRecordMessage : ValueChangedMessage<RecordingAction>
    {
        public ToggleRecordMessage(RecordingAction recordingAction) : base(recordingAction)
        {
        }
    }
}
