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
using System.Text;

namespace linerider.IO.ffmpeg
{
    public class FFMPEGParameters
    {
        public string OutputFilePath;
        public string InputFilePath;
        public string Options;

        private StringBuilder m_assembledOptions;

        public FFMPEGParameters()
        {
            m_assembledOptions = new StringBuilder();
        }

        public void AddOption(string option)
        {
            if ((m_assembledOptions.Length > 0) && (m_assembledOptions.ToString().EndsWith(" ", StringComparison.OrdinalIgnoreCase) == false))
            {
                m_assembledOptions.Append(" ");
            }

            m_assembledOptions.Append("-");
            m_assembledOptions.Append(option);
        }

        public void AddParameter(string parameter)
        {
            m_assembledOptions.Append(parameter);
        }

        public void AddOption(string option, string parameter)
        {
            AddOption(option);
            m_assembledOptions.Append(" ");
            AddParameter(parameter);
        }

        public void AddOption(string option, string parameter1, string separator, string parameter2)
        {
            AddOption(option);
            m_assembledOptions.Append(" ");
            AddParameter(parameter1);
            m_assembledOptions.Append(separator);
            AddParameter(parameter2);
        }

        public void AddSeparator(string separator)
        {
            m_assembledOptions.Append(separator);
        }

        public void AddRawOptions(string rawOptions)
        {
            m_assembledOptions.Append(rawOptions);
        }

        protected void AssembleGeneralOptions()
        {
            if (!String.IsNullOrWhiteSpace(Options))
            {
                AddSeparator(" ");
                AddRawOptions(Options);
            }
        }


        public override string ToString()
        {
            AssembleGeneralOptions();
            return m_assembledOptions.ToString() + " \"" + OutputFilePath+"\"";
        }
    }
}