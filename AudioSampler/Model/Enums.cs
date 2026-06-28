
namespace AudioSampler.Model
{
    public enum RecordingState
    {
        Initial, Record, Stop, Pause
    }

    public enum RecordingAction
    {
        Start, Stop, Pause, Cancel, Resume
    }

    public enum ExportFormat
    {
        wav, mp3
    }

    public enum SettingKey
    {
        Theme,
        FolderBookmark
    }
}
