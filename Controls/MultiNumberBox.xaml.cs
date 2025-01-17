﻿// © XIV-Tools.
// Licensed under the MIT license.

namespace XivToolsWpf.Controls;

using PropertyChanged;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using XivToolsWpf;
using XivToolsWpf.DependencyProperties;

[AddINotifyPropertyChangedInterface]
public partial class MultiNumberBox : UserControl, INotifyPropertyChanged
{
	public static readonly IBind<double> XDp = Binder.Register<double, MultiNumberBox>(nameof(X), OnValueChanged);
	public static readonly IBind<double> YDp = Binder.Register<double, MultiNumberBox>(nameof(Y), OnValueChanged);
	public static readonly IBind<double> ZDp = Binder.Register<double, MultiNumberBox>(nameof(Z), OnValueChanged);

	public static readonly IBind<double> TickDp = Binder.Register<double, MultiNumberBox>(nameof(TickFrequency), BindMode.OneWay);
	public static readonly IBind<double> MinDp = Binder.Register<double, MultiNumberBox>(nameof(Minimum), BindMode.OneWay);
	public static readonly IBind<double> MaxDp = Binder.Register<double, MultiNumberBox>(nameof(Maximum), BindMode.OneWay);
	public static readonly IBind<bool> WrapDp = Binder.Register<bool, MultiNumberBox>(nameof(Wrap), BindMode.OneWay);

	private Key keyHeld = Key.None;

	public MultiNumberBox()
	{
		this.InitializeComponent();
		this.InputBox.DataContext = this;

		this.TickFrequency = 1;
		this.Minimum = double.MinValue;
		this.Maximum = double.MaxValue;
		this.Wrap = false;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public double X
	{
		get => XDp.Get(this);
		set => XDp.Set(this, value);
	}

	public double Y
	{
		get => YDp.Get(this);
		set => YDp.Set(this, value);
	}

	public double Z
	{
		get => ZDp.Get(this);
		set => ZDp.Set(this, value);
	}

	public double TickFrequency
	{
		get => TickDp.Get(this);
		set => TickDp.Set(this, value);
	}

	public double Minimum
	{
		get => MinDp.Get(this);
		set => MinDp.Set(this, value);
	}

	public double Maximum
	{
		get => MaxDp.Get(this);
		set => MaxDp.Set(this, value);
	}

	public bool Wrap
	{
		get => WrapDp.Get(this);
		set => WrapDp.Set(this, value);
	}

	public string Display
	{
		get
		{
			return $"{Math.Round(this.X, 3)}, {Math.Round(this.Y, 3)}, {Math.Round(this.Z, 3)}";
		}
		set
		{
			string[] parts = value.Split(',');
			if (parts.Length == 3)
			{
				if (double.TryParse(parts[0], out var x))
					this.X = x;

				if (double.TryParse(parts[1], out var y))
					this.Y = y;

				if (double.TryParse(parts[2], out var z))
					this.Z = z;
			}
		}
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		if (!this.InputBox.IsKeyboardFocused)
			return;

		if (e.Key == Key.Up || e.Key == Key.Down)
		{
			e.Handled = true;

			if (e.IsRepeat)
			{
				if (this.keyHeld == e.Key)
					return;

				this.keyHeld = e.Key;
				Task.Run(this.TickHeldKey);
			}
			else
			{
				this.TickKey(e.Key);
			}
		}
	}

	protected override void OnPreviewKeyUp(KeyEventArgs e)
	{
		if (this.keyHeld == e.Key)
		{
			e.Handled = true;
			this.keyHeld = Key.None;
		}
	}

	protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
	{
		if (!this.InputBox.IsFocused)
			return;

		e.Handled = true;
		this.TickValue(e.Delta > 0);
	}

	private static void OnValueChanged(MultiNumberBox sender, double value)
	{
		sender.PropertyChanged?.Invoke(sender, new(nameof(Display)));
	}

	private async Task TickHeldKey()
	{
		while (this.keyHeld != Key.None)
		{
			await this.Dispatcher.MainThread();
			this.TickKey(this.keyHeld);
			await Task.Delay(10);
		}
	}

	private void TickKey(Key key)
	{
		if (key == Key.Up)
		{
			this.TickValue(true);
		}
		else if (key == Key.Down)
		{
			this.TickValue(false);
		}
	}

	private double Validate(double v)
	{
		if (this.Wrap)
		{
			if (v > this.Maximum)
			{
				v = this.Minimum;
			}

			if (v < this.Minimum)
			{
				v = this.Maximum;
			}
		}
		else
		{
			v = Math.Min(v, this.Maximum);
			v = Math.Max(v, this.Minimum);
		}

		return v;
	}

	private void TickValue(bool increase)
	{
		double delta = increase ? this.TickFrequency : -this.TickFrequency;

		if (Keyboard.IsKeyDown(Key.LeftShift))
			delta *= 10;

		if (Keyboard.IsKeyDown(Key.LeftCtrl))
			delta /= 10;

		// Find which number block the caret is in
		int caretIndex = this.InputBox.CaretIndex;
		string str = this.Display;
		int boxNum = 0;
		for (int i = 0; i < caretIndex; i++)
		{
			if (str[i] == ',')
			{
				boxNum++;
			}
		}

		double value = -1;
		if (boxNum == 0)
		{
			value = this.X;
		}
		else if (boxNum == 1)
		{
			value = this.Y;
		}
		else if (boxNum == 2)
		{
			value = this.Z;
		}

		double newValue = value + delta;
		newValue = this.Validate(newValue);

		if (newValue == value)
			return;

		if (boxNum == 0)
		{
			this.X = newValue;
		}
		else if (boxNum == 1)
		{
			this.Y = newValue;
		}
		else if (boxNum == 2)
		{
			this.Z = newValue;
		}

		// restore the caret index as changing the display may reset it.
		this.InputBox.CaretIndex = caretIndex;
	}
}
