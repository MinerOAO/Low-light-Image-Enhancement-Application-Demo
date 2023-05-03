using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using CommunityToolkit.Maui.Views;
using System.Globalization;
using System.ComponentModel;
using MauiDemo.Models.Interface.PickPageModel;
using System.Runtime.CompilerServices;

namespace MauiDemo.Views;

public enum PickPageInternalState
{
    Default,
    LoadingImage,
    ImageLoaded,
    Inferencing,
    Previewing,
    Others
}
public class PickPageState : INotifyPropertyChanged
{
    private PickPageInternalState _state;

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // The constructor is private to enforce the factory pattern.  
    public PickPageState()
    {
        _state = PickPageInternalState.Default;
    }

    public PickPageInternalState State
    {
        get
        {
            return this._state;
        }

        set
        {
            this._state = value;
            NotifyPropertyChanged();
        }
    }
}
public partial class PickPage : ContentPage
{
    PickPageState pickPageState;
    static PickPageModel model;

    private FileResult result = null;

    PopupPage popup = null;
	public PickPage()
	{
		InitializeComponent();
        pickPageState = new PickPageState();
        model = new PickPageModel();
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
        pickPageState.PropertyChanged += PickPageState_PropertyChanged;

        GammaSlider.BindingContext = model;
        StrengthSlider.BindingContext = model;
        QualitySlider.BindingContext = model;
        typePicker.BindingContext = model;
        DownSampleCheckBox.BindingContext = model;
        PickBtn.BindingContext = model;
    }

    private void StateV2_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch(model.StateV2.ModelState)
        {
            case InternalState.Inferencing:
                {
                    InferenceBtn.IsEnabled = false;
                    PickBtn.IsEnabled = false;
                    PhotoBtn.IsEnabled = false;
                    InferenceBtn.Text = "Inferencing...";
                    break;
                }
            case InternalState.ImageLoaded:
                {
                    InferenceBtn.IsEnabled = true;
                    PickBtn.IsEnabled = true;
                    PhotoBtn.IsEnabled = true;
                    InferenceBtn.Text = "Start!";
                    break;
                }
            case InternalState.Idle:
                {
                    InferenceBtn.IsEnabled = false;
                    PickBtn.IsEnabled = true;
                    PhotoBtn.IsEnabled = true;
                    InferenceBtn.Text = "Start!";
                    break;
                }
        }
        SemanticScreenReader.Announce(PhotoBtn.Text);
    }
    private async void PickPageState_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (pickPageState.State)
        {
            case PickPageInternalState.Previewing:
                {
                    PickedImage.Source = ImageSource.FromFile(null);
                    model.IsPreViewDownSample = true;
                    if (model.StateV2.ModelState == InternalState.ImageLoaded)
                    {
                        var result = await model.Inference();
                        if (result != null)
                            PickedImage.Source = ImageSource.FromFile(
                                Path.Combine(FileSystem.Current.CacheDirectory, result));
                    }
                    break;
                }
            case PickPageInternalState.Inferencing:
                {
                    popup = new PopupPage(ref model);
                    popup.State.State = PopupInternalState.Inferencing;
                    this.ShowPopup(popup);
                    if (model.StateV2.ModelState == InternalState.ImageLoaded)
                    {
                        var result = await model.Inference();
                        if (popup.IsClosed != true)
                            popup.Close();
                        if (result != null)
                            await Navigation.PushAsync(new ResultPage(model.originalImgName, result));
                        pickPageState.State = PickPageInternalState.ImageLoaded;
                    }
                    break;
                }
            case PickPageInternalState.ImageLoaded:
                {
                    //https://github.com/dotnet/maui/issues/14052
                    //https://github.com/dotnet/maui/issues/14128
                    PickedImage.Source = model.LoadToDisplay(await result.OpenReadAsync());
                    break;
                }
            case PickPageInternalState.LoadingImage:
                {
                    popup = new PopupPage(ref model);
                    popup.State.State = PopupInternalState.LoadingImage;
                    this.ShowPopup(popup);

                    await model.LoadToRgb24(await result.OpenReadAsync(), result.FileName);

                    if (popup.IsClosed != true)
                        popup.Close();

                    pickPageState.State = PickPageInternalState.ImageLoaded;
                    break;
                }
            default:
            case PickPageInternalState.Default:
                {
                    PickedImage.Source = ImageSource.FromFile(null);
                    await model.LoadToRgb24(null, null);
                    break;
                }
        }
    }

    private void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
    {
        switch(args.Buttons)
        {
            case 0:
            case ButtonsMask.Primary:
            case ButtonsMask.Secondary:
                {
                    if (pickPageState.State == PickPageInternalState.ImageLoaded)
                        pickPageState.State = PickPageInternalState.Previewing;
                    else if(pickPageState.State == PickPageInternalState.Previewing)
                        pickPageState.State = PickPageInternalState.ImageLoaded;
                    break;
                }
        }
    }
    private async void OnPickClicked(object sender, EventArgs e)
    {
        result = await model.PickImage();
        if(result != null) 
        {
            pickPageState.State = PickPageInternalState.LoadingImage;
        }
        else
        {
            pickPageState.State = PickPageInternalState.Default;
        }
    }
    private void OnInferenceClicked(object sender, EventArgs e)
    {
        pickPageState.State = PickPageInternalState.Inferencing;
    }
    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        result = await model.ShotPhoto();
        if(result != null)
        {
            pickPageState.State = PickPageInternalState.LoadingImage;
        }
        else
        {
            pickPageState.State = PickPageInternalState.Default;
        }
        //await Navigation.PushAsync(new ResultPage());
    }
    private void OnDownSampleCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {

    }
    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {

    }
}

internal class CircleClip : Microsoft.Maui.Controls.Shapes.Geometry
{
    int radius = 0;
    public CircleClip(double size)
    {
        this.radius = (int)size / 2;
    }

    public override void AppendPath(PathF path)
    {
        path.AppendCircle(radius, radius, radius);
    }
}
internal class PanClip : Microsoft.Maui.Controls.Shapes.Geometry
{
    RectF rectangle;
    public PanClip(RectF rect)
    {
        this.rectangle = rect;
    }

    public override void AppendPath(PathF path)
    {
        path.AppendRectangle(rectangle);
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