using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPU_T.Services;
using Avalonia.Threading;
using Avalonia.Controls;

namespace GPU_T.ViewModels;

public class RefreshRateItem
{
    public string Label { get; set; } = "";
    public double Seconds { get; set; }
    
    public override string ToString() => Label;
}