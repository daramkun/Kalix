using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kalix.Controls
{
    /// <summary>
    /// NumericUpDownBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NumericUpDownBox : UserControl
    {
	    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDownBox), new PropertyMetadata(OnMinMaxChanged));
	    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDownBox), new PropertyMetadata(OnMinMaxChanged));
	    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDownBox), new PropertyMetadata(OnValueChanged));

	    public int Minimum
	    {
		    get => (int)GetValue(MinimumProperty);
		    set
		    {
			    if (value > Maximum)
				    throw new ArgumentOutOfRangeException();
			    SetValue(MinimumProperty, value);
		    }
	    }

	    public int Maximum
	    {
		    get => (int)GetValue(MaximumProperty);
		    set
		    {
			    if (value < Minimum)
				    throw new ArgumentOutOfRangeException();
			    SetValue(MaximumProperty, value);
		    }
	    }

	    public int Value
	    {
		    get => (int)GetValue(ValueProperty);
		    set
		    {
			    if (value > Maximum)
				    value = Maximum;
			    else if (value < Minimum)
				    value = Minimum;
			    SetValue(ValueProperty, value);
		    }
	    }

		public NumericUpDownBox()
        {
            InitializeComponent();
            SetValue(MinimumProperty, 0);
            SetValue(MaximumProperty, 100);
            Value = 0;
		}

		private static void OnMinMaxChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if ((int)sender.GetValue(MaximumProperty) < (int)sender.GetValue(ValueProperty))
				sender.SetValue(ValueProperty, sender.GetValue(MaximumProperty));
			else if ((int)sender.GetValue(MinimumProperty) > (int)sender.GetValue(ValueProperty))
				sender.SetValue(ValueProperty, sender.GetValue(MinimumProperty));
		}

		private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var self = sender as NumericUpDownBox;
			self.TextBoxNumeric.Text = self.Value.ToString();
		}

		private void UpButton_Click(object sender, RoutedEventArgs e)
		{
			++Value;
		}

		private void DownButton_Click(object sender, RoutedEventArgs e)
		{
			--Value;
		}

		private static readonly Regex NumericRegex = new Regex("^-?[0-9]+$");
		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !NumericRegex.IsMatch(e.Text);
		}

		private void TextBoxNumeric_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is not TextBox senderTextBox)
				return;

			if (!NumericRegex.IsMatch(senderTextBox.Text))
			{
				senderTextBox.Text = new Regex("[^0-9\\-]+").Replace(senderTextBox.Text, "");
				if (string.IsNullOrEmpty(senderTextBox.Text))
					senderTextBox.Text = "0";
				if (senderTextBox.Text.IndexOf('-', 1) <= 0)
					return;

				var signed = senderTextBox.Text.IndexOf('-') == 0;
				senderTextBox.Text = (signed ? "-" : "") + senderTextBox.Text.Replace("-", "");

				return;
			}

			if (e.Changes.Count == 0)
				return;

			var value = int.Parse(senderTextBox.Text);
			Value = value;
			senderTextBox.Text = Value.ToString();
		}
    }
}
