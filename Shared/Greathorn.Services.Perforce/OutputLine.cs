// Copyright Greathorn Games Inc. All Rights Reserved.

namespace Greathorn.Services.Perforce
{
    public class OutputLine
    {
        public enum OutputChannel
        {
            Unknown,
            Text,
            Info,
            TaggedInfo,
            Warning,
            Error,
            Exit
        }
        
        public readonly OutputChannel Channel;
        public readonly string Text;

        public OutputLine(OutputChannel inChannel, string inText)
        {
            Channel = inChannel;
            Text = inText;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Channel, Text);
        }
    }
}