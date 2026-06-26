using AudioSampler.Model;
using AudioSampler.ViewModels;
using AudioSampler.ViewModels.Modal;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AudioSampler.Services
{
    public class ModalService
    {
        private readonly ObservableCollection<IModal> _activeModals = new();
        private readonly ViewModelFactory _factory;

        public IReadOnlyList<IModal> ActiveModals => _activeModals;


        public ModalService(ViewModelFactory factory)
        {
            _factory = factory;
        }

        public async Task<ModalResult<TResult>> ShowModalAsync<TResult>(BaseModalViewModel<TResult> modalViewModel)
        {
            //await Task.Delay(30);

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

        public async Task<ModalResult<AudioSample>> OpenAudioSampleDetailModal(AudioSample audioSample)
        {
            return await ShowModalAsync(_factory.Create<AudioSampleDetailViewModel>(audioSample));
        }

        public async Task<ModalResult<object>> OpenSettingsModal()
        {
            return await ShowModalAsync(_factory.Create<SettingsViewModel>());
        }
    }
}
