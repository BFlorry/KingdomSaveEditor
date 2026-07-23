using Xe.Tools;

namespace KHSave.SaveEditor.ViewModels
{
    public class HomeViewModel : BaseNotifyPropertyChanged
    {
        private const string DefaultFundLink = "";
        private string _fundLink;
        public string FundLink
        {
            get => _fundLink;
            set { _fundLink = value; OnPropertyChanged(); }
        }

        public HomeViewModel()
        {
            FundLink = DefaultFundLink;
        }
    }
}
