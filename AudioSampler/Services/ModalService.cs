using AudioSampler.Model;
using AudioSampler.ViewModels.Modal;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AudioSampler.Services
{
    public class ModalService
    {
        private readonly ObservableCollection<IModal> _activeModals = new();
        public IReadOnlyList<IModal> ActiveModals => _activeModals;

        public async Task<ModalResult<TResult>> ShowModalAsync<TResult>(BaseModalViewModel<TResult> modalViewModel)
        {
            await Task.Delay(30);

            return await App.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    _activeModals.Add(modalViewModel);
                    var result = await modalViewModel.ResultTask;
                    return result;
                }
                finally
                {
                    _activeModals.Remove(modalViewModel);
                }
            });

        }

        public async Task<ModalResult<AudioSample>> OpenAudioSampleDetailedModal(AudioSample audioSample)
        {
            return await ShowModalAsync(new AudioSampleDetailViewModel(audioSample));
        }

        public async Task<ModalResult<object>> OpenSettingsModal()
        {
            return await ShowModalAsync(new SettingsViewModel());
        }
    }
}
