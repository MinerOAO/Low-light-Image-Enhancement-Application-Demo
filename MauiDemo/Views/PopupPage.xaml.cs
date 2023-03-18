namespace MauiDemo.Views;

using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using MauiDemo.Models;
using System.Diagnostics;
using System.Net.WebSockets;

public partial class PopupPage : Popup
{
	readonly SelectPageModel _model = null;
    public bool IsClosed = false;
    public PopupPage(ref SelectPageModel model)
	{
		_model = model;
        this.Closed += OnPopupPageClosed;
        InitializeComponent();
	}

    private void OnPopupPageClosed(object sender, PopupClosedEventArgs e)
    {
        IsClosed = true;
    }

    private void OnBGClicked(object sender, EventArgs e)
	{

	}


}