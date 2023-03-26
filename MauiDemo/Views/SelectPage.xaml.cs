using MauiDemo.Models;
using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using CommunityToolkit.Maui.Views;
using System.Globalization;
using System.Diagnostics;
using System.ComponentModel;

namespace MauiDemo.Views;

public partial class SelectPage : ContentPage
{
    static SelectPageModel model;
    PopupPage popup = null;
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

        DownSampleCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding(path: "IsDownSample", mode: BindingMode.TwoWay, source: model));
        //SelectBtn.SetBinding(Button.IsEnabledProperty, new Binding(path: "StateV2", mode: BindingMode.TwoWay, converter: new StateClassToBoolConverter(), source: model));
        model.StateV2.PropertyChanged += StateV2_PropertyChanged;

        GammaSlider.BindingContext = model;
        StrengthSlider.BindingContext = model;
        QualitySlider.BindingContext = model;
        typePicker.BindingContext = model;
        DownSampleCheckBox.BindingContext = model;
        SelectBtn.BindingContext = model;

    }

    private void StateV2_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch(model.StateV2.ModelState)
        {
            case InternalState.Inferencing:
                {
                    InferenceBtn.IsEnabled = true;
                    SelectBtn.IsEnabled = false;
                    PhotoBtn.IsEnabled = false;
                    InferenceBtn.Text = "Inferencing...";
                    break;
                }
            case InternalState.ImageLoaded:
                {
                    InferenceBtn.IsEnabled = true;
                    SelectBtn.IsEnabled = true;
                    PhotoBtn.IsEnabled = true;
                    InferenceBtn.Text = "Start!";
                    break;
                }
            case InternalState.Idle:
                {
                    InferenceBtn.IsEnabled = false;
                    SelectBtn.IsEnabled = true;
                    PhotoBtn.IsEnabled = true;
                    InferenceBtn.Text = "Start!";
                    break;
                }
        }
        SemanticScreenReader.Announce(PhotoBtn.Text);
    }

    private async void OnSelectClicked(object sender, EventArgs e)
    {
        var result = await model.PickImage();
        if(result != null) 
        {
            SelectedImage.Source = model.LoadToDisplay(await result.OpenReadAsync());

            popup = new PopupPage(ref model);
            popup.CanBeDismissedByTappingOutsideOfPopup = false;
            this.ShowPopup(popup);

            await model.LoadToRgb24(await result.OpenReadAsync(), result.FileName);
            if (popup.IsClosed != true)
                popup.Close();
        }
        else
        {
            SelectedImage.Source = ImageSource.FromFile(null);
            await model.LoadToRgb24(null, null);
        }

        //model.TestCounter++;

        //if (model.TestCounter == 1)
        //    PhotoBtn.Text = $"Clicked {model.TestCounter} time";
        //else
        //    PhotoBtn.Text = $"Clicked {model.TestCounter} times";
        //SemanticScreenReader.Announce(PhotoBtn.Text);
    }
    private async void OnInferenceClicked(object sender, EventArgs e)
    {
        popup = new PopupPage(ref model);
        this.ShowPopup(popup);
        if(model.StateV2.ModelState == InternalState.ImageLoaded)
        {
            var result = await model.Inference();
            if(popup.IsClosed != true)
                popup.Close();
            if(result != null)
                await Navigation.PushAsync(new ResultPage(model.originalImgName, result));
        }

    }
    private void OnPhotoClicked(object sender, EventArgs e)
    {
        //await Navigation.PushAsync(new ResultPage());
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