using CommunityToolkit.Mvvm.ComponentModel;

namespace AudioSampler.ViewModels
{
    public class ModdedObservableObject : ObservableObject
    {

        public void UpdateProperties()
        {
            var type = this.GetType();
            foreach (var property in type.GetProperties())
            {
                OnPropertyChanged(property.Name);
            }
        }

    }
}
