using MauiDemo.Models;

namespace MauiDemo.Views;

public partial class SelectPage : ContentPage
{
    SelectPageModel model;
	public SelectPage()
	{
		InitializeComponent();
        model = new SelectPageModel();
        //AbsoluteLayout
        //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/layouts/absolutelayout?view=net-maui-7.0
        //Define in XAML
        //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/visual-states?view=net-maui-7.0
        //VisualStateManager.GoToState(StackLayout, "");
    }

    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        var result = await model.PickImage();
        if(result != null) 
        {
            SelectedImage.Source = model.LoadToDisplay(await result.OpenReadAsync());
            await model.LoadToRgb24(await result.OpenReadAsync());
            model.Inference(0.6f, 0.01f, 100);
        }
        

        model.TestCounter++;

        if (model.TestCounter == 1)
            PhotoBtn.Text = $"Clicked {model.TestCounter} time";
        else
            PhotoBtn.Text = $"Clicked {model.TestCounter} times";

        SemanticScreenReader.Announce(PhotoBtn.Text);
    }
    private void OnSelectClicked(object sender, EventArgs e)
    {
        model.TestCounter++;

        if (model.TestCounter == 1)
            SelectBtn.Text = $"Clicked {model.TestCounter} time";
        else
            SelectBtn.Text = $"Clicked {model.TestCounter} times";

        SemanticScreenReader.Announce(SelectBtn.Text);
    }

    private void OnSplitCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {

    }
    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {

    }
}