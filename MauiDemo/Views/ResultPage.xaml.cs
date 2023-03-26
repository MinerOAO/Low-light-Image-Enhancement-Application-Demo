using CommunityToolkit.Maui.Alerts;
using MauiDemo.Models;
using System.Threading;

namespace MauiDemo.Views;

public partial class ResultPage : ContentPage
{
    ResultPageModel model;
    private ResultPage() { }
    public ResultPage(string originalName, string tempName)
    {
        model = new ResultPageModel(originalName, tempName);
        InitializeComponent();
        InitViews();
    }
    private void InitViews()
    {
        ResultImage.Source = ImageSource.FromFile(null);
        ResultImage.Source = model.ReadImageFromTemp();
    }
    private async void OnShareClicked(object sender, EventArgs e)
	{
        var cts = new CancellationTokenSource();
        var result = await model.SaveImgToDestination(cts.Token);
        if (result != null && result.IsSuccessful)
        {
            await Toast.Make($"File is saved: {result.FilePath}").Show(cts.Token);
            await model.ShareImgAfterSave(result.FilePath);
        }
    }
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var cts = new CancellationTokenSource();
        var result = await model.SaveImgToDestination(cts.Token);
        if(result != null && result.IsSuccessful)
        {
            await Toast.Make($"File is saved: {result.FilePath}").Show(cts.Token);
        }
    }
}