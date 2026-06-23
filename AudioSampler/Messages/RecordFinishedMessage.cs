
using AudioSampler.Model;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AudioSampler.Messages
{
    public class RecordFinishedMessage : ValueChangedMessage<RecordResult>
    {
        public RecordFinishedMessage(RecordResult recordFinishedEventArgs) : base(recordFinishedEventArgs) { }
    }
}
