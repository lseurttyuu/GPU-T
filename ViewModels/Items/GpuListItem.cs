using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPU_T.Services;
using Avalonia.Threading;
using Avalonia.Controls;

namespace GPU_T.ViewModels;



public class GpuListItem
{
    public string Id { get; set; }          // Np. "card0"
    public string DisplayName { get; set; } // Np. "AMD Radeon RX 7900 XTX (card0)"
    public override string ToString() => DisplayName; 
}