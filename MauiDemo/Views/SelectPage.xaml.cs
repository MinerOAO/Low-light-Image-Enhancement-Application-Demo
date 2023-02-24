using MauiDemo.Models;
using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace MauiDemo.Views;

public partial class SelectPage : ContentPage
{
    SelectPageModel model;
	public SelectPage()
	{
		InitializeComponent();
        model = new SelectPageModel();
        ValueBinding();
        
        //AbsoluteLayout
        //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/layouts/absolutelayout?view=net-maui-7.0
        //Define in XAML
        //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/visual-states?view=net-maui-7.0
        //VisualStateManager.GoToState(StackLayout, "");
    }
    private void ValueBinding()
    {
        GammaSlider.SetBinding(Slider.ValueProperty, new Binding("Gamma", BindingMode.TwoWay, source: model));
        StrengthSlider.SetBinding(Slider.ValueProperty, new Binding(path: "Strength", mode: BindingMode.TwoWay, source: model));
        QualitySlider.SetBinding(Slider.ValueProperty, new Binding("Quality", BindingMode.TwoWay, source: model));

        typePicker.ItemsSource = Enum.GetNames(
            typeof(MauiDemo.Models.Interface.OnnxRuntimeWrapper.InferenceType)).ToList();
        typePicker.SetBinding(
            Picker.SelectedIndexProperty, new Binding(path: "Type", mode: BindingMode.TwoWay, converter: new EnumToIntConverter(), source: model));
    }
    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        var result = await model.PickImage();
        if(result != null) 
        {
            SelectedImage.Source = model.LoadToDisplay(await result.OpenReadAsync());
            await model.LoadToRgb24(await result.OpenReadAsync());
            model.Inference();
        }
        

        model.TestCounter++;

        if (model.TestCounter == 1)
            PhotoBtn.Text = $"Clicked {model.TestCounter} time";
        else
            PhotoBtn.Text = $"Clicked {model.TestCounter} times";

        SemanticScreenReader.Announce(PhotoBtn.Text);
    }
    private async void OnSelectClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ResultPage());
    }

    private void OnDownSampleCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {

    }
    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {

    }
}
public class EnumToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (InferenceType)value;
    }
}