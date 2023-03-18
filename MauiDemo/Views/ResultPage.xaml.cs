using MauiDemo.Models;

namespace MauiDemo.Views;

public partial class ResultPage : ContentPage
{
    ResultPageModel model;
    private ResultPage() { }
    public ResultPage(string imgName)
    {
        model = new ResultPageModel(imgName);
        InitializeComponent();
    }
    private void OnShareClicked(object sender, EventArgs e)
	{

	}
    private void OnCompleteClicked(object sender, EventArgs e)
    {

    }
}