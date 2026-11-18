namespace FixIt.ViewModels;

/// <summary>A value with a friendly label for Pickers (Picker shows ToString()).</summary>
public sealed record PickerOption<T>(T Value, string Label)
{
    public override string ToString() => Label;
}
