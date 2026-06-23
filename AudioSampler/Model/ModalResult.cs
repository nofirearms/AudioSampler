using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    public class ModalResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ButtonTag { get; set; }
    }
}
