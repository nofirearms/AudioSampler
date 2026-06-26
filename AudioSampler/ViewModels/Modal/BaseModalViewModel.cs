using AudioSampler.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;


namespace AudioSampler.ViewModels.Modal
{
    public interface IModal
    {
        string Header { get; set; }
        void CloseModal(); // Общий метод для принудительного закрытия
    }
    public abstract partial class BaseModalViewModel<TResult> : ObservableObject, IModal
    {
        public string Header { get; set; }

        private TaskCompletionSource<ModalResult<TResult>> _completionSource;

        public Task<ModalResult<TResult>> ResultTask => _completionSource.Task;

        protected BaseModalViewModel()
        {
            _completionSource = new TaskCompletionSource<ModalResult<TResult>>();  
        }

        protected virtual async void Close(bool success = true, TResult data = default, string buttonTag = "Close")
        {
            
            _completionSource.TrySetResult(new ModalResult<TResult>
            { 
                Success = success,
                Data = data,
                ButtonTag = buttonTag
            });
        }

        protected virtual void Cancel()
        {
            _completionSource.TrySetResult(new ModalResult<TResult>
            {
                Success = false,
                Data = default,
                ButtonTag = "Cancel"
            });
        }

        public void CloseModal() => Cancel();

   
        //public abstract void Dispose();
    }
}
