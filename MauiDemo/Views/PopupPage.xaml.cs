namespace MauiDemo.Views;

using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using MauiDemo.Models.Interface.PickPageModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public enum PopupInternalState
{
    Default,
    LoadingImage,
    Inferencing,
    Others
}
public partial class PopupPage : Popup
{
    readonly PickPageModel _model = null;
    public bool IsClosed = false;
    public string PopupText => this.Info.Text;
    public PopupState State;
    public PopupPage(ref PickPageModel model)
	{
		_model = model;
        State = new PopupState();
        State.PropertyChanged += OnStateChanged;
        this.Closed += OnPopupPageClosed;
        InitializeComponent();
	}

    private void OnPopupPageClosed(object sender, PopupClosedEventArgs e)
    {
        IsClosed = true;
        State.State = PopupInternalState.Default;
    }

    private void OnBGClicked(object sender, EventArgs e)
	{

	}
    private void OnStateChanged(object sender, PropertyChangedEventArgs e)
    {
        switch(State.State)
        {
            case PopupInternalState.LoadingImage:
                {
                    Info.Text = "Loading Image...";
                    break;
                }
            case PopupInternalState.Inferencing:
                {
                    Info.Text = "Inferencing. Please wait...";
                    break;
                }
            case PopupInternalState.Others:
                {
                    Info.Text = "Loading...";
                    break;
                }
            default:
            case PopupInternalState.Default:
                {
                    Info.Text = "Welcome to .NET MAUI!";
                    break;
                }

        }

    }
    public class PopupState : INotifyPropertyChanged
    {
        private PopupInternalState _state;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // The constructor is private to enforce the factory pattern.  
        public PopupState()
        {
            _state = PopupInternalState.Default;
        }

        public PopupInternalState State
        {
            get
            {
                return this._state;
            }

            set
            {
                if (value != this._state)
                {
                    this._state = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

}