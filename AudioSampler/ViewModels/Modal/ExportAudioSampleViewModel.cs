using AudioSampler.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace AudioSampler.ViewModels.Modal
{
    public partial class ExportAudioSampleViewModel : BaseModalViewModel<ExportSettings>
    {

        [ObservableProperty]
        private string _path;

        [ObservableProperty]
        private string _fileName;

        public List<ExportFormat> Formats => Enum.GetValues(typeof(ExportFormat)).Cast<ExportFormat>().ToList();

        [ObservableProperty]
        private ExportFormat _format;

        [ObservableProperty]
        private bool _trim;

        [ObservableProperty]
        private bool _normalize;

        public ExportAudioSampleViewModel(ExportSettings exportSettings)
        {
            _path = exportSettings.Path;
            _fileName = exportSettings.Name;
            _format = exportSettings.Format;
            _trim = exportSettings.Trim;
            _normalize = exportSettings.Normalize;
        }


        [RelayCommand]
        public void Export()
        {
            var export = new ExportSettings
            {
                Format = Format,
                Name = FileName,
                Normalize = Normalize,
                Path = Path,
                Trim = Trim
            };

            Close(true, export);
        }
    }
}
