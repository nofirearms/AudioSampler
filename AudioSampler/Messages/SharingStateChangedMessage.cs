using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Messages
{
    public class SharingStateChangedMessage
    {
        public bool IsActive { get; }
        public SharingStateChangedMessage(bool isActive) => IsActive = isActive;
    }
}
