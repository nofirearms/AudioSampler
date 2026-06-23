using AudioSampler.Model;


namespace AudioSampler.ViewModels.Modal
{
    public class AudioSampleDetailViewModel : BaseModalViewModel<AudioSample>
    {
        private AudioSample _audioSample;

        public AudioSampleDetailViewModel(AudioSample audioSample)
        {
            _audioSample = audioSample;
        }
    }
}
