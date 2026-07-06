using AudioSampler.Services;
using AudioSampler.ViewModels.Modal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace AudioSampler.ViewModels
{
    public partial class ShellViewModel : ViewModelBase
    {
        private readonly ModalService _modalService;

        //------------------------ MODAL -------------------------------------
        public IReadOnlyList<IModal> ActiveModals => _modalService.ActiveModals;
        public bool HasActiveModals => _modalService.ActiveModals.Any();
        //--------------------------------------------------------------------

        public MainViewModel MainViewModel { get; set; }

        public ShellViewModel() { }
        public ShellViewModel(ModalService modalService, MainViewModel mainViewModel)
        {
            _modalService = modalService;
            MainViewModel = mainViewModel;

            ((INotifyCollectionChanged)_modalService.ActiveModals).CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(ActiveModals));
                OnPropertyChanged(nameof(HasActiveModals));
            };
        }
    }
}
