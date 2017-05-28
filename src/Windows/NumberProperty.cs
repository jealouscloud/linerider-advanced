//
//  NumberProperty.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using Gwen;
using System.Globalization;
using System.Drawing;
namespace Gwen.Controls.Property
{
	public class NumberProperty : Text
	{
		private double _maxvalue = 1000;
		public double MaxValue
		{
			get
			{
				return _maxvalue;
			}
			set
			{
				if (_maxvalue != value)
				{
					_maxvalue = value;
				}
			}
		}
		private double _minvalue = 0;
		public double MinValue
		{
			get
			{
				return _minvalue;
			}
			set
			{
				if (_minvalue != value)
				{
					_minvalue = value;
				}
			}
		}
		public double NumValue
		{
			get
			{
				double ret;
				if (double.TryParse(base.Value, System.Globalization.NumberStyles.Number, linerider.Program.Culture, out ret))
				{
					if (ret < _minvalue)
					{
						ret = _minvalue;
						NumValue = ret;
					}
					if (ret > _maxvalue)
					{
						ret = _maxvalue;
						NumValue = ret;
					}
					return ret;
				}
				else return double.NaN;
			}
			set
			{
				SetValue(value.ToString(linerider.Program.Culture));
			}
		}
		private System.Drawing.Color lastcolor;
		public NumberProperty(ControlBase parent) : base(parent)
		{
			Value = "";
		}
		protected override void OnValueChanged(ControlBase control, EventArgs args)
		{
			base.OnValueChanged(control, args);
			var pr = (PropertyRow)Parent;
			if (double.IsNaN(NumValue))
			{
				if (pr.LabelColor != System.Drawing.Color.Red)
				{
					lastcolor = pr.LabelColor;
					pr.LabelColor = System.Drawing.Color.Red;
				}
			}
			else
			{
				pr.LabelColor = lastcolor;
			}
		}
	}
}